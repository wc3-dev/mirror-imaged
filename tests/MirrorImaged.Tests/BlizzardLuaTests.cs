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

    [Fact]
    public void The_lua_is_written_once_and_read_from_then_on()
    {
        var file = Path.Combine(_folder, "blizzard-a-1.lua");
        int transpiles = 0;
        string Transpile()
        {
            transpiles++;
            return "x = 1";
        }

        Assert.Equal("x = 1", Blizzard.Cached(file, Transpile));
        Assert.Equal("x = 1", File.ReadAllText(file));
        File.WriteAllText(file, "x = 2");
        Assert.Equal("x = 2", Blizzard.Cached(file, Transpile));
        Assert.Equal(1, transpiles);
        Assert.Equal([file], Directory.GetFiles(_folder));
    }

    [Fact]
    public void Writing_a_build_s_lua_removes_the_other_builds_files_and_nothing_else()
    {
        Directory.CreateDirectory(_folder);
        File.WriteAllText(Path.Combine(_folder, "blizzard-a-1.lua"), "old");
        File.WriteAllText(Path.Combine(_folder, "blizzard-a-1.lua.123"), "left by a run that couldn't finish writing");
        File.WriteAllText(Path.Combine(_folder, "other.txt"), "kept");

        Blizzard.Cached(Path.Combine(_folder, "blizzard-a-2.lua"), () => "new");

        Assert.Equal(["blizzard-a-2.lua", "other.txt"], Directory.GetFiles(_folder).Select(Path.GetFileName).Order(StringComparer.Ordinal));
    }

    [Fact]
    public void A_cache_that_cant_be_written_still_gives_the_lua()
    {
        File.WriteAllText(_folder, "a file where the cache's folder would be");

        Assert.Equal("x = 1", Blizzard.Cached(Path.Combine(_folder, "blizzard-a-1.lua"), () => "x = 1"));
    }
}
