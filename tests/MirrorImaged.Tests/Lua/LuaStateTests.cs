using MirrorImaged.Lua;

namespace MirrorImaged.Tests.Lua;

public sealed class LuaStateTests : IDisposable
{
    readonly LuaState _lua = new();

    public void Dispose() => _lua.Dispose();

    [Fact]
    public void RunReturnsWhatTheChunkReturns() =>
        Assert.Equal([1, 2.5f, "ü", true, null], _lua.Run("return 1, 2.5, 'ü', true, nil"));

    [Fact]
    public void GlobalsAreReadAndWritten()
    {
        _lua["x"] = 41;
        _lua["name"] = "peon";
        _lua["speed"] = 1.5;
        Assert.Equal([42, "peon!", 3f], _lua.Run("y = x + 1 return y, name .. '!', speed * 2"));
        Assert.Equal(42, _lua["y"]);
        Assert.Null(_lua["nothing"]);
    }

    [Fact]
    public void CallsAGlobalFunction()
    {
        _lua.Run("function twice(x) return x * 2, 'done' end");
        Assert.Equal([42, "done"], _lua.Call("twice", 21));
    }

    [Fact]
    public void LuaCallsACSharpFunction()
    {
        _lua.Register("add", args => [(int)args[0]! + (int)args[1]!]);
        _lua.Register("nothing", _ => []);
        Assert.Equal([5, null], _lua.Run("return add(2, 3), (nothing())"));
    }

    [Fact]
    public void ACSharpFunctionWorksInACoroutine()
    {
        _lua.Register("add", args => [(int)args[0]! + (int)args[1]!]);
        Assert.Equal([3], _lua.Run("return coroutine.wrap(function() coroutine.yield(add(1, 2)) end)()"));
    }

    [Fact]
    public void AnErrorNamesTheChunkAndLineAndHasATraceback()
    {
        var e = Assert.Throws<LuaException>(() => _lua.Run("local a\nreturn a.b", "war3map.lua"));
        Assert.StartsWith("war3map.lua:2: attempt to index a nil value", e.Message);
        Assert.Contains("stack traceback:", e.Message);
    }

    [Fact]
    public void ASyntaxErrorNamesTheChunkAndLine()
    {
        var e = Assert.Throws<LuaException>(() => _lua.Run("\nreturn +", "war3map.lua"));
        Assert.StartsWith("war3map.lua:2:", e.Message);
    }

    [Fact]
    public void ACSharpExceptionIsALuaErrorAtTheCallingLine()
    {
        _lua.Register("boom", _ => throw new InvalidOperationException("no"));
        var e = Assert.Throws<LuaException>(() => _lua.Run("\nboom()", "war3map.lua"));
        Assert.StartsWith("war3map.lua:2: no", e.Message);
        Assert.Equal([false, "war3map.lua:1: no"], _lua.Run("return pcall(function() boom() end)", "war3map.lua"));
    }

    [Fact]
    public void PcallOfErrorGivesThePositionOfTheLevel() =>
        Assert.Equal(["", "war3map.lua:2: "], _lua.Run("""
            local _, one = pcall(error, "", 1)
            local _, two = pcall(error, "", 2)
            return one, two
            """, "war3map.lua"));

    [Fact]
    public void ATableCantBeReadInCSharp() =>
        Assert.Throws<InvalidOperationException>(() => _lua.Run("return {}"));

    [Fact]
    public void ADisposedStateCantBeUsed()
    {
        var lua = new LuaState();
        lua.Dispose();
        Assert.Throws<ObjectDisposedException>(() => lua.Run("return 1"));
    }
}
