-- Handles: the ids the game gives (agents, what a trigger makes, Convert handles, players), whether a destroyed handle's
-- id comes back and when, == and rawequal between handles, table keys, tostring, and Condition(nil) in a boolexpr.
-- Each part runs in its own timer callback. An id is given less the id of a timer made just before it. Every handle a
-- part destroys is held in `held`, so Lua lets go of none before the part that collects.
local function id(h) return h == nil and "nil" or tostring(GetHandleId(h)) end
local function kind(h) return h == nil and "nil" or (tostring(h):match("^[%a_]+")) end
local function part(at, label, f)
  TimerStart(CreateTimer(), at, false, function()
    out(label)
    local ok, err = pcall(f)
    if not ok then out(label .. " error " .. tostring(err)) end
  end)
end
local held = {}
local function kill(destroy, h) held[#held + 1] = h destroy(h) end
local yes = function() return true end
local log = {}
local function cond(name, value) return Condition(function() log[#log + 1] = name return value end) end
local function flush(label) out(label .. " " .. table.concat(log, ",")) log = {} end
local function evaluate(...)
  local t = CreateTrigger()
  for _, c in ipairs({...}) do TriggerAddCondition(t, c) end
  return tostring(TriggerEvaluate(t))
end

TimerStart(CreateTimer(), 5.0, false, guard(done))

out("first " .. id(CreateTimer()) .. " players " .. id(Player(0)) .. " " .. id(Player(1)) .. " " .. id(Player(27)) .. " " .. id(GetLocalPlayer()))
out("blizzard " .. id(bj_volumeGroupsTimer) .. " " .. id(bj_queuedExecTimeoutTimer) .. " " .. id(bj_suspendDecayFleshGroup) .. " " .. id(bj_suspendDecayBoneGroup)
  .. " " .. id(bj_delayedSuspendDecayTimer) .. " " .. id(bj_lastCreatedGroup) .. " " .. id(bj_lastStartedTimer))

part(0.1, "ids", function()
  -- how many ids a call takes, and its handle's id less the timer's made just before it
  local function taken(label, make)
    local before = GetHandleId(CreateTimer())
    local h = make()
    local after = GetHandleId(CreateTimer())
    out(label .. " " .. (after - before - 1) .. " " .. (h == nil and "nil" or (GetHandleId(h) - before)))
  end
  local trig, c1, c2 = CreateTrigger(), Condition(yes), Condition(yes)
  taken("timer", CreateTimer)
  taken("trigger", CreateTrigger)
  taken("group", CreateGroup)
  taken("force", CreateForce)
  taken("hashtable", InitHashtable)
  taken("condition", function() return Condition(yes) end)
  taken("filter", function() return Filter(yes) end)
  taken("and", function() return And(c1, c2) end)
  taken("not", function() return Not(c1) end)
  taken("action", function() return TriggerAddAction(trig, yes) end)
  taken("triggercondition", function() return TriggerAddCondition(trig, c1) end)
  taken("event", function() return TriggerRegisterTimerEvent(trig, 100, false) end)
  local before = GetHandleId(CreateTimer())
  local r5, pc100, pcm1, ge4 = ConvertRace(5), ConvertPlayerColor(100), ConvertPlayerColor(-1), ConvertGameEvent(4)
  local after = GetHandleId(CreateTimer())
  out("convert " .. id(r5) .. " " .. id(pc100) .. " " .. id(pcm1) .. " " .. id(ge4) .. " " .. id(PLAYER_COLOR_RED) .. " " .. id(RACE_HUMAN) .. " took " .. (after - before - 1))
  local ok, n = pcall(GetHandleId, nil)
  out("outside " .. id(GetTriggerEventId()) .. " nil " .. tostring(ok) .. " " .. tostring(n))
end)

part(0.2, "identity", function()
  local t = CreateTimer()
  local n = GetHandleId(t)
  out("rawequal " .. tostring(rawequal(Player(0), Player(0))) .. " " .. tostring(rawequal(Player(0), GetLocalPlayer())) .. " " .. tostring(rawequal(ConvertRace(1), ConvertRace(1)))
    .. " " .. tostring(rawequal(RACE_HUMAN, ConvertRace(1))) .. " " .. tostring(rawequal(Condition(nil), Condition(nil))))
  out("equal by id " .. tostring(ConvertRace(1) == ConvertPlayerColor(1)) .. " " .. tostring(rawequal(ConvertRace(1), ConvertPlayerColor(1))) .. " " .. tostring(ConvertPlayerColor(n) == t)
    .. " " .. tostring(ConvertRace(n) == ConvertPlayerColor(n)) .. " " .. tostring(ConvertRace(GetHandleId(Player(0))) == Player(0)) .. " " .. tostring(t == Player(0)))
  out("metatable " .. tostring(getmetatable(t)) .. " " .. tostring(getmetatable(RACE_HUMAN)) .. " " .. tostring(getmetatable(Condition(nil))))
  local keys = {}
  keys[Player(0)], keys[ConvertRace(1)], keys[t] = "p0", "r1", "t"
  out("keys " .. tostring(keys[GetLocalPlayer()]) .. " " .. tostring(keys[ConvertRace(1)]) .. " " .. tostring(keys[RACE_HUMAN]) .. " " .. tostring(keys[ConvertPlayerColor(1)])
    .. " " .. tostring(keys[ConvertPlayerColor(n)]))
  local function shape(h) return (tostring(h):gsub(": %x+$", ": hex")) .. " " .. #tostring(h) end
  out("tostring " .. shape(t) .. ", " .. shape(Player(0)) .. ", " .. shape(ConvertRace(1)) .. ", " .. shape(GetTriggerEventId()) .. ", " .. shape(Condition(yes))
    .. ", " .. shape(Filter(nil)) .. ", " .. shape(And(nil, nil)) .. ", " .. shape(TriggerAddAction(CreateTrigger(), yes)))
  TimerStart(t, 0.01, false, function()
    local ok, err = pcall(function() out("expired " .. tostring(rawequal(GetExpiredTimer(), t)) .. " " .. tostring(GetExpiredTimer() == t)) end)
    if not ok then out("expired error " .. tostring(err)) end
  end)
end)

part(0.3, "destroyed and held", function()
  local t = CreateTimer()
  local n = GetHandleId(t)
  kill(DestroyTimer, t)
  local u = CreateTimer()
  out("timer again " .. (GetHandleId(u) - n) .. " old " .. (GetHandleId(t) - n) .. " " .. tostring(t == u) .. " " .. tostring(rawequal(t, u)) .. " " .. kind(t))
  local a = CreateTimer()
  n = GetHandleId(a)
  kill(DestroyTimer, a)
  local tr = CreateTrigger()
  out("timer then trigger " .. (GetHandleId(tr) - n) .. " " .. tostring(a == tr))
  local c = Condition(yes)
  n = GetHandleId(c)
  kill(DestroyCondition, c)
  local c2 = Condition(yes)
  out("condition again " .. (GetHandleId(c2) - n) .. " old " .. (GetHandleId(c) - n) .. " " .. tostring(c == c2) .. " " .. tostring(rawequal(c, c2)))
  local function again(label, make, destroy)
    local h = make()
    local m = GetHandleId(h)
    kill(destroy, h)
    out(label .. " again " .. (GetHandleId(make()) - m))
  end
  again("trigger", CreateTrigger, DestroyTrigger)
  again("force", CreateForce, DestroyForce)
  again("hashtable", InitHashtable, FlushParentHashtable)
  again("filter", function() return Filter(yes) end, DestroyFilter)
  again("and", function() return And(nil, nil) end, DestroyBoolExpr)
  local x, y, z = CreateTimer(), CreateTimer(), CreateTimer()
  local base = GetHandleId(x)
  kill(DestroyTimer, x)
  kill(DestroyTimer, y)
  kill(DestroyTimer, z)
  local order = {}
  for i = 1, 4 do order[i] = GetHandleId(CreateTimer()) - base end
  out("three destroyed " .. table.concat(order, " "))
end)

part(0.4, "stale", function()
  local t = CreateTimer()
  kill(DestroyTimer, t)
  local u = CreateTimer()
  TimerStart(t, 0.5, false, nil)
  out("stale timer " .. tostring(TimerGetTimeout(u)) .. " " .. tostring(TimerGetTimeout(t)))
  local d = CreateTrigger()
  local dc = cond("destroyed", true)
  TriggerAddCondition(d, dc)
  local n = GetHandleId(dc)
  kill(DestroyCondition, dc)
  local nc = cond("new", false)
  out("condition place " .. (GetHandleId(nc) - n) .. " " .. tostring(TriggerEvaluate(d)) .. " " .. tostring(dc == nc))
  flush("ran")
end)

part(0.5, "collected", function()
  local weak = setmetatable({}, {__mode = "k"})
  local ids = {}
  for i = 1, 3 do
    local t = CreateTimer()
    ids[i] = GetHandleId(t)
    weak[t] = true
    DestroyTimer(t)
  end
  local alive = CreateTimer()
  local aliveId = GetHandleId(alive)
  weak[alive] = true
  alive = nil
  local spins = 0
  while next(weak) ~= nil and spins < 3000000 do
    local junk = {spins}
    spins = spins + 1
  end
  out("collected " .. tostring(next(weak) == nil))
  local again = {}
  for i = 1, 5 do again[i] = GetHandleId(CreateTimer()) - ids[1] end
  out("after collection " .. table.concat(again, " ") .. " alive " .. (aliveId - ids[1]))
end)

part(0.6, "nil boolexprs", function()
  local cn = Condition(nil)
  out("nil handles " .. tostring(cn == Filter(nil)) .. " " .. tostring(rawequal(cn, Filter(nil))) .. " " .. kind(cn) .. " " .. kind(Filter(nil)) .. " " .. tostring(GetHandleId(cn) == GetHandleId(Filter(nil))))
  out("after a pass " .. evaluate(And(cond("t1", true), Not(cn))) .. " " .. evaluate(Not(And(cond("t2", true), cn))) .. " " .. evaluate(And(cond("t3", true), And(cn, cond("x3", true))))
    .. " " .. evaluate(And(Or(cond("f4", false), cond("t4", true)), cn)) .. " " .. evaluate(Or(cond("f5", false), cn)))
  flush("ran")
  out("across conditions " .. evaluate(cond("t6", true), Not(cn)) .. " " .. evaluate(cond("t7", true), cn))
  flush("ran")
  local t = CreateTrigger()
  local dd = cond("d9", true)
  TriggerAddCondition(t, And(cond("t9", true), dd))
  DestroyCondition(dd)
  out("destroyed after a pass " .. tostring(TriggerEvaluate(t)))
  flush("ran")
end)
