-- What units-more left open (W2-10), on the probe map (custom_v1's data): the unit state events one registration a
-- unit, the filter unit events with and without a filter, the food event's place in a death and a dead hero's food,
-- timed life's summoned kind and its edges, the alarm on buildings, a paused unit given away and unpaused, and when a
-- creep falls asleep again after UnitWakeUp, with no creep sleep, at dusk and at dawn. Units of Player 0 stand in a grid
-- at the bottom left; the creeps far from it at the top right.
-- Every player passive to every other but the creeps, so no units fight
for i = 0, 15 do
  for j = 0, 15 do SetPlayerAlliance(Player(i), Player(j), ALLIANCE_PASSIVE, true) end
end
local clock = CreateTimer()
TimerStart(clock, 1000.0, false, nil)
local function now() return string.format("%.2f", TimerGetElapsed(clock)) end
local function pid(p) return p == nil and "nil" or tostring(GetPlayerId(p)) end
local function tf(v) return v and "t" or "f" end
local spot = 0
local function place()
  spot = spot % 100 + 1
  return (spot % 10) * 200 - 1900, (spot // 10) * 200 - 1900
end
local function make(id, p, x, y)
  if not x then x, y = place() end
  return CreateUnit(p or Player(0), FourCC(id), x, y, 0)
end
local function part(at, label, f)
  TimerStart(CreateTimer(), at, false, function()
    out(label)
    local ok, err = pcall(f)
    if not ok then out(label .. " error " .. tostring(err)) end
  end)
end

TimerStart(CreateTimer(), 13, false, guard(function() done() end))

part(0.05, "state", function()
  local function state(label, u, which, op, limit)
    local t = CreateTrigger()
    TriggerRegisterUnitStateEvent(t, u, which, op, limit)
    TriggerAddAction(t, function()
      out("state " .. label .. " event " .. string.format("%.4f %.4f %.4f", GetWidgetLife(u), GetUnitState(u, UNIT_STATE_MANA), GetUnitState(u, UNIT_STATE_MAX_LIFE))
        .. " " .. now())
    end)
    return u
  end
  local a = state("a", make("hfoo"), UNIT_STATE_LIFE, LESS_THAN, 200)
  local b = state("b", make("Hpal"), UNIT_STATE_MANA, LESS_THAN_OR_EQUAL, 100)
  local c = state("c", make("Hpal"), UNIT_STATE_LIFE, EQUAL, 500)
  local d = state("d", make("Hpal"), UNIT_STATE_MAX_LIFE, GREATER_THAN, 700)
  local e = state("e", make("Hpal"), UNIT_STATE_MANA, LESS_THAN_OR_EQUAL, 100)
  local g = make("hfoo")
  SetWidgetLife(g, 150)
  state("g", g, UNIT_STATE_LIFE, LESS_THAN, 200)
  local k = state("k", make("hfoo"), UNIT_STATE_LIFE, LESS_THAN, 200)
  local m = state("m", make("Hpal"), UNIT_STATE_MAX_MANA, LESS_THAN, 200)
  local n = state("n", make("hfoo"), UNIT_STATE_LIFE, GREATER_THAN_OR_EQUAL, 420)
  local q = state("q", make("hfoo"), UNIT_STATE_LIFE, NOT_EQUAL, 300)
  out("set a 150")
  SetWidgetLife(a, 150)
  out("set b 100")
  SetUnitState(b, UNIT_STATE_MANA, 100)
  out("set c 500")
  SetWidgetLife(c, 500)
  out("set d 800")
  BlzSetUnitMaxHP(d, 800)
  out("set e 100")
  SetUnitState(e, UNIT_STATE_MANA, 100)
  out("set e life 500")
  SetWidgetLife(e, 500)
  out("kill k")
  KillUnit(k)
  out("set m 150")
  BlzSetUnitMaxMana(m, 150)
  out("set n 400")
  SetWidgetLife(n, 400)
  out("set n 420")
  SetWidgetLife(n, 420)
  out("set q 350")
  SetWidgetLife(q, 350)
  out("set q 300")
  SetWidgetLife(q, 300)
  out("set q 250")
  SetWidgetLife(q, 250)
  out("state end " .. now())
  TimerStart(CreateTimer(), 0, false, function() out("state 0s " .. now()) end)
  TimerStart(CreateTimer(), 0.5, false, function() out("state 0.5s " .. now()) end)
end)

part(0.2, "filter", function()
  local u, v, w, x = make("hfoo"), make("hfoo"), make("hfoo"), make("hfoo")
  local t = CreateTrigger()
  TriggerRegisterFilterUnitEvent(t, u, EVENT_UNIT_DEATH, nil)
  TriggerRegisterFilterUnitEvent(t, v, EVENT_UNIT_CHANGE_OWNER, Filter(function()
    out("filter v runs")
    return true
  end))
  TriggerRegisterFilterUnitEvent(t, w, EVENT_UNIT_CHANGE_OWNER, Filter(function()
    out("filter w runs")
    return false
  end))
  TriggerRegisterFilterUnitEvent(t, x, EVENT_UNIT_DEATH, Condition(function() return true end))
  TriggerAddAction(t, function()
    local y = GetTriggerUnit()
    out("filter event " .. GetHandleId(GetTriggerEventId()) .. " " .. (y == u and "u" or y == v and "v" or y == w and "w" or y == x and "x" or "?"))
  end)
  KillUnit(u)
  SetUnitOwner(v, Player(1), false)
  SetUnitOwner(w, Player(1), false)
  KillUnit(x)
  out("filter done")
end)

part(0.3, "food", function()
  local p = Player(9)
  local function food() return GetPlayerState(p, PLAYER_STATE_RESOURCE_FOOD_USED) end
  local t = CreateTrigger()
  TriggerRegisterPlayerStateEvent(t, p, PLAYER_STATE_RESOURCE_FOOD_USED, GREATER_THAN_OR_EQUAL, 0)
  TriggerAddAction(t, function() out("food event " .. food()) end)
  local f, h = make("hfoo", p), make("Hpal", p)
  local d = CreateTrigger()
  TriggerRegisterPlayerUnitEvent(d, p, EVENT_PLAYER_UNIT_DEATH, nil)
  TriggerAddAction(d, function() out("food death " .. food()) end)
  out("kill footman")
  KillUnit(f)
  out("kill hero")
  KillUnit(h)
  out("food hero dead " .. food())
  local x, y = place()
  CreateCorpse(p, FourCC("Hpal"), x, y, 0)
  out("food hero corpse " .. food())
  RemoveUnit(h)
  local unused = make("hfoo", p)
  SetUnitUseFood(unused, false)
  RemoveUnit(unused)
  out("food removed " .. food())
  TimerStart(CreateTimer(), 0, false, function() out("food after step " .. food()) end)
  for _ = 1, 18 do make("hhou", p) end
  out("food cap " .. GetPlayerState(p, PLAYER_STATE_RESOURCE_FOOD_CAP) .. " " .. GetPlayerState(p, PLAYER_STATE_FOOD_CAP_CEILING))
end)

part(0.4, "timed", function()
  local function watch(label, u)
    local t = CreateTrigger()
    TriggerRegisterUnitEvent(t, u, EVENT_UNIT_DEATH)
    TriggerAddAction(t, function() out("timed " .. label .. " death " .. now()) end)
    return u
  end
  local a = watch("a", make("hfoo"))
  UnitApplyTimedLife(a, FourCC("BTLF"), 5)
  out("timed class " .. BlzGetUnitIntegerField(a, UNIT_IF_UNIT_CLASSIFICATION) .. " " .. tf(IsUnitType(a, UNIT_TYPE_SUMMONED)) .. " "
    .. tf(UnitRemoveType(a, UNIT_TYPE_SUMMONED)) .. tf(IsUnitType(a, UNIT_TYPE_SUMMONED)))
  out("timed cancel a")
  BlzUnitCancelTimedLife(a)
  local b = watch("b", make("hfoo"))
  out("timed cancel without")
  BlzUnitCancelTimedLife(b)
  UnitPauseTimedLife(b, true)
  UnitPauseTimedLife(b, false)
  local z = watch("zero", make("hfoo"))
  UnitApplyTimedLife(z, FourCC("BTLF"), 0)
  out("timed zero " .. tf(IsUnitType(z, UNIT_TYPE_SUMMONED)))
  UnitApplyTimedLife(z, FourCC("BTLF"), 1)
  local s = watch("then zero", make("hfoo"))
  UnitApplyTimedLife(s, FourCC("BTLF"), 1)
  UnitApplyTimedLife(s, FourCC("BTLF"), 0)
  local tiny = watch("tiny", make("hfoo"))
  UnitApplyTimedLife(tiny, FourCC("BTLF"), 0.01)
  local r = watch("removed", make("hfoo"))
  UnitApplyTimedLife(r, FourCC("BTLF"), 0.5)
  RemoveUnit(r)
  local pc = watch("paused cancelled", make("hfoo"))
  UnitApplyTimedLife(pc, FourCC("BTLF"), 1)
  UnitPauseTimedLife(pc, true)
  BlzUnitCancelTimedLife(pc)
  out("timed made " .. now())
end)

part(0.5, "alarm", function()
  for _, id in ipairs({ "hbar", "htow", "hfoo" }) do
    local u = make(id)
    local fresh = UnitIgnoreAlarmToggled(u)
    local r1 = UnitIgnoreAlarm(u, true)
    local t1 = UnitIgnoreAlarmToggled(u)
    local r2 = UnitIgnoreAlarm(u, false)
    out("alarm " .. id .. " " .. tf(fresh) .. " " .. tf(r1) .. tf(t1) .. " " .. tf(r2) .. tf(UnitIgnoreAlarmToggled(u)))
  end
end)

part(0.6, "paused", function()
  local names = {}
  local o = CreateTrigger()
  for _, e in ipairs({ EVENT_PLAYER_UNIT_ISSUED_ORDER, EVENT_PLAYER_UNIT_ISSUED_POINT_ORDER, EVENT_PLAYER_UNIT_CHANGE_OWNER, EVENT_PLAYER_UNIT_DEATH }) do
    TriggerRegisterPlayerUnitEvent(o, Player(0), e, nil)
    TriggerRegisterPlayerUnitEvent(o, Player(1), e, nil)
  end
  TriggerAddAction(o, function()
    local u = GetTriggerUnit()
    out("> " .. (names[u] or "?") .. " " .. GetHandleId(GetTriggerEventId()) .. " " .. GetIssuedOrderId() .. " " .. pid(GetTriggerPlayer()) .. " paused "
      .. tf(IsUnitPaused(u)) .. " " .. now())
  end)
  local function unit(name)
    local u = make("hfoo")
    names[u] = name
    return u
  end
  local f, g, m, h = unit("f"), unit("g"), unit("m"), unit("h")
  out("pause f")
  PauseUnit(f, true)
  out("give paused f")
  SetUnitOwner(f, Player(1), false)
  out("unpause f " .. now())
  PauseUnit(f, false)
  PauseUnit(g, true)
  out("kill paused g")
  KillUnit(g)
  PauseUnit(m, true)
  out("unpause and pause m")
  PauseUnit(m, false)
  PauseUnit(m, true)
  PauseUnit(h, true)
  out("unpause and kill h")
  PauseUnit(h, false)
  KillUnit(h)
  out("paused end " .. now())
end)

part(1.0, "sleep", function()
  local na = Player(PLAYER_NEUTRAL_AGGRESSIVE)
  local x, y = place()
  local all = {
    { "w1", make("nmrl", na, 1900, 1900) },
    { "w2", make("nmrl", na, 1900, 1500) },
    { "off", make("nmrl", na, 1500, 1900) },
    { "passive", make("nmrl", Player(PLAYER_NEUTRAL_PASSIVE), 1100, 1900) },
    { "dead", make("nmrl", na, 1500, 1500) },
    { "corpse", CreateCorpse(na, FourCC("nmrl"), 1100, 1500, 0) },
    { "hidden", make("nmrl", na, 1900, 1100) },
    { "paused", make("nmrl", na, 1500, 1100) },
    { "mine", make("nmrl", Player(0), x, y) },
  }
  local by = {}
  for _, e in ipairs(all) do by[e[1]] = e[2] end
  KillUnit(by.dead)
  ShowUnit(by.hidden, false)
  PauseUnit(by.paused, true)
  local last = {}
  local function poll(label)
    local parts = {}
    for _, e in ipairs(all) do
      local s = UnitIsSleeping(e[2])
      if last[e[1]] ~= s then parts[#parts + 1] = e[1] .. " " .. tf(s) end
      last[e[1]] = s
    end
    if #parts > 0 or label then
      out("sleep " .. (label or "") .. " " .. now() .. " " .. string.format("%.3f", GetFloatGameState(GAME_STATE_TIME_OF_DAY)) .. " " .. table.concat(parts, " "))
    end
  end
  poll("made")
  TimerStart(CreateTimer(), 0.05, true, function() poll() end)
  TimerStart(CreateTimer(), 0.5, false, function()
    UnitWakeUp(by.w1)
    poll("woke w1")
  end)
  TimerStart(CreateTimer(), 0.8, false, function()
    UnitWakeUp(by.w2)
    UnitAddSleep(by.off, false)
    poll("woke w2, off")
  end)
  TimerStart(CreateTimer(), 4.5, false, function()
    SetPlayerState(na, PLAYER_STATE_NO_CREEP_SLEEP, 1)
    poll("no creep sleep")
  end)
  TimerStart(CreateTimer(), 5.5, false, function()
    SetPlayerState(na, PLAYER_STATE_NO_CREEP_SLEEP, 0)
    poll("creep sleep")
  end)
  TimerStart(CreateTimer(), 6.5, false, function()
    SetFloatGameState(GAME_STATE_TIME_OF_DAY, 17.95)
    poll("17.95")
  end)
  TimerStart(CreateTimer(), 8.5, false, function()
    SetFloatGameState(GAME_STATE_TIME_OF_DAY, 5.95)
    poll("5.95")
  end)
  TimerStart(CreateTimer(), 11.5, false, function() poll("end") end)
end)
