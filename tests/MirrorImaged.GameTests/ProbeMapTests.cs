using MirrorImaged.Lua;
using MirrorImaged.Probes;
using War3Net.Build;
using War3Net.Build.Info;
using War3Net.IO.Mpq;

namespace MirrorImaged.GameTests;

public sealed class ProbeMapTests : IDisposable
{
    readonly string _folder = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())).FullName;

    public void Dispose() => Directory.Delete(_folder, true);

    [Fact]
    public void A_probe_map_is_a_small_flat_Lua_map_with_one_player()
    {
        var map = Build("hello", "out(1)\ndone()");

        Assert.Equal(ScriptLanguage.Lua, map.Info!.ScriptLanguage);
        Assert.Equal(32u, map.Environment!.Width);
        Assert.Equal(32u, map.Environment.Height);
        var first = map.Environment.TerrainTiles[0];
        Assert.All(map.Environment.TerrainTiles, tile =>
        {
            Assert.Equal(first.Height, tile.Height);
            Assert.Equal(first.CliffLevel, tile.CliffLevel);
            Assert.False(tile.IsWater);
        });
        Assert.Single(map.Info.Players);
        Assert.False(map.Info.MapFlags.HasFlag(MapFlags.MeleeMap));
    }

    // The lobby and events probes' map (decisions D140)
    [Fact]
    public void The_map_with_a_computer_has_three_named_slots_in_custom_forces_and_config_keeps_what_it_read_first()
    {
        var map = Build("lobby", "done()", withComputer: true);

        Assert.Equal(["0 User Probe User", "1 Computer Probe Computer", "2 User Probe Empty"], map.Info!.Players.Select(p => $"{p.Id} {p.Controller} {p.Name}"));
        Assert.True(map.Info.MapFlags.HasFlag(MapFlags.UseCustomForces | MapFlags.FixedPlayerSettingsForCustomForces));
        Assert.Equal([0b101, 0b10], map.Info.Forces.Select(force => force.Players.ToInt32()));
        var script = map.Script!;
        var config = script[script.IndexOf("function config()", StringComparison.Ordinal)..];
        Assert.True(config.IndexOf("ProbeConfig[#ProbeConfig + 1]", StringComparison.Ordinal) < config.IndexOf("SetMapName", StringComparison.Ordinal));
        Assert.Contains("SetPlayerController(Player(i), i == 1 and MAP_CONTROL_COMPUTER or MAP_CONTROL_USER)", config);
        Assert.DoesNotContain("ProbeConfig", Build("hello", "done()").Script!);
    }

    [Fact]
    public void A_probe_map_has_a_minimap_image()
    {
        var path = Path.Combine(_folder, "hello.w3x");
        ProbeMap.Build("hello", "done()", path);
        using var archive = MpqArchive.Open(path, true);
        using var stream = archive.OpenFile("war3mapMap.tga");
        var tga = new byte[stream.Length];
        stream.ReadExactly(tga);

        Assert.Equal(2, tga[2]);
        Assert.Equal(256, BitConverter.ToInt16(tga, 12));
        Assert.Equal(256, BitConverter.ToInt16(tga, 14));
        Assert.Equal(32, tga[16]);
        Assert.Equal(18 + 256 * 256 * 4, tga.Length);
    }

    [Fact]
    public void The_probe_runs_once_the_game_has_started_and_writes_its_lines_to_its_Preload_file_then_ends_the_game()
    {
        var script = Build("hello", "out(1)\ndone()").Script!;

        Assert.Contains("local probe = guard(function()\nout(1)\ndone()\nend)", script);
        Assert.Contains(
            "    InitBlizzard()\n    mark(\"MirrorImaged\\\\loaded.pld\")\n    TimerStart(CreateTimer(), 0.0, false, function()\n" +
            "        mark(\"MirrorImaged\\\\started.pld\")\n        probe()\n",
            script);
        Assert.Equal(@"MirrorImaged\loaded.pld", ProbeMap.Loaded);
        Assert.Equal(@"MirrorImaged\started.pld", ProbeMap.Started);
        Assert.Contains("Preload(\"\\\")\\n\" .. line .. \"\\n//\")\n    end\n    PreloadGenEnd(\"MirrorImaged\\\\hello.pld\")\n    EndGame(false)", script);
    }

    [Fact]
    public void Lines_come_back_as_given()
    {
        Assert.Equal(["1", "a \"quoted\" line", new string('\\', 126) + "a"], Run("out(1)\nout('a \"quoted\" line')\nout(string.rep('\\\\', 126) .. 'a')\ndone()"));
    }

    [Theory]
    [InlineData("out(string.rep('a', 254))")]
    [InlineData("out(string.rep('\\\\', 127))")]
    [InlineData("out('two\\nlines')")]
    [InlineData("out('a\\rb')")]
    public void A_line_too_long_for_one_Preload_call_a_backslash_counting_two_or_with_a_line_break_ends_the_probe_with_its_error(string line)
    {
        var lines = Run($"out('kept')\n{line}\nout('never')\ndone()");

        Assert.Equal(2, lines.Count);
        Assert.Equal("kept", lines[0]);
        Assert.StartsWith("error: ", lines[1]);
        Assert.Contains("a line is at most 253 bytes, a backslash counting two, with no line break", lines[1]);
    }

    [Fact]
    public void An_error_line_is_cut_to_fit_one_Preload_call_a_backslash_counting_two()
    {
        var lines = Run("error(string.rep('\\\\', 300), 0)");

        var line = Assert.Single(lines);
        Assert.StartsWith("error: \\\\\\", line);
        Assert.Equal(253, line.Length + line.Count(letter => letter == '\\'));
    }

    [Fact]
    public void An_error_in_a_guarded_timer_function_ends_the_probe_with_its_error()
    {
        var lines = Run("out('before')\nTimerStart(CreateTimer(), 1.0, false, guard(function() error('in the timer', 0) end))");

        Assert.Equal(["before", "error: in the timer"], lines);
    }
    [Theory]
    [InlineData("")]
    [InlineData("a b")]
    [InlineData("a\\b")]
    [InlineData("a\"b")]
    [InlineData("a\n")]
    [InlineData("Åsa")]
    public void A_probe_name_is_letters_digits_and_dashes(string name)
    {
        Assert.Throws<ArgumentException>(() => ProbeMap.Build(name, "done()", Path.Combine(_folder, "x.w3x")));
    }

    [Fact]
    public void Every_probe_source_builds()
    {
        var sources = Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "probes"), "*.lua");

        Assert.NotEmpty(sources);
        foreach (var source in sources)
        {
            var name = Path.GetFileNameWithoutExtension(source);
            Assert.Contains(ProbeMap.Source(name).ReplaceLineEndings("\n"), Build(name, ProbeMap.Source(name)).Script!);
        }
    }

    // Runs a probe map's script in the game's Lua, with stand-ins for the natives it calls; returns the lines done() wrote.
    List<string> Run(string lua)
    {
        var script = Build("probe", lua).Script!;
        using var state = new LuaState();
        var written = new List<string>();
        state.Register("record", values => { written.Add((string)values[0]!); return []; });
        state.Run("""
            local timers = {}
            function CreateTimer() return {} end
            function TimerStart(timer, timeout, periodic, f) timers[#timers + 1] = f end
            function InitBlizzard() end
            function PreloadGenClear() end
            function PreloadGenStart() end
            function Preload(text) record(text) end
            function PreloadGenEnd(file) end
            function EndGame() end
            function runTimers()
                for _, f in ipairs(timers) do f() end
            end
            """);
        state.Run(script);
        state.Call("main");
        state.Call("runTimers");
        return written.Select(text => text["\")\n".Length..^"\n//".Length]).ToList();
    }

    // Line ends as \n whatever the checkout gives the raw string the script is written from.
    Map Build(string name, string lua, bool withComputer = false)
    {
        var path = Path.Combine(_folder, name + ".w3x");
        ProbeMap.Build(name, lua, path, withComputer);
        var map = Map.Open(path);
        map.Script = map.Script!.ReplaceLineEndings("\n");
        return map;
    }
}
