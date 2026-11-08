using MirrorImaged.Lua;
using MirrorImaged.Natives;

namespace MirrorImaged.Tests;

/// <summary>Small Lua maps run from their archives, as the game runs them: the script, config, main, then time.</summary>
public sealed class SimulationTests : IDisposable
{
    readonly TestMaps _maps = new();

    public void Dispose() => _maps.Dispose();

    Simulation Load(string main, int seed = 0) =>
        Simulation.Load(_maps.Build($"function config() end\nfunction main()\n{main}\nend\n"), new SimulationOptions { Seed = seed });

    [Fact]
    public void The_script_then_config_then_main_run_and_print_goes_in_the_log()
    {
        using var simulation = Simulation.Load(_maps.Build("""
            print("script")
            function config() print("config", 1, nil, true) end
            function main() print("main") end
            """));

        Assert.Equal(["0.000 print: script", "0.000 print: config\t1\tnil\ttrue", "0.000 print: main"], simulation.Log);
    }

    [Fact]
    public void Timers_fire_in_order_at_the_right_simulated_times()
    {
        using var simulation = Load("""
            TimerStart(CreateTimer(), 2.0, false, function() print("two") end)
            TimerStart(CreateTimer(), 0.5, false, function() print("half") end)
            TimerStart(CreateTimer(), 0.75, true, function() print("every") end)
            TimerStart(CreateTimer(), 0, false, function() print("started") end)
            """);

        simulation.Advance(TimeSpan.FromSeconds(2));

        Assert.Equal(["0.000 print: started", "0.500 print: half", "0.750 print: every", "1.500 print: every", "2.000 print: two"], simulation.Log);
    }

    [Fact]
    public void A_periodic_timer_doesnt_drift()
    {
        using var simulation = Load("""
            local n = 0
            TimerStart(CreateTimer(), 0.1, true, function()
              n = n + 1
              if n % 1000 == 0 then print(n) end
            end)
            """);

        simulation.Advance(TimeSpan.FromSeconds(300));

        Assert.Equal(["100.000 print: 1000", "200.000 print: 2000", "300.000 print: 3000"], simulation.Log);
    }

    [Fact]
    public void Time_moves_on_across_advances()
    {
        using var simulation = Load("TimerStart(CreateTimer(), 1.5, false, function() print('done') end)");

        simulation.Advance(TimeSpan.FromSeconds(1));
        Assert.Empty(simulation.Log);
        simulation.Advance(TimeSpan.FromSeconds(1));

        Assert.Equal(["1.500 print: done"], simulation.Log);
    }

    const string Varied = """
        print(GetRandomInt(1, 1000000), math.random(1, 1000000), math.random())
        print(tostring(CreateTimer()), tostring(Player(3)), tostring({}))
        TimerStart(CreateTimer(), 0.25, true, function()
          print(GetRandomReal(0, 1), os.clock(), os.time(), os.date('%H:%M:%S'))
        end)
        """;

    [Fact]
    public void The_same_seed_gives_the_same_log()
    {
        string[] Run(int seed)
        {
            using var simulation = Load(Varied, seed);
            simulation.Advance(TimeSpan.FromSeconds(2));
            return [.. simulation.Log];
        }

        var first = Run(7);
        Assert.Equal(first, Run(7));
        Assert.NotEqual(first[0], Run(8)[0]);
        Assert.Equal(10, first.Length);
    }

    [Fact]
    public void A_missing_native_stops_the_run_with_its_name_and_line()
    {
        var map = _maps.Build("""
            function config() end
            function main()
              print("before")
              local u = CreateUnit(Player(0), FourCC("hfoo"), 0, 0, 0)
              print("after")
            end
            """);

        var error = Assert.Throws<LuaException>(() => Simulation.Load(map));

        Assert.StartsWith("war3map.lua:4: The native CreateUnit is Missing", error.Message);
        Assert.Equal("CreateUnit", Assert.IsType<MissingNativeException>(error.InnerException).Native);
    }

    [Fact]
    public void A_missing_native_stops_the_run_from_a_timer_even_inside_pcall()
    {
        using var simulation = Load("""
            TimerStart(CreateTimer(), 1, false, function()
              local ok = pcall(function()
                ClearTextMessages()
              end)
              print("carried on", ok)
            end)
            TimerStart(CreateTimer(), 2, false, function() print("later") end)
            """);

        var error = Assert.Throws<LuaException>(() => simulation.Advance(TimeSpan.FromSeconds(3)));

        Assert.StartsWith("war3map.lua:5: The native ClearTextMessages is Missing", error.Message);
        Assert.IsType<MissingNativeException>(error.InnerException);
        Assert.Empty(simulation.Log);
        Assert.Throws<LuaException>(() => simulation.Advance(TimeSpan.FromSeconds(3)));
    }

    [Fact]
    public void A_lua_error_goes_in_the_log_and_the_run_carries_on()
    {
        using var simulation = Load("""
            TimerStart(CreateTimer(), 1, false, function() error("in a timer") end)
            TimerStart(CreateTimer(), 2, false, function() print("still running") end)
            local missing = nil
            missing()
            """);

        simulation.Advance(TimeSpan.FromSeconds(2));

        Assert.Equal([
            "0.000 error: war3map.lua:6: attempt to call a nil value (local 'missing')",
            "1.000 error: war3map.lua:3: in a timer",
            "2.000 print: still running",
        ], simulation.Log);
    }

    [Fact]
    public void Text_shown_to_players_goes_in_the_log_with_the_player()
    {
        using var simulation = Load("""
            DisplayTextToPlayer(Player(0), 0, 0, "hello")
            DisplayTimedTextToPlayer(GetLocalPlayer(), 0, 0, 5, "again")
            DisplayTextToPlayer(Player(2), 0, 0, "to blue")
            """);

        Assert.Equal(["0.000 player 0: hello", "0.000 player 0: again", "0.000 player 2: to blue"], simulation.Log);
    }

    // facts, "Handles and identity" and "Version and launching"
    [Fact]
    public void Handles_are_userdata_named_after_their_type_and_the_same_while_held()
    {
        using var simulation = Load("""
            local t = CreateTimer()
            print(type(t), tostring(t):match("^timer: %x+$") ~= nil, tostring(Player(0)):match("^player: ") ~= nil)
            print(GetLocalPlayer() == Player(0), Player(0) == Player(1), GetPlayerId(Player(5)), Player(28))
            local keyed = {[Player(1)] = "one"}
            print(keyed[Player(1)], Player(1.0) == Player(1))
            """);

        Assert.Equal([
            "0.000 print: userdata\ttrue\ttrue",
            "0.000 print: true\tfalse\t5\tnil",
            "0.000 print: one\ttrue",
        ], simulation.Log);
    }

    [Fact]
    public void A_native_checks_its_arguments_as_the_game_does()
    {
        using var simulation = Load("""
            print(pcall(TimerStart, Player(0), 1, false, nil))
            print(pcall(Player, 1.5))
            print(pcall(Player, "1"))
            print(pcall(TimerStart, CreateTimer(), 1, false, 5))
            """);

        Assert.Equal([
            "0.000 print: false\tbad argument #1 to 'TimerStart' (timer expected, got player)",
            "0.000 print: false\tbad argument #1 to 'Player' (number has no integer representation)",
            "0.000 print: false\tbad argument #1 to 'Player' (number expected, got string)",
            "0.000 print: false\tbad argument #4 to 'TimerStart' (function expected, got number)",
        ], simulation.Log);
    }

    [Fact]
    public void A_destroyed_timer_reads_as_nothing()
    {
        using var simulation = Load("""
            local t = CreateTimer()
            TimerStart(t, 1, false, function() print("ran") end)
            DestroyTimer(t)
            print(TimerGetTimeout(t), type(t))
            """);

        simulation.Advance(TimeSpan.FromSeconds(2));

        Assert.Equal(["0.000 print: 0.0\tuserdata"], simulation.Log);
    }

    [Fact]
    public void A_timer_callback_reads_its_timer_and_the_clock()
    {
        using var simulation = Load("""
            local start, t = os.time(), CreateTimer()
            TimerStart(t, 1.5, false, function()
              print(GetExpiredTimer() == t, TimerGetElapsed(t), os.clock(), os.time() - start, os.date("!%H:%M:%S"))
            end)
            """);

        simulation.Advance(TimeSpan.FromSeconds(2));

        Assert.Equal(["1.500 print: true\t1.5\t1.5\t1\t00:00:01"], simulation.Log);
    }

    // facts: six 0 s timers, each started in the one before, read the same elapsed time and os.clock as the 0.5 s timer
    [Fact]
    public void Zero_second_timers_started_in_a_callback_run_in_the_same_step()
    {
        using var simulation = Load("""
            local clock = CreateTimer()
            TimerStart(clock, 100000, false, nil)
            local function link(left)
              print(TimerGetElapsed(clock), os.clock())
              if left > 0 then TimerStart(CreateTimer(), 0, false, function() link(left - 1) end) end
            end
            TimerStart(CreateTimer(), 0.5, false, function() link(6) end)
            """);

        simulation.Advance(TimeSpan.FromSeconds(1));

        Assert.Equal(Enumerable.Repeat("0.500 print: 0.5\t0.5", 7), simulation.Log);
    }

    // facts, "Lua runtime": math.random is the game's generator, shared with GetRandomInt; math.randomseed reseeds it
    [Fact]
    public void Math_random_shares_the_games_generator()
    {
        using var simulation = Load("""
            local function three(f) return f(1, 1000) .. " " .. f(1, 1000) .. " " .. f(1, 1000) end
            SetRandomSeed(42)
            local a = three(math.random)
            SetRandomSeed(42)
            local b = three(GetRandomInt)
            math.randomseed(42)
            local c = three(GetRandomInt)
            print(a == b, b == c, math.random(5, 5), math.random(1) == 1, math.type(math.random(3)))
            print(pcall(math.random, 2, 1))
            """);

        Assert.Equal([
            "0.000 print: true\ttrue\t5\ttrue\tinteger",
            "0.000 print: false\tbad argument #1 to 'random' (interval is empty)",
        ], simulation.Log);
    }

    // facts, "Lua runtime": FourCC is callable
    [Fact]
    public void FourCC_reads_an_object_id()
    {
        using var simulation = Load("print(FourCC('hfoo'), FourCC('A000'))");

        Assert.Equal(["0.000 print: 1751543663\t1093677104"], simulation.Log);
    }
}
