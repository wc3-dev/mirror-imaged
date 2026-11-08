using MirrorImaged.Lua;

namespace MirrorImaged.Tests;

/// <summary>JASS made into Lua as blizzard.j is, and the Lua kept between processes, in a folder of the test's own.</summary>
public sealed class BlizzardLuaTests : IDisposable
{
    readonly string _folder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

    public void Dispose()
    {
        if (Directory.Exists(_folder)) Directory.Delete(_folder, true);
        else File.Delete(_folder);
    }

    // War3Net writes such a name as it is, as blizzard.j's SubStringBJ's parameter end
    [Fact]
    public void A_jass_name_that_is_a_lua_keyword_is_renamed_so_the_lua_loads()
    {
        var lua = Blizzard.ToLua(
            "native SubString takes string source, integer start, integer end returns string",
            """
            function Cut takes string source, integer end returns string
                local integer while = end
                return SubString(source, 0, while) + "end"
            endfunction
            """);

        using var state = new LuaState();
        state.Register("SubString", args => [((string)args[0]!)[(int)args[1]!..(int)args[2]!]]);
        state.Run(lua);

        Assert.Equal("heend", state.Call("Cut", "hello", 2)[0]);
    }

    // the JASS manual (a claim): debug statements never run in the game; blizzard.j's PlayDialogueFromSpeakerEx has one
    [Fact]
    public void Debug_statements_are_left_out()
    {
        var lua = Blizzard.ToLua(
            "native Fail takes nothing returns nothing",
            """
            function Count takes nothing returns integer
                local integer n = 1
                debug set n = 2
                if n == 1 then
                    debug call Fail()
                    set n = n + 10
                endif
                return n
            endfunction
            """);

        using var state = new LuaState();
        state.Run(lua);

        Assert.Equal(11, state.Call("Count")[0]);
    }

    [Fact]
    public void The_cache_s_file_is_named_for_the_game_s_build_in_the_user_s_cache()
    {
        var file = Blizzard.CacheFile("build a")!;

        Assert.Equal(file, Blizzard.CacheFile("build a"));
        Assert.NotEqual(file, Blizzard.CacheFile("build b"));
        Assert.Equal(
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MirrorImaged", "cache"),
            Path.GetDirectoryName(file));
        Assert.Matches(@"^blizzard-[0-9a-f]{32}\.lua$", Path.GetFileName(file));
    }

    [Fact]
    public void The_lua_is_written_once_and_read_from_then_on_each_read_marking_the_file_used()
    {
        var file = Path.Combine(_folder, "blizzard-1.lua");
        int transpiles = 0;
        string Transpile()
        {
            transpiles++;
            return "x = 1";
        }

        Assert.Equal("x = 1", Blizzard.Cached(file, Transpile));
        Assert.Equal("x = 1", File.ReadAllText(file));
        File.WriteAllText(file, "x = 2");
        File.SetLastWriteTimeUtc(file, DateTime.UtcNow.AddDays(-10));
        Assert.Equal("x = 2", Blizzard.Cached(file, Transpile));
        Assert.Equal(1, transpiles);
        Assert.Equal([file], Directory.GetFiles(_folder));
        Assert.InRange(File.GetLastWriteTimeUtc(file), DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddMinutes(1));
    }

    // parallel runs of other builds keep their files
    [Fact]
    public void Writing_removes_the_blizzard_files_not_used_for_7_days_and_nothing_else()
    {
        Directory.CreateDirectory(_folder);
        foreach (var (name, days) in new[] { ("blizzard-old.lua", 8), ("blizzard-old.lua.123", 8), ("blizzard-recent.lua", 6), ("other.txt", 30) })
        {
            File.WriteAllText(Path.Combine(_folder, name), "");
            File.SetLastWriteTimeUtc(Path.Combine(_folder, name), DateTime.UtcNow.AddDays(-days));
        }

        Blizzard.Cached(Path.Combine(_folder, "blizzard-new.lua"), () => "new");

        Assert.Equal(
            ["blizzard-new.lua", "blizzard-recent.lua", "other.txt"],
            Directory.GetFiles(_folder).Select(Path.GetFileName).Order(StringComparer.Ordinal));
    }

    [Fact]
    public void A_cache_that_cant_be_written_still_gives_the_lua()
    {
        File.WriteAllText(_folder, "a file where the cache's folder would be");

        Assert.Equal("x = 1", Blizzard.Cached(Path.Combine(_folder, "blizzard-1.lua"), () => "x = 1"));
    }

    [Fact]
    public void With_no_cache_file_the_lua_is_transpiled_each_time()
    {
        int transpiles = 0;

        Blizzard.Cached(null, () => $"x = {++transpiles}");
        Assert.Equal("x = 2", Blizzard.Cached(null, () => $"x = {++transpiles}"));
    }
}
