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

    [Fact]
    public void A_map_has_a_minimap_image()
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
    public void Every_map_marks_when_it_has_loaded_and_when_its_game_has_started()
    {
        var path = Path.Combine(_folder, "hub-1.w3x");
        ProbeMap.BuildHub(1, path);

        foreach (var script in new[] { Build("hello", "done()").Script!, Map.Open(path).Script!.ReplaceLineEndings("\n") })
        {
            var main = script[script.IndexOf("function main()", StringComparison.Ordinal)..];
            Assert.Equal(@"MirrorImaged\loaded.pld", ProbeMap.Loaded);
            Assert.Equal(@"MirrorImaged\started.pld", ProbeMap.Started);
            Assert.Contains(
                "    InitBlizzard()\n    mark(\"MirrorImaged\\\\loaded.pld\")\n    TimerStart(CreateTimer(), 0.0, false, function()\n" +
                "        mark(\"MirrorImaged\\\\started.pld\")\n        start()\n",
                main);
        }
    }

    [Fact]
    public void The_probe_runs_at_game_start_and_writes_its_lines_to_its_Preload_file()
    {
        var script = Build("hello", "out(1)\ndone()").Script!;

        Assert.Contains("local function probe()\nout(1)\ndone()\nend", script);
        Assert.Contains("TimerStart(CreateTimer(), 0.0, false, function()", script);
        Assert.Contains("local ok, problem = pcall(probe)", script);
        Assert.Contains("PreloadGenEnd(\"MirrorImaged\\\\hello.pld\")", script);
        Assert.Contains("Preload(\"\\\")\\n\" .. line .. \"\\n//\")", script);
    }

    [Fact]
    public void A_probe_ends_the_game_or_loads_the_next_map()
    {
        var alone = Build("hello", "done()").Script!;
        var inHub = Build("hello", "done()", @"C:\maps\hub-2.w3x").Script!;

        Assert.Contains("EndGame(false)", alone);
        Assert.DoesNotContain("ChangeLevel", alone);
        Assert.Contains("ChangeLevel(\"C:\\\\maps\\\\hub-2.w3x\", false)", inHub);
        Assert.DoesNotContain("EndGame", inHub);
    }

    [Fact]
    public void An_error_in_the_probe_becomes_a_line()
    {
        var script = Build("hello", "error('x')").Script!;

        Assert.Contains("local ok, problem = pcall(probe)", script);
        Assert.Contains("out(\"error: \" .. tostring(problem))", script);
    }

    [Theory]
    [InlineData("")]
    [InlineData("a b")]
    [InlineData("a\\b")]
    [InlineData("a\"b")]
    [InlineData("Åsa")]
    public void A_probe_name_is_letters_digits_and_dashes(string name)
    {
        Assert.Throws<ArgumentException>(() => ProbeMap.Build(name, "done()", Path.Combine(_folder, "x.w3x")));
    }

    [Fact]
    public void The_hub_polls_its_request_file_and_loads_the_map_it_names()
    {
        var path = Path.Combine(_folder, "hub-3.w3x");
        ProbeMap.BuildHub(3, path);
        var script = Map.Open(path).Script!;

        Assert.Contains("Preloader(\"MirrorImaged\\\\hub\\\\3.pld\")", script);
        Assert.Contains("BlzSetAbilityTooltip(FourCC(\"AHbz\"), \"\", 0)", script);
        Assert.Contains("local map = BlzGetAbilityTooltip(FourCC(\"AHbz\"), 0)", script);
        Assert.Contains("ChangeLevel(map, false)", script);
        // The tooltip is emptied before each read: a missing file leaves it as it was.
        Assert.True(script.IndexOf("BlzSetAbilityTooltip", StringComparison.Ordinal) < script.IndexOf("Preloader", StringComparison.Ordinal));
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

    // Line ends as \n whatever the checkout gives the raw strings the scripts are written from.
    Map Build(string name, string lua, string? next = null)
    {
        var path = Path.Combine(_folder, name + ".w3x");
        ProbeMap.Build(name, lua, path, next);
        var map = Map.Open(path);
        map.Script = map.Script!.ReplaceLineEndings("\n");
        return map;
    }
}
