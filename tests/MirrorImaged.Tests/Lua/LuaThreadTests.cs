using MirrorImaged.Lua;

namespace MirrorImaged.Tests.Lua;

/// <summary>Lua threads C# starts and resumes, as the natives run a trigger's actions, and a C# function making one wait.</summary>
public sealed class LuaThreadTests : IDisposable
{
    readonly LuaState _lua = new();
    int _waits;

    public LuaThreadTests()
    {
        _lua.Register("wait", _ =>
        {
            _waits++;
            _lua.Yield();
            return [];
        });
    }

    public void Dispose() => _lua.Dispose();

    [Fact]
    public void A_thread_runs_to_its_end_and_keeps_what_its_function_returned()
    {
        _lua.Run("function f() return 1, 'two', {} end");

        var thread = _lua.Start("f");

        Assert.False(thread.Waiting);
        Assert.Equal([1, "two", new LuaOpaque("table")], thread.Results);
        Assert.Throws<InvalidOperationException>(thread.Resume);
    }

    // facts, "Errors and callbacks": pcall around a wait keeps working, and an error after it reaches pcall
    [Fact]
    public void A_csharp_function_makes_its_thread_wait_until_it_is_resumed_and_pcall_holds_across_it()
    {
        _lua.Run("""
            function f()
              wait()
              return pcall(function() wait() error("after") end)
            end
            """, "f.lua");

        var thread = _lua.Start("f");
        Assert.True(thread.Waiting);
        thread.Resume();
        Assert.True(thread.Waiting);
        thread.Resume();

        Assert.False(thread.Waiting);
        Assert.Equal([false, "f.lua:3: after"], thread.Results);
        Assert.Equal(2, _waits);
    }

    // the game's callbacks run in threads the script can't yield (probe triggers): only a wait can
    [Fact]
    public void The_script_cant_yield_the_thread_but_a_coroutine_it_makes_can()
    {
        _lua.Run("""
            function f()
              local inside = coroutine.wrap(function() coroutine.yield(coroutine.isyieldable()) end)()
              return coroutine.isyieldable(), select(2, coroutine.running()), inside, pcall(coroutine.yield)
            end
            """);

        Assert.Equal([false, false, true, false, "attempt to yield across a C-call boundary"], _lua.Start("f").Results);
    }

    // not measured: the game's own threads aren't Lua's, so only the natives run one on after its wait
    [Fact]
    public void The_script_cant_resume_a_thread_the_natives_run_but_resumes_its_own()
    {
        _lua.Run("function f() waiting = coroutine.running() wait() return 'ran on' end");

        var thread = _lua.Start("f");

        Assert.Equal([false, "cannot resume non-suspended coroutine", true, 2], _lua.Run("""
            local ok, problem = coroutine.resume(waiting)
            return ok, problem, coroutine.resume(coroutine.create(function(a) return a + 1 end), 1)
            """));
        Assert.True(thread.Waiting);
        thread.Resume();
        Assert.Equal(["ran on"], thread.Results);
    }

    // as stock Lua's: coroutine.status, which isn't wrapped, says the same for its argument
    [Fact]
    public void Coroutine_resume_checks_its_argument_as_stock_lua_does()
    {
        Assert.Equal([
            false, "f.lua:1: bad argument #1 to 'resume' (thread expected)",
            false, "f.lua:2: bad argument #1 to 'resume' (thread expected)",
            false, "f.lua:3: bad argument #1 to 'status' (thread expected)",
        ], _lua.Run("""
            local a, b = pcall(function() coroutine.resume(5) end)
            local c, d = pcall(function() coroutine.resume() end)
            return a, b, c, d, pcall(function() coroutine.status(5) end)
            """, "f.lua"));
    }

    [Fact]
    public void A_wait_in_the_main_thread_raises_luas_error()
    {
        Assert.Equal([false, "attempt to yield from outside a coroutine"], _lua.Run("return pcall(wait)"));
    }

    [Fact]
    public void An_error_ends_the_thread_and_is_thrown()
    {
        _lua.Run("function f() wait() error('boom') end", "f.lua");
        var thread = _lua.Start("f");

        Assert.StartsWith("f.lua:1: boom", Assert.Throws<LuaException>(thread.Resume).Message);
        Assert.False(thread.Waiting);
        Assert.StartsWith("attempt to call a nil value", Assert.Throws<LuaException>(() => _lua.Start("missing")).Message);
    }

    [Fact]
    public void A_waiting_thread_let_go_of_is_collected()
    {
        _lua.Run("""
            weak = setmetatable({}, {__mode = 'k'})
            function f() weak[coroutine.running()] = true wait() end
            """);

        _lua.Start("f").Dispose();

        Assert.Equal([true], _lua.Run("for i = 1, 300000 do local junk = {i} end return next(weak) == nil"));
    }
}
