using MirrorImaged.Lua;
using MirrorImaged.Natives;

namespace MirrorImaged.Tests;

/// <summary>
/// Small Lua maps run from their archives, as the game runs them: the script, config, main, then time; without the
/// game's blizzard.j, which none of them needs.
/// </summary>
public sealed class SimulationTests : IDisposable
{
    readonly TestMaps _maps = new();

    public void Dispose() => _maps.Dispose();

    Simulation Load(string main, int seed = 0) =>
        Simulation.Load(_maps.Build($"function config() end\nfunction main()\n{main}\nend\n"), "", new SimulationOptions { Seed = seed });

    [Fact]
    public void The_script_then_config_then_main_run_and_print_goes_in_the_log()
    {
        using var simulation = Simulation.Load(_maps.Build("""
            print("script")
            function config() print("config", 1, nil, true) end
            function main() print("main") end
            """), "");

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
    public void A_missing_native_stops_the_run_with_its_name_and_line_and_keeps_the_log()
    {
        using var simulation = Simulation.Load(_maps.Build("""
            function config() print("config") end
            function main()
              print("main")
              StartMeleeAI(Player(0), "human.ai")
              print("after")
            end
            """), "");

        var stop = simulation.Stopped!;
        Assert.StartsWith("war3map.lua:4: The native StartMeleeAI is Missing", stop.Message);
        Assert.Equal("StartMeleeAI", Assert.IsType<MissingNativeException>(stop.InnerException).Native);
        Assert.Equal(["0.000 print: config", "0.000 print: main"], simulation.Log);
        Assert.Same(stop, Assert.Throws<LuaException>(() => simulation.Advance(TimeSpan.FromSeconds(1))));
    }

    [Fact]
    public void A_missing_native_stops_the_run_before_its_arguments_are_read()
    {
        using var simulation = Load("StartMeleeAI({}, coroutine.create(print))");

        Assert.StartsWith("war3map.lua:3: The native StartMeleeAI is Missing", simulation.Stopped!.Message);
    }

    [Theory]
    [InlineData("pcall(ClearTextMessages)")]
    [InlineData("xpcall(ClearTextMessages, print)")]
    [InlineData("coroutine.wrap(ClearTextMessages)()")]
    public void A_missing_native_called_straight_from_c_names_the_nearest_lua_line(string call)
    {
        using var simulation = Load(call);

        Assert.StartsWith("war3map.lua:3: The native ClearTextMessages is Missing", simulation.Stopped!.Message);
        Assert.Empty(simulation.Log);
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
        Assert.Same(error, simulation.Stopped);
        Assert.Empty(simulation.Log);
        Assert.Same(error, Assert.Throws<LuaException>(() => simulation.Advance(TimeSpan.FromSeconds(3))));
    }

    [Fact]
    public void Short_advances_add_up_as_one_long_one()
    {
        using var simulation = Load("TimerStart(CreateTimer(), 10, false, function() print('ten') end)");

        for (int i = 0; i < 599; i++) simulation.Advance(TimeSpan.FromSeconds(1 / 60.0));
        Assert.Empty(simulation.Log);
        simulation.Advance(TimeSpan.FromSeconds(1 / 60.0));

        Assert.Equal(["10.000 print: ten"], simulation.Log);
    }

    [Fact]
    public void A_print_of_several_lines_is_one_entry_with_every_line_marked()
    {
        using var simulation = Load("print('one\\ntwo')");

        Assert.Equal(["0.000 print: one\n0.000 print: two"], simulation.Log);
    }

    [Fact]
    public void A_map_without_config_or_main_logs_which_is_missing()
    {
        using var simulation = Simulation.Load(_maps.Build("print('script')"), "");

        Assert.Equal([
            "0.000 print: script",
            "0.000 error: attempt to call a nil value (global 'config')",
            "0.000 error: attempt to call a nil value (global 'main')",
        ], simulation.Log);
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

    // GetTriggerEventId declares eventid, the parent of gameevent, limitop and the other event types, so a converted
    // handle can come back as its parent; gameevent 0 and limitop 0 are both eventids standing for 0
    [Fact]
    public void A_handle_is_found_whatever_type_lua_sees_it_as()
    {
        using var simulation = Load("");
        var gameEvent = simulation.Natives.ConvertGameEvent(0)!;
        var limitOp = simulation.Natives.ConvertLimitOp(0)!;
        var timer = simulation.Natives.CreateTimer()!;

        Assert.Same(gameEvent, simulation.ToCSharp(new LuaHandle("eventid", gameEvent.Key, gameEvent.Id, false), typeof(eventid), 1, "F"));
        Assert.Same(limitOp, simulation.ToCSharp(new LuaHandle("eventid", limitOp.Key, limitOp.Id, false), typeof(eventid), 1, "F"));
        Assert.Same(limitOp, simulation.ToCSharp(new LuaHandle("handle", limitOp.Key, limitOp.Id, false), typeof(handle), 1, "F"));
        Assert.Same(timer, simulation.ToCSharp(new LuaHandle("handle", timer.Key, timer.Id, false), typeof(handle), 1, "F"));
        var error = Assert.Throws<ArgumentException>(() => simulation.ToCSharp(new LuaHandle("eventid", timer.Key, timer.Id, false), typeof(eventid), 1, "F"));
        Assert.Equal("bad argument #1 to 'F' (eventid expected)", error.Message);
    }

    [Fact]
    public void A_converted_handle_is_the_same_userdata_for_the_same_value_and_type_and_takes_no_game_id()
    {
        using var simulation = Load("""
            print(PLAYER_COLOR_RED == ConvertPlayerColor(0), ConvertPlayerColor(0) == ConvertRace(0), ConvertGameEvent(0) == ConvertLimitOp(0), rawequal(ConvertPlayerColor(0), ConvertRace(0)))
            print(TimerGetTimeout(CreateTimer()), pcall(TimerStart, ConvertPlayerColor(0), 1, false, nil))
            """);

        Assert.Equal([
            "0.000 print: true\ttrue\ttrue\tfalse",
            "0.000 print: 0.0\tfalse\tbad argument #1 to 'TimerStart' (timer expected)",
        ], simulation.Log);
        Assert.Equal(0x10001D, simulation.Natives.CreateTimer()!.Id); // after the 28 players and the script's timer
    }

    [Fact]
    public void A_blizzard_lua_that_doesnt_load_or_run_stops_the_run_before_the_map()
    {
        var map = _maps.Build("print('never') function config() end function main() end");

        Assert.StartsWith("blizzard.lua:1: broken", Assert.Throws<LuaException>(() => Simulation.Load(map, "error('broken')")).Message);
        Assert.StartsWith("blizzard.lua:1:", Assert.Throws<LuaException>(() => Simulation.Load(map, "function (")).Message);
        using var stopped = Simulation.Load(map, "\nClearTextMessages()");
        Assert.StartsWith("blizzard.lua:2: The native ClearTextMessages is Missing", stopped.Stopped!.Message);
        Assert.Empty(stopped.Log);
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
            print(pcall(Player, "x"))
            print(pcall(TimerStart, CreateTimer(), 1, false, 5))
            print(pcall(TimerStart, {}, 1, false, nil))
            print(pcall(TimerGetTimeout, coroutine.create(print)))
            """);

        Assert.Equal([
            "0.000 print: false\tbad argument #1 to 'TimerStart' (timer expected)",
            "0.000 print: false\tbad argument #1 to 'Player' (number has no integer representation)",
            "0.000 print: false\tbad argument #1 to 'Player' (number expected, got string)",
            "0.000 print: false\tbad argument #4 to 'TimerStart' (function expected)",
            "0.000 print: false\tbad argument #1 to 'TimerStart' (timer expected)",
            "0.000 print: false\tbad argument #1 to 'TimerGetTimeout' (timer expected)",
        ], simulation.Log);
    }

    // a function a native refuses, or can't keep, is let go of, so Lua collects it
    [Theory]
    [InlineData("pcall(TimerStart, {}, 1, false, f)")]
    [InlineData("TimerStart(nil, 1, false, f)")]
    [InlineData("local t = CreateTimer() DestroyTimer(t) TimerStart(t, 1, false, f)")]
    [InlineData("pcall(Player, 0, f)")]
    public void A_function_a_native_doesnt_keep_is_let_go_of(string call)
    {
        using var simulation = Load($$"""
            local weak = setmetatable({}, {__mode = 'k'})
            local f = function() end
            weak[f] = true
            {{call}}
            f = nil
            for i = 1, 300000 do local junk = {i} end
            print(next(weak) == nil)
            """);

        Assert.Equal(["0.000 print: true"], simulation.Log);
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
            print(pcall(math.random, -2, 2147483647))
            print(math.type(math.random(0, 2147483647)), math.random(-1, 2147483646) >= -1)
            """);

        Assert.Equal([
            "0.000 print: true\ttrue\t5\ttrue\tinteger",
            "0.000 print: false\tbad argument #1 to 'random' (interval is empty)",
            "0.000 print: false\tbad argument #1 to 'random' (interval too large)",
            "0.000 print: integer\ttrue",
        ], simulation.Log);
    }

    // facts, "Lua runtime": FourCC is callable
    [Fact]
    public void FourCC_reads_an_object_id_from_its_bytes()
    {
        using var simulation = Load("print(FourCC('hfoo'), FourCC('A000'), FourCC('\\255\\0\\0\\1'), pcall(FourCC, 5))");

        Assert.Equal(["0.000 print: 1751543663\t1093677104\t-16777215\tfalse\tbad argument #1 to 'FourCC' (string expected, got number)"], simulation.Log);
    }

    // World Editor writes its array variables as udg_name = __jarray(default)
    [Fact]
    public void A_jarray_reads_its_default_where_nothing_is_set()
    {
        using var simulation = Load("""
            local counts, names, flags = __jarray(0), __jarray(""), __jarray(false)
            counts[2] = 5
            print(counts[1], counts[2], names[7] == "", flags[3], rawget(counts, 1))
            """);

        Assert.Equal(["0.000 print: 0\t5\ttrue\tfalse\tnil"], simulation.Log);
    }
}
