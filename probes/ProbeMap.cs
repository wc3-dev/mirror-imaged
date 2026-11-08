using System.Text.RegularExpressions;
using War3Net.Build;
using War3Net.Build.Common;
using War3Net.Build.Info;
using War3Net.IO.Mpq;

namespace MirrorImaged.Probes;

/// <summary>
/// Builds probe maps: a small flat Lua map for one player that runs a probe's Lua at game start, writes the lines the
/// probe gave to <c>out</c> into <c>CustomMapData\MirrorImaged\&lt;name&gt;.pld</c> when it calls <c>done</c>, and then
/// ends the game or loads the next map. Also builds the hub, which waits for a request and loads the map it names.
/// </summary>
public static partial class ProbeMap
{
    /// <summary>The folder under <c>CustomMapData</c> the probes and the hub use.</summary>
    public const string Folder = "MirrorImaged";

    // The ability whose tooltip carries a request to the hub: Blizzard, which has levels to hold one (facts, "Files and Preload").
    internal const string Carrier = "AHbz";

    /// <summary>The Lua of a probe in <c>probes/</c>, by its name.</summary>
    public static string Source(string name) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "probes", name + ".lua"));

    /// <summary>Builds a probe's map at <paramref name="path"/>.</summary>
    /// <param name="name">The probe's name, letters, digits and dashes; its lines go to <c>MirrorImaged\&lt;name&gt;.pld</c>.</param>
    /// <param name="lua">The probe: Lua that calls <c>out(text)</c> for each line and <c>done()</c> when it has finished.</param>
    /// <param name="path">The map to write.</param>
    /// <param name="next">The map to load after <c>done()</c>; the game ends when there is none.</param>
    public static void Build(string name, string lua, string path, string? next = null)
    {
        if (!Name().IsMatch(name))
            throw new ArgumentException($"A probe's name is letters, digits and dashes, not \"{name}\"", nameof(name));

        Write(path, name, $$"""
            local lines = {}

            function out(text)
                lines[#lines + 1] = tostring(text)
            end

            function done()
                PreloadGenClear()
                PreloadGenStart()
                for _, line in ipairs(lines) do
                    Preload("\")\n" .. line .. "\n//")
                end
                PreloadGenEnd({{Lua($@"{Folder}\{name}.pld")}})
                {{(next is null ? "EndGame(false)" : $"ChangeLevel({Lua(next)}, false)")}}
            end

            local function probe()
            {{lua}}
            end

            function start()
                local ok, problem = pcall(probe)
                if not ok then
                    out("error: " .. tostring(problem))
                    done()
                end
            end
            """);
    }

    /// <summary>Builds the hub at <paramref name="path"/>: it waits for <c>MirrorImaged\hub\&lt;request&gt;.pld</c> and loads the map that file names.</summary>
    public static void BuildHub(int request, string path) => Write(path, "hub", $$"""
        function start()
            local poll = CreateTimer()
            TimerStart(poll, 0.25, true, function()
                BlzSetAbilityTooltip(FourCC("{{Carrier}}"), "", 0)
                Preloader({{Lua($@"{Folder}\hub\{request}.pld")}})
                local map = BlzGetAbilityTooltip(FourCC("{{Carrier}}"), 0)
                if map ~= "" then
                    PauseTimer(poll)
                    ChangeLevel(map, false)
                end
            end)
        end
        """);

    /// <summary>The file under <c>CustomMapData</c> every map writes at the end of <c>main</c>, when it has loaded.</summary>
    public const string Loaded = Folder + @"\loaded.pld";

    /// <summary>The file under <c>CustomMapData</c> every map writes when its game has started, past the loading screen.</summary>
    public const string Started = Folder + @"\started.pld";

    // config and main as World Editor writes them for one user player. main marks the map loaded, and a zero-second timer,
    // which runs once the game has started (facts, "Initialisation"), marks it started and runs start.
    const string ConfigAndMain = """


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
            mark("MirrorImaged\\loaded.pld")
            TimerStart(CreateTimer(), 0.0, false, function()
                mark("MirrorImaged\\started.pld")
                start()
            end)
        end

        """;

    // A black minimap in place of World Editor's war3mapMap.blp: without either, the game closes as the map loads (docs/facts.md).
    static readonly byte[] Minimap = MakeMinimap(256);

    static void Write(string path, string name, string script)
    {
        var info = MapFactory.Info(32, 32);
        info.MapName = name;
        info.MapFlags &= ~MapFlags.MeleeMap;
        info.Forces[0].Players = new Bitmask32(1);
        var map = new Map { Info = info, Environment = MapFactory.Environment(info), Script = script + ConfigAndMain };
        var builder = new MapBuilder(map);
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

    [GeneratedRegex("^[A-Za-z0-9-]+$")]
    private static partial Regex Name();
}
