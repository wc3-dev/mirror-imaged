-- Units without movement (W2-2), on the probe map (custom_v1's data): creating, the handles a unit takes, the
-- property and state natives, life and mana at their edges, owner changes and the stop inside SetUnitOwner, the death
-- and owner events and their order, the event responses, IsUnitType, GetObjectName, and every unit field native read
-- on four kinds of unit and set on three. Each unit is made on a spot of its own (a grid 200 apart), as the game moves a
-- unit made on another; the hero attribute fields are read and set on the Paladin only, as reading UNIT_IF_AGILITY on a
-- footman closes the game (probe units-attribute), and so does setting a footman's primary attribute to 3 (probe
-- units-primary).
local clock = CreateTimer()
TimerStart(clock, 1000.0, false, nil)
local function now() return string.format("%.4f", TimerGetElapsed(clock)) end
local function show(v) return v == nil and "nil" or tostring(v) end
local function kind(h) return h == nil and "nil" or tostring(h):match("^[%a_]+") end
local function pid(p) return p == nil and "nil" or tostring(GetPlayerId(p)) end
local spot = 0
local function make(id, x, y, face)
  if not x then
    spot = spot % 225 + 1
    x, y = (spot % 15) * 200 - 1400, (spot // 15) * 200 - 1400
  end
  return CreateUnit(Player(0), FourCC(id), x, y, face or 0)
end
local function part(at, label, f)
  TimerStart(CreateTimer(), at, false, function()
    out(label)
    local ok, err = pcall(f)
    if not ok then out(label .. " error " .. tostring(err)) end
  end)
end

TimerStart(CreateTimer(), 7.5, false, guard(function() done() end))

part(0.05, "create", function()
  local before = CreateTimer()
  local u = make("hfoo", 1500.5, -1700.25, 30)
  local between = CreateTimer()
  local h = make("Hpal", 1700, -1700)
  local after = CreateTimer()
  out("ids " .. (GetHandleId(u) - GetHandleId(before)) .. " " .. (GetHandleId(between) - GetHandleId(u))
    .. " " .. (GetHandleId(h) - GetHandleId(between)) .. " " .. (GetHandleId(after) - GetHandleId(h)))
  out("hfoo " .. kind(u) .. " " .. tostring(GetUnitTypeId(u) == FourCC("hfoo")) .. " " .. show(GetUnitName(u)) .. " owner "
    .. pid(GetOwningPlayer(u)) .. " at " .. GetUnitX(u) .. " " .. GetUnitY(u) .. " " .. GetWidgetX(u) .. " " .. GetWidgetY(u)
    .. " facing " .. GetUnitFacing(u) .. " z " .. BlzGetUnitZ(u))
  out("hfoo life " .. GetWidgetLife(u) .. " " .. GetUnitState(u, UNIT_STATE_LIFE) .. " " .. GetUnitState(u, UNIT_STATE_MAX_LIFE)
    .. " " .. BlzGetUnitMaxHP(u) .. " mana " .. GetUnitState(u, UNIT_STATE_MANA) .. " " .. GetUnitState(u, UNIT_STATE_MAX_MANA)
    .. " " .. BlzGetUnitMaxMana(u) .. " level " .. GetUnitLevel(u) .. " alive " .. tostring(UnitAlive(u)) .. " "
    .. tostring(BlzUnitIsAlive(u)))
  out("hfoo speed " .. GetUnitMoveSpeed(u) .. " " .. GetUnitDefaultMoveSpeed(u) .. " turn " .. GetUnitTurnSpeed(u) .. " "
    .. GetUnitDefaultTurnSpeed(u) .. " fly " .. GetUnitFlyHeight(u) .. " " .. GetUnitDefaultFlyHeight(u) .. " acquire "
    .. GetUnitAcquireRange(u) .. " " .. GetUnitDefaultAcquireRange(u) .. " prop " .. GetUnitPropWindow(u) .. " "
    .. GetUnitDefaultPropWindow(u))
  out("hfoo food " .. GetUnitFoodUsed(u) .. " " .. GetUnitFoodMade(u) .. " points " .. GetUnitPointValue(u) .. " "
    .. GetUnitPointValueByType(FourCC("hfoo")) .. " race " .. GetHandleId(GetUnitRace(u)) .. " " .. tostring(IsUnitRace(u, RACE_HUMAN))
    .. " data " .. GetUnitUserData(u) .. " skin " .. tostring(BlzGetUnitSkin(u) == FourCC("hfoo")) .. " owned "
    .. tostring(IsUnitOwnedByPlayer(u, Player(0))) .. " " .. tostring(IsUnitOwnedByPlayer(u, Player(1))))
  out("Hpal life " .. GetWidgetLife(h) .. " " .. BlzGetUnitMaxHP(h) .. " mana " .. GetUnitState(h, UNIT_STATE_MANA) .. " "
    .. BlzGetUnitMaxMana(h) .. " level " .. GetUnitLevel(h) .. " name " .. show(GetUnitName(h)) .. " fly " .. GetUnitFlyHeight(h))
  for _, id in ipairs({ "hbar", "hgry", "hpea", "hkni", "nshe", "hhou" }) do
    local v = make(id)
    out(id .. " life " .. GetWidgetLife(v) .. " " .. BlzGetUnitMaxHP(v) .. " mana " .. GetUnitState(v, UNIT_STATE_MANA)
      .. " level " .. GetUnitLevel(v) .. " speed " .. GetUnitMoveSpeed(v) .. " turn " .. GetUnitTurnSpeed(v) .. " fly "
      .. GetUnitFlyHeight(v) .. " " .. GetUnitDefaultFlyHeight(v) .. " z " .. BlzGetUnitZ(v) .. " food " .. GetUnitFoodUsed(v)
      .. " " .. GetUnitFoodMade(v) .. " race " .. GetHandleId(GetUnitRace(v)) .. " acquire " .. GetUnitAcquireRange(v))
  end
  for _, id in ipairs({ "zzzz", "phea", "LTlt", "AHhb" }) do
    out("make " .. id .. " " .. kind(make(id)))
  end
  out("make 0 " .. kind(CreateUnit(Player(0), 0, 0, 0, 0)))
  for _, face in ipairs({ 400, -90, 720, 359.9, -0.5, 1e9, 1e5, -1e5, 3600.5 }) do
    out("face " .. face .. " " .. GetUnitFacing(make("hfoo", nil, nil, face)))
  end
  local far = make("hfoo", 5000, -5000)
  out("far " .. GetUnitX(far) .. " " .. GetUnitY(far))
  local skin = BlzCreateUnitWithSkin(Player(0), FourCC("hfoo"), 0, 0, 0, FourCC("hkni"))
  out("skin " .. tostring(GetUnitTypeId(skin) == FourCC("hfoo")) .. " " .. tostring(BlzGetUnitSkin(skin) == FourCC("hkni")) .. " "
    .. show(GetUnitName(skin)) .. " " .. BlzGetUnitMaxHP(skin) .. " " .. BlzGetUnitRealField(skin, UNIT_RF_SCALING_VALUE))
  local corpse = CreateCorpse(Player(0), FourCC("hfoo"), 0, 0, 0)
  out("corpse " .. kind(corpse) .. " " .. tostring(GetUnitTypeId(corpse) == FourCC("hfoo")) .. " " .. GetWidgetLife(corpse) .. " "
    .. tostring(UnitAlive(corpse)) .. " " .. tostring(IsUnitType(corpse, UNIT_TYPE_DEAD)) .. " " .. BlzGetUnitMaxHP(corpse))
  for _, id in ipairs({ "ogru", "ugho", "earc", "ninf", "nmrl", "nmyr", "nvil", "nfrl", "ncop", "hbar", "nshe" }) do
    local v = make(id)
    out("race " .. id .. " " .. GetHandleId(GetUnitRace(v)) .. " turn " .. GetUnitDefaultTurnSpeed(v) .. " " .. GetUnitTurnSpeed(v))
  end
  out("hero id " .. tostring(IsHeroUnitId(FourCC("Hpal"))) .. " " .. tostring(IsHeroUnitId(FourCC("hfoo"))) .. " "
    .. tostring(IsHeroUnitId(FourCC("Hzzz"))) .. " " .. tostring(IsHeroUnitId(FourCC("Ozzz"))) .. " " .. tostring(IsHeroUnitId(FourCC("Nngs"))))
end)

part(0.1, "states", function()
  local u = make("hfoo")
  local function life(label) out(label .. " " .. GetWidgetLife(u) .. " " .. BlzGetUnitMaxHP(u) .. " " .. tostring(UnitAlive(u))) end
  SetWidgetLife(u, 100.5) life("life 100.5")
  SetWidgetLife(u, 500) life("life 500")
  SetUnitState(u, UNIT_STATE_LIFE, 200.25) life("state 200.25")
  SetUnitState(u, UNIT_STATE_MAX_LIFE, 600) life("max state 600")
  BlzSetUnitMaxHP(u, 150) life("max 150")
  BlzSetUnitMaxHP(u, 600) life("max 600")
  BlzSetUnitMaxHP(u, 0) life("max 0")
  BlzSetUnitMaxHP(u, -5) life("max -5")
  for _, v in ipairs({ 0.41, 0.405, 0.4049, 0.4, 1.1, 419.99 }) do
    for _, id in ipairs({ "hfoo", "Hpal" }) do
      local w = make(id)
      SetWidgetLife(w, v)
      out("edge " .. id .. " " .. v .. " " .. GetWidgetLife(w) .. " " .. tostring(UnitAlive(w)))
    end
  end
  local r = make("hfoo")
  SetWidgetLife(r, 0.5)
  BlzSetUnitMaxHP(r, 1000)
  out("kept 0.5 " .. GetWidgetLife(r))
  local d = make("hfoo")
  KillUnit(d)
  SetWidgetLife(d, 100)
  out("dead set " .. GetWidgetLife(d) .. " " .. tostring(UnitAlive(d)))
  BlzSetUnitMaxHP(d, 50)
  out("dead max " .. BlzGetUnitMaxHP(d) .. " " .. GetWidgetLife(d))
  SetWidgetLife(d, -5)
  out("dead -5 " .. GetWidgetLife(d) .. " " .. tostring(UnitAlive(d)))
  local m = make("hfoo")
  SetUnitState(m, UNIT_STATE_MANA, 10)
  out("no mana " .. GetUnitState(m, UNIT_STATE_MANA) .. " " .. GetUnitState(m, UNIT_STATE_MAX_MANA))
  BlzSetUnitMaxMana(m, 50)
  out("max mana 50 " .. GetUnitState(m, UNIT_STATE_MANA) .. " " .. BlzGetUnitMaxMana(m))
  SetUnitState(m, UNIT_STATE_MANA, 70)
  out("mana 70 " .. GetUnitState(m, UNIT_STATE_MANA))
  SetUnitState(m, UNIT_STATE_MANA, 20.5)
  BlzSetUnitMaxMana(m, 10)
  out("max mana 10 " .. GetUnitState(m, UNIT_STATE_MANA) .. " " .. BlzGetUnitMaxMana(m))
  SetUnitState(m, UNIT_STATE_MANA, -5)
  out("mana -5 " .. GetUnitState(m, UNIT_STATE_MANA))
  SetUnitState(m, UNIT_STATE_MAX_MANA, 90)
  out("max mana state 90 " .. BlzGetUnitMaxMana(m))
  local h = make("Hpal")
  SetUnitState(h, UNIT_STATE_MANA, 300)
  out("Hpal mana 300 " .. GetUnitState(h, UNIT_STATE_MANA))
  SetUnitState(h, UNIT_STATE_MANA, 100.5)
  out("Hpal mana 100.5 " .. GetUnitState(h, UNIT_STATE_MANA))
end)

part(0.15, "flags", function()
  local u = make("hfoo")
  out("pause " .. tostring(IsUnitPaused(u)))
  PauseUnit(u, true) PauseUnit(u, true) PauseUnit(u, false)
  out("pause twice then once " .. tostring(IsUnitPaused(u)))
  BlzPauseUnitEx(u, true)
  out("ex once " .. tostring(IsUnitPaused(u)))
  BlzPauseUnitEx(u, true) BlzPauseUnitEx(u, false)
  out("ex twice then once " .. tostring(IsUnitPaused(u)))
  BlzPauseUnitEx(u, false)
  out("ex again " .. tostring(IsUnitPaused(u)))
  BlzPauseUnitEx(u, true) PauseUnit(u, false)
  out("ex then pause false " .. tostring(IsUnitPaused(u)))
  out("hidden " .. tostring(IsUnitHidden(u)))
  ShowUnit(u, false)
  out("hide " .. tostring(IsUnitHidden(u)) .. " " .. GetUnitX(u) .. " " .. GetUnitY(u) .. " " .. GetWidgetLife(u))
  local far = make("hfoo", 900, 1300)
  ShowUnit(far, false)
  out("hidden at " .. GetUnitX(far) .. " " .. GetUnitY(far))
  ShowUnit(u, true)
  out("show " .. tostring(IsUnitHidden(u)))
  out("invulnerable " .. tostring(BlzIsUnitInvulnerable(u)))
  SetUnitInvulnerable(u, true)
  out("set invulnerable " .. tostring(BlzIsUnitInvulnerable(u)))
  SetUnitInvulnerable(u, false)
  out("cleared invulnerable " .. tostring(BlzIsUnitInvulnerable(u)))
  SetUnitUserData(u, 42)
  out("data " .. GetUnitUserData(u))
  BlzSetUnitName(u, "Renamed")
  out("name " .. GetUnitName(u) .. " " .. GetObjectName(FourCC("hfoo")) .. " " .. GetUnitName(make("hfoo")))
  SetUnitScale(u, 2.5, 1, 1)
  SetUnitVertexColor(u, 300, -50, 100, 128)
  SetUnitColor(u, ConvertPlayerColor(3))
  out("scale " .. BlzGetUnitRealField(u, UNIT_RF_SCALING_VALUE) .. " tint " .. BlzGetUnitIntegerField(u, UNIT_IF_TINTING_COLOR_RED) .. " "
    .. BlzGetUnitIntegerField(u, UNIT_IF_TINTING_COLOR_GREEN) .. " " .. BlzGetUnitIntegerField(u, UNIT_IF_TINTING_COLOR_BLUE) .. " "
    .. BlzGetUnitIntegerField(u, UNIT_IF_TINTING_COLOR_ALPHA))
  SetUnitX(u, 5096.5) SetUnitY(u, -300.25)
  out("moved " .. GetUnitX(u) .. " " .. GetUnitY(u) .. " " .. tostring(UnitAlive(u)))
  SetUnitMoveSpeed(u, 1000) local fast = GetUnitMoveSpeed(u)
  SetUnitMoveSpeed(u, 5) local slow = GetUnitMoveSpeed(u)
  SetUnitMoveSpeed(u, -10)
  out("speed " .. fast .. " " .. slow .. " " .. GetUnitMoveSpeed(u) .. " " .. GetUnitDefaultMoveSpeed(u))
  SetUnitTurnSpeed(u, 5) local turn = GetUnitTurnSpeed(u)
  SetUnitTurnSpeed(u, -1)
  out("turn " .. turn .. " " .. GetUnitTurnSpeed(u) .. " " .. GetUnitDefaultTurnSpeed(u))
  local ranges = {}
  for _, v in ipairs({ 0, 7.5, 50, 55, 60, 64, 70, 80, 88, 89, 90, 91, 100 }) do
    local w = make("hfoo")
    SetUnitAcquireRange(w, v)
    local b = make("hfoo")
    BlzSetUnitRealField(b, UNIT_RF_ACQUISITION_RANGE, v)
    ranges[#ranges + 1] = v .. " " .. GetUnitAcquireRange(w) .. " " .. GetUnitAcquireRange(b)
  end
  out("acquire ranges " .. table.concat(ranges, ", "))
  ranges = {}
  for _, id in ipairs({ "hrif", "hbar", "nshe" }) do
    for _, v in ipairs({ 30, 100, 300, 499, 600 }) do
      local w = make(id)
      SetUnitAcquireRange(w, v)
      ranges[#ranges + 1] = v .. " " .. GetUnitAcquireRange(w)
    end
    out("acquire " .. id .. " " .. GetUnitDefaultAcquireRange(make(id)) .. ": " .. table.concat(ranges, ", "))
    ranges = {}
  end
  SetUnitTurnSpeed(u, 0) local zero = GetUnitTurnSpeed(u)
  SetUnitTurnSpeed(u, 7) out("turn 0 " .. zero .. " 7 " .. GetUnitTurnSpeed(u))
  SetUnitAcquireRange(u, 300.5) local acquire = GetUnitAcquireRange(u)
  SetUnitAcquireRange(u, -5)
  out("acquire " .. acquire .. " " .. GetUnitAcquireRange(u) .. " " .. GetUnitDefaultAcquireRange(u))
  SetUnitPropWindow(u, 1)
  out("prop " .. GetUnitPropWindow(u) .. " " .. GetUnitDefaultPropWindow(u))
  SetUnitFlyHeight(u, 200, 0)
  out("fly " .. GetUnitFlyHeight(u) .. " " .. GetUnitDefaultFlyHeight(u) .. " " .. BlzGetUnitZ(u))
  local types = {}
  for _, id in ipairs({ "hfoo", "Hpal", "hbar", "hgry", "hpea", "hkni", "nshe", "ugho", "hmtm", "otau", "ugol", "ewsp" }) do
    local v = make(id)
    local bits = {}
    for t = 0, 26 do bits[#bits + 1] = IsUnitType(v, ConvertUnitType(t)) and "1" or "0" end
    out("types " .. id .. " " .. table.concat(bits))
  end
  local d = make("hfoo")
  KillUnit(d)
  local bits = {}
  for t = 0, 26 do bits[#bits + 1] = IsUnitType(d, ConvertUnitType(t)) and "1" or "0" end
  out("types dead " .. table.concat(bits))
end)

part(0.2, "names", function()
  for _, id in ipairs({ "hfoo", "Hpal", "phea", "AHhb", "Rhme", "Bdef", "BHbd", "LTlt", "YTlb", "ZPfw", "zzzz" }) do
    out("object " .. id .. " [" .. show(GetObjectName(FourCC(id))) .. "]")
  end
  out("object 0 [" .. show(GetObjectName(0)) .. "]")
end)

part(0.3, "death", function()
  local log = {}
  local function note(s) log[#log + 1] = s end
  local u = make("hfoo")
  local function respond(label)
    return function()
      note(label .. " " .. GetHandleId(GetTriggerEventId()) .. " trig " .. tostring(GetTriggerUnit() == u) .. " widget "
        .. kind(GetTriggerWidget()) .. " " .. tostring(GetTriggerWidget() == u) .. " dying " .. tostring(GetDyingUnit() == u)
        .. " killer " .. show(GetKillingUnit()) .. " player " .. pid(GetTriggerPlayer()) .. " life " .. GetWidgetLife(u)
        .. " alive " .. tostring(UnitAlive(u)))
    end
  end
  local a = CreateTrigger() TriggerRegisterUnitEvent(a, u, EVENT_UNIT_DEATH) TriggerAddAction(a, respond("unit"))
  local b = CreateTrigger()
  TriggerRegisterPlayerUnitEvent(b, Player(0), EVENT_PLAYER_UNIT_DEATH, Filter(function()
    note("filter " .. tostring(GetFilterUnit() == u) .. " " .. tostring(GetTriggerUnit() == u) .. " " .. pid(GetTriggerPlayer()))
    return true
  end))
  TriggerAddAction(b, respond("player"))
  local c = CreateTrigger() TriggerRegisterDeathEvent(c, u) TriggerAddAction(c, respond("widget"))
  local e = CreateTrigger() TriggerRegisterUnitEvent(e, u, EVENT_UNIT_DEATH) TriggerAddAction(e, respond("unit again"))
  local f = CreateTrigger() TriggerRegisterPlayerUnitEvent(f, Player(1), EVENT_PLAYER_UNIT_DEATH, nil) TriggerAddAction(f, respond("other player"))
  local g = CreateTrigger() TriggerRegisterPlayerUnitEvent(g, Player(0), EVENT_PLAYER_UNIT_DEATH, Filter(function() return false end))
  TriggerAddAction(g, respond("refused"))
  note("kill")
  KillUnit(u)
  note("killed " .. GetWidgetLife(u) .. " " .. tostring(UnitAlive(u)))
  KillUnit(u)
  note("again")
  for _, line in ipairs(log) do out(line) end
  log = {}
  local w = make("hfoo")
  local t = CreateTrigger() TriggerRegisterUnitEvent(t, w, EVENT_UNIT_DEATH)
  TriggerAddAction(t, function() note("life death " .. GetWidgetLife(w) .. " " .. tostring(UnitAlive(w))) end)
  SetWidgetLife(w, 0.3)
  note("after " .. GetWidgetLife(w))
  local x = make("hfoo")
  local tx = CreateTrigger() TriggerRegisterUnitEvent(tx, x, EVENT_UNIT_DEATH)
  TriggerAddAction(tx, function() note("state death " .. GetWidgetLife(x)) end)
  SetUnitState(x, UNIT_STATE_LIFE, -5)
  local y = make("hfoo")
  local ty = CreateTrigger() TriggerRegisterUnitEvent(ty, y, EVENT_UNIT_DEATH)
  TriggerAddAction(ty, function() note("max death " .. GetWidgetLife(y)) end)
  BlzSetUnitMaxHP(y, 0)
  note("max 0 " .. GetWidgetLife(y) .. " " .. tostring(UnitAlive(y)))
  for _, line in ipairs(log) do out(line) end
end)

part(0.4, "decay", function()
  local u = make("hfoo")
  local t = CreateTrigger()
  TriggerRegisterUnitEvent(t, u, EVENT_UNIT_DECAY)
  TriggerRegisterPlayerUnitEvent(t, Player(0), EVENT_PLAYER_UNIT_DECAY, nil)
  TriggerAddAction(t, function()
    out("decay " .. now() .. " " .. GetHandleId(GetTriggerEventId()) .. " " .. tostring(GetDecayingUnit() == u) .. " "
      .. tostring(GetTriggerUnit() == u) .. " " .. pid(GetTriggerPlayer()) .. " " .. GetWidgetLife(u))
  end)
  out("killed at " .. now())
  KillUnit(u)
end)

part(0.45, "flier", function()
  local g = make("hgry")
  BlzSetUnitRealField(g, UNIT_RF_FLY_HEIGHT, 100)
  out("flier field " .. GetUnitFlyHeight(g) .. " " .. BlzGetUnitRealField(g, UNIT_RF_FLY_HEIGHT) .. " " .. BlzGetUnitRealField(g, UNIT_RF_FLY_MAX_HEIGHT))
  BlzSetUnitRealField(g, UNIT_RF_FLY_MAX_HEIGHT, 50)
  out("flier max " .. GetUnitFlyHeight(g) .. " " .. GetUnitDefaultFlyHeight(g) .. " " .. BlzGetUnitRealField(g, UNIT_RF_FLY_MAX_HEIGHT))
  local b = make("hbar")
  SetUnitMoveSpeed(b, 200)
  BlzSetUnitRealField(b, UNIT_RF_SPEED, 300)
  out("building speed " .. GetUnitMoveSpeed(b) .. " " .. BlzGetUnitRealField(b, UNIT_RF_SPEED))
  local h = make("Hpal")
  BlzSetUnitIntegerField(h, UNIT_IF_PRIMARY_ATTRIBUTE, 2)
  out("primary " .. BlzGetUnitIntegerField(h, UNIT_IF_PRIMARY_ATTRIBUTE))
  BlzSetUnitIntegerField(h, UNIT_IF_AGILITY_PERMANENT, 0)
  out("permanent 0 " .. BlzGetUnitIntegerField(h, UNIT_IF_AGILITY_PERMANENT) .. " " .. BlzGetUnitIntegerField(h, UNIT_IF_AGILITY))
  BlzSetUnitIntegerField(h, UNIT_IF_STRENGTH, 30)
  out("strength 30 " .. GetWidgetLife(h) .. " " .. BlzGetUnitMaxHP(h) .. " " .. BlzGetUnitIntegerField(h, UNIT_IF_STRENGTH_WITH_BONUS))
  BlzSetUnitIntegerField(h, UNIT_IF_LEVEL, 3)
  out("hero level 3 " .. GetUnitLevel(h) .. " " .. BlzGetUnitIntegerField(h, UNIT_IF_LEVEL))
end)

part(0.5, "owner", function()
  local log = {}
  local function note(s) log[#log + 1] = s end
  local u = make("hfoo")
  local o = CreateTrigger()
  TriggerRegisterUnitEvent(o, u, EVENT_UNIT_ISSUED_ORDER)
  TriggerRegisterPlayerUnitEvent(o, Player(0), EVENT_PLAYER_UNIT_ISSUED_ORDER, nil)
  TriggerRegisterPlayerUnitEvent(o, Player(1), EVENT_PLAYER_UNIT_ISSUED_ORDER, nil)
  TriggerAddAction(o, function()
    note("order " .. GetHandleId(GetTriggerEventId()) .. " " .. GetIssuedOrderId() .. " " .. tostring(GetOrderedUnit() == u) .. " "
      .. tostring(GetTriggerUnit() == u) .. " owner " .. pid(GetOwningPlayer(u)) .. " player " .. pid(GetTriggerPlayer()))
  end)
  local c = CreateTrigger()
  TriggerRegisterUnitEvent(c, u, EVENT_UNIT_CHANGE_OWNER)
  TriggerRegisterPlayerUnitEvent(c, Player(0), EVENT_PLAYER_UNIT_CHANGE_OWNER, nil)
  TriggerRegisterPlayerUnitEvent(c, Player(1), EVENT_PLAYER_UNIT_CHANGE_OWNER, nil)
  TriggerAddAction(c, function()
    note("change " .. GetHandleId(GetTriggerEventId()) .. " " .. tostring(GetChangingUnit() == u) .. " " .. tostring(GetTriggerUnit() == u)
      .. " prev " .. pid(GetChangingUnitPrevOwner()) .. " owner " .. pid(GetOwningPlayer(u)) .. " player " .. pid(GetTriggerPlayer()))
  end)
  SetUnitOwner(u, Player(1), true)
  note("given " .. pid(GetOwningPlayer(u)) .. " " .. tostring(IsUnitOwnedByPlayer(u, Player(1))))
  SetUnitOwner(u, Player(1), false)
  note("given again " .. pid(GetOwningPlayer(u)))
  SetUnitOwner(u, Player(0), false)
  note("given back " .. pid(GetOwningPlayer(u)))
  local d = make("hfoo")
  KillUnit(d)
  local td = CreateTrigger()
  TriggerRegisterUnitEvent(td, d, EVENT_UNIT_ISSUED_ORDER)
  TriggerRegisterUnitEvent(td, d, EVENT_UNIT_CHANGE_OWNER)
  TriggerAddAction(td, function() note("dead " .. GetHandleId(GetTriggerEventId())) end)
  SetUnitOwner(d, Player(2), true)
  note("dead given " .. pid(GetOwningPlayer(d)))
  for _, line in ipairs(log) do out(line) end
end)

part(0.6, "remove", function()
  local u = make("hfoo", 1700, 1700)
  local d = make("hfoo")
  KillUnit(d)
  local t = CreateTrigger()
  TriggerRegisterUnitEvent(t, u, EVENT_UNIT_DEATH)
  TriggerRegisterDeathEvent(t, u)
  TriggerAddAction(t, function() out("removed death") end)
  RemoveUnit(u)
  RemoveUnit(d)
  out("removed " .. tostring(GetUnitTypeId(u) == FourCC("hfoo")) .. " " .. GetWidgetLife(u) .. " " .. tostring(UnitAlive(u)) .. " "
    .. pid(GetOwningPlayer(u)) .. " " .. GetUnitX(u) .. " dead " .. tostring(GetUnitTypeId(d) == FourCC("hfoo")) .. " " .. tostring(UnitAlive(d)))
  KillUnit(u)
  SetUnitOwner(u, Player(1), true)
  out("removed then " .. pid(GetOwningPlayer(u)) .. " " .. GetWidgetLife(u))
  TimerStart(CreateTimer(), 0, false, function()
    out("removed later " .. GetUnitTypeId(u) .. " " .. GetWidgetLife(u) .. " " .. tostring(UnitAlive(u)) .. " " .. pid(GetOwningPlayer(u))
      .. " " .. GetUnitX(u) .. " " .. show(GetUnitName(u)) .. " " .. GetUnitTypeId(d) .. " " .. kind(u))
  end)
end)

-- The unit field natives on a footman, a Paladin, a barracks and a gryphon rider, each read then set on a fresh unit
local fields = {
  integer = { "AGILITY", "AGILITY_PERMANENT", "AGILITY_WITH_BONUS", "ARMOR_TYPE", "DEFENSE_TYPE", "ELEVATION_SAMPLE_POINTS",
    "FORMATION_RANK", "GOLD_BOUNTY_AWARDED_BASE", "GOLD_BOUNTY_AWARDED_NUMBER_OF_DICE", "GOLD_BOUNTY_AWARDED_SIDES_PER_DIE",
    "HIT_POINTS_REGENERATION_TYPE", "INTELLIGENCE", "INTELLIGENCE_PERMANENT", "INTELLIGENCE_WITH_BONUS", "LEVEL",
    "LOOPING_FADE_IN_RATE", "LOOPING_FADE_OUT_RATE", "LUMBER_BOUNTY_AWARDED_BASE", "LUMBER_BOUNTY_AWARDED_NUMBER_OF_DICE",
    "LUMBER_BOUNTY_AWARDED_SIDES_PER_DIE", "MOVE_TYPE", "ORIENTATION_INTERPOLATION", "PLACEMENT_PREVENTED_BY",
    "PRIMARY_ATTRIBUTE", "STRENGTH", "STRENGTH_PERMANENT", "STRENGTH_WITH_BONUS", "TARGETED_AS", "TINTING_COLOR_ALPHA",
    "TINTING_COLOR_BLUE", "TINTING_COLOR_GREEN", "TINTING_COLOR_RED", "UNIT_CLASSIFICATION" },
  real = { "ACQUISITION_RANGE", "AGILITY_PER_LEVEL", "ANIMATION_RUN_SPEED", "ANIMATION_WALK_SPEED", "CAST_BACK_SWING",
    "CAST_POINT", "DEATH_TIME", "DEFENSE", "ELEVATION_SAMPLE_RADIUS", "FLY_HEIGHT", "FLY_MAX_HEIGHT",
    "FOG_OF_WAR_SAMPLE_RADIUS", "HIT_POINTS_REGENERATION_RATE", "HP", "INTELLIGENCE_PER_LEVEL", "MANA", "MANA_REGENERATION",
    "MAXIMUM_PITCH_ANGLE_DEGREES", "MAXIMUM_ROLL_ANGLE_DEGREES", "MINIMUM_ATTACK_RANGE", "OCCLUDER_HEIGHT", "PRIORITY",
    "SCALING_VALUE", "SELECTION_CIRCLE_HEIGHT", "SELECTION_SCALE", "SHADOW_IMAGE_CENTER_X", "SHADOW_IMAGE_CENTER_Y",
    "SHADOW_IMAGE_HEIGHT", "SHADOW_IMAGE_WIDTH", "SIGHT_RADIUS", "SPEED", "STRENGTH_PER_LEVEL", "TURN_RATE" },
  boolean = { "DECAYABLE", "FORCE_DISPLAY_HP", "HAS_WATER_SHADOW", "HERO_HIDE_HERO_DEATH_MESSAGE",
    "HERO_HIDE_HERO_INTERFACE_ICON", "HERO_HIDE_HERO_MINIMAP_DISPLAY", "HIDE_MINIMAP_DISPLAY", "IS_A_BUILDING",
    "NEUTRAL_BUILDING_SHOWS_MINIMAP_ICON", "RAISABLE", "SCALE_PROJECTILES", "SELECTION_CIRCLE_ON_WATER", "SHOW_AIR_TO_GROUND",
    "USE_EXTENDED_LINE_OF_SIGHT" },
  string = { "GROUND_TEXTURE", "NAME", "PROPER_NAMES", "SHADOW_IMAGE_UNIT" },
}
local access = {
  integer = { "IF", BlzGetUnitIntegerField, BlzSetUnitIntegerField, 7 },
  real = { "RF", BlzGetUnitRealField, BlzSetUnitRealField, 7.5 },
  boolean = { "BF", BlzGetUnitBooleanField, BlzSetUnitBooleanField },
  string = { "SF", BlzGetUnitStringField, BlzSetUnitStringField, "Probe" },
}

-- Kept in a hero's attribute data: on another unit, reading one closes the game (probe units-attribute)
local attributes = { AGILITY = true, AGILITY_PERMANENT = true, AGILITY_WITH_BONUS = true, INTELLIGENCE = true,
  INTELLIGENCE_PERMANENT = true, INTELLIGENCE_WITH_BONUS = true, STRENGTH = true, STRENGTH_PERMANENT = true,
  STRENGTH_WITH_BONUS = true }

local at = 1.0
for _, kind in ipairs({ "integer", "real", "boolean", "string" }) do
  local prefix, get, set, value = table.unpack(access[kind])
  for _, name in ipairs(fields[kind]) do
    part(at, "field " .. name, function()
      local field = _G["UNIT_" .. prefix .. "_" .. name]
      local read = {}
      for _, id in ipairs(attributes[name] and { "Hpal" } or { "hfoo", "Hpal", "hbar", "hgry" }) do
        local v = make(id)
        local text = tostring(get(v, field))
        -- a hero's proper name is drawn at random from its type's list, and numbered once taken (heroes, W2-3)
        if name == "PROPER_NAMES" and id == "Hpal" then text = text ~= "" and "named" or "" end
        if kind == "string" then out("get " .. id .. " [" .. text:sub(1, 200) .. "]") else read[#read + 1] = id .. " " .. text end
        RemoveUnit(v)
      end
      if #read > 0 then out("get " .. table.concat(read, " ")) end
      local wrote = {}
      for _, id in ipairs(attributes[name] and { "Hpal" } or { "hfoo", "Hpal", "hbar" }) do
        local v = make(id)
        local new = value
        if kind == "boolean" then new = not get(v, field) end
        wrote[#wrote + 1] = id .. " " .. tostring(set(v, field, new)) .. " " .. tostring(get(v, field))
        RemoveUnit(v)
      end
      out("set " .. table.concat(wrote, " "))
    end)
    at = at + 0.02
  end
end

part(3.5, "links", function()
  local function fresh() return make("hfoo") end
  local u = fresh() BlzSetUnitRealField(u, UNIT_RF_SPEED, 100) out("RF_SPEED " .. GetUnitMoveSpeed(u))
  u = fresh() BlzSetUnitRealField(u, UNIT_RF_TURN_RATE, 0.3) out("RF_TURN_RATE " .. GetUnitTurnSpeed(u))
  u = fresh() BlzSetUnitRealField(u, UNIT_RF_ACQUISITION_RANGE, 300) out("RF_ACQUISITION_RANGE " .. GetUnitAcquireRange(u))
  u = fresh() BlzSetUnitRealField(u, UNIT_RF_FLY_HEIGHT, 50) out("RF_FLY_HEIGHT " .. GetUnitFlyHeight(u))
  u = fresh() BlzSetUnitRealField(u, UNIT_RF_HP, 100) out("RF_HP " .. GetWidgetLife(u) .. " " .. BlzGetUnitMaxHP(u))
  u = fresh() BlzSetUnitMaxMana(u, 50) BlzSetUnitRealField(u, UNIT_RF_MANA, 20) out("RF_MANA " .. GetUnitState(u, UNIT_STATE_MANA) .. " " .. BlzGetUnitMaxMana(u))
  u = fresh() BlzSetUnitStringField(u, UNIT_SF_NAME, "Field Name") out("SF_NAME " .. GetUnitName(u))
  u = fresh() BlzSetUnitIntegerField(u, UNIT_IF_LEVEL, 5) out("IF_LEVEL " .. GetUnitLevel(u))
  u = fresh() BlzSetUnitRealField(u, UNIT_RF_DEATH_TIME, 0.5) out("RF_DEATH_TIME " .. BlzGetUnitRealField(u, UNIT_RF_DEATH_TIME))
  u = fresh() SetUnitMoveSpeed(u, 200) out("speed " .. BlzGetUnitRealField(u, UNIT_RF_SPEED))
  u = fresh() SetUnitTurnSpeed(u, 0.2) out("turn " .. BlzGetUnitRealField(u, UNIT_RF_TURN_RATE))
  u = fresh() SetUnitAcquireRange(u, 250) out("acquire " .. BlzGetUnitRealField(u, UNIT_RF_ACQUISITION_RANGE))
  u = fresh() SetUnitFlyHeight(u, 60, 0) out("fly " .. BlzGetUnitRealField(u, UNIT_RF_FLY_HEIGHT))
  u = fresh() SetWidgetLife(u, 200) out("life " .. BlzGetUnitRealField(u, UNIT_RF_HP))
  u = fresh() BlzSetUnitMaxHP(u, 500) out("max life " .. BlzGetUnitRealField(u, UNIT_RF_HP) .. " " .. GetWidgetLife(u))
  u = fresh() BlzSetUnitName(u, "Named") out("name " .. BlzGetUnitStringField(u, UNIT_SF_NAME))
  u = fresh() SetUnitScale(u, 2, 1, 1) out("scale " .. BlzGetUnitRealField(u, UNIT_RF_SCALING_VALUE))
  u = make("Hpal") out("hero str " .. BlzGetUnitIntegerField(u, UNIT_IF_STRENGTH) .. " " .. BlzGetUnitIntegerField(u, UNIT_IF_STRENGTH_WITH_BONUS)
    .. " level " .. GetUnitLevel(u) .. " " .. BlzGetUnitIntegerField(u, UNIT_IF_LEVEL))
end)

part(3.6, "turning", function()
  local runs = {
    { "hfoo", 180 }, { "hfoo", 90 }, { "hkni", 180 }, { "hgry", 180 }, { "hfoo", -90 }, { "hfoo slow", 180 },
  }
  for i, run in ipairs(runs) do
    local u = make(run[1]:sub(1, 4), 200 * i, 0, 0)
    if run[1] == "hfoo slow" then SetUnitTurnSpeed(u, 0.2) end
    SetUnitFacing(u, run[2])
    local reads = { string.format("%.1f", GetUnitFacing(u)) }
    local tick = CreateTimer()
    TimerStart(tick, 0.0625, true, guard(function()
      reads[#reads + 1] = string.format("%.1f", GetUnitFacing(u))
      if #reads == 17 then
        PauseTimer(tick)
        out("turn " .. run[1] .. " " .. run[2] .. " " .. table.concat(reads, " "))
      end
    end))
  end
  local e = make("hfoo")
  BlzSetUnitFacingEx(e, 123.5)
  out("facing ex " .. GetUnitFacing(e))
  BlzSetUnitFacingEx(e, -30)
  out("facing ex -30 " .. GetUnitFacing(e))
  local g = make("hgry")
  SetUnitFlyHeight(g, 100, 0)
  local first = GetUnitFlyHeight(g)
  SetUnitFlyHeight(g, 300, 100)
  local heights = { first, GetUnitFlyHeight(g) }
  local tick = CreateTimer()
  TimerStart(tick, 0.5, true, guard(function()
    heights[#heights + 1] = GetUnitFlyHeight(g)
    if #heights == 6 then
      PauseTimer(tick)
      out("fly rate " .. table.concat(heights, " "))
    end
  end))
end)
