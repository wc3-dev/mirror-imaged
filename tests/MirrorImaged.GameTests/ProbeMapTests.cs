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
    public void A_line_is_refused_when_it_is_too_long_for_one_Preload_call_or_has_a_line_break()
    {
        var script = Build("hello", "done()").Script!;

        Assert.Contains("    text = tostring(text)\n    if #text > 253 or text:find(\"[\\r\\n]\") then\n        error(", script);
    }

    [Fact]
    public void An_error_in_the_probe_or_a_guarded_function_becomes_its_last_line_and_ends_the_probe()
    {
        var script = Build("hello", "error('x')").Script!;

        Assert.Contains(
            "        local ok, problem = pcall(f, ...)\n        if not ok then\n" +
            "            lines[#lines + 1] = (\"error: \" .. tostring(problem)):gsub(\"[\\r\\n]\", \" \"):sub(1, 253)\n" +
            "            done()\n",
            script);
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

    // Line ends as \n whatever the checkout gives the raw string the script is written from.
    Map Build(string name, string lua)
    {
        var path = Path.Combine(_folder, name + ".w3x");
        ProbeMap.Build(name, lua, path);
        var map = Map.Open(path);
        map.Script = map.Script!.ReplaceLineEndings("\n");
        return map;
    }
}
