-- Triggers, third part: which sets of an integer and a float game state fire each comparison, the event ids they
-- give, and Condition(nil) in a boolexpr.
local function id(h) return h == nil and "nil" or tostring(GetHandleId(h)) end
local log = {}
local function flush(label) out(label .. " " .. table.concat(log, ",")) log = {} end
local function cond(value) return Condition(function() return value end) end
local function evaluate(c)
  local t = CreateTrigger()
  TriggerAddCondition(t, c)
  return tostring(TriggerEvaluate(t))
end

local ops = {LESS_THAN = LESS_THAN, LESS_THAN_OR_EQUAL = LESS_THAN_OR_EQUAL, EQUAL = EQUAL,
  GREATER_THAN_OR_EQUAL = GREATER_THAN_OR_EQUAL, GREATER_THAN = GREATER_THAN, NOT_EQUAL = NOT_EQUAL}
local names = {"LESS_THAN", "LESS_THAN_OR_EQUAL", "EQUAL", "GREATER_THAN_OR_EQUAL", "GREATER_THAN", "NOT_EQUAL"}

out("start " .. GetIntegerGameState(GAME_STATE_DIVINE_INTERVENTION) .. " " .. GetIntegerGameState(GAME_STATE_DISCONNECTED))
for _, name in ipairs(names) do
  local t = CreateTrigger()
  TriggerRegisterGameStateEvent(t, GAME_STATE_DIVINE_INTERVENTION, ops[name], 2)
  TriggerAddAction(t, function() log[#log + 1] = GetIntegerGameState(GAME_STATE_DIVINE_INTERVENTION) .. ":" .. id(GetTriggerEventId()) end)
  SetIntegerGameState(GAME_STATE_DIVINE_INTERVENTION, 5)
  for _, value in ipairs({0, 1, 2, 2, 3, 2, 1, 0, 4}) do SetIntegerGameState(GAME_STATE_DIVINE_INTERVENTION, value) end
  DisableTrigger(t)
  flush("divine " .. name)
end
for _, name in ipairs(names) do
  local t = CreateTrigger()
  TriggerRegisterGameStateEvent(t, GAME_STATE_DISCONNECTED, ops[name], 0)
  TriggerAddAction(t, function() log[#log + 1] = GetIntegerGameState(GAME_STATE_DISCONNECTED) .. ":" .. id(GetTriggerEventId()) end)
  for _, value in ipairs({0, 2, 2, 0, -1, 0}) do SetIntegerGameState(GAME_STATE_DISCONNECTED, value) end
  DisableTrigger(t)
  flush("disconnected " .. name)
end
SuspendTimeOfDay(true)
for _, name in ipairs(names) do
  local t = CreateTrigger()
  TriggerRegisterGameStateEvent(t, GAME_STATE_TIME_OF_DAY, ops[name], 12)
  TriggerAddAction(t, function() log[#log + 1] = string.format("%.1f", GetFloatGameState(GAME_STATE_TIME_OF_DAY)) .. ":" .. id(GetTriggerEventId()) end)
  SetFloatGameState(GAME_STATE_TIME_OF_DAY, 20)
  for _, value in ipairs({10, 11, 12, 12, 13, 12, 11, 10, 14}) do SetFloatGameState(GAME_STATE_TIME_OF_DAY, value) end
  DisableTrigger(t)
  flush("time " .. name)
end

local cn = Condition(nil)
out("nil condition " .. id(cn) .. " " .. id(Not(cn)) .. " " .. evaluate(And(cn, cond(false))) .. " " .. evaluate(And(cn, cond(true))) .. " " .. evaluate(Or(cn, cond(false)))
  .. " " .. evaluate(And(cond(true), cn)) .. " " .. evaluate(Not(Not(cn))) .. " " .. evaluate(Not(cond(nil))))
done()
