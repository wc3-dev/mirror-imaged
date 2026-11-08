-- Handles, third part: every player's id, whether a callback given to a native takes an id, how soon a freed id comes
-- back and in which order, Condition(nil) in more boolexprs, and the type a number parameter names for a handle.
-- Each part runs in its own timer callback.
local function kind(h) return h == nil and "nil" or (tostring(h):match("^[%a_]+")) end
local function part(at, label, f)
  TimerStart(CreateTimer(), at, false, function()
    out(label)
    local ok, err = pcall(f)
    if not ok then out(label .. " error " .. tostring(err)) end
  end)
end
local function try(f, ...)
  local ok, v = pcall(f, ...)
  return ok and ("ok " .. (type(v) == "userdata" and kind(v) or tostring(v))) or ("error " .. tostring(v))
end
local function collect(weak)
  local spins = 0
  while next(weak) ~= nil and spins < 3000000 do
    local junk = {spins}
    spins = spins + 1
  end
  return tostring(next(weak) == nil)
end
local yes = function() return true end
local log = {}
local function cond(name, value) return Condition(function() log[#log + 1] = name return value end) end
local function flush(label) out(label .. " " .. table.concat(log, ",")) log = {} end
local function evaluate(...)
  local t = CreateTrigger()
  for _, c in ipairs({...}) do TriggerAddCondition(t, c) end
  return tostring(TriggerEvaluate(t))
end

TimerStart(CreateTimer(), 8.0, false, guard(done))

local players = {}
for i = 0, 13 do players[#players + 1] = GetHandleId(Player(i)) - GetHandleId(Player(0)) end
out("players " .. GetHandleId(Player(0)) .. " " .. table.concat(players, " "))
players = {}
for i = 14, 27 do players[#players + 1] = GetHandleId(Player(i)) - GetHandleId(Player(0)) end
out("players " .. table.concat(players, " "))

-- the timers' ids freed: a and b destroyed; b collected at 0.1, a held until 1.1, both checked at 2.1 for which comes
-- back first (given in their order, then the count of other ids, as where another id freed with them comes may vary);
-- c destroyed and collected at 3.0, then one timer made at 3.01, 3.05, 3.2, 3.5 and 4.0
local ids, a = {}, nil
local function named(n)
  for k, v in pairs(ids) do if v == n then return k end end
  return "new"
end
part(0.1, "order", function()
  a = CreateTimer()
  local b = CreateTimer()
  ids.a, ids.b = GetHandleId(a), GetHandleId(b)
  DestroyTimer(a)
  DestroyTimer(b)
  local weak = setmetatable({}, {__mode = "k"})
  weak[b] = true
  b = nil
  out("b collected " .. collect(weak))
end)
part(1.1, "order a", function()
  local weak = setmetatable({}, {__mode = "k"})
  weak[a] = true
  a = nil
  out("a collected " .. collect(weak))
end)
part(2.1, "order after", function()
  local names, others = {}, 0
  for i = 1, 4 do
    local name = named(GetHandleId(CreateTimer()))
    if name ~= "new" then names[#names + 1] = name else others = others + 1 end
  end
  out("new " .. table.concat(names, " ") .. ", " .. others .. " other")
end)
part(3.0, "when", function()
  local c = CreateTimer()
  ids.c = GetHandleId(c)
  DestroyTimer(c)
  local weak = setmetatable({}, {__mode = "k"})
  weak[c] = true
  c = nil
  out("c collected " .. collect(weak) .. " at once " .. named(GetHandleId(CreateTimer())))
end)
for _, at in ipairs({3.01, 3.05, 3.2, 3.5, 4.0}) do
  part(at, "when " .. at, function() out("made " .. named(GetHandleId(CreateTimer()))) end)
end

part(5.0, "callbacks", function()
  function MirrorProbeNothing() end
  local function taken(label, call)
    local before = GetHandleId(CreateTimer())
    call()
    out(label .. " " .. (GetHandleId(CreateTimer()) - before - 1))
  end
  local t, g, f, trig = CreateTimer(), CreateGroup(), CreateForce(), CreateTrigger()
  taken("TimerStart", function() TimerStart(t, 100, false, yes) end)
  taken("TimerStart nil", function() TimerStart(t, 100, false, nil) end)
  taken("ForForce", function() ForForce(f, yes) end)
  taken("ExecuteFunc", function() ExecuteFunc("MirrorProbeNothing") end)
  taken("TriggerExecute", function() TriggerExecute(trig) end)
  taken("TriggerEvaluate", function() TriggerEvaluate(trig) end)
end)

part(5.1, "nil boolexprs", function()
  local cn = Condition(nil)
  out("predicted " .. evaluate(And(cond("t1", true), Or(cn, cond("f1", false)))) .. " " .. evaluate(And(cond("t2", true), Not(Not(cn)))) .. " "
    .. evaluate(Or(cond("f3", false), Not(cn))) .. " " .. evaluate(And(Not(cond("f5", false)), cn)) .. " " .. evaluate(And(cond("t6", true), And(cond("u6", true), cn))) .. " "
    .. evaluate(And(Not(cn), cond("t7", true))) .. " " .. evaluate(And(And(cond("t8", true), cond("u8", true)), Not(cn))))
  flush("ran")
end)

part(5.2, "got", function()
  local t = CreateTimer()
  out("I2S " .. try(I2S, t))
  out("R2S " .. try(R2S, RACE_HUMAN))
  out("StringLength " .. try(StringLength, t))
  out("I2S nil condition " .. try(I2S, Condition(nil)))
end)
