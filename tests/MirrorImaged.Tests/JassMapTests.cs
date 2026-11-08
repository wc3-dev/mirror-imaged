using MirrorImaged.Lua;
using MirrorImaged.Natives;
using War3Net.Build.Info;

namespace MirrorImaged.Tests;

/// <summary>A JASS map's war3map.j made into Lua and run as a Lua map runs (decisions D130 to D137).</summary>
public sealed class JassMapTests : IDisposable
{
    readonly TestMaps _maps = new();

    public void Dispose() => _maps.Dispose();

    // Without blizzard.j, as the tests of the runtime run: the map knows common.j only
    Simulation Run(string jass, double seconds = 10)
    {
        var simulation = Simulation.Load(_maps.Build(jass, ScriptLanguage.Jass), "");
        simulation.Advance(TimeSpan.FromSeconds(seconds));
        return simulation;
    }

    [Fact]
    public void A_jass_map_with_triggers_timers_and_hashtables_gives_the_log_of_the_same_map_in_Lua()
    {
        using var jass = Run("""
            globals
                hashtable store = InitHashtable()
                integer ticks = 0
                trigger counter = null
                real array delays
            endglobals

            function Show takes string text returns nothing
                call DisplayTextToPlayer(GetLocalPlayer(), 0, 0, text)
            endfunction

            function Tick takes nothing returns nothing
                set ticks = ticks + 1
                call SaveInteger(store, 0, ticks, ticks * ticks)
                call SaveStr(store, 1, ticks, "tick " + I2S(ticks))
                call Show(LoadStr(store, 1, ticks) + " at " + R2S(TimerGetElapsed(GetExpiredTimer())))
                if ticks == 3 then
                    call PauseTimer(GetExpiredTimer())
                    call TriggerExecute(counter)
                endif
            endfunction

            function Odd takes nothing returns boolean
                return ticks / 2 * 2 != ticks
            endfunction

            function Count takes nothing returns nothing
                local integer i = 1
                local integer sum = 0
                loop
                    exitwhen i > ticks
                    set sum = sum + LoadInteger(store, 0, i)
                    set i = i + 1
                endloop
                call Show("sum " + I2S(sum) + ", a quarter " + I2S(sum / 4) + ", odd " + I2S(ticks) + " " + R2S(delays[1]))
                call TriggerSleepAction(1.0)
                call Show("after the wait " + I2S(LoadInteger(store, 0, 2)))
                call FlushChildHashtable(store, 0)
                call Show("flushed " + I2S(LoadInteger(store, 0, 2)))
            endfunction

            function Every takes nothing returns nothing
                call Show("every second, saved " + I2S(LoadInteger(store, 0, ticks)))
            endfunction

            function config takes nothing returns nothing
            endfunction

            function main takes nothing returns nothing
                local trigger every = CreateTrigger()
                set counter = CreateTrigger()
                set delays[1] = 0.5
                call TriggerAddCondition(counter, Condition(function Odd))
                call TriggerAddAction(counter, function Count)
                call TriggerRegisterTimerEvent(every, 1.0, true)
                call TriggerAddAction(every, function Every)
                call TimerStart(CreateTimer(), delays[1], true, function Tick)
            endfunction
            """, 4);
        using var lua = Simulation.Load(_maps.Build("""
            store = InitHashtable()
            ticks = 0
            counter = nil
            delays = __jarray(0.0)

            function Show(text)
              DisplayTextToPlayer(GetLocalPlayer(), 0, 0, text)
            end

            function Tick()
              ticks = ticks + 1
              SaveInteger(store, 0, ticks, ticks * ticks)
              SaveStr(store, 1, ticks, "tick " .. I2S(ticks))
              Show(LoadStr(store, 1, ticks) .. " at " .. R2S(TimerGetElapsed(GetExpiredTimer())))
              if ticks == 3 then
                PauseTimer(GetExpiredTimer())
                TriggerExecute(counter)
              end
            end

            function Odd()
              return ticks // 2 * 2 ~= ticks
            end

            function Count()
              local i, sum = 1, 0
              while i <= ticks do
                sum = sum + LoadInteger(store, 0, i)
                i = i + 1
              end
              Show("sum " .. I2S(sum) .. ", a quarter " .. I2S(sum // 4) .. ", odd " .. I2S(ticks) .. " " .. R2S(delays[1]))
              TriggerSleepAction(1.0)
              Show("after the wait " .. I2S(LoadInteger(store, 0, 2)))
              FlushChildHashtable(store, 0)
              Show("flushed " .. I2S(LoadInteger(store, 0, 2)))
            end

            function Every()
              Show("every second, saved " .. I2S(LoadInteger(store, 0, ticks)))
            end

            function config() end

            function main()
              local every = CreateTrigger()
              counter = CreateTrigger()
              delays[1] = 0.5
              TriggerAddCondition(counter, Condition(Odd))
              TriggerAddAction(counter, Count)
              TriggerRegisterTimerEvent(every, 1.0, true)
              TriggerAddAction(every, Every)
              TimerStart(CreateTimer(), delays[1], true, Tick)
            end
            """), "");
        lua.Advance(TimeSpan.FromSeconds(4));

        Assert.Equal(lua.Log, jass.Log);
        Assert.Contains("1.500 player 0: tick 3 at 0.000", jass.Log);
        Assert.Contains("1.500 player 0: sum 14, a quarter 3, odd 3 0.500", jass.Log);
        Assert.Equal(10, jass.Log.Count);
    }

    // probe jass-threads: the thread ends where it reads the variable; the log names it
    [Fact]
    public void Reading_a_variable_never_set_ends_the_thread_and_says_which()
    {
        using var simulation = Run("""
            globals
                timer never
            endglobals

            function Show takes string text returns nothing
                call DisplayTextToPlayer(GetLocalPlayer(), 0, 0, text)
            endfunction

            function config takes nothing returns nothing
            endfunction

            function main takes nothing returns nothing
                local integer x
                call Show("before")
                if never == null then
                    call Show("never")
                endif
                call Show("not reached")
            endfunction
            """);

        Assert.Equal(2, simulation.Log.Count);
        Assert.Equal("0.000 player 0: before", simulation.Log[0]);
        Assert.Matches(@"^0\.000 error: war3map\.lua:\d+: never was read before it was set, which ends the thread$", simulation.Log[1]);
    }

    // probe jass-threads: the game ends a thread past 3,000,000 operations, and the rest of the game goes on
    [Fact]
    public void A_loop_that_never_ends_stops_at_the_operation_limit_and_the_game_goes_on()
    {
        using var simulation = Run("""
            globals
                integer n = 0
            endglobals

            function Later takes nothing returns nothing
                call DisplayTextToPlayer(GetLocalPlayer(), 0, 0, "later " + I2S(n))
            endfunction

            function config takes nothing returns nothing
            endfunction

            function main takes nothing returns nothing
                call TimerStart(CreateTimer(), 1.0, false, function Later)
                loop
                    set n = n + 1
                endloop
            endfunction
            """);

        Assert.Equal(2, simulation.Log.Count);
        Assert.Matches(@"^0\.000 error: war3map\.lua:\d+: the thread passed JASS's operation limit$", simulation.Log[0]);
        Assert.StartsWith("1.000 player 0: later ", simulation.Log[1]);
        Assert.InRange(int.Parse(simulation.Log[1][22..]), Simulation.OperationLimit / 4 - 10, Simulation.OperationLimit / 4);
    }

    // probe jass-divide-overflow: the game closed; the stop names the JASS's line
    [Fact]
    public void Dividing_the_lowest_integer_by_minus_one_closes_the_game()
    {
        var map = _maps.Build("""
            function config takes nothing returns nothing
            endfunction

            function main takes nothing returns nothing
                local integer low = -2147483647 - 1
                call I2S(low / -1)
            endfunction
            """, ScriptLanguage.Jass);

        using var simulation = Simulation.Load(map, "");

        Assert.IsType<GameClosedException>(simulation.Stopped?.InnerException);
        Assert.Matches(@"^war3map\.lua:\d+: The game closes: -2147483648 was divided by -1", simulation.Stopped.Message);
    }

    // The game doesn't load a map whose JASS doesn't compile (a rawcode 'a\bc': the loaded mark never came, D130)
    [Theory]
    [InlineData("    call Unknown()", "Unknown is not declared")]
    [InlineData("    call TimerStart(null, 0, false, function Unknown)", "Unknown is not declared")]
    [InlineData("    call I2S(unknown)", "unknown is not declared")]
    [InlineData("    call I2S(", "doesn't parse")]
    [InlineData("    set nope = 1", "nope is not declared")]
    [InlineData("    set nopes[2] = 3", "nopes is not declared")]
    public void A_war3map_j_that_doesnt_compile_doesnt_load(string line, string problem)
    {
        var map = _maps.Build($"function main takes nothing returns nothing\n{line}\nendfunction\n", ScriptLanguage.Jass);

        var error = Assert.Throws<InvalidDataException>(() => Simulation.Load(map, ""));
        Assert.StartsWith($"The game wouldn't load {map}: its war3map.j can't be made into Lua (", error.Message);
        Assert.Contains(problem, error.Message);
    }

    // as many maps declare common.ai's UnitAlive (Aspect of Doom, Twilight's Eve and others in Documents' maps)
    [Fact]
    public void A_map_may_declare_a_native_itself()
    {
        using var simulation = Simulation.Load(_maps.Build("""
            native UnitAlive takes unit id returns boolean

            function config takes nothing returns nothing
            endfunction

            function main takes nothing returns nothing
                if not UnitAlive(null) then
                    call DisplayTextToPlayer(GetLocalPlayer(), 0, 0, "declared")
                endif
            endfunction
            """, ScriptLanguage.Jass), "");

        Assert.Null(simulation.Stopped);
        Assert.Equal(["0.000 player 0: declared"], simulation.Log);
    }

    // probe jass-threads: natives take null strings (Lua's binding refuses nil, decisions D80)
    [Fact]
    public void A_jass_map_gives_natives_null_strings()
    {
        using var simulation = Run("""
            function config takes nothing returns nothing
            endfunction

            function main takes nothing returns nothing
                local string s = null
                call DisplayTextToPlayer(GetLocalPlayer(), 0, 0, I2S(StringLength(s)) + " " + I2S(S2I(s)) + " [" + s + "]")
            endfunction
            """);

        Assert.Equal(["0.000 player 0: 0 0 []"], simulation.Log);
    }

    // probe jass-threads: null joins as "", and null + null is null
    [Fact]
    public void A_null_written_out_joins_as_a_null_string()
    {
        using var simulation = Run("""
            function config takes nothing returns nothing
            endfunction

            function main takes nothing returns nothing
                local string both = null + null
                call DisplayTextToPlayer(GetLocalPlayer(), 0, 0, "a" + null + "b")
                if both == null then
                    call DisplayTextToPlayer(GetLocalPlayer(), 0, 0, "null")
                endif
            endfunction
            """);

        Assert.Equal(["0.000 player 0: ab", "0.000 player 0: null"], simulation.Log);
    }

    // probe jass-integers: a decimal integer past 32 bits wraps, where Lua would read a float
    [Fact]
    public void A_decimal_integer_past_32_bits_wraps()
    {
        using var simulation = Run("""
            function config takes nothing returns nothing
            endfunction

            function main takes nothing returns nothing
                local real r = 4294967296
                call DisplayTextToPlayer(GetLocalPlayer(), 0, 0, I2S(4294967295) + " " + I2S(-2147483648) + " " + I2S(99999999999) + " " + R2S(r))
            endfunction
            """);

        Assert.Equal(["0.000 player 0: -1 -2147483648 1215752191 0.000"], simulation.Log);
    }

    [Fact]
    public void A_lua_map_still_refuses_a_nil_string()
    {
        using var simulation = Simulation.Load(_maps.Build("function config() end\nfunction main() print(pcall(StringLength, nil)) end\n"), "");

        Assert.Equal(["0.000 print: false\tbad argument #1 to 'StringLength' (string expected, got nil)"], simulation.Log);
    }
}
