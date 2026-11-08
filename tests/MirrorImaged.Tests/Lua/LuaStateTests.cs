using System.Runtime.CompilerServices;
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
    public void AStrictGlobalTableRaisesLuaErrors()
    {
        _lua.Run("setmetatable(_G, {__index = function(_, k) error('undefined ' .. k, 2) end, __newindex = function(_, k) error('no new global ' .. k, 2) end})");
        Assert.Contains("undefined x", Assert.Throws<LuaException>(() => _lua["x"]).Message);
        Assert.Contains("no new global y", Assert.Throws<LuaException>(() => { _lua["y"] = 1; }).Message);
        Assert.Contains("undefined missing", Assert.Throws<LuaException>(() => _lua.Call("missing")).Message);
        Assert.Contains("no new global add", Assert.Throws<LuaException>(() => _lua.Register("add", _ => [])).Message);
        Assert.Equal([2], _lua.Run("return 1 + 1"));
    }

    // No finalizer runs while C#'s values are pushed (the collector is stopped); a failing one later is a Lua error.
    [Fact]
    public void AFailingGcBreaksNeitherAPushNorTheState()
    {
        _lua.Register("many", _ => Enumerable.Range(0, 100000).Select(_ => (object?)new string('x', 64)).ToArray());
        Assert.Equal([100000], _lua.Run("""
            local function junk() setmetatable({}, {__gc = function() error('gc failed') end}) end
            junk()
            return select('#', many())
            """));
        var e = Assert.Throws<LuaException>(() => _lua.Run("for i = 1, 1000000 do local t = {i} end"));
        Assert.Contains("gc failed", e.Message);
        Assert.Equal([2], _lua.Run("return 1 + 1"));
    }
    [Fact]
    public void FinalizersCallingCSharpDuringALargePushDontChangeIt()
    {
        var values = Enumerable.Range(0, 300000).Select(i => (object?)$"v{i}{new string('x', 50)}").ToArray();
        _lua.Register("many", _ => values);
        var more = Enumerable.Range(0, 200000).Select(i => (object?)$"w{i}").ToArray();  // writes over most of the buffer
        _lua.Register("more", _ => more);
        _lua.Run("""
            function junk()
              for i = 1, 10 do setmetatable({}, {__gc = function() more() end}) end
            end
            function check(...)
              local t = table.pack(...)
              if t.n ~= 300000 then return t.n end
              for i = 1, t.n do
                if t[i] ~= 'v' .. (i - 1) .. string.rep('x', 50) then return i end
              end
              return 0
            end
            """);
        Assert.Equal([0], _lua.Run("junk() return check(many())"));
        _lua.Run("junk()");
        Assert.Equal([0], _lua.Call("check", values));
    }

    // C#'s values are pushed with the collector held, which must not cost it its pace (decisions D38).
    [Fact]
    public void LargeStringsFromCSharpAreCollectedAsLuasOwnAre()
    {
        var big = new string('x', 100_000);
        _lua.Register("big", _ => [big]);
        _lua.Run("for i = 1, 20000 do local s = big() end");
        using var lua = new LuaState();
        lua.Run("local base = string.rep('x', 99995) for i = 1, 20000 do local s = base .. i end");
        Assert.InRange(_lua.CommittedBytes, 0, lua.CommittedBytes + (4 << 20));
    }

    [Fact]
    public void ACSharpCallPerStepKeepsTheCollectorsPace()
    {
        const string loop = """
            local cycles = 0
            local function arm() setmetatable({}, {__gc = function() cycles = cycles + 1 arm() end}) end
            arm()
            for i = 1, 200000 do local t = {i, small()} end
            return cycles
            """;
        _lua.Register("small", _ => ["x"]);
        var fromCSharp = (int)_lua.Run(loop)[0]!;
        using var lua = new LuaState();
        var fromLua = (int)lua.Run("local function small() return 'x' end\n" + loop)[0]!;
        Assert.InRange(fromLua, 1, int.MaxValue);
        Assert.InRange(fromCSharp, 1, 2 * fromLua + 2);
    }

    [Fact]
    public void AFunctionCanBeCalledWhileItIsRegistered()
    {
        var calls = new List<string>();
        _lua.Run("setmetatable(_G, {__newindex = function(t, k, v) rawset(t, k, v) v() end})");
        _lua.Register("hello", _ => { calls.Add("hello"); return []; });
        _lua.Run("setmetatable(_G, {__newindex = function(t, k, v) v() error('refused') end})");
        Assert.Contains("refused", Assert.Throws<LuaException>(() => _lua.Register("bye", _ => { calls.Add("bye"); return []; })).Message);
        _lua.Run("setmetatable(_G, nil)");
        _lua.Register("again", _ => { calls.Add("again"); return []; });
        _lua.Run("hello() again()");
        Assert.Null(_lua["bye"]);
        Assert.Equal(["hello", "bye", "hello", "again"], calls);
    }

    [Fact]
    public void AFailedRegisterLeavesNoGlobalCallingAnotherFunction()
    {
        _lua.Run("setmetatable(_G, {__newindex = function(t, k, v) rawset(t, k, v) error('refused') end})");
        Assert.Contains("refused", Assert.Throws<LuaException>(() => _lua.Register("a", _ => ["a"])).Message);
        _lua.Run("setmetatable(_G, nil)");
        _lua.Register("b", _ => ["b"]);
        Assert.Equal(["a", "b"], _lua.Run("return a(), b()"));
    }

    [Fact]
    public void ADisposeDuringACallFreesTheStateOnlyAfterIt()
    {
        var lua = new LuaState();
        lua.Register("dispose", _ =>
        {
            lua.Dispose();
            return [];
        });
        Assert.Equal([100000], lua.Run("dispose() local t = {} for i = 1, 100000 do t[i] = {i} end return #t"));
        Assert.Throws<ObjectDisposedException>(() => lua.Run("return 1"));
    }

    // Shows only in an optimised build (Release, no tiered compilation), where nothing holds the state during Run;
    // the test above checks the same guard in any build.
    [Fact]
    public async Task AStateNothingHoldsIsNotFreedWhileItRuns()
    {
        using var stop = new CancellationTokenSource();
        var collector = Task.Run(() =>
        {
            while (!stop.IsCancellationRequested)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        });
        try
        {
            for (int i = 0; i < 20; i++) Assert.Equal([200000], RunUnheld());
        }
        finally
        {
            stop.Cancel();
            await collector;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static object?[] RunUnheld() => new LuaState().Run("local t = {} for i = 1, 200000 do t[i] = {i} end return #t");

    [Fact]
    public void AValueThatCantBePassedLeavesTheStateAsItWas()
    {
        _lua.Run("function f(...) return select('#', ...) end");
        for (int i = 0; i < 1000; i++) Assert.Throws<ArgumentException>(() => _lua.Call("f", 1, new object()));
        Assert.Equal([2], _lua.Call("f", 1, 2));
    }

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
