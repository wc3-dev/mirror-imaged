using MirrorImaged.Lua;

namespace MirrorImaged.Tests.Lua;

/// <summary>
/// The operation limit a JASS map's state has (decisions D135): a thread raises once it has run so many Lua
/// instructions, counted afresh as it starts or is resumed, as the game's JASS counts a thread's operations (probe
/// jass-threads).
/// </summary>
public sealed class LuaLimitTests : IDisposable
{
    readonly LuaState _lua = new();

    public LuaLimitTests()
    {
        _lua.Register("wait", _ =>
        {
            _lua.Yield();
            return [];
        });
        _lua.Run("""
            n = 0
            function spin(times)
              for i = 1, times do n = n + 1 end
            end
            """);
    }

    public void Dispose() => _lua.Dispose();

    [Fact]
    public void A_state_has_no_limit_until_given_one()
    {
        _lua.Call("spin", 1_000_000);

        Assert.Equal(1_000_000, _lua["n"]);
        Assert.Equal(0, _lua.OperationLimit);
    }

    [Fact]
    public void The_main_thread_raises_at_the_running_line_past_the_limit_and_counts_afresh_each_call()
    {
        _lua.Run("function forever() while true do n = n + 1 end end", "war3map.lua");
        _lua.OperationLimit = 10_000;

        var error = Assert.Throws<LuaException>(() => _lua.Call("forever"));
        Assert.StartsWith("war3map.lua:1: the thread passed JASS's operation limit", error.Message);
        var first = (int)_lua["n"]!;
        Assert.InRange(first, 2_000, 5_000);
        _lua.Call("spin", 100);
        Assert.Throws<LuaException>(() => _lua.Call("forever"));
        Assert.Equal(2 * first + 100, _lua["n"]);
    }

    [Fact]
    public void A_thread_counts_its_own_from_its_start_and_again_after_each_wait()
    {
        _lua.Run("""
            function waits(times)
              spin(times)
              wait()
              spin(times)
              wait()
              spin(3 * times)
            end
            function both()
              spin(10)
              waits(2000)
            end
            """);
        _lua.OperationLimit = 10_000;

        var thread = _lua.Start("both");
        thread.Resume();
        Assert.Equal(4_010, _lua["n"]);
        Assert.True(thread.Waiting);
        Assert.Throws<LuaException>(thread.Resume);
        Assert.InRange((int)_lua["n"]!, 4_010, 10_010);
    }

    // MirrorImaged's own Lua (the chunk "mirror") is passed over, so a limit or a native's error names the script's line
    [Fact]
    public void An_error_raised_in_MirrorImaged_s_own_Lua_names_the_script_s_line()
    {
        _lua.Register("fail", _ => throw new ArgumentException("refused"));
        _lua.Run("function helper() fail() end\nfunction helperForever() while true do n = n + 1 end end", "mirror");
        _lua.Run("function script() helper() end\nfunction scriptForever() helperForever() end", "war3map.lua");
        _lua.OperationLimit = 10_000;

        Assert.StartsWith("war3map.lua:1: refused", Assert.Throws<LuaException>(() => _lua.Call("script")).Message);
        Assert.StartsWith("war3map.lua:2: the thread passed JASS's operation limit", Assert.Throws<LuaException>(() => _lua.Call("scriptForever")).Message);
    }

    [Fact]
    public void A_limit_of_zero_takes_it_away()
    {
        _lua.OperationLimit = 1_000;
        _lua.OperationLimit = 0;

        _lua.Run("function more() spin(100000) end");
        _lua.Call("spin", 100_000);
        _lua.Start("more");

        Assert.Equal(200_000, _lua["n"]);
        Assert.Throws<ArgumentOutOfRangeException>(() => _lua.OperationLimit = -1);
    }
}
