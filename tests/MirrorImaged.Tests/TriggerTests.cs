using MirrorImaged.Lua;
using MirrorImaged.Natives;

namespace MirrorImaged.Tests;

/// <summary>
/// Triggers, conditions, threads and waits, against the facts' "Errors and callbacks" and "Initialisation" and the
/// probes triggers, triggers-2 and triggers-3 (docs/facts.md, "Triggers and threads"). Each map's main is the script
/// given, from line 3, without blizzard.j.
/// </summary>
public sealed class TriggerTests : IDisposable
{
    readonly TestMaps _maps = new();

    public void Dispose() => _maps.Dispose();

    Simulation Load(string main) =>
        Simulation.Load(_maps.Build($"function config() end\nfunction main()\n{main}\nend\n"), "");

    static IEnumerable<string> Printed(Simulation simulation) => simulation.Log.Select(line => line.Replace(" print: ", " "));

    const string Cond = """
        local ran = {}
        local function cond(name, value) return Condition(function() ran[#ran + 1] = name return value end) end
        local function evaluate(e) local t = CreateTrigger() TriggerAddCondition(t, e) return TriggerEvaluate(t) end
        local function done() local s = table.concat(ran, ",") ran = {} return s end

        """;

    [Fact]
    public void A_new_trigger_is_enabled_and_execute_counts_an_evaluation_and_a_run_where_evaluate_counts_none()
    {
        using var simulation = Load("""
            local t = CreateTrigger()
            print(IsTriggerEnabled(t), IsTriggerWaitOnSleeps(t), GetTriggerEvalCount(t), GetTriggerExecCount(t))
            print(TriggerEvaluate(t), GetTriggerEvalCount(t), GetTriggerExecCount(t))
            TriggerExecute(t)
            print(GetTriggerEvalCount(t), GetTriggerExecCount(t))
            ResetTrigger(t)
            TriggerWaitOnSleeps(t, true)
            print(GetTriggerEvalCount(t), GetTriggerExecCount(t), IsTriggerWaitOnSleeps(t))
            """);

        Assert.Equal(["0.000 true\tfalse\t0\t0", "0.000 true\t0\t0", "0.000 1\t1", "0.000 0\t0\ttrue"], Printed(simulation));
    }

    // facts: TriggerEvaluate runs every condition in order with no short-circuit (a claim, measured by probe triggers);
    // a condition that raises counts as false and the caller goes on
    [Fact]
    public void Every_condition_runs_in_order_and_one_that_raises_fails()
    {
        using var simulation = Load(Cond + """
            local t = CreateTrigger()
            TriggerAddCondition(t, cond("a", false))
            TriggerAddCondition(t, cond("b", true))
            TriggerAddCondition(t, Condition(function() ran[#ran + 1] = "e" error("raised") end))
            TriggerAddCondition(t, cond("d", true))
            print(TriggerEvaluate(t), done())
            """);

        Assert.Equal(["0.000 error: war3map.lua:10: raised", "0.000 false\ta,b,e,d"], Printed(simulation));
    }

    [Fact]
    public void A_condition_passes_on_true_or_a_number_other_than_0()
    {
        using var simulation = Load("""
            local function passes(...)
              local values, t = table.pack(...), CreateTrigger()
              TriggerAddCondition(t, Condition(function() return table.unpack(values, 1, values.n) end))
              return TriggerEvaluate(t)
            end
            print(passes(), passes(nil), passes(0), passes(1), passes(""), passes(true), passes(false))
            print(passes("x"), passes("1"), passes(0.5), passes({}), passes(-1), passes(false, true))
            """);

        Assert.Equal([
            "0.000 false\tfalse\tfalse\ttrue\tfalse\ttrue\tfalse",
            "0.000 false\tfalse\ttrue\tfalse\ttrue\tfalse",
        ], Printed(simulation));
    }

    // facts: Condition and Filter return a new handle every call, even for the same function
    [Fact]
    public void Condition_and_Filter_give_a_new_handle_every_call_but_one_for_nil()
    {
        using var simulation = Load("""
            local f = function() return true end
            local cn = Condition(nil)
            print(Condition(f) == Condition(f), Filter(f) == Filter(f), rawequal(cn, Condition(nil)), rawequal(Filter(nil), Filter(nil)), cn == Filter(nil), rawequal(cn, Filter(nil)))
            print(tostring(cn):match("^%a+"), tostring(Filter(nil)):match("^%a+"), pcall(Condition, 5))
            """);

        Assert.Equal([
            "0.000 false\tfalse\ttrue\ttrue\ttrue\tfalse",
            "0.000 conditionfunc\tfilterfunc\tfalse\tbad argument #1 to 'Condition' (function expected)",
        ], Printed(simulation));
    }

    [Fact]
    public void And_and_Or_run_their_second_operand_only_when_needed_and_a_nil_operand_is_false()
    {
        using var simulation = Load(Cond + """
            print(evaluate(And(cond("f1", false), cond("x1", true))), evaluate(Or(cond("t2", true), cond("x2", true))), evaluate(Not(cond("t3", true))), done())
            print(evaluate(And(cond("t", true), nil)), evaluate(And(nil, cond("u", true))), evaluate(Or(nil, cond("v", true))), evaluate(Or(cond("w", false), nil)), evaluate(Or(cond("x", true), nil)), done())
            print(evaluate(And(nil, nil)), evaluate(Not(nil)), evaluate(nil), evaluate(Not(cond("a", false))), evaluate(And(cond("b", true), cond("c", true))), evaluate(Or(cond("d", false), cond("e", false))), done())
            """);

        Assert.Equal([
            "0.000 false\ttrue\tfalse\tf1,t2,t3",
            "0.000 false\tfalse\ttrue\tfalse\ttrue\tt,v,w,x",
            "0.000 false\tfalse\tfalse\ttrue\ttrue\tfalse\ta,b,c,d,e",
        ], Printed(simulation));
    }

    // probes triggers-2 and triggers-3; And(true, Condition(nil)) is left out: true in the game, false here
    [Fact]
    public void Condition_nil_passes_alone_and_is_false_in_an_And_Or_or_Not()
    {
        using var simulation = Load(Cond + """
            local cn = Condition(nil)
            print(evaluate(cn), evaluate(Filter(nil)), evaluate(Not(cn)), evaluate(Not(Not(cn))), evaluate(And(cn, cond("y", true))), evaluate(Or(cn, cond("z", false))), evaluate(Not(Condition(function() end))), done())
            """);

        Assert.Equal(["0.000 true\ttrue\ttrue\tfalse\tfalse\tfalse\ttrue\tz"], Printed(simulation));
    }

    // Every case the probes triggers-2, triggers-3, handles and handles-3 measured with Condition(nil) (cn) in a trigger's
    // conditions, the game's result beside the model's (decisions D114): t and u pass, f fails, x passes. A list of
    // several is a trigger's conditions in turn.
    static readonly (string Conditions, bool Game, bool Model)[] NilConditionCases =
    [
        ("cn", true, true),
        ("Not(cn)", true, true),
        ("Not(Not(cn))", false, false),
        ("And(cn, f)", false, false),
        ("And(cn, x)", false, false),
        ("Or(cn, f)", false, false),
        ("And(t, cn)", true, false),
        ("And(t, Not(cn))", false, true),
        ("Not(And(t, cn))", false, true),
        ("And(t, And(cn, x))", false, false),
        ("And(Or(f, t), cn)", false, false),
        ("Or(f, cn)", false, false),
        ("t, Not(cn)", true, true),
        ("t, cn", true, true),
        ("And(t, Or(cn, f))", false, false),
        ("And(t, Not(Not(cn)))", false, false),
        ("Or(f, Not(cn))", true, true),
        ("And(Not(f), cn)", false, false),
        ("And(t, And(u, cn))", true, false),
        ("And(Not(cn), t)", false, true),
        ("And(And(t, u), Not(cn))", false, true),
    ];

    [Fact]
    public void Condition_nil_in_a_boolexpr_gives_the_games_result_in_15_of_the_21_cases_measured()
    {
        using var simulation = Load($$"""
            local cn = Condition(nil)
            local yes, no = function() return true end, function() return false end
            local t, u, f, x = Condition(yes), Condition(yes), Condition(no), Condition(yes)
            local function evaluate(...) local trig = CreateTrigger() for _, c in ipairs({...}) do TriggerAddCondition(trig, c) end return TriggerEvaluate(trig) end
            {{string.Join("\n", NilConditionCases.Select(c => $"print(evaluate({c.Conditions}))"))}}
            """);

        Assert.Equal(NilConditionCases.Select(c => $"0.000 {(c.Model ? "true" : "false")}"), Printed(simulation));
        Assert.Equal(15, NilConditionCases.Count(c => c.Game == c.Model));
    }

    // W1-9's probe events: a destroyed Filter given to ForceEnumPlayers after a Condition had taken its place ran the new
    // condition; a destroyed condition given to a native is its place's (decisions D115)
    [Fact]
    public void A_destroyed_condition_given_to_a_native_runs_what_its_place_holds()
    {
        using var simulation = Load(Cond + """
            local df = Filter(function() ran[#ran + 1] = "d" return true end)
            DestroyFilter(df)
            local t = CreateTrigger()
            TriggerAddCondition(t, df)
            print("empty", TriggerEvaluate(t), done())
            cond("c", true)
            print("taken", TriggerEvaluate(t), evaluate(And(df, cond("y", true))), GetHandleId(df) > 0, done())
            """);

        Assert.Equal(["0.000 empty\tfalse\t", "0.000 taken\ttrue\ttrue\ttrue\tc,c,y"], Printed(simulation));
    }

    // Condition(nil) and Filter(nil) are one handle in the game: destroying either destroys both (decisions D114; not
    // measured)
    [Fact]
    public void Destroying_Condition_nil_destroys_Filter_nil()
    {
        using var simulation = Load(Cond + """
            local cn, fn = Condition(nil), Filter(nil)
            print(evaluate(fn))
            DestroyCondition(cn)
            print(evaluate(fn), evaluate(cn), GetHandleId(fn) == GetHandleId(cn), GetHandleId(fn) > 0)
            """);

        Assert.Equal(["0.000 true", "0.000 false\tfalse\ttrue\ttrue"], Printed(simulation));
    }

    // facts: a self-removing condition ran
    [Fact]
    public void A_removed_or_destroyed_condition_stops_running_and_a_destroyed_one_fails()
    {
        using var simulation = Load(Cond + """
            local function show(label, t) print(label, TriggerEvaluate(t), done()) end
            local t = CreateTrigger()
            TriggerRemoveCondition(t, TriggerAddCondition(t, cond("r1", false)))
            show("removed only", t)
            t = CreateTrigger()
            local first = TriggerAddCondition(t, cond("first", true))
            TriggerAddCondition(t, cond("second", true))
            TriggerRemoveCondition(t, first)
            show("removed first", t)
            t = CreateTrigger()
            TriggerAddCondition(t, cond("k1", false))
            TriggerClearConditions(t)
            show("cleared", t)
            local c = cond("destroyed", true)
            t = CreateTrigger()
            TriggerAddCondition(t, c)
            DestroyCondition(c)
            show("destroyed", t)
            local f = Filter(function() ran[#ran + 1] = "filter" return true end)
            t = CreateTrigger()
            TriggerAddCondition(t, f)
            DestroyFilter(f)
            show("destroyed filter", t)
            local e = And(cond("and1", true), cond("and2", true))
            t = CreateTrigger()
            TriggerAddCondition(t, e)
            DestroyBoolExpr(e)
            show("destroyed and", t)
            local inner = cond("inner", true)
            t = CreateTrigger()
            TriggerAddCondition(t, Not(inner))
            DestroyCondition(inner)
            show("destroyed operand", t)
            local mine
            t = CreateTrigger()
            mine = TriggerAddCondition(t, Condition(function() ran[#ran + 1] = "self" TriggerRemoveCondition(t, mine) return false end))
            TriggerAddCondition(t, cond("next", true))
            show("self-removing", t)
            show("after", t)
            """);

        Assert.Equal([
            "0.000 removed only\ttrue\t",
            "0.000 removed first\ttrue\tsecond",
            "0.000 cleared\ttrue\t",
            "0.000 destroyed\tfalse\t",
            "0.000 destroyed filter\tfalse\t",
            "0.000 destroyed and\ttrue\tand1,and2",
            "0.000 destroyed operand\ttrue\t",
            "0.000 self-removing\tfalse\tself,next",
            "0.000 after\ttrue\tnext",
        ], Printed(simulation));
    }

    // probes triggers, handles and handles-2: a Condition or Filter made after one was destroyed takes its place, the first
    // destroyed first, and what still holds the destroyed one runs the new one; other handles don't take it, and a new
    // boolexpr doesn't take a destroyed And's, which still runs its own operands
    [Fact]
    public void A_condition_made_after_one_was_destroyed_runs_in_its_place()
    {
        using var simulation = Load(Cond + """
            local function trig(...) local t = CreateTrigger() for _, c in ipairs({...}) do TriggerAddCondition(t, c) end return t end
            local dc = cond("d1", true)
            local t = trig(dc)
            DestroyCondition(dc)
            local before = TriggerEvaluate(t)
            local nc = cond("n1", false)
            print("new", before, TriggerEvaluate(t), done(), dc == nc)
            dc = cond("d2", true) t = trig(dc) DestroyCondition(dc)
            Filter(function() ran[#ran + 1] = "filter2" return false end)
            print("filter", TriggerEvaluate(t), done())
            local df = Filter(function() ran[#ran + 1] = "d3" return true end) t = trig(df) DestroyFilter(df)
            cond("n3", false)
            print("filter then condition", TriggerEvaluate(t), done())
            local a, b = cond("a4", true), cond("b4", true) t = trig(a, b) DestroyCondition(a) DestroyCondition(b)
            cond("x4", true) cond("y4", true)
            print("two", TriggerEvaluate(t), done())
            dc = cond("d5", true) t = trig(dc) DestroyCondition(dc)
            CreateTimer() CreateTrigger()
            cond("n5", true)
            print("after other handles", TriggerEvaluate(t), done())
            dc = cond("d6", true) t = trig(Not(dc)) DestroyCondition(dc)
            cond("n6", true)
            print("in a not", TriggerEvaluate(t), done())
            local de = And(cond("a7", true), cond("b7", true)) t = trig(de) DestroyBoolExpr(de)
            Or(cond("x7", true), cond("y7", true))
            print("and then or", TriggerEvaluate(t), done())
            t = CreateTrigger()
            dc = cond("destroyed", true)
            TriggerAddCondition(t, dc)
            DestroyCondition(dc)
            TriggerRemoveCondition(t, TriggerAddCondition(t, cond("removed", false)))
            print("removed", TriggerEvaluate(t), done())
            """);

        Assert.Equal([
            "0.000 new\tfalse\tfalse\tn1\tfalse",
            "0.000 filter\tfalse\tfilter2",
            "0.000 filter then condition\tfalse\tn3",
            "0.000 two\ttrue\tx4,y4",
            "0.000 after other handles\ttrue\tn5",
            "0.000 in a not\tfalse\tn6",
            "0.000 and then or\ttrue\ta7,b7",
            "0.000 removed\tfalse\tremoved",
        ], Printed(simulation));
    }

    // facts: errors in trigger callbacks are dropped (MirrorImaged logs them), and a self-removing action ran
    [Fact]
    public void Actions_run_in_order_an_error_ends_only_its_own_and_a_run_keeps_the_actions_it_started_with()
    {
        using var simulation = Load("""
            local ran = {}
            local t = CreateTrigger()
            local first
            first = TriggerAddAction(t, function() ran[#ran + 1] = "a1" TriggerRemoveAction(t, first) end)
            TriggerAddAction(t, function() ran[#ran + 1] = "a2 " .. GetTriggerExecCount(t) error("raised") end)
            TriggerAddAction(t, function() ran[#ran + 1] = "a3" end)
            TriggerExecute(t)
            TriggerExecute(t)
            print(table.concat(ran, ","))
            ran = {}
            local u = CreateTrigger()
            TriggerAddAction(u, function() ran[#ran + 1] = "first" TriggerAddAction(u, function() ran[#ran + 1] = "added" end) end)
            TriggerAddAction(u, function() ran[#ran + 1] = "second" TriggerClearActions(u) end)
            TriggerAddAction(u, function() ran[#ran + 1] = "third" end)
            TriggerExecute(u)
            TriggerExecute(u)
            print(table.concat(ran, ","))
            """);

        Assert.Equal([
            "0.000 error: war3map.lua:7: raised",
            "0.000 error: war3map.lua:7: raised",
            "0.000 a1,a2 1,a3,a2 2,a3",
            "0.000 first,second,third",
        ], Printed(simulation));
    }

    [Fact]
    public void A_disabled_trigger_still_runs_by_TriggerExecute_and_TriggerEvaluate()
    {
        using var simulation = Load("""
            local ran = {}
            local t = CreateTrigger()
            TriggerAddAction(t, function() ran[#ran + 1] = "action" end)
            TriggerAddCondition(t, Condition(function() ran[#ran + 1] = "condition" return true end))
            DisableTrigger(t)
            TriggerExecute(t)
            print(IsTriggerEnabled(t), TriggerEvaluate(t), GetTriggerExecCount(t), table.concat(ran, ","))
            EnableTrigger(t)
            print(IsTriggerEnabled(t))
            """);

        Assert.Equal(["0.000 false\ttrue\t1\taction,condition", "0.000 true"], Printed(simulation));
    }

    // probes triggers and triggers-2: outside an event, the event id is a handle with id 0 (EVENT_GAME_VICTORY's)
    [Fact]
    public void The_triggering_trigger_and_event_are_the_running_triggers_and_nesting_puts_them_back()
    {
        using var simulation = Load("""
            local e = GetTriggerEventId()
            print(GetTriggeringTrigger(), tostring(e):match("^%a+"), e == EVENT_GAME_VICTORY, rawequal(e, GetTriggerEventId()))
            local inner, outer = CreateTrigger(), CreateTrigger()
            TriggerAddAction(inner, function() print("inner", GetTriggeringTrigger() == inner, GetTriggerEventId() == EVENT_GAME_VICTORY) end)
            TriggerAddAction(outer, function()
              print("outer", GetTriggeringTrigger() == outer)
              TriggerExecute(inner)
              print("back", GetTriggeringTrigger() == outer)
            end)
            TriggerExecute(outer)
            local c = CreateTrigger()
            TriggerAddCondition(c, Condition(function() print("condition", GetTriggeringTrigger() == c, GetTriggerEvalCount(c)) return true end))
            TriggerEvaluate(c)
            print(GetTriggeringTrigger())
            """);

        Assert.Equal([
            "0.000 nil\teventid\ttrue\ttrue",
            "0.000 outer\ttrue",
            "0.000 inner\ttrue\ttrue",
            "0.000 back\ttrue",
            "0.000 condition\ttrue\t0",
            "0.000 nil",
        ], Printed(simulation));
    }

    // probe triggers-2: TriggerExecute keeps the event and names the trigger it runs; ExecuteFunc keeps both. After
    // ExecuteFunc's wait the game read only the trigger: that the event is kept there too is the model (decisions D75)
    [Fact]
    public void An_event_is_kept_through_TriggerExecute_and_ExecuteFunc_and_across_a_wait()
    {
        using var simulation = Load("""
            local p, inner = CreateTrigger(), CreateTrigger()
            TriggerRegisterTimerEvent(p, 0.1, false)
            TriggerAddAction(inner, function() print("inner", GetTriggeringTrigger() == inner, GetTriggerEventId() == EVENT_GAME_TIMER_EXPIRED) end)
            function Later()
              print("func", GetTriggeringTrigger() == p, GetTriggerEventId() == EVENT_GAME_TIMER_EXPIRED)
              TriggerSleepAction(0.2)
              print("func after", GetTriggeringTrigger() == p, GetTriggerEventId() == EVENT_GAME_TIMER_EXPIRED)
            end
            TriggerAddAction(p, function()
              local x = GetTriggerEventId()
              print("event", tostring(x):match("^%a+"), x == EVENT_GAME_TIMER_EXPIRED, rawequal(x, EVENT_GAME_TIMER_EXPIRED), x == ConvertGameEvent(4))
              TriggerExecute(inner)
              ExecuteFunc("Later")
            end)
            """);

        simulation.Advance(TimeSpan.FromSeconds(1));

        Assert.Equal([
            "0.100 event\teventid\ttrue\tfalse\ttrue",
            "0.100 inner\ttrue\ttrue",
            "0.100 func\ttrue\ttrue",
            "0.400 func after\ttrue\ttrue",
        ], Printed(simulation));
    }

    // probe triggers: the game runs its callbacks in threads (not the main one) the script can't yield
    [Fact]
    public void Callbacks_run_in_threads_the_script_cant_yield()
    {
        using var simulation = Load("""
            local function where() return tostring(coroutine.isyieldable()) .. " " .. tostring(select(2, coroutine.running())) end
            TimerStart(CreateTimer(), 0, false, function()
              print("timer", where())
              local t = CreateTrigger()
              TriggerAddCondition(t, Condition(function() print("condition", where()) return true end))
              TriggerAddAction(t, function()
                print("action", where())
                print(pcall(coroutine.yield))
                print(coroutine.wrap(function() coroutine.yield(5) end)())
              end)
              if TriggerEvaluate(t) then TriggerExecute(t) end
            end)
            """);

        simulation.Advance(TimeSpan.FromSeconds(1));

        Assert.Equal([
            "0.000 timer\tfalse false",
            "0.000 condition\tfalse false",
            "0.000 action\tfalse false",
            "0.000 false\tattempt to yield across a C-call boundary",
            "0.000 5",
        ], Printed(simulation));
    }

    // probe triggers: TriggerExecute's caller goes on when an action waits, and the next action runs after it; a wait of
    // 1 s from 0.6 ends at 1.7, and one of 0 or less takes 0.1 s (decisions D71)
    [Fact]
    public void A_wait_lets_the_caller_go_on_and_ends_on_the_first_tenth_of_a_second_after_its_timeout()
    {
        using var simulation = Load("""
            TimerStart(CreateTimer(), 0.6, false, function()
              local w = CreateTrigger()
              TriggerAddAction(w, function()
                print("start")
                TriggerSleepAction(1)
                print("1", GetTriggeringTrigger() == w)
                TriggerSleepAction(0)
                print("0")
                TriggerSleepAction(-1)
                print("-1")
                TriggerSleepAction(0.33)
                print("0.33")
              end)
              TriggerAddAction(w, function() print("second action") end)
              TriggerExecute(w)
              print("after execute")
            end)
            """);

        simulation.Advance(TimeSpan.FromSeconds(3));

        Assert.Equal([
            "0.600 start",
            "0.600 after execute",
            "1.700 1\ttrue",
            "1.800 0",
            "1.900 -1",
            "2.300 0.33",
            "2.300 second action",
        ], Printed(simulation));
    }

    // facts: xpcall and pcall around a callback keep working across TriggerSleepAction, and an error after the wait
    // reaches the handler
    [Fact]
    public void Pcall_and_xpcall_keep_working_across_a_wait()
    {
        using var simulation = Load("""
            local t = CreateTrigger()
            TriggerAddAction(t, function()
              print(pcall(function() TriggerSleepAction(0.5) error("after") end))
              print(xpcall(function() TriggerSleepAction(0.5) error("again") end, function(e) return "handled " .. e end))
              print(pcall(TriggerSleepAction, 0.1))
            end)
            TriggerExecute(t)
            """);

        simulation.Advance(TimeSpan.FromSeconds(2));

        Assert.Equal([
            "0.600 false\twar3map.lua:5: after",
            "1.200 false\thandled war3map.lua:6: again",
            "1.400 true",
        ], Printed(simulation));
    }

    // probe triggers: a wait in a timer's callback or in a condition never ends, pcall or not; in main it raises
    [Fact]
    public void A_wait_in_a_timer_callback_or_a_condition_never_ends()
    {
        using var simulation = Load("""
            print(pcall(TriggerSleepAction, 1))
            TimerStart(CreateTimer(), 0.1, false, function()
              print("timer")
              print(pcall(TriggerSleepAction, 0.5))
              print("never")
            end)
            local c = CreateTrigger()
            TriggerAddCondition(c, Condition(function() print("condition") print(pcall(TriggerSleepAction, 0.1)) return true end))
            TimerStart(CreateTimer(), 0.2, false, function() print("evaluated", TriggerEvaluate(c)) end)
            """);

        simulation.Advance(TimeSpan.FromSeconds(3));

        Assert.Equal([
            "0.000 false\tattempt to yield from outside a coroutine",
            "0.100 timer",
            "0.200 condition",
            "0.200 evaluated\tfalse",
        ], Printed(simulation));
    }

    // probes triggers and triggers-2: TriggerExecuteWait doesn't wait, with or without TriggerWaitOnSleeps
    [Fact]
    public void TriggerExecuteWait_doesnt_wait_for_the_triggers_waits()
    {
        using var simulation = Load("""
            local slow = CreateTrigger()
            TriggerWaitOnSleeps(slow, true)
            TriggerAddAction(slow, function() TriggerSleepAction(0.5) print("slow done") end)
            local waiter = CreateTrigger()
            TriggerAddAction(waiter, function() print("start") TriggerExecuteWait(slow) print("after") end)
            TriggerExecute(waiter)
            """);

        simulation.Advance(TimeSpan.FromSeconds(1));

        Assert.Equal(["0.000 start", "0.000 after", "0.600 slow done"], Printed(simulation));
    }

    // a nil name is refused before the native runs, as every string parameter (D80)
    [Fact]
    public void ExecuteFunc_runs_a_global_function_in_a_new_thread_that_can_wait()
    {
        using var simulation = Load("""
            function Later()
              print("func", GetTriggeringTrigger() == t, coroutine.isyieldable())
              TriggerSleepAction(0.25)
              print("func after", GetTriggeringTrigger() == t)
            end
            t = CreateTrigger()
            TriggerAddAction(t, function() ExecuteFunc("Later") print("caller") end)
            TriggerExecute(t)
            ExecuteFunc("Missing")
            ExecuteFunc(nil)
            """);

        simulation.Advance(TimeSpan.FromSeconds(1));

        Assert.Equal([
            "0.000 func\ttrue\tfalse",
            "0.000 caller",
            "0.000 error: attempt to call a nil value",
            "0.000 error: war3map.lua:12: bad argument #1 to 'ExecuteFunc' (string expected, got nil)",
            "0.300 func after\ttrue",
        ], Printed(simulation));
    }

    // probe triggers-2: a timeout below 0.01 s is 0.01 s, events due together fire in the order registered, and a
    // periodic 0 s event fires 10 times in 0.1 s, keeping its place before the 0.1 s timer started after it (D76)
    [Fact]
    public void A_timer_event_fires_after_its_timeout_and_at_least_0_01_s()
    {
        using var simulation = Load("""
            for _, timeout in ipairs({0.02, 0.01, 0.005, 0}) do
              local t = CreateTrigger()
              TriggerRegisterTimerEvent(t, timeout, false)
              TriggerAddAction(t, function() print(timeout) end)
            end
            local a, b = CreateTrigger(), CreateTrigger()
            TriggerRegisterTimerEvent(b, 0.05, false)
            TriggerRegisterTimerEvent(a, 0.05, false)
            TriggerRegisterTimerEvent(b, 0.05, false)
            TriggerAddAction(a, function() print("a") end)
            TriggerAddAction(b, function() print("b") end)
            local z, count = CreateTrigger(), 0
            TriggerRegisterTimerEvent(z, 0, true)
            TriggerAddAction(z, function() count = count + 1 end)
            TimerStart(CreateTimer(), 0.1, false, function() DestroyTrigger(z) print("periodic zero", count) end)
            """);

        simulation.Advance(TimeSpan.FromSeconds(1));

        Assert.Equal([
            "0.010 0.01",
            "0.010 0.005",
            "0.010 0",
            "0.020 0.02",
            "0.050 b",
            "0.050 a",
            "0.050 b",
            "0.100 periodic zero\t10",
        ], Printed(simulation));
    }

    // probe triggers: a disabled trigger's events are counted, run no condition and no action, and GetExpiredTimer is
    // nil; the event due with the timer that enables it again fires first, still disabled (D76)
    [Fact]
    public void A_periodic_timer_event_is_counted_while_disabled_and_runs_again_when_enabled()
    {
        using var simulation = Load("""
            local p, count = CreateTrigger(), 0
            TriggerRegisterTimerEvent(p, 0.5, true)
            TriggerAddCondition(p, Condition(function() print("condition") return true end))
            TriggerAddAction(p, function()
              count = count + 1
              print(GetTriggerEventId() == EVENT_GAME_TIMER_EXPIRED, GetExpiredTimer(), GetTriggerEvalCount(p), GetTriggerExecCount(p))
              if count == 2 then
                DisableTrigger(p)
                TimerStart(CreateTimer(), 1.0, false, function() EnableTrigger(p) end)
              end
            end)
            """);

        simulation.Advance(TimeSpan.FromSeconds(2.5));

        Assert.Equal([
            "0.500 condition",
            "0.500 true\tnil\t1\t1",
            "1.000 condition",
            "1.000 true\tnil\t2\t2",
            "2.500 condition",
            "2.500 true\tnil\t5\t3",
        ], Printed(simulation));
    }

    // probes triggers and triggers-2: an event's condition runs with its trigger and event; a failing one stops the action
    [Fact]
    public void An_events_condition_runs_with_its_trigger_and_event_and_counts_as_an_evaluation()
    {
        using var simulation = Load("""
            local gated = CreateTrigger()
            TriggerRegisterTimerEvent(gated, 0.25, false)
            TriggerAddCondition(gated, Condition(function()
              print("condition", GetTriggeringTrigger() == gated, GetTriggerEventId() == EVENT_GAME_TIMER_EXPIRED, coroutine.isyieldable())
              return false
            end))
            TriggerAddAction(gated, function() print("never") end)
            TimerStart(CreateTimer(), 0.5, false, function() print(GetTriggerEvalCount(gated), GetTriggerExecCount(gated)) end)
            """);

        simulation.Advance(TimeSpan.FromSeconds(1));

        Assert.Equal(["0.250 condition\ttrue\ttrue\tfalse", "0.500 1\t0"], Printed(simulation));
    }

    // facts: a timer-expiry trigger fires before the timer's own callback; probe triggers-2: in the order registered
    [Fact]
    public void A_timers_expiry_events_fire_before_its_callback_with_the_timer_expired()
    {
        using var simulation = Load("""
            local tm = CreateTimer()
            local x1, x2 = CreateTrigger(), CreateTrigger()
            TriggerRegisterTimerExpireEvent(x2, tm)
            TriggerRegisterTimerExpireEvent(x1, tm)
            TriggerRegisterTimerExpireEvent(x2, tm)
            TriggerAddAction(x1, function() print("x1", GetExpiredTimer() == tm, GetTriggerEventId() == EVENT_GAME_TIMER_EXPIRED) end)
            TriggerAddAction(x2, function() print("x2") end)
            TimerStart(tm, 0.75, false, function() print("callback", GetExpiredTimer() == tm) end)
            """);

        simulation.Advance(TimeSpan.FromSeconds(1));

        Assert.Equal(["0.750 x2", "0.750 x1\ttrue\ttrue", "0.750 x2", "0.750 callback\ttrue"], Printed(simulation));
    }

    // probe triggers: the game's variable event doesn't fire for a global set from Lua
    [Fact]
    public void A_variable_event_never_fires()
    {
        using var simulation = Load("""
            MirrorVar = 0
            local v = CreateTrigger()
            print(tostring(TriggerRegisterVariableEvent(v, "MirrorVar", GREATER_THAN, 5)):match("^%a+"))
            TriggerAddAction(v, function() print("fired") end)
            MirrorVar = 10
            TimerStart(CreateTimer(), 0.5, false, function() MirrorVar = 20 end)
            """);

        simulation.Advance(TimeSpan.FromSeconds(1));

        Assert.Equal(["0.000 event"], Printed(simulation));
    }

    // probe triggers-3: each comparison against the same sets; the event id is the comparison less 255
    [Fact]
    public void An_integer_game_state_event_fires_on_a_change_to_a_value_that_passes()
    {
        using var simulation = Load("""
            local names = {"LESS_THAN", "LESS_THAN_OR_EQUAL", "EQUAL", "GREATER_THAN_OR_EQUAL", "GREATER_THAN", "NOT_EQUAL"}
            local function id() for n = -255, -250 do if GetTriggerEventId() == ConvertGameEvent(n) then return n end end end
            local function run(state, limit, first, values)
              for _, name in ipairs(names) do
                local log, t = {}, CreateTrigger()
                TriggerRegisterGameStateEvent(t, state, _G[name], limit)
                TriggerAddAction(t, function() log[#log + 1] = GetIntegerGameState(state) .. ":" .. id() end)
                if first then SetIntegerGameState(state, first) end
                for _, value in ipairs(values) do SetIntegerGameState(state, value) end
                DisableTrigger(t)
                print(name, table.concat(log, ","))
              end
            end
            print(GetIntegerGameState(GAME_STATE_DIVINE_INTERVENTION), GetIntegerGameState(GAME_STATE_DISCONNECTED))
            run(GAME_STATE_DIVINE_INTERVENTION, 2, 5, {0, 1, 2, 2, 3, 2, 1, 0, 4})
            run(GAME_STATE_DISCONNECTED, 0, nil, {0, 2, 2, 0, -1, 0})
            """);

        Assert.Equal([
            "0.000 0\t0",
            "0.000 LESS_THAN\t0:-255,1:-255,1:-255,0:-255",
            "0.000 LESS_THAN_OR_EQUAL\t0:-254,1:-254,2:-254,2:-254,1:-254,0:-254",
            "0.000 EQUAL\t2:-253,2:-253",
            "0.000 GREATER_THAN_OR_EQUAL\t5:-252,2:-252,3:-252,2:-252,4:-252",
            "0.000 GREATER_THAN\t5:-251,3:-251,4:-251",
            "0.000 NOT_EQUAL\t5:-250,0:-250,1:-250,2:-250,3:-250,2:-250,1:-250,0:-250,4:-250",
            "0.000 LESS_THAN\t-1:-255",
            "0.000 LESS_THAN_OR_EQUAL\t0:-254,-1:-254,0:-254",
            "0.000 EQUAL\t0:-253,0:-253",
            "0.000 GREATER_THAN_OR_EQUAL\t2:-252,0:-252,0:-252",
            "0.000 GREATER_THAN\t2:-251",
            "0.000 NOT_EQUAL\t2:-250,0:-250,-1:-250,0:-250",
        ], Printed(simulation));
    }

    // probe triggers-2
    [Fact]
    public void A_game_state_event_reads_its_state_inside_the_set()
    {
        using var simulation = Load("""
            local g = CreateTrigger()
            TriggerRegisterGameStateEvent(g, GAME_STATE_DIVINE_INTERVENTION, GREATER_THAN, 1)
            TriggerAddAction(g, function()
              local state = GetEventGameState()
              print(GetIntegerGameState(GAME_STATE_DIVINE_INTERVENTION), GetTriggerEventId() == EVENT_GAME_STATE_LIMIT, tostring(state):match("^%a+"), state == GAME_STATE_DIVINE_INTERVENTION)
            end)
            for _, value in ipairs({2, 2, 0, 3, 1, 5}) do SetIntegerGameState(GAME_STATE_DIVINE_INTERVENTION, value) end
            print(GetEventGameState())
            """);

        Assert.Equal([
            "0.000 2\tfalse\tgamestate\ttrue",
            "0.000 3\tfalse\tgamestate\ttrue",
            "0.000 5\tfalse\tgamestate\ttrue",
            "0.000 nil",
        ], Printed(simulation));
    }

    // facts, "Initialisation": the game hasn't started inside main. World Editor's main runs the map-initialisation
    // triggers (ConditionalTriggerExecute: evaluate, then execute), whose waits end after it
    [Fact]
    public void Main_runs_the_initialisation_triggers_and_the_game_starts_after_it()
    {
        using var simulation = Load("""
            local init, started = CreateTrigger(), CreateTrigger()
            TriggerAddAction(init, function() print("init") TriggerSleepAction(0) print("init after its wait") end)
            TriggerRegisterTimerEvent(started, 0, false)
            TriggerAddAction(started, function() print("game started") end)
            if TriggerEvaluate(init) then TriggerExecute(init) end
            print("end of main")
            """);

        simulation.Advance(TimeSpan.FromSeconds(1));

        Assert.Equal(["0.000 init", "0.000 end of main", "0.010 game started", "0.100 init after its wait"], Printed(simulation));
    }

    [Fact]
    public void A_destroyed_triggers_events_stop_and_its_running_thread_goes_on()
    {
        using var simulation = Load("""
            local t, n = CreateTrigger(), 0
            TriggerRegisterTimerEvent(t, 0.1, true)
            TriggerAddAction(t, function()
              n = n + 1
              if n == 1 then TriggerSleepAction(0.5) print("slept", n) end
            end)
            TimerStart(CreateTimer(), 0.25, false, function() DestroyTrigger(t) end)
            """);

        simulation.Advance(TimeSpan.FromSeconds(1));

        Assert.Equal(["0.700 slept\t2"], Printed(simulation));
    }

    // a wait of 1e15 s ends 31 million years on, just inside the clock's range, and one of 1e30 s or math.huge past it:
    // none ends in the run, and nothing hangs working out when
    [Theory]
    [InlineData("1e15")]
    [InlineData("1e30")]
    [InlineData("math.huge")]
    public void A_wait_longer_than_the_run_never_ends_and_nothing_hangs(string timeout)
    {
        using var simulation = Load($$"""
            local t = CreateTrigger()
            TriggerAddAction(t, function() TriggerSleepAction({{timeout}}) print("never") end)
            TriggerExecute(t)
            print("main goes on")
            """);

        simulation.Advance(TimeSpan.FromSeconds(1));

        Assert.Equal(["0.000 main goes on"], Printed(simulation));
    }

    // probe triggers-4: running while a run's action runs or waits, not in TriggerEvaluate's conditions; an interrupt
    // lets the running action end, runs no other action and stops a run that waits at its wait's end
    [Fact]
    public void BlzTriggerInterrupt_stops_a_triggers_runs_and_BlzTriggerIsRunning_tells_one_hasnt_ended()
    {
        using var simulation = Load("""
            local log = {}
            local function running(t) return tostring(BlzTriggerIsRunning(t)) end
            local t = CreateTrigger()
            print("new", running(t))
            TriggerAddAction(t, function()
              log[#log + 1] = "t in action " .. running(t)
              TriggerSleepAction(0.5)
              print("t after wait", running(t))
            end)
            TriggerExecute(t)
            print("t waiting", running(t), table.concat(log, ","))
            log = {}
            local u = CreateTrigger()
            TriggerAddAction(u, function() log[#log + 1] = "u start" TriggerSleepAction(0.3) log[#log + 1] = "u after wait" end)
            TriggerAddAction(u, function() log[#log + 1] = "u second" end)
            TriggerExecute(u)
            BlzTriggerInterrupt(u)
            print("u interrupted", running(u))
            local v = CreateTrigger()
            TriggerAddAction(v, function()
              log[#log + 1] = "v start"
              BlzTriggerInterrupt(v)
              log[#log + 1] = "v after interrupt " .. running(v)
            end)
            TriggerAddAction(v, function() log[#log + 1] = "v second" end)
            TriggerExecute(v)
            print("v", running(v))
            local c = CreateTrigger()
            TriggerAddCondition(c, Condition(function() log[#log + 1] = "c condition " .. running(c) return true end))
            TriggerEvaluate(c)
            print("c", running(c))
            TimerStart(CreateTimer(), 1, false, function() print("after", running(t), running(u), table.concat(log, ",")) end)
            """);

        simulation.Advance(TimeSpan.FromSeconds(1));

        Assert.Equal([
            "0.000 new\tfalse",
            "0.000 t waiting\ttrue\tt in action true",
            "0.000 u interrupted\ttrue",
            "0.000 v\tfalse",
            "0.000 c\tfalse",
            "0.600 t after wait\ttrue",
            "1.000 after\tfalse\tfalse\tu start,v start,v after interrupt true,c condition false",
        ], Printed(simulation));
    }

    // decisions D77, not measured: an action that interrupts its own trigger and then waits stops at its wait's end
    [Fact]
    public void An_action_that_interrupts_its_trigger_and_then_waits_stops_at_its_waits_end()
    {
        using var simulation = Load("""
            local v = CreateTrigger()
            TriggerAddAction(v, function() print("start") BlzTriggerInterrupt(v) TriggerSleepAction(0.2) print("never") end)
            TriggerAddAction(v, function() print("never either") end)
            TriggerExecute(v)
            TimerStart(CreateTimer(), 0.25, false, function() print("running", BlzTriggerIsRunning(v)) end)
            TimerStart(CreateTimer(), 0.5, false, function() print("running", BlzTriggerIsRunning(v)) end)
            """);

        simulation.Advance(TimeSpan.FromSeconds(1));

        Assert.Equal(["0.000 start", "0.250 running\ttrue", "0.500 running\tfalse"], Printed(simulation));
    }

    [Fact]
    public void A_destroyed_triggers_events_are_let_go_of()
    {
        using var simulation = Load("");
        var natives = simulation.Natives;
        var t = natives.CreateTrigger();
        @event?[] events = [
            natives.TriggerRegisterTimerExpireEvent(t, natives.CreateTimer()),
            natives.TriggerRegisterGameStateEvent(t, natives.ConvertIGameState(0), natives.ConvertLimitOp(2), 1),
            natives.TriggerRegisterTimerEvent(t, 1, true),
            natives.TriggerRegisterVariableEvent(t, "x", null, 0),
        ];

        natives.DestroyTrigger(t);

        Assert.All(events, e => Assert.Null(simulation.ToCSharp(new LuaHandle("event", e!.Key, e.Id, true), typeof(@event), 1, "F")));
    }

    // a function a trigger no longer has is let go of, so Lua collects it, once no running thread can still run it
    [Theory]
    [InlineData("local t = CreateTrigger() TriggerAddAction(t, f) DestroyTrigger(t)")]
    [InlineData("local t = CreateTrigger() TriggerRemoveAction(t, TriggerAddAction(t, f))")]
    [InlineData("TriggerAddAction(nil, f)")]
    [InlineData("DestroyCondition(Condition(f))")]
    [InlineData("DestroyFilter(Filter(f))")]
    [InlineData("local t = CreateTrigger() TriggerAddAction(t, function() TriggerClearActions(t) end) TriggerAddAction(t, f) TriggerExecute(t)")]
    public void A_function_a_trigger_lets_go_of_is_collected(string call)
    {
        using var simulation = Load($$"""
            local weak = setmetatable({}, {__mode = 'k'})
            local f = function() return true end
            weak[f] = true
            {{call}}
            f = nil
            for i = 1, 300000 do local junk = {i} end
            print(next(weak) == nil)
            """);

        Assert.Equal(["0.000 true"], Printed(simulation));
    }
}
