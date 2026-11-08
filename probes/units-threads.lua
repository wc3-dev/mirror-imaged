-- The threads of the callbacks a native runs (W2-6; decisions D70 measured timers, conditions, actions and ExecuteFunc):
-- ForGroup's and ForForce's callbacks, the unit, player and destructable enumerations' filters and EnumDestructablesInRect's
-- callback, run from a trigger's action and from a timer's callback. For each call: whether it runs in the calling
-- thread ("same") or one of its own ("own"), coroutine.isyieldable and whether it is the main thread; then a wait inside
-- each, from an action: when the callback and the code after the call go on.
local clock = CreateTimer()
TimerStart(clock, 1000.0, false, nil)
local function now() return string.format("%.1f", TimerGetElapsed(clock)) end
local g = CreateGroup()
GroupAddUnit(g, CreateUnit(Player(0), FourCC("hfoo"), 0, 0, 0))
GroupAddUnit(g, CreateUnit(Player(0), FourCC("hfoo"), 200, 0, 0))
local f = CreateForce()
ForceAddPlayer(f, Player(0))
ForceAddPlayer(f, Player(1))
CreateDestructable(FourCC("LTlt"), 500, 500, 270, 1, 0)
local calls = {
  { "forgroup", function(cb) ForGroup(g, cb) end },
  { "forforce", function(cb) ForForce(f, cb) end },
  { "unit filter", function(cb) GroupEnumUnitsInRange(CreateGroup(), 0, 0, 500, Filter(cb)) end },
  { "player filter", function(cb) ForceEnumPlayers(CreateForce(), Filter(cb)) end },
  { "destructable", function(cb) EnumDestructablesInRect(Rect(0, 0, 1000, 1000), Filter(cb), cb) end },
}

local function threads(where, label, call)
  local caller = coroutine.running()
  local seen = {}
  call(function()
    local co, main = coroutine.running()
    seen[#seen + 1] = (co == caller and "same" or "own") .. "/" .. tostring(coroutine.isyieldable()) .. "/" .. tostring(main)
    return true
  end)
  out(where .. " " .. label .. " " .. table.concat(seen, ","))
end
for _, c in ipairs(calls) do threads("timer", c[1], c[2]) end
local t = CreateTrigger()
TriggerAddAction(t, function()
  out("action yieldable " .. tostring(coroutine.isyieldable()))
  for _, c in ipairs(calls) do threads("action", c[1], c[2]) end
end)
TriggerExecute(t)

for i, c in ipairs(calls) do
  TimerStart(CreateTimer(), i * 1.5, false, function()
    local w = CreateTrigger()
    TriggerAddAction(w, function()
      out(c[1] .. " start " .. now())
      local ok, err = pcall(c[2], function()
        out(c[1] .. " callback " .. now())
        TriggerSleepAction(0.5)
        out(c[1] .. " callback after wait " .. now())
        return true
      end)
      out(c[1] .. " after call " .. now() .. " " .. tostring(ok) .. " " .. tostring(err))
    end)
    TriggerExecute(w)
    out(c[1] .. " after execute " .. now())
  end)
end
TimerStart(CreateTimer(), 10, false, guard(function() done() end))
