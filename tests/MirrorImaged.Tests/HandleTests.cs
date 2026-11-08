namespace MirrorImaged.Tests;

/// <summary>
/// Handles' ids, identity and equality, and the natives' argument checks, against the probes handles, handles-2,
/// handles-3 and arguments (docs/facts.md, "Handles and errors"). Each map's main is the script given, from line 3,
/// without blizzard.j.
/// </summary>
public sealed class HandleTests : IDisposable
{
    readonly TestMaps _maps = new();

    public void Dispose() => _maps.Dispose();

    Simulation Load(string main) =>
        Simulation.Load(_maps.Build($"function config() end\nfunction main()\n{main}\nend\n"), "");

    static IEnumerable<string> Printed(Simulation simulation) => simulation.Log.Select(line => line.Replace("0.000 print: ", ""));

    // probe handles: every handle a native makes takes the next id, a trigger's actions, conditions and events too
    [Fact]
    public void Every_handle_a_native_makes_takes_the_next_id()
    {
        using var simulation = Load("""
            local yes = function() return true end
            local trig, c1, c2 = CreateTrigger(), Condition(yes), Condition(yes)
            local function taken(make)
              local before = GetHandleId(CreateTimer())
              local h = make()
              return (GetHandleId(CreateTimer()) - before - 1) .. " " .. (GetHandleId(h) - before)
            end
            print(taken(CreateTimer), taken(CreateTrigger), taken(CreateGroup), taken(CreateForce), taken(InitHashtable))
            print(taken(function() return Condition(yes) end), taken(function() return Filter(yes) end), taken(function() return And(c1, c2) end), taken(function() return Not(c1) end))
            print(taken(function() return TriggerAddAction(trig, yes) end), taken(function() return TriggerAddCondition(trig, c1) end), taken(function() return TriggerRegisterTimerEvent(trig, 100, false) end))
            """);

        Assert.Equal(["1 1\t1 1\t1 1\t1 1\t1 1", "1 1\t1 1\t1 1\t1 1", "1 1\t1 1\t1 1"], Printed(simulation));
    }

    // probe handles: a Convert handle's id is its value, and it takes none; nil, nothing and the event outside one are 0
    [Fact]
    public void A_converted_handles_id_is_its_value_and_nil_is_0()
    {
        using var simulation = Load("""
            local before = GetHandleId(CreateTimer())
            local r5, pc100, pcm1, ge4 = ConvertRace(5), ConvertPlayerColor(100), ConvertPlayerColor(-1), ConvertGameEvent(4)
            local took = GetHandleId(CreateTimer()) - before - 1
            print(GetHandleId(r5), GetHandleId(pc100), GetHandleId(pcm1), GetHandleId(ge4), GetHandleId(PLAYER_COLOR_RED), GetHandleId(RACE_HUMAN), took)
            print(GetHandleId(GetTriggerEventId()), GetHandleId(nil), GetHandleId())
            """);

        Assert.Equal(["5\t100\t-1\t4\t0\t1\t0", "0\t0\t0"], Printed(simulation));
    }

    // probes handles and handles-2: a destroyed handle keeps its id, which no new handle takes while Lua holds it, nor at
    // once after Lua collected it; the game gives it again 0.1 to 0.2 s later, which MirrorImaged doesn't (decisions D111)
    [Fact]
    public void A_destroyed_handle_keeps_its_id_and_ids_are_never_given_again()
    {
        using var simulation = Load("""
            local t = CreateTimer()
            local n = GetHandleId(t)
            DestroyTimer(t)
            local u = CreateTimer()
            print(GetHandleId(u) - n, GetHandleId(t) - n, t == u, rawequal(t, u), tostring(t):match("^%a+"))
            local weak, ids = setmetatable({}, {__mode = "k"}), {}
            for i = 1, 3 do
              local d = CreateTimer()
              ids[i] = GetHandleId(d)
              weak[d] = true
              DestroyTimer(d)
            end
            for i = 1, 300000 do local junk = {i} end
            print(next(weak) == nil, GetHandleId(CreateTimer()) - ids[1])
            TimerStart(CreateTimer(), 1, false, function() print(GetHandleId(CreateTimer()) - ids[1]) end)
            """);
        simulation.Advance(TimeSpan.FromSeconds(1));

        Assert.Equal(["1\t0\tfalse\tfalse\ttimer", "true\t3", "1.000 print: 5"], Printed(simulation));
    }

    // probes triggers and handles: Condition(nil) and Filter(nil) are one handle, made by the first call of either, seen
    // as each declares
    [Fact]
    public void Condition_nil_and_Filter_nil_are_one_handle()
    {
        using var simulation = Load("""
            local before = GetHandleId(CreateTimer())
            local f, c = Filter(nil), Condition(nil)
            print(GetHandleId(f) - before, GetHandleId(c) - before, GetHandleId(CreateTimer()) - before)
            print(c == f, rawequal(c, f), rawequal(c, Condition(nil)), tostring(c):match("^%a+"), tostring(f):match("^%a+"))
            """);

        Assert.Equal(["1\t1\t2", "true\tfalse\ttrue\tconditionfunc\tfilterfunc"], Printed(simulation));
    }

    // probes handles and handles-2: == compares ids whatever the types; an agent's refuses a second operand that isn't an
    // agent, at the comparing line; a handle's metatable reads false
    [Fact]
    public void Handles_are_equal_by_id_and_an_agent_refuses_a_handle_that_isnt_one()
    {
        using var simulation = Load("""
            local function try(f) local ok, v = pcall(f) return (ok and "ok " or "error ") .. tostring(v) end
            local t, u, g = CreateTimer(), CreateTimer(), CreateGroup()
            local n = GetHandleId(t)
            print(try(function() return t == u end), try(function() return t == g end), try(function() return t == Player(0) end))
            print(try(function() return t == ConvertPlayerColor(n) end), try(function() return Player(0) == RACE_HUMAN end))
            print(try(function() return ConvertPlayerColor(n) == t end), try(function() return RACE_HUMAN == Player(0) end), try(function() return ConvertRace(GetHandleId(Player(0))) == Player(0) end))
            print(try(function() return ConvertRace(1) == ConvertPlayerColor(1) end), try(function() return GetTriggerEventId() == ConvertPlayerColor(0) end), try(function() return ConvertRace(1) == ConvertRace(2) end))
            print(try(function() return t == {} end), try(function() return t == nil end), try(function() return t ~= u end))
            print(getmetatable(t), getmetatable(RACE_HUMAN), getmetatable(Condition(nil)))
            """);

        Assert.Equal([
            "ok false\tok false\tok false",
            "error war3map.lua:7: bad argument #2 to '__eq' (agent expected)\terror war3map.lua:7: bad argument #2 to '__eq' (agent expected)",
            "ok true\tok false\tok true",
            "ok true\tok true\tok false",
            "ok false\tok false\tok true",
            "false\tfalse\tfalse",
        ], Printed(simulation));
    }

    // probes handles and handles-2: the same handle as the same type is the same userdata, so it is one table key; an equal
    // handle of another type is another key; tostring names the declared type and the userdata's place in 16 digits
    [Fact]
    public void A_handle_is_one_table_key_as_one_type_and_tostring_names_the_type()
    {
        using var simulation = Load("""
            local t = CreateTimer()
            local n = GetHandleId(t)
            print(rawequal(Player(0), Player(0)), rawequal(Player(0), GetLocalPlayer()), rawequal(ConvertRace(1), ConvertRace(1)), rawequal(RACE_HUMAN, ConvertRace(1)), rawequal(Condition(nil), Condition(nil)))
            local keys = {}
            keys[Player(0)], keys[ConvertRace(1)], keys[t] = "p0", "r1", "t"
            print(keys[GetLocalPlayer()], keys[ConvertRace(1)], keys[RACE_HUMAN], keys[ConvertPlayerColor(1)], keys[ConvertPlayerColor(n)])
            local function shape(h) return (tostring(h):gsub(": %x+$", ": hex")) .. " " .. #tostring(h) end
            print(shape(t), shape(Player(0)), shape(ConvertRace(1)), shape(GetTriggerEventId()), shape(Condition(function() end)), shape(Filter(nil)), shape(And(nil, nil)), shape(TriggerAddAction(CreateTrigger(), print)))
            TimerStart(t, 0.01, false, function() print(rawequal(GetExpiredTimer(), t), GetExpiredTimer() == t) end)
            """);
        simulation.Advance(TimeSpan.FromSeconds(0.1));

        Assert.Equal([
            "true\ttrue\ttrue\ttrue\ttrue",
            "p0\tr1\tr1\tnil\tnil",
            "timer: hex 23\tplayer: hex 24\trace: hex 22\teventid: hex 25\tconditionfunc: hex 31\tfilterfunc: hex 28\tboolexpr: hex 26\ttriggeraction: hex 31",
            "0.010 print: true\ttrue",
        ], Printed(simulation));
    }

    // probe arguments: a handle or function parameter takes nil or nothing and refuses anything else with no "got"; a number
    // parameter takes a numeral string as Lua's own functions do; nothing is "no value"; a boolean is Lua's truth
    [Fact]
    public void A_native_checks_its_arguments_as_the_game_does()
    {
        using var simulation = Load("""
            local function show(v) if type(v) == "userdata" or type(v) == "table" then return (tostring(v):match("^[%a_]+")) end return tostring(v) end
            local function try(label, f, ...) local ok, v = pcall(f, ...) print(label .. " " .. (ok and ("ok " .. show(v)) or ("error " .. show(v)))) end
            local t, trig = CreateTimer(), CreateTrigger()
            try("trigger", TimerGetTimeout, trig)
            try("race", TimerGetTimeout, RACE_HUMAN)
            try("nil", TimerGetTimeout, nil)
            try("none", TimerGetTimeout)
            try("number", TimerGetTimeout, 5)
            try("string", TimerGetTimeout, "x")
            try("boolean", TimerGetTimeout, true)
            try("table", TimerGetTimeout, {})
            try("function", TimerGetTimeout, print)
            try("thread", TimerGetTimeout, coroutine.create(print))
            try("named table", TimerGetTimeout, setmetatable({}, {__name = "timer"}))
            try("GetHandleId number", GetHandleId, 5)
            try("eventid for limitop", TriggerRegisterVariableEvent, trig, "x", GetTriggerEventId(), 0)
            try("I2S numeral", I2S, "5")
            try("I2S hex numeral", I2S, "0x10")
            try("I2S float numeral", I2S, "2.0")
            try("I2S fraction numeral", I2S, "1.5")
            try("I2S word", I2S, "x")
            try("I2S nil", I2S, nil)
            try("I2S none", I2S)
            try("I2S timer", I2S, t)
            try("R2S numeral", R2S, "1.5")
            try("R2S none", R2S)
            try("R2S race", R2S, RACE_HUMAN)
            try("StringLength none", StringLength)
            try("StringLength timer", StringLength, t)
            try("TriggerAddAction number", TriggerAddAction, trig, 5)
            try("TriggerAddAction none", TriggerAddAction, trig)
            try("Condition number", Condition, 5)
            try("Condition none", Condition)
            try("TimerStart number", TimerStart, t, 100, false, 5)
            local function flag(...) local tt = CreateTrigger() TriggerWaitOnSleeps(tt, ...) return IsTriggerWaitOnSleeps(tt) end
            print(flag(true), flag(nil), flag(), flag(0), flag("false"), flag({}))
            try("in a function", function() local x = TimerGetTimeout(trig) return x end)
            try("in a tail call", function() return I2S("x") end)
            """);

        Assert.Equal([
            "trigger error bad argument #1 to 'TimerGetTimeout' (timer expected)",
            "race error bad argument #1 to 'TimerGetTimeout' (timer expected)",
            "nil ok 0.0",
            "none ok 0.0",
            "number error bad argument #1 to 'TimerGetTimeout' (timer expected)",
            "string error bad argument #1 to 'TimerGetTimeout' (timer expected)",
            "boolean error bad argument #1 to 'TimerGetTimeout' (timer expected)",
            "table error bad argument #1 to 'TimerGetTimeout' (timer expected)",
            "function error bad argument #1 to 'TimerGetTimeout' (timer expected)",
            "thread error bad argument #1 to 'TimerGetTimeout' (timer expected)",
            "named table error bad argument #1 to 'TimerGetTimeout' (timer expected)",
            "GetHandleId number error bad argument #1 to 'GetHandleId' (handle expected)",
            "eventid for limitop error bad argument #3 to 'TriggerRegisterVariableEvent' (limitop expected)",
            "I2S numeral ok 5",
            "I2S hex numeral ok 16",
            "I2S float numeral ok 2",
            "I2S fraction numeral error bad argument #1 to 'I2S' (number has no integer representation)",
            "I2S word error bad argument #1 to 'I2S' (number expected, got string)",
            "I2S nil error bad argument #1 to 'I2S' (number expected, got nil)",
            "I2S none error bad argument #1 to 'I2S' (number expected, got no value)",
            "I2S timer error bad argument #1 to 'I2S' (number expected, got timer)",
            "R2S numeral ok 1.500",
            "R2S none error bad argument #1 to 'R2S' (number expected, got no value)",
            "R2S race error bad argument #1 to 'R2S' (number expected, got race)",
            "StringLength none error bad argument #1 to 'StringLength' (string expected, got no value)",
            "StringLength timer error bad argument #1 to 'StringLength' (string expected, got timer)",
            "TriggerAddAction number error bad argument #2 to 'TriggerAddAction' (function expected)",
            "TriggerAddAction none ok triggeraction",
            "Condition number error bad argument #1 to 'Condition' (function expected)",
            "Condition none ok conditionfunc",
            "TimerStart number error bad argument #4 to 'TimerStart' (function expected)",
            "true\tfalse\tfalse\ttrue\ttrue\ttrue",
            "in a function error war3map.lua:39: bad argument #1 to 'TimerGetTimeout' (timer expected)",
            "in a tail call error war3map.lua:40: bad argument #1 to 'I2S' (number expected, got string)",
        ], Printed(simulation));
    }

    // facts, "Errors and callbacks", and probe arguments: a Lua error's message starts with the chunk war3map.lua and the
    // line; error's level 2 names the caller's line, level 0 none
    [Fact]
    public void A_lua_errors_message_starts_with_war3map_lua_and_the_line()
    {
        using var simulation = Load("""
            print(pcall(function() error("raised") end))
            print(pcall(function() local function inner() error("raised", 2) end inner() end))
            print(pcall(function() error("raised", 0) end))
            print(pcall(function() local x = nil return x.y end))
            print(pcall(error, "", 2))
            """);

        Assert.Equal([
            "false\twar3map.lua:3: raised",
            "false\twar3map.lua:4: raised",
            "false\traised",
            "false\twar3map.lua:6: attempt to index a nil value (local 'x')",
            "false\twar3map.lua:7: ",
        ], Printed(simulation));
    }
}
