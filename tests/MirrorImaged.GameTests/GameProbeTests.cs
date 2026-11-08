using MirrorImaged.Data;
using MirrorImaged.Probes;

namespace MirrorImaged.GameTests;

/// <summary>A test that launches the installed game; skipped when it isn't installed.</summary>
public sealed class GameFactAttribute : FactAttribute
{
    public GameFactAttribute()
    {
        if (!File.Exists(GameRunner.Default.Game))
            Skip = $"No Warcraft III at {GameRunner.Default.Game}; set {GameInstall.Variable} to the game's folder to run it";
    }
}

[Trait("Needs", "Game")]
public sealed class GameProbeTests
{
    [GameFact]
    public void The_hello_probe_prints_its_lines_in_the_game()
    {
        var run = GameRunner.Default.Run("hello", ProbeMap.Source("hello"), TimeSpan.FromMinutes(2));

        Assert.True(run.State == ProbeState.Done, $"{run.State}: {run.Detail}");
        Assert.Equal(["hello from the game", "3"], run.Lines);
    }

    [GameFact]
    public void Game_time_runs_at_full_speed_on_the_hidden_desktop()
    {
        var run = GameRunner.Default.Run("game-time", ProbeMap.Source("game-time"), TimeSpan.FromMinutes(2));

        Assert.True(run.State == ProbeState.Done, $"{run.State}: {run.Detail}");
        var last = run.Lines[^1].Split(' ');
        Assert.Equal("20", last[0]);
        Assert.InRange(int.Parse(last[2]), 19, 21);
    }

    [GameFact]
    public void A_line_too_long_for_Preload_ends_the_probe_with_its_error()
    {
        // 126 backslashes and a letter count 253, the most a line holds, and come back whole.
        var run = GameRunner.Default.Run("refused", "out(string.rep('\\\\', 126) .. 'a')\nout(string.rep('a', 254))\nout('never')\ndone()", TimeSpan.FromMinutes(2));

        Assert.True(run.State == ProbeState.Done, $"{run.State}: {run.Detail}");
        Assert.Equal(new string('\\', 126) + "a", run.Lines[0]);
        Assert.StartsWith("error: ", run.Lines[1]);
        Assert.Contains("a line is at most 253 bytes, a backslash counting two, with no line break", run.Lines[1]);
        Assert.Equal(2, run.Lines.Count);
    }

    // docs/facts.md, "Triggers and threads": the game's lines when they were measured (11-08), the same in two runs
    [GameFact]
    public void The_triggers_probe_gives_the_lines_measured()
    {
        var run = GameRunner.Default.Run("triggers", ProbeMap.Source("triggers"), TimeSpan.FromMinutes(2));

        Assert.True(run.State == ProbeState.Done, $"{run.State}: {run.Detail}");
        Assert.Equal([
            "timer thread false false trig nil event 0",
            "basics",
            "new true false 0 0",
            "evaluate empty true 0 0",
            "execute empty 1 1",
            "reset 0 0",
            "conditions",
            "conditions false",
            "ran a,b,e,d",
            "values nothing=false,nil=false,zero=false,one=true,empty=false,true=true",
            "boolexprs",
            "handles false 1048736 1048736 1048737 1048738",
            "boolexpr false",
            "ran f1,t2,t3,t4,f5",
            "nil operands false false false false",
            "destroyed condition false",
            "ran removed",
            "actions",
            "ran a1,a2 1,a3,a2 2,a3",
            "disabled false true 3",
            "ran a2 3,a3,disabled condition",
            "nested outer false false true event 0,inner true,back true",
            "condition thread",
            "in condition false false true 0",
            "condition evaluated false 0.5001",
            "waits",
            "w before 0.6000",
            "w start 0.6000 false false",
            "w after execute 0.6000",
            "wait in a coroutine",
            "execute wait",
            "waiter start 0.8000",
            "waiter after 0.8000",
            "waiter left 0.8000",
            "execute func",
            "func false false 0.9000 trig nil",
            "after ExecuteFunc 0.9000",
            "timer events",
            "zero event 1.0100",
            "variable",
            "variable set 1.1000",
            "game state",
            "state 1.2000 0 -253",
            "state set 1.2000 1",
            "gated condition 1.2501 false false true 4",
            "func after 1.2999",
            "slow done 1.2999",
            "periodic 1.5001 4 true nil 1 1",
            "w 1 1.7000 true",
            "expire trigger 1.7501 true 4",
            "expire callback 1.7501 false false",
            "w 0 1.7999",
            "w 0.05 1.9999",
            "periodic 2.0001 4 true nil 2 2",
            "w 0.33 2.3999",
            "w -1 2.4999",
            "enable 3.0001",
            "w pcall 3.0999 false war3map.lua:175: after",
            "periodic 3.5001 4 true nil 5 3",
            "w polled 4.2999",
            "w second action 4.2999",
            "timer sleep",
            "timer bare sleep",
            "late ",
            "at end 10 13 15 13",
        ], run.Lines);
    }

    // the wait lengths differed between the two runs, so only their lines' labels are compared
    [GameFact]
    public void The_second_triggers_probe_gives_the_lines_measured()
    {
        string[] waits = ["off grid ", "quarter ", "tenths ", "polled "];
        var run = GameRunner.Default.Run("triggers-2", ProbeMap.Source("triggers-2"), TimeSpan.FromMinutes(2));

        Assert.True(run.State == ProbeState.Done, $"{run.State}: {run.Detail}");
        Assert.Equal([
            "removal",
            "removed only true",
            "ran ",
            "removed first true",
            "ran second",
            "removed second true",
            "ran c1",
            "cleared true",
            "ran ",
            "destroyed only false",
            "ran ",
            "destroyed filter false",
            "ran ",
            "destroyed and true",
            "ran and1,and2",
            "destroyed operand true",
            "ran ",
            "nil boolexprs",
            "nil handles 1048732 1048732 1048732 conditionfunc filterfunc true true",
            "nil values true true false false true false true",
            "ran t,v,w,x",
            "values true true false",
            "ran a,b,c,d,e",
            "returns false false true false true false",
            "counts",
            "evaluate counts 0 0 0 0",
            "ran y,n",
            "event counts 1 0 1 0",
            "ran gated",
            "execute wait",
            "waiter start 0.6000 true",
            "waiter after 0.6000",
            "waiter after execute 0.6000",
            "waiter left 0.6000",
            "event ids",
            "timer callback 0 eventid true true true",
            "event 4 eventid gameevent true false true true true",
            "nested inner 4 true,func 4 true",
            "game states",
            "divine 2 -251 false gamestate true,3 -251 false gamestate true,5 -251 false gamestate true",
            "disconnected now 0",
            "ran disconnected 2,disconnected 0",
            "time of day time 3 true 2 true",
            "slow done 1.0999",
            "slow done 1.0999",
            "timer events",
            "periodic zero in 0.1 s 10",
            "timer events func after 1.0999 true,0.01@1.5100,0.005@1.5100,0.001@1.5100,0@1.5100,0.02@1.5200,b,a,b,x2,x1,x2,callback",
            "wait lengths",
            "changes while running",
            "ran first,second,third",
            "same unit as widget",
            "widget widget unit true false true 1048875 true",
            "late s1,s1 after,s2",
        ], run.Lines.Where(line => !waits.Any(line.StartsWith)));
        Assert.All(waits, label => Assert.Single(run.Lines, line => line.StartsWith(label)));
    }

    [GameFact]
    public void The_third_triggers_probe_gives_the_lines_measured()
    {
        var run = GameRunner.Default.Run("triggers-3", ProbeMap.Source("triggers-3"), TimeSpan.FromMinutes(2));

        Assert.True(run.State == ProbeState.Done, $"{run.State}: {run.Detail}");
        Assert.Equal([
            "start 0 0",
            "divine LESS_THAN 0:-255,1:-255,1:-255,0:-255",
            "divine LESS_THAN_OR_EQUAL 0:-254,1:-254,2:-254,2:-254,1:-254,0:-254",
            "divine EQUAL 2:-253,2:-253",
            "divine GREATER_THAN_OR_EQUAL 5:-252,2:-252,3:-252,2:-252,4:-252",
            "divine GREATER_THAN 5:-251,3:-251,4:-251",
            "divine NOT_EQUAL 5:-250,0:-250,1:-250,2:-250,3:-250,2:-250,1:-250,0:-250,4:-250",
            "disconnected LESS_THAN -1:-255",
            "disconnected LESS_THAN_OR_EQUAL 0:-254,-1:-254,0:-254",
            "disconnected EQUAL 0:-253,0:-253",
            "disconnected GREATER_THAN_OR_EQUAL 2:-252,0:-252,0:-252",
            "disconnected GREATER_THAN 2:-251",
            "disconnected NOT_EQUAL 2:-250,0:-250,-1:-250,0:-250",
            "time LESS_THAN 10.0:3,12.0:3",
            "time LESS_THAN_OR_EQUAL 10.0:3,12.0:3",
            "time EQUAL 12.0:3,12.0:3",
            "time GREATER_THAN_OR_EQUAL 12.0:3,13.0:3,14.0:3",
            "time GREATER_THAN 12.0:3,13.0:3,14.0:3",
            "time NOT_EQUAL ",
            "nil condition 1048743 1048744 false false false true false true",
        ], run.Lines);
    }
}
