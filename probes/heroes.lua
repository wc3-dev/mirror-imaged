-- Heroes (W2-3), on the probe map (custom_v1's data): levels and what they give, the attributes (base, permanent and
-- with bonus) and what they give (life, mana and armour, at full life and below it), the attribute fields, skill
-- points and learning, revival, proper names, and the hero events and their responses. Each hero is made on a spot of
-- its own (a grid 200 apart), as the game moves a unit made on another.
local function show(v) return v == nil and "nil" or tostring(v) end
local function pid(p) return p == nil and "nil" or tostring(GetPlayerId(p)) end
local spot = 0
local function make(id, p)
  spot = spot % 225 + 1
  return CreateUnit(p or Player(0), FourCC(id), (spot % 15) * 200 - 1400, (spot // 15) * 200 - 1400, 0)
end
local function part(at, label, f)
  TimerStart(CreateTimer(), at, false, function()
    out(label)
    local ok, err = pcall(f)
    if not ok then out(label .. " error " .. tostring(err)) end
  end)
end
local function attrs(h)
  return GetHeroStr(h, false) .. " " .. GetHeroStr(h, true) .. " " .. GetHeroAgi(h, false) .. " " .. GetHeroAgi(h, true) .. " "
    .. GetHeroInt(h, false) .. " " .. GetHeroInt(h, true)
end
local function fields(h)
  return BlzGetUnitIntegerField(h, UNIT_IF_STRENGTH) .. " " .. BlzGetUnitIntegerField(h, UNIT_IF_STRENGTH_PERMANENT) .. " "
    .. BlzGetUnitIntegerField(h, UNIT_IF_STRENGTH_WITH_BONUS) .. " " .. BlzGetUnitIntegerField(h, UNIT_IF_AGILITY) .. " "
    .. BlzGetUnitIntegerField(h, UNIT_IF_AGILITY_PERMANENT) .. " " .. BlzGetUnitIntegerField(h, UNIT_IF_AGILITY_WITH_BONUS) .. " "
    .. BlzGetUnitIntegerField(h, UNIT_IF_INTELLIGENCE) .. " " .. BlzGetUnitIntegerField(h, UNIT_IF_INTELLIGENCE_PERMANENT) .. " "
    .. BlzGetUnitIntegerField(h, UNIT_IF_INTELLIGENCE_WITH_BONUS)
end
local function state(h)
  return GetWidgetLife(h) .. " " .. BlzGetUnitMaxHP(h) .. " " .. GetUnitState(h, UNIT_STATE_MANA) .. " " .. BlzGetUnitMaxMana(h)
    .. " " .. BlzGetUnitRealField(h, UNIT_RF_DEFENSE)
end
local function hero(label, h)
  out(label .. " level " .. GetHeroLevel(h) .. " " .. GetUnitLevel(h) .. " xp " .. GetHeroXP(h) .. " points " .. GetHeroSkillPoints(h)
    .. " attrs " .. attrs(h))
  out(label .. " fields " .. fields(h) .. " state " .. state(h))
end

TimerStart(CreateTimer(), 7.0, false, guard(function() done() end))

part(0.05, "start", function()
  hero("Hpal", make("Hpal"))
  hero("Hblm", make("Hblm"))
  hero("Obla", make("Obla"))
  local h = make("Hpal")
  out("suspended " .. tostring(IsSuspendedXP(h)))
  SuspendHeroXP(h, true)
  out("suspended " .. tostring(IsSuspendedXP(h)))
end)

part(0.1, "levels", function()
  local h = make("Hpal")
  for _, level in ipairs({ 2, 3, 5, 10 }) do
    SetHeroLevel(h, level, false)
    hero("level " .. level, h)
  end
  local d = make("Hpal")
  SetWidgetLife(d, 325)
  SetUnitState(d, UNIT_STATE_MANA, 100)
  SetHeroLevel(d, 4, false)
  hero("hurt level 4", d)
  local g = make("Hpal")
  BlzSetUnitRealField(g, UNIT_RF_STRENGTH_PER_LEVEL, 10)
  SetHeroLevel(g, 3, false)
  hero("gain 10 level 3", g)
end)

part(0.15, "set", function()
  local h = make("Hpal")
  SetHeroStr(h, 30, true) hero("str 30 true", h)
  SetHeroAgi(h, 20, true) hero("agi 20 true", h)
  SetHeroInt(h, 25, true) hero("int 25 true", h)
  SetHeroStr(h, 40, false) hero("str 40 false", h)
  SetHeroLevel(h, 2, false) hero("then level 2", h)
  SetHeroAgi(h, 5, false) hero("agi 5 false", h)
  SetHeroStr(h, 0, true) hero("str 0 true", h)
  SetHeroStr(h, -5, true) hero("str -5 true", h)
  local d = make("Hpal")
  SetWidgetLife(d, 325)
  SetUnitState(d, UNIT_STATE_MANA, 100)
  SetHeroStr(d, 32, true)
  SetHeroInt(d, 27, true)
  hero("hurt str 32 int 27", d)
  SetHeroStr(d, 12, true)
  SetHeroInt(d, 7, true)
  hero("hurt str 12 int 7", d)
end)

part(0.2, "fields", function()
  local h = make("Hpal")
  BlzSetUnitIntegerField(h, UNIT_IF_STRENGTH, 30) hero("IF_STRENGTH 30", h)
  BlzSetUnitIntegerField(h, UNIT_IF_STRENGTH_PERMANENT, 7) hero("IF_STRENGTH_PERMANENT 7", h)
  BlzSetUnitIntegerField(h, UNIT_IF_AGILITY_PERMANENT, 0) hero("IF_AGILITY_PERMANENT 0", h)
  SetHeroStr(h, 40, true) hero("then str 40 true", h)
  SetHeroLevel(h, 2, false) hero("then level 2", h)
  local p = make("Hpal")
  SetHeroStr(p, 40, false)
  BlzSetUnitIntegerField(p, UNIT_IF_STRENGTH_PERMANENT, 9) hero("str 40 false, then IF_STRENGTH_PERMANENT 9", p)
  local five = make("Hpal")
  SetHeroLevel(five, 5, false)
  BlzSetUnitIntegerField(five, UNIT_IF_LEVEL, 3) hero("level 5, IF_LEVEL 3", five)
  local one = make("Hpal")
  BlzSetUnitIntegerField(one, UNIT_IF_LEVEL, 3) hero("level 1, IF_LEVEL 3", one)
  BlzSetUnitIntegerField(one, UNIT_IF_LEVEL, 1) hero("then IF_LEVEL 1", one)
end)

part(0.25, "xp", function()
  local h = make("Hpal")
  local t = CreateTrigger()
  TriggerRegisterUnitEvent(t, h, EVENT_UNIT_HERO_LEVEL)
  TriggerRegisterPlayerUnitEvent(t, Player(0), EVENT_PLAYER_HERO_LEVEL, nil)
  TriggerAddAction(t, function()
    out("event " .. GetHandleId(GetTriggerEventId()) .. " " .. tostring(GetLevelingUnit() == h) .. " " .. tostring(GetTriggerUnit() == h)
      .. " " .. pid(GetTriggerPlayer()) .. " level " .. GetHeroLevel(h) .. " xp " .. GetHeroXP(h) .. " points " .. GetHeroSkillPoints(h)
      .. " str " .. GetHeroStr(h, false) .. " life " .. GetWidgetLife(h))
  end)
  SetHeroXP(h, 450, false) hero("xp 450", h)
  AddHeroXP(h, 100, false) hero("add 100", h)
  SuspendHeroXP(h, true)
  AddHeroXP(h, 1000, false) hero("suspended add 1000", h)
  SuspendHeroXP(h, false)
  UnitStripHeroLevel(h, 1) hero("strip 1", h)
  out("strip to 0 " .. tostring(UnitStripHeroLevel(h, 5)))
  hero("strip 5", h)
  out("level of nothing " .. GetHeroLevel(nil) .. " " .. GetHeroXP(nil) .. " " .. GetHeroSkillPoints(nil))
end)

part(0.3, "skills", function()
  local h = make("Hpal")
  local t = CreateTrigger()
  TriggerRegisterUnitEvent(t, h, EVENT_UNIT_HERO_SKILL)
  TriggerRegisterPlayerUnitEvent(t, Player(0), EVENT_PLAYER_HERO_SKILL, nil)
  TriggerAddAction(t, function()
    out("event " .. GetHandleId(GetTriggerEventId()) .. " " .. tostring(GetLearningUnit() == h) .. " " .. tostring(GetTriggerUnit() == h)
      .. " " .. pid(GetTriggerPlayer()) .. " skill " .. tostring(GetLearnedSkill() == FourCC("AHhb")) .. " " .. GetLearnedSkillLevel()
      .. " points " .. GetHeroSkillPoints(h))
  end)
  local function learn(label, id)
    SelectHeroSkill(h, FourCC(id))
    out(label .. " points " .. GetHeroSkillPoints(h) .. " AHhb " .. GetUnitAbilityLevel(h, FourCC("AHhb")) .. " AHre "
      .. GetUnitAbilityLevel(h, FourCC("AHre")) .. " learned " .. GetLearnedSkill() .. " " .. GetLearnedSkillLevel())
  end
  learn("ultimate at 1", "AHre")
  learn("AHhb at 1", "AHhb")
  learn("AHhb again, no point", "AHhb")
  SetHeroLevel(h, 2, false)
  learn("AHhb 2 at hero 2", "AHhb")
  SetHeroLevel(h, 3, false)
  learn("AHhb 2 at hero 3", "AHhb")
  learn("AHbz at hero 3", "AHbz")
  out("modify 3 " .. tostring(UnitModifySkillPoints(h, 3)) .. " " .. GetHeroSkillPoints(h))
  out("modify -10 " .. tostring(UnitModifySkillPoints(h, -10)) .. " " .. GetHeroSkillPoints(h))
  SetHeroLevel(h, 6, false)
  learn("ultimate at 6", "AHre")
  BlzResetUnitTalents(h)
  out("reset talents points " .. GetHeroSkillPoints(h) .. " AHhb " .. GetUnitAbilityLevel(h, FourCC("AHhb")))
end)

part(0.35, "revive", function()
  local h = make("Hpal")
  SetHeroInt(h, 30, true)
  SetHeroLevel(h, 3, false)
  local t = CreateTrigger()
  TriggerRegisterUnitEvent(t, h, EVENT_UNIT_HERO_REVIVE_START)
  TriggerRegisterUnitEvent(t, h, EVENT_UNIT_HERO_REVIVE_FINISH)
  TriggerRegisterUnitEvent(t, h, EVENT_UNIT_HERO_REVIVABLE)
  TriggerRegisterPlayerUnitEvent(t, Player(0), EVENT_PLAYER_HERO_REVIVE_START, nil)
  TriggerRegisterPlayerUnitEvent(t, Player(0), EVENT_PLAYER_HERO_REVIVE_FINISH, nil)
  TriggerRegisterPlayerUnitEvent(t, Player(0), EVENT_PLAYER_HERO_REVIVABLE, nil)
  TriggerAddAction(t, function()
    out("event " .. GetHandleId(GetTriggerEventId()) .. " " .. tostring(GetRevivingUnit() == h) .. " " .. tostring(GetRevivableUnit() == h)
      .. " " .. tostring(GetTriggerUnit() == h) .. " " .. pid(GetTriggerPlayer()) .. " alive " .. tostring(UnitAlive(h)) .. " "
      .. GetWidgetLife(h) .. " " .. GetUnitState(h, UNIT_STATE_MANA) .. " at " .. GetUnitX(h) .. " " .. GetUnitY(h))
  end)
  out("living " .. tostring(ReviveHero(h, 0, 0, false)))
  KillUnit(h)
  out("killed " .. tostring(UnitAlive(h)))
  out("revived " .. tostring(ReviveHero(h, 300.5, -400.25, false)))
  hero("revived", h)
  out("at " .. GetUnitX(h) .. " " .. GetUnitY(h) .. " facing " .. GetUnitFacing(h))
end)

part(0.4, "names", function()
  local seen, first = {}, {}
  local names = {}
  for i = 1, 16 do
    local h = make("Hpal")
    names[i] = GetHeroProperName(h)
  end
  local list = {}
  for n in ("Granis Darkhammer,Jorn the Redeemer,Sage Truthbearer,Malak the Avenger,Gavinrad the Dire,Morlune the Mighty,Agamand the True,Ballador the Bright,Manadar the Healer,Zann the Defender,Arius the Seeker,Aurrius the Pure,Karnwield the Seeker,Buzan the Fearless"):gmatch("[^,]+") do
    list[n] = true
  end
  local distinct, listed = 0, 0
  for i = 1, 14 do
    if not seen[names[i]] then distinct = distinct + 1 seen[names[i]] = true end
    if list[names[i]] then listed = listed + 1 end
  end
  out("first 14 distinct " .. distinct .. " listed " .. listed)
  for i = 15, 16 do
    local base, suffix = names[i]:match("^(.-) ([IVXL]+)$")
    out("then " .. i .. " " .. tostring(base ~= nil and list[base] == true) .. " " .. show(suffix))
  end
  local h = make("Hpal")
  BlzSetHeroProperName(h, "Probe Name")
  out("set " .. show(GetHeroProperName(h)) .. " " .. show(BlzGetUnitStringField(h, UNIT_SF_PROPER_NAMES)) .. " " .. show(GetUnitName(h)))
  BlzSetUnitStringField(h, UNIT_SF_PROPER_NAMES, "Field Name")
  out("field " .. show(GetHeroProperName(h)) .. " " .. show(BlzGetUnitStringField(h, UNIT_SF_PROPER_NAMES)))
  local b = make("Hblm")
  local base = GetHeroProperName(b)
  out("Hblm " .. tostring(base:match(" [IVXL]+$") == nil))
end)
