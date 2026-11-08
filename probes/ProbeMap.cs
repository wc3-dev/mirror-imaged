using System.Text.RegularExpressions;
using War3Net.Build;
using War3Net.Build.Common;
using War3Net.Build.Info;
using War3Net.IO.Mpq;

namespace MirrorImaged.Probes;

/// <summary>
/// Builds probe maps: a small flat Lua map for one player that runs a probe's Lua once its game has started, writes the
/// lines the probe gave to <c>out</c> into <c>CustomMapData\MirrorImaged\&lt;name&gt;.pld</c> when it calls <c>done</c>,
/// and ends the game.
/// </summary>
public static partial class ProbeMap
{
    /// <summary>The folder under <c>CustomMapData</c> the probes write to.</summary>
    public const string Folder = "MirrorImaged";

    /// <summary>The file under <c>CustomMapData</c> a probe map writes at the end of <c>main</c>, when it has loaded.</summary>
    public const string Loaded = Folder + @"\loaded.pld";

    /// <summary>The file under <c>CustomMapData</c> a probe map writes when its game has started, past the loading screen.</summary>
    public const string Started = Folder + @"\started.pld";

    // A black minimap in place of World Editor's war3mapMap.blp: without either, the game closes as the map loads (docs/facts.md).
    static readonly byte[] Minimap = MakeMinimap(256);

    /// <summary>The Lua of a probe in <c>probes/</c>, by its name.</summary>
    public static string Source(string name) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "probes", name + ".lua"));

    /// <summary>Builds a probe's map at <paramref name="path"/>.</summary>
    /// <param name="name">The probe's name, letters, digits and dashes; its lines go to <c>MirrorImaged\&lt;name&gt;.pld</c>.</param>
    /// <param name="lua">
    /// The probe: Lua that calls <c>out(text)</c> for each line (at most 253 bytes, a backslash counting two, no line
    /// breaks) and <c>done()</c> when
    /// it has finished; a function it hands to the game, such as a timer's, goes through <c>guard</c> so its error ends
    /// the probe too.
    /// </param>
    /// <param name="path">The map to write.</param>
    public static void Build(string name, string lua, string path)
    {
        if (!Name().IsMatch(name))
            throw new ArgumentException($"A probe's name is letters, digits and dashes, not \"{name}\"", nameof(name));

        // config and main are as World Editor writes them for one user player. main marks the map loaded, and a
        // zero-second timer, which runs once the game has started (facts, "Initialisation"), marks it started and runs
        // the probe.
        var script = $$"""
            local lines = {}

            -- The game writes a backslash doubled, so it counts two.
            local function size(text)
                return #text + select(2, text:gsub("\\", ""))
            end

            function out(text)
                text = tostring(text)
                if size(text) > 253 or text:find("[\r\n]") then
                    error("a line is at most 253 bytes, a backslash counting two, with no line break: "
                        .. (text:sub(1, 40):gsub("[\r\n]", " ")), 2)
                end
                lines[#lines + 1] = text
            end

            function done()
                PreloadGenClear()
                PreloadGenStart()
                for _, line in ipairs(lines) do
                    Preload("\")\n" .. line .. "\n//")
                end
                PreloadGenEnd({{Lua($@"{Folder}\{name}.pld")}})
                EndGame(false)
            end

            function guard(f)
                return function(...)
                    local ok, problem = pcall(f, ...)
                    if not ok then
                        local line = ("error: " .. tostring(problem)):gsub("[\r\n]", " ")
                        while size(line) > 253 do
                            line = line:sub(1, -2)
                        end
                        lines[#lines + 1] = line
                        done()
                    end
                end
            end

            local probe = guard(function()
            {{lua}}
            end)

            function config()
                SetMapName("MirrorImaged probe")
                SetPlayers(1)
                SetTeams(1)
                SetGamePlacement(MAP_PLACEMENT_USE_MAP_SETTINGS)
                DefineStartLocation(0, 0.0, 0.0)
                SetPlayerStartLocation(Player(0), 0)
                SetPlayerColor(Player(0), ConvertPlayerColor(0))
                SetPlayerRacePreference(Player(0), RACE_PREF_HUMAN)
                SetPlayerRaceSelectable(Player(0), true)
                SetPlayerController(Player(0), MAP_CONTROL_USER)
                SetPlayerSlotAvailable(Player(0), MAP_CONTROL_USER)
                InitGenericPlayerSlots()
            end

            local function mark(file)
                PreloadGenClear()
                PreloadGenStart()
                PreloadGenEnd(file)
            end

            function main()
                InitBlizzard()
                mark({{Lua(Loaded)}})
                TimerStart(CreateTimer(), 0.0, false, function()
                    mark({{Lua(Started)}})
                    probe()
                end)
            end

            """;

        var info = MapFactory.Info(32, 32);
        info.MapName = name;
        info.MapFlags &= ~MapFlags.MeleeMap;
        info.Forces[0].Players = new Bitmask32(1);
        var builder = new MapBuilder(new Map { Info = info, Environment = MapFactory.Environment(info), Script = script });
        builder.AddFiles(MpqFile.New(new MemoryStream(Minimap), "war3mapMap.tga"));
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        builder.Build(path);
    }

    // An uncompressed 32-bit TGA, top row first.
    static byte[] MakeMinimap(int size)
    {
        var tga = new byte[18 + size * size * 4];
        tga[2] = 2;
        BitConverter.TryWriteBytes(tga.AsSpan(12), (short)size);
        BitConverter.TryWriteBytes(tga.AsSpan(14), (short)size);
        tga[16] = 32;
        tga[17] = 0x28;
        return tga;
    }

    static string Lua(string text) => "\"" + text.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";

    [GeneratedRegex(@"^[A-Za-z0-9-]+\z")]
    private static partial Regex Name();
}
