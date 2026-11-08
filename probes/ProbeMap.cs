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

    /// <summary>The JASS of a probe in <c>probes/</c>, by its name (<c>&lt;name&gt;.j</c>).</summary>
    public static string JassSource(string name) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "probes", name + ".j"));

    /// <summary>Builds a probe's map at <paramref name="path"/>.</summary>
    /// <param name="name">The probe's name, letters, digits and dashes; its lines go to <c>MirrorImaged\&lt;name&gt;.pld</c>.</param>
    /// <param name="lua">
    /// The probe: Lua that calls <c>out(text)</c> for each line (at most 253 bytes, a backslash counting two, no line
    /// breaks) and <c>done()</c> when
    /// it has finished; a function it hands to the game, such as a timer's, goes through <c>guard</c> so its error ends
    /// the probe too.
    /// </param>
    /// <param name="path">The map to write.</param>
    /// <param name="withComputer">
    /// A map of three named slots in place of one: a user (slot 0), a computer (1) and a user no one plays (2), each named
    /// in <c>war3map.w3i</c> ("Probe User", "Probe Computer", "Probe Empty") with a selectable race, in custom forces
    /// with fixed player settings (0 and 2 in one force, 1 in the other). Its <c>config</c> adds what it reads before
    /// it sets anything to the global list <c>ProbeConfig</c>, one string a call.
    /// </param>
    /// <param name="change">Changes the map before it is written: its info, strings or object data.</param>
    public static void Build(string name, string lua, string path, bool withComputer = false, Action<Map>? change = null)
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

            {{(withComputer ? ConfigWithComputer : Config)}}

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
        if (withComputer)
        {
            info.MapFlags |= MapFlags.UseCustomForces | MapFlags.FixedPlayerSettingsForCustomForces;
            var start = info.Players[0].StartPosition;
            info.Players.Clear();
            info.Players.AddRange([Slot(0, PlayerController.User, "Probe User"), Slot(1, PlayerController.Computer, "Probe Computer"), Slot(2, PlayerController.User, "Probe Empty")]);
            info.Forces[0].Players = new Bitmask32(0b101);
            info.Forces.Add(new ForceData { Name = "Computer", Players = new Bitmask32(0b10) });

            PlayerData Slot(int id, PlayerController controller, string slotName) =>
                new() { Id = id, Controller = controller, Race = PlayerRace.Human, Flags = PlayerFlags.RaceSelectable, Name = slotName, StartPosition = start };
        }
        Write(info, script, path, change);
    }

    /// <summary>
    /// Builds a JASS probe's map at <paramref name="path"/>: the same map, its script <c>war3map.j</c>, which the game
    /// runs in its JASS machine. <paramref name="jass"/> may start with a <c>globals</c> block, which joins the map's,
    /// and defines <c>function probe takes nothing returns nothing</c>, which runs once the game has started; it calls
    /// <c>out(text)</c> for each line and <c>done()</c> when it has finished. JASS has no <c>pcall</c>: a thread the
    /// probe crashes just stops, so it runs what may crash in threads of its own (<c>ExecuteFunc</c>), and a timer calls
    /// <c>done()</c> 30 s in, in case the probe's own thread stopped. A line over 253 bytes becomes an error line. The
    /// map calls no blizzard.j function, so MirrorImaged can run it without the game's blizzard.j.
    /// </summary>
    public static void BuildJass(string name, string jass, string path)
    {
        if (!Name().IsMatch(name))
            throw new ArgumentException($"A probe's name is letters, digits and dashes, not \"{name}\"", nameof(name));

        var globals = GlobalsBlock().Match(jass);
        var script = $$"""
            globals
                string array probe_lines
                integer probe_count = 0
                boolean probe_done = false
            {{globals.Groups[1].Value}}
            endglobals

            function out takes string text returns nothing
                if StringLength(text) > 253 then
                    set text = "error: a line is at most 253 bytes: " + SubString(text, 0, 40)
                endif
                set probe_lines[probe_count] = text
                set probe_count = probe_count + 1
            endfunction

            function done takes nothing returns nothing
                local integer i = 0
                if probe_done then
                    return
                endif
                set probe_done = true
                call PreloadGenClear()
                call PreloadGenStart()
                loop
                    exitwhen i >= probe_count
                    call Preload("\")\n" + probe_lines[i] + "\n//")
                    set i = i + 1
                endloop
                call PreloadGenEnd({{Lua($@"{Folder}\{name}.pld")}})
                call EndGame(false)
            endfunction

            {{jass[(globals.Index + globals.Length)..]}}

            function probe_mark takes string file returns nothing
                call PreloadGenClear()
                call PreloadGenStart()
                call PreloadGenEnd(file)
            endfunction

            function probe_start takes nothing returns nothing
                call probe_mark({{Lua(Started)}})
                call TimerStart(CreateTimer(), 30.0, false, function done)
                call probe()
            endfunction

            function config takes nothing returns nothing
                call SetMapName("MirrorImaged probe")
                call SetPlayers(1)
                call SetTeams(1)
                call SetGamePlacement(MAP_PLACEMENT_USE_MAP_SETTINGS)
                call DefineStartLocation(0, 0.0, 0.0)
                call SetPlayerStartLocation(Player(0), 0)
                call SetPlayerColor(Player(0), ConvertPlayerColor(0))
                call SetPlayerRacePreference(Player(0), RACE_PREF_HUMAN)
                call SetPlayerRaceSelectable(Player(0), true)
                call SetPlayerController(Player(0), MAP_CONTROL_USER)
            endfunction

            function main takes nothing returns nothing
                call probe_mark({{Lua(Loaded)}})
                call TimerStart(CreateTimer(), 0.0, false, function probe_start)
            endfunction

            """;

        var info = MapFactory.Info(32, 32);
        info.MapName = name;
        info.MapFlags &= ~MapFlags.MeleeMap;
        info.Forces[0].Players = new Bitmask32(1);
        info.ScriptLanguage = ScriptLanguage.Jass;
        Write(info, script, path, null);
    }

    static void Write(MapInfo info, string script, string path, Action<Map>? change)
    {
        var map = new Map { Info = info, Environment = MapFactory.Environment(info), Script = script };
        change?.Invoke(map);
        var builder = new MapBuilder(map);
        builder.AddFiles(MpqFile.New(new MemoryStream(Minimap), "war3mapMap.tga"));
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        builder.Build(path);
    }

    // config as World Editor writes it for one user player.
    const string Config = """
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
        """;

    // config for the computer map, with what the lobby had set before it: the game type, MAP_LOCK_ALLIANCE_CHANGES, the
    // local player, the player and team counts, and each slot's name, slot state, controller and race as their ids.
    const string ConfigWithComputer = """
        function config()
            local slots = {}
            for i = 0, 2 do
                local p = Player(i)
                slots[#slots + 1] = string.format("%d %s %d %d %d %s", i, GetPlayerName(p), GetHandleId(GetPlayerSlotState(p)),
                    GetHandleId(GetPlayerController(p)), GetHandleId(GetPlayerRace(p)), tostring(IsPlayerRacePrefSet(p, RACE_PREF_RANDOM)))
            end
            ProbeConfig = ProbeConfig or {}
            ProbeConfig[#ProbeConfig + 1] = string.format("type %d lock %s local %d players %d teams %d; %s",
                GetHandleId(GetGameTypeSelected()), tostring(IsMapFlagSet(MAP_LOCK_ALLIANCE_CHANGES)), GetPlayerId(GetLocalPlayer()),
                GetPlayers(), GetTeams(), table.concat(slots, "; "))
            SetMapName("MirrorImaged probe")
            SetPlayers(3)
            SetTeams(2)
            SetGamePlacement(MAP_PLACEMENT_TEAMS_TOGETHER)
            for i = 0, 2 do
                DefineStartLocation(i, 0.0, 0.0)
                SetPlayerStartLocation(Player(i), i)
                SetPlayerColor(Player(i), ConvertPlayerColor(i))
                SetPlayerRacePreference(Player(i), RACE_PREF_HUMAN)
                SetPlayerRaceSelectable(Player(i), true)
                SetPlayerController(Player(i), i == 1 and MAP_CONTROL_COMPUTER or MAP_CONTROL_USER)
                SetPlayerTeam(Player(i), i == 1 and 1 or 0)
            end
        end
        """;

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

    // A JASS probe's own globals block, at its start after any comments
    [GeneratedRegex(@"\A(?:\s|//[^\n]*)*globals\b(.*?)^\s*endglobals\b", RegexOptions.Singleline | RegexOptions.Multiline)]
    private static partial Regex GlobalsBlock();
}
