-- A body's whole decay (W2-10), on the probe map (custom_v1's data): when the bones of a footman, a rifleman and a
-- footman killed by its timed life go after their decay (D228 inferred DecayTime 2 plus BoneDecayTime 88), with the
-- order events (the undefend a footman's Defend gives) on the unit's and its owner's registrations; a CreateCorpse
-- corpse and a suspended body past 45 s; and a body whose time came while suspended, let go once the footman's body is
-- gone. Polled every 0.25 s; it ends 1 s after the footman's body is gone, or at 100 s, as the runner gives a probe 2
-- minutes from the launch.
local clock = CreateTimer()
TimerStart(clock, 1000.0, false, nil)
local function now() return string.format("%.2f", TimerGetElapsed(clock)) end
local watched, labels = {}, {}

local function watch(label, id, x, u)
  u = u or CreateUnit(Player(0), FourCC(id), x, 0, 0)
  local t = CreateTrigger()
  for _, e in ipairs({ EVENT_UNIT_DEATH, EVENT_UNIT_DECAY, EVENT_UNIT_ISSUED_ORDER }) do TriggerRegisterUnitEvent(t, u, e) end
  TriggerAddAction(t, function()
    local e = GetTriggerEventId()
    out(label .. " " .. (e == EVENT_UNIT_DEATH and "death" or e == EVENT_UNIT_DECAY and "decay" or ("order " .. GetIssuedOrderId())) .. " "
      .. now() .. " " .. tostring(GetUnitTypeId(u) ~= 0))
  end)
  watched[#watched + 1] = { label, u }
  labels[u] = label
  return u
end

local p = CreateTrigger()
TriggerRegisterPlayerUnitEvent(p, Player(0), EVENT_PLAYER_UNIT_ISSUED_ORDER, nil)
TriggerAddAction(p, function() out("player order " .. (labels[GetTriggerUnit()] or "?") .. " " .. GetIssuedOrderId() .. " " .. now()) end)

local f = watch("hfoo", "hfoo", -600)
local r = watch("hrif", "hrif", -400)
local timed = watch("timed", "hfoo", -200)
local s = watch("suspended", "hfoo", 0)
local late = watch("late", "hfoo", 200)
watch("corpse", nil, nil, CreateCorpse(Player(0), FourCC("hfoo"), 400, 0, 0))
KillUnit(f)
KillUnit(r)
KillUnit(s)
UnitSuspendDecay(s, true)
KillUnit(late)
UnitSuspendDecay(late, true)
UnitApplyTimedLife(timed, FourCC("BTLF"), 1)
out("start " .. now())

local ending
local tick = CreateTimer()
TimerStart(tick, 0.25, true, guard(function()
  for i = #watched, 1, -1 do
    local label, u = watched[i][1], watched[i][2]
    if GetUnitTypeId(u) == 0 then
      out(label .. " gone " .. now())
      table.remove(watched, i)
      if u == f then
        UnitSuspendDecay(late, false)
        out("late let go " .. now())
        ending = TimerGetElapsed(clock) + 1
      end
    end
  end
  if TimerGetElapsed(clock) > (ending or 100) then
    for _, w in ipairs(watched) do out(w[1] .. " still there " .. tostring(UnitAlive(w[2])) .. " " .. GetWidgetLife(w[2])) end
    PauseTimer(tick)
    done()
  end
end))
