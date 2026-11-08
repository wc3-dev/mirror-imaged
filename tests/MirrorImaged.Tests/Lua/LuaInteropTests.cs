using MirrorImaged.Lua;

namespace MirrorImaged.Tests.Lua;

/// <summary>Handles, functions and halting errors between C# and Lua.</summary>
public sealed class LuaInteropTests : IDisposable
{
    readonly LuaState _lua = new();
    bool _reached;

    public LuaInteropTests()
    {
        _lua.Register("reach", _ =>
        {
            _reached = true;
            return [];
        });
    }

    public void Dispose() => _lua.Dispose();

    [Fact]
    public void AHandleIsAUserdataNamedAfterItsType()
    {
        _lua.Register("make", _ => [new LuaHandle("timer", 7, 1048600, true)]);
        Assert.Equal(["userdata", true, false], _lua.Run("return type(make()), tostring(make()):match('^timer: %x+$') ~= nil, getmetatable(make())"));
        Assert.Equal(new LuaHandle("timer", 7, 1048600, true), _lua.Run("return make()")[0]);
    }

    // facts, "Handles and identity": the same object returned as another type is another userdata; == holds between
    // them (probe triggers-2: GetTriggerWidget() == u, GetTriggerEventId() == EVENT_GAME_TIMER_EXPIRED)
    [Fact]
    public void AHandleIsTheSameUserdataAsOneTypeAndAnotherAsAnother()
    {
        _lua.Register("timer", args => [new LuaHandle("timer", (int)args[0]!, (int)args[0]!, true)]);
        _lua.Register("agent", args => [new LuaHandle("agent", (int)args[0]!, (int)args[0]!, true)]);
        Assert.Equal([true, false, false, true, true, false, true], _lua.Run("""
            local t = {[timer(1)] = 'one'}
            return rawequal(timer(1), timer(1)), timer(1) == timer(2), rawequal(timer(1), agent(1)), t[timer(1)] == 'one',
              timer(1) == agent(1), agent(1) == agent(2), t[agent(1)] == nil
            """));
        Assert.StartsWith("agent: ", (string)_lua.Run("return tostring(agent(1))")[0]!);
    }

    // == compares the game's ids, whatever the types and keys (probe triggers: GetTriggerEventId() with id 4 equals
    // ConvertPlayerColor(4)); an agent's == refuses a second operand that isn't an agent, as the game's (probe handles-2)
    [Fact]
    public void HandlesAreEqualByTheirIdsAndAnAgentsEqualityRefusesAHandleThatIsntOne()
    {
        // each its own key, which Lua's handles are found by; the ids are what == compares
        _lua.Register("timer", args => [new LuaHandle("timer", (int)args[0]!, (int)args[0]!, true)]);
        _lua.Register("color", args => [new LuaHandle("playercolor", 1000 + (int)args[0]!, (int)args[0]!, false)]);
        _lua.Register("race", args => [new LuaHandle("race", 2000 + (int)args[0]!, (int)args[0]!, false)]);
        Assert.Equal([true, false, true, false, false, true], _lua.Run("""
            return color(4) == race(4), rawequal(color(4), race(4)), color(9) == timer(9), color(8) == timer(9), timer(9) == {},
              timer(9) ~= timer(8)
            """));
        Assert.Equal([false, "chunk:2: bad argument #2 to '__eq' (agent expected)"], _lua.Run("""
            return pcall(function()
              local equal = timer(9) == color(9)
            end)
            """));
    }

    // as luaL_checkinteger and luaL_checknumber read a string, which the game's natives take for a number (probe arguments)
    [Theory]
    [InlineData("5", 5)]
    [InlineData(" -7\t", -7)]
    [InlineData("0x10", 16)]
    [InlineData("0xffffffff", -1)]
    [InlineData("2.0", 2f)]
    [InlineData("1.5", 1.5f)]
    [InlineData("1e2", 100f)]
    [InlineData("2147483648", 2147483648f)]
    [InlineData("x", null)]
    [InlineData("", null)]
    [InlineData("5x", null)]
    [InlineData("5\0", null)]
    [InlineData("inf", null)]
    [InlineData("nan", null)]
    public void AStringIsReadAsANumberAsLuaReadsIt(string text, object? number) => Assert.Equal(number, LuaState.ToNumber(text));

    [Fact]
    public void ACSharpFunctionReadsTheHandleItIsGiven()
    {
        _lua.Register("make", _ => [new LuaHandle("player", 3, 1048584, true)]);
        object? given = null;
        _lua.Register("take", args =>
        {
            given = args[0];
            return [];
        });
        _lua.Run("take(make())");
        Assert.Equal(new LuaHandle("player", 3, 1048584, true), given);
    }

    [Fact]
    public void HandlesAreAtTheSamePlacesEveryRun()
    {
        const string code = """
            local keys, order = {}, {}
            for i = 1, 100 do keys[make(i)] = i end
            for _, i in pairs(keys) do order[#order + 1] = i end
            return table.concat(order, ' '), tostring(make(1)), tostring(make(100))
            """;
        object?[] Run()
        {
            using var lua = new LuaState();
            lua.Register("make", args => [new LuaHandle("timer", (int)args[0]!, (int)args[0]!, true)]);
            return lua.Run(code);
        }
        Assert.Equal(Run(), Run());
    }

    [Fact]
    public void AKeptFunctionCanBeCalledAfterTheCallThatGaveIt()
    {
        LuaFunction? kept = null;
        _lua.Register("keep", args =>
        {
            kept = ((LuaFunction)args[0]!).Keep();
            return [];
        });
        _lua.Run("local n = 40 keep(function() n = n + 1 return n end)");
        Assert.Equal([41], kept!.Start().Results);
        Assert.Equal([42], kept.Start().Results);
        kept.Dispose();
        Assert.Throws<ObjectDisposedException>(() => kept.Start());
    }

    [Fact]
    public void AFunctionNotKeptIsLetGoOfWhenTheCallReturns()
    {
        LuaFunction? given = null;
        _lua.Register("look", args =>
        {
            given = (LuaFunction)args[0]!;
            return [given.Start().Results[0]];
        });
        Assert.Equal([5], _lua.Run("return look(function() return 5 end)"));
        Assert.Throws<ObjectDisposedException>(() => given!.Start());
    }

    [Fact]
    public void LuaCollectsAFunctionOnceCSharpLetsGoOfIt()
    {
        LuaFunction? kept = null;
        _lua.Register("keep", args =>
        {
            kept = ((LuaFunction)args[0]!).Keep();
            return [];
        });
        _lua.Register("look", _ => []);
        const string count = """
            for i = 1, 300000 do local junk = {i} end
            local n = 0
            for _ in pairs(weak) do n = n + 1 end
            return n
            """;
        _lua.Run("weak = setmetatable({}, {__mode = 'k'}) local f, g = function() end, function() end weak[f], weak[g] = 1, 2 keep(f) look(g)");
        Assert.Equal([1], _lua.Run(count));
        kept!.Dispose();
        Assert.Equal([0], _lua.Run(count));
    }

    [Fact]
    public void AHaltingExceptionStopsTheStateWithItsLine()
    {
        _lua.Halts = e => e is TimeoutException;
        _lua.Register("stop", _ => throw new TimeoutException("stopped here"));
        var e = Assert.Throws<LuaException>(() => _lua.Run("""
            local ok = pcall(function()
              local co = coroutine.create(function()
                xpcall(function()
                  stop()
                end, function(m) return m end)
              end)
              coroutine.resume(co)
            end)
            reach()
            """, "war3map.lua"));
        Assert.StartsWith("war3map.lua:4: stopped here", e.Message);
        Assert.IsType<TimeoutException>(e.InnerException);
        Assert.Same(e.InnerException, _lua.Halted);
        Assert.False(_reached);
        _lua.Halts = null;
        Assert.Same(_lua.Halted, Assert.Throws<LuaException>(() => _lua.Run("reach()")).InnerException);
        Assert.False(_reached);
    }

    [Theory]
    [InlineData("pcall(stop)")]
    [InlineData("pcall(function() stop() end)")]
    [InlineData("xpcall(stop, function(m) reach() return m end)")]
    [InlineData("coroutine.resume(coroutine.create(stop))")]
    [InlineData("coroutine.resume(coroutine.create(function() pcall(stop) reach() end))")]
    [InlineData("pcall(coroutine.wrap(function() pcall(stop) reach() end))")]
    [InlineData("load(function() pcall(stop) reach() end)")]
    [InlineData("pcall(string.gsub, 'a', 'a', function() pcall(stop) reach() end)")]
    public void NoCatchCarriesOnPastAHalt(string code)
    {
        _lua.Halts = e => e is TimeoutException;
        _lua.Register("stop", _ => throw new TimeoutException("stopped"));
        Assert.IsType<TimeoutException>(Assert.Throws<LuaException>(() => _lua.Run($"{code} reach()")).InnerException);
        Assert.False(_reached);
    }

    [Fact]
    public void ASuspendedCoroutineCantCarryOnPastAHalt()
    {
        _lua.Halts = e => e is TimeoutException;
        _lua.Register("stop", _ => throw new TimeoutException("stopped"));
        _lua.Run("resume = coroutine.wrap(function() coroutine.yield() reach() end) resume()");
        Assert.Throws<LuaException>(() => _lua.Run("pcall(stop)"));
        Assert.Throws<LuaException>(() => _lua.Call("resume"));
        Assert.False(_reached);
    }

    [Fact]
    public void CSharpSwallowingAHaltDoesntCarryOnEither()
    {
        _lua.Halts = e => e is TimeoutException;
        _lua.Register("stop", _ => throw new TimeoutException("stopped"));
        _lua.Run("function inner() stop() end", "war3map.lua");
        _lua.Register("swallow", _ =>
        {
            try { _lua.Call("inner"); }
            catch (LuaException) { }
            return [];
        });
        var e = Assert.Throws<LuaException>(() => _lua.Run("swallow() reach()", "war3map.lua"));
        Assert.StartsWith("war3map.lua:1: stopped", e.Message);
        Assert.IsType<TimeoutException>(e.InnerException);
        Assert.False(_reached);
    }

    // A C# handler or finalizer is called without a Lua instruction, so the hook can't stop it.
    [Fact]
    public void NoCSharpFunctionRunsAfterAHalt()
    {
        _lua.Halts = e => e is TimeoutException;
        _lua.Register("stop", _ => throw new TimeoutException("stopped"));

        Assert.Throws<LuaException>(() => _lua.Run("xpcall(stop, reach)"));
        Assert.False(_reached);
    }

    [Theory]
    [InlineData("pcall(stop)")]
    [InlineData("xpcall(stop, print)")]
    [InlineData("coroutine.wrap(stop)()")]
    [InlineData("coroutine.resume(coroutine.create(stop))")]
    public void AHaltCalledStraightFromCNamesTheNearestLuaLine(string call)
    {
        _lua.Halts = e => e is TimeoutException;
        _lua.Register("stop", _ => throw new TimeoutException("stopped"));

        Assert.StartsWith("war3map.lua:2: stopped", Assert.Throws<LuaException>(() => _lua.Run($"local x = 1\n{call}", "war3map.lua")).Message);
    }

    [Fact]
    public void ACSharpFunctionGetsWhatItCantReadAsItsType()
    {
        object?[] given = [];
        _lua.Register("look", args =>
        {
            given = args;
            return [];
        });
        _lua.Run("look({}, coroutine.create(print), 1)");
        Assert.Equal([new LuaOpaque("table"), new LuaOpaque("thread"), 1], given);
    }

    [Fact]
    public void CallingAMissingGlobalNamesIt() =>
        Assert.StartsWith("attempt to call a nil value (global 'nothing')", Assert.Throws<LuaException>(() => _lua.Call("nothing")).Message);

    [Fact]
    public void OtherExceptionsCanStillBeCaught()
    {
        _lua.Halts = e => e is TimeoutException;
        _lua.Register("fail", _ => throw new InvalidOperationException("no"));
        Assert.Equal([false], _lua.Run("return (pcall(fail))"));
        Assert.Null(_lua.Halted);
    }
}
