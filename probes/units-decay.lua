-- Death, decay and corpses over time (W2-2), on the probe map (custom_v1's data): when the decay event comes and when a
-- dead unit is gone (its type id 0), for a footman, a rifleman, a barracks, a Paladin, a gryphon rider, an exploded
-- footman, a corpse made by CreateCorpse, a footman whose decay is suspended and a footman removed after death; for
-- 45 s, as a unit's corpse lasts 90 s more after its decay (BoneDecayTime 88 and DecayTime 2), past what the runner
-- gives a probe.
local clock = CreateTimer()
TimerStart(clock, 1000.0, false, nil)
local function now() return string.format("%.2f", TimerGetElapsed(clock)) end
local watched = {}

local function watch(label, id, prepare)
  local u = CreateUnit(Player(0), FourCC(id), 0, 0, 0)
  local t = CreateTrigger()
  TriggerRegisterUnitEvent(t, u, EVENT_UNIT_DEATH)
  TriggerRegisterUnitEvent(t, u, EVENT_UNIT_DECAY)
  TriggerAddAction(t, function()
    out(label .. " " .. (GetTriggerEventId() == EVENT_UNIT_DEATH and "death" or "decay") .. " " .. now() .. " life "
      .. GetWidgetLife(u) .. " alive " .. tostring(UnitAlive(u)))
  end)
  watched[#watched + 1] = { label, u }
  if prepare then prepare(u) end
  return u
end

TimerStart(CreateTimer(), 0.1, false, guard(function()
  out("start " .. now())
  KillUnit(watch("hfoo", "hfoo"))
  KillUnit(watch("hrif", "hrif"))
  KillUnit(watch("hbar", "hbar"))
  KillUnit(watch("Hpal", "Hpal"))
  KillUnit(watch("hgry", "hgry"))
  local x = watch("exploded", "hfoo")
  SetUnitExploded(x, true)
  KillUnit(x)
  local s = watch("suspended", "hfoo")
  KillUnit(s)
  UnitSuspendDecay(s, true)
  local r = watch("removed", "hfoo")
  KillUnit(r)
  RemoveUnit(r)
  local c = CreateCorpse(Player(0), FourCC("hfoo"), 0, 0, 0)
  watched[#watched + 1] = { "corpse", c }
  local t = CreateTrigger()
  TriggerRegisterUnitEvent(t, c, EVENT_UNIT_DECAY)
  TriggerAddAction(t, function() out("corpse decay " .. now()) end)
end))

local tick = CreateTimer()
TimerStart(tick, 0.25, true, guard(function()
  for i = #watched, 1, -1 do
    local label, u = watched[i][1], watched[i][2]
    if GetUnitTypeId(u) == 0 then
      out(label .. " gone " .. now())
      table.remove(watched, i)
    end
  end
  if TimerGetElapsed(clock) > 45 then
    for _, w in ipairs(watched) do out(w[1] .. " still there " .. tostring(UnitAlive(w[2])) .. " " .. GetWidgetLife(w[2])) end
    PauseTimer(tick)
    done()
  end
end))
