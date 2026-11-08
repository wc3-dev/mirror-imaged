-- Triggers, fourth part: BlzTriggerIsRunning and BlzTriggerInterrupt, before, during and after a run and its wait. The
-- lines with a time after a wait vary from run to run (decisions D71).
local clock = CreateTimer()
TimerStart(clock, 1000.0, false, nil)
local function now() return string.format("%.1f", TimerGetElapsed(clock)) end
local log = {}
local function running(t) return tostring(BlzTriggerIsRunning(t)) end

local t = CreateTrigger()
out("new " .. running(t))
TriggerAddAction(t, function()
  log[#log + 1] = "t in action " .. running(t)
  TriggerSleepAction(0.5)
  out("t after wait " .. now() .. " " .. running(t))
end)
TriggerExecute(t)
out("t waiting " .. running(t) .. " " .. table.concat(log, ","))

local u = CreateTrigger()
TriggerAddAction(u, function() log[#log + 1] = "u start" TriggerSleepAction(0.3) log[#log + 1] = "u after wait" end)
TriggerAddAction(u, function() log[#log + 1] = "u second" end)
TriggerExecute(u)
BlzTriggerInterrupt(u)
out("u interrupted " .. running(u))

local v = CreateTrigger()
TriggerAddAction(v, function()
  log[#log + 1] = "v start"
  BlzTriggerInterrupt(v)
  log[#log + 1] = "v after interrupt " .. running(v)
end)
TriggerAddAction(v, function() log[#log + 1] = "v second" end)
TriggerExecute(v)
out("v " .. running(v))

local w = CreateTrigger()
TriggerAddAction(w, function() TriggerSleepAction(0.2) out("w done " .. now()) end)
TriggerExecute(w)
TriggerExecute(w)
out("w twice " .. running(w))

local c = CreateTrigger()
TriggerAddCondition(c, Condition(function() log[#log + 1] = "c condition " .. running(c) return true end))
TriggerEvaluate(c)
BlzTriggerInterrupt(c)
out("c " .. running(c))

TimerStart(CreateTimer(), 1.0, false, guard(function()
  out("after " .. running(t) .. " " .. running(u) .. " " .. running(w))
  out("log " .. table.concat(log, ","))
  done()
end))
