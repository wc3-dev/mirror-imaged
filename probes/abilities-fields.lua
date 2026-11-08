-- The ability field natives on a unit's ability (W2-7), on the probe map (custom_v1's data): each kind of field read
-- and set on Holy Light, Blizzard, Storm Bolt and Defend, at levels from -1 past the last, a field of another ability
-- and a field read as another type; the level array fields; and the unit's cooldown and mana cost natives, with a
-- cooldown started without a cast.
local function show(v) return v == nil and "nil" or tostring(v) end
local function cut(s) return s == nil and "nil" or (#s .. ":" .. s:sub(1, 40)) end
local spot = 0
local function make(id)
  spot = spot % 225 + 1
  return CreateUnit(Player(0), FourCC(id), (spot % 15) * 200 - 1400, (spot // 15) * 200 - 1400, 0)
end
local function part(at, label, f)
  TimerStart(CreateTimer(), at, false, function()
    out(label)
    local ok, err = pcall(f)
    if not ok then out(label .. " error " .. tostring(err)) end
  end)
end
local function levels(get, ab, field)
  local r = {}
  for l = -1, 4 do r[#r + 1] = show(get(ab, field, l)) end
  return table.concat(r, " ")
end
local function with(id)
  local u = make("hfoo")
  UnitAddAbility(u, FourCC(id))
  return u, BlzGetUnitAbility(u, FourCC(id))
end

TimerStart(CreateTimer(), 3.0, false, guard(function() done() end))

part(0.05, "plain", function()
  for _, id in ipairs({ "AHhb", "AHbz", "Adef", "AHds", "Aatk" }) do
    local _, a = with(id)
    if id == "Adef" or id == "Aatk" then a = BlzGetUnitAbility(make("hfoo"), FourCC(id)) end
    local ints = {}
    for _, f in ipairs({ ABILITY_IF_LEVELS, ABILITY_IF_REQUIRED_LEVEL, ABILITY_IF_LEVEL_SKIP_REQUIREMENT, ABILITY_IF_PRIORITY,
      ABILITY_IF_BUTTON_POSITION_NORMAL_X, ABILITY_IF_BUTTON_POSITION_NORMAL_Y, ABILITY_IF_BUTTON_POSITION_ACTIVATED_X,
      ABILITY_IF_BUTTON_POSITION_ACTIVATED_Y, ABILITY_IF_BUTTON_POSITION_RESEARCH_X, ABILITY_IF_BUTTON_POSITION_RESEARCH_Y,
      ABILITY_IF_MISSILE_SPEED, ABILITY_IF_TARGET_ATTACHMENTS, ABILITY_IF_CASTER_ATTACHMENTS }) do
      ints[#ints + 1] = BlzGetAbilityIntegerField(a, f)
    end
    out(id .. " int " .. table.concat(ints, " ") .. " real " .. BlzGetAbilityRealField(a, ABILITY_RF_ARF_MISSILE_ARC) .. " bool "
      .. show(BlzGetAbilityBooleanField(a, ABILITY_BF_HERO_ABILITY)) .. " " .. show(BlzGetAbilityBooleanField(a, ABILITY_BF_ITEM_ABILITY))
      .. " " .. show(BlzGetAbilityBooleanField(a, ABILITY_BF_CHECK_DEPENDENCIES)))
    out(id .. " name " .. show(BlzGetAbilityStringField(a, ABILITY_SF_NAME)) .. " sound " .. show(BlzGetAbilityStringField(a, ABILITY_SF_EFFECT_SOUND))
      .. " " .. show(BlzGetAbilityStringField(a, ABILITY_SF_EFFECT_SOUND_LOOPING)))
    out(id .. " icons " .. show(BlzGetAbilityStringField(a, ABILITY_SF_ICON_ACTIVATED)) .. " " .. show(BlzGetAbilityStringField(a, ABILITY_SF_ICON_RESEARCH)))
  end
  out("nil " .. BlzGetAbilityIntegerField(nil, ABILITY_IF_LEVELS) .. " " .. show(BlzGetAbilityStringField(nil, ABILITY_SF_NAME)) .. " "
    .. BlzGetAbilityIntegerLevelField(nil, ABILITY_ILF_MANA_COST, 0))
end)

part(0.1, "levels", function()
  local _, hb = with("AHhb")
  local I, R, B, S = BlzGetAbilityIntegerLevelField, BlzGetAbilityRealLevelField, BlzGetAbilityBooleanLevelField, BlzGetAbilityStringLevelField
  out("mana " .. levels(I, hb, ABILITY_ILF_MANA_COST))
  out("cooldown " .. levels(R, hb, ABILITY_RLF_COOLDOWN))
  out("range " .. levels(R, hb, ABILITY_RLF_CAST_RANGE) .. " area " .. levels(R, hb, ABILITY_RLF_AREA_OF_EFFECT))
  out("duration " .. levels(R, hb, ABILITY_RLF_DURATION_NORMAL) .. " hero " .. levels(R, hb, ABILITY_RLF_DURATION_HERO))
  out("casting " .. levels(R, hb, ABILITY_RLF_CASTING_TIME))
  out("Hhb1 " .. levels(R, hb, ABILITY_RLF_AMOUNT_HEALED_DAMAGED_HHB1))
  out("Htb1 " .. levels(R, hb, ABILITY_RLF_DAMAGE_HTB1) .. " Hbz1 " .. levels(I, hb, ABILITY_ILF_NUMBER_OF_WAVES))
  out("Hds1 " .. levels(B, hb, ABILITY_BLF_CAN_DEACTIVATE) .. " Def1 " .. levels(R, hb, ABILITY_RLF_DAMAGE_TAKEN_PERCENT_DEF1))
  out("acdn int " .. levels(I, hb, ConvertAbilityIntegerLevelField(FourCC("acdn"))) .. " amcs real " .. levels(R, hb, ConvertAbilityRealLevelField(FourCC("amcs"))))
  out("Hhb1 int " .. levels(I, hb, ConvertAbilityIntegerLevelField(FourCC("Hhb1"))) .. " anam level " .. show(S(hb, ConvertAbilityStringLevelField(FourCC("anam")), 0)))
  out("atar " .. levels(I, hb, ConvertAbilityIntegerLevelField(FourCC("atar"))) .. " zzzz " .. levels(R, hb, ConvertAbilityRealLevelField(FourCC("zzzz"))))
  out("tip " .. cut(S(hb, ABILITY_SLF_TOOLTIP_NORMAL, 0)) .. " | " .. cut(S(hb, ABILITY_SLF_TOOLTIP_NORMAL, 2)))
  out("tip -1 " .. cut(S(hb, ABILITY_SLF_TOOLTIP_NORMAL, -1)) .. " | 3 " .. cut(S(hb, ABILITY_SLF_TOOLTIP_NORMAL, 3)))
  out("uber " .. cut(S(hb, ABILITY_SLF_TOOLTIP_NORMAL_EXTENDED, 0)) .. " | 3 " .. cut(S(hb, ABILITY_SLF_TOOLTIP_NORMAL_EXTENDED, 3)))
  out("learn " .. cut(S(hb, ABILITY_SLF_TOOLTIP_LEARN, 0)) .. " | 1 " .. cut(S(hb, ABILITY_SLF_TOOLTIP_LEARN, 1)))
  out("learn uber " .. cut(S(hb, ABILITY_SLF_TOOLTIP_LEARN_EXTENDED, 0)) .. " | off " .. cut(S(hb, ABILITY_SLF_TOOLTIP_TURN_OFF, 0)))
  out("icon " .. cut(S(hb, ABILITY_SLF_ICON_NORMAL, 0)) .. " | 1 " .. cut(S(hb, ABILITY_SLF_ICON_NORMAL, 1)))
  out("target " .. cut(S(hb, ABILITY_SLF_TARGET, 0)) .. " | effect " .. cut(S(hb, ABILITY_SLF_EFFECT, 0)))
  local _, bz = with("AHbz")
  out("Hbz1 " .. levels(I, bz, ABILITY_ILF_NUMBER_OF_WAVES) .. " real " .. levels(R, bz, ConvertAbilityRealLevelField(FourCC("Hbz1"))))
  out("Hbz2 " .. levels(R, bz, ABILITY_RLF_DAMAGE_HBZ2) .. " Hhb1 " .. levels(R, bz, ABILITY_RLF_AMOUNT_HEALED_DAMAGED_HHB1))
  local _, ds = with("AHds")
  out("Hds1 " .. levels(B, ds, ABILITY_BLF_CAN_DEACTIVATE) .. " off " .. cut(S(ds, ABILITY_SLF_TOOLTIP_TURN_OFF, 0)))
  local df = BlzGetUnitAbility(make("hfoo"), FourCC("Adef"))
  out("Def1 " .. levels(R, df, ABILITY_RLF_DAMAGE_TAKEN_PERCENT_DEF1) .. " Def2 " .. levels(R, df, ABILITY_RLF_DAMAGE_DEALT_PERCENT_DEF2))
end)

part(0.15, "arrays", function()
  local _, bz = with("AHbz")
  for _, f in ipairs({ "abuf", "aeff", "atar", "aeat", "Hbz1" }) do
    local s, i, r, b = {}, {}, {}, {}
    for k = -1, 2 do
      s[#s + 1] = show(BlzGetAbilityStringLevelArrayField(bz, ConvertAbilityStringLevelArrayField(FourCC(f)), 0, k))
      i[#i + 1] = show(BlzGetAbilityIntegerLevelArrayField(bz, ConvertAbilityIntegerLevelArrayField(FourCC(f)), 0, k))
      r[#r + 1] = show(BlzGetAbilityRealLevelArrayField(bz, ConvertAbilityRealLevelArrayField(FourCC(f)), 0, k))
      b[#b + 1] = show(BlzGetAbilityBooleanLevelArrayField(bz, ConvertAbilityBooleanLevelArrayField(FourCC(f)), 0, k))
    end
    out(f .. " " .. table.concat(s, ",") .. " | " .. table.concat(i, ",") .. " | " .. table.concat(r, ",") .. " | " .. table.concat(b, ","))
  end
  local buf = ConvertAbilityStringLevelArrayField(FourCC("abuf"))
  out("set " .. show(BlzSetAbilityStringLevelArrayField(bz, buf, 0, 0, "Bslo")) .. " " .. show(BlzGetAbilityStringLevelArrayField(bz, buf, 0, 0))
    .. " " .. show(BlzGetAbilityStringLevelArrayField(bz, buf, 0, 1)) .. " level 1 " .. show(BlzGetAbilityStringLevelArrayField(bz, buf, 1, 0)))
  out("set index 3 " .. show(BlzSetAbilityStringLevelArrayField(bz, buf, 0, 3, "Bslo")) .. " " .. show(BlzGetAbilityStringLevelArrayField(bz, buf, 0, 2))
    .. " " .. show(BlzGetAbilityStringLevelArrayField(bz, buf, 0, 3)))
  out("add " .. show(BlzAddAbilityStringLevelArrayField(bz, buf, 0, "BHds")) .. " " .. show(BlzGetAbilityStringLevelArrayField(bz, buf, 0, 2))
    .. " " .. show(BlzGetAbilityStringLevelArrayField(bz, buf, 0, 3)))
  out("remove " .. show(BlzRemoveAbilityStringLevelArrayField(bz, buf, 0, "Bslo")) .. " " .. show(BlzGetAbilityStringLevelArrayField(bz, buf, 0, 0))
    .. " " .. show(BlzGetAbilityStringLevelArrayField(bz, buf, 0, 1)) .. " " .. show(BlzGetAbilityStringLevelArrayField(bz, buf, 0, 2)))
  out("remove missing " .. show(BlzRemoveAbilityStringLevelArrayField(bz, buf, 0, "Bzzz")) .. " past " .. show(BlzSetAbilityStringLevelArrayField(bz, buf, 5, 0, "Bslo")))
  local ibuf = ConvertAbilityIntegerLevelArrayField(FourCC("abuf"))
  out("int set " .. show(BlzSetAbilityIntegerLevelArrayField(bz, ibuf, 0, 0, FourCC("Bslo"))) .. " add " .. show(BlzAddAbilityIntegerLevelArrayField(bz, ibuf, 0, 7))
    .. " " .. show(BlzGetAbilityStringLevelArrayField(bz, buf, 0, 0)))
end)

part(0.2, "sets", function()
  local u, hb = with("AHhb")
  local v, other = with("AHhb")
  local R, I = BlzGetAbilityRealLevelField, BlzGetAbilityIntegerLevelField
  out("cooldown " .. show(BlzSetAbilityRealLevelField(hb, ABILITY_RLF_COOLDOWN, 0, 2.5)) .. " " .. levels(R, hb, ABILITY_RLF_COOLDOWN) .. " other " .. R(other, ABILITY_RLF_COOLDOWN, 0))
  out("cooldown past " .. show(BlzSetAbilityRealLevelField(hb, ABILITY_RLF_COOLDOWN, 3, 7)) .. " " .. show(BlzSetAbilityRealLevelField(hb, ABILITY_RLF_COOLDOWN, -1, 7)) .. " " .. levels(R, hb, ABILITY_RLF_COOLDOWN))
  out("mana " .. show(BlzSetAbilityIntegerLevelField(hb, ABILITY_ILF_MANA_COST, 1, 10)) .. " " .. levels(I, hb, ABILITY_ILF_MANA_COST) .. " unit " .. BlzGetUnitAbilityManaCost(u, FourCC("AHhb"), 1))
  out("Hhb1 " .. show(BlzSetAbilityRealLevelField(hb, ABILITY_RLF_AMOUNT_HEALED_DAMAGED_HHB1, 0, 1.5)) .. " " .. R(hb, ABILITY_RLF_AMOUNT_HEALED_DAMAGED_HHB1, 0))
  out("Htb1 " .. show(BlzSetAbilityRealLevelField(hb, ABILITY_RLF_DAMAGE_HTB1, 0, 5)) .. " " .. R(hb, ABILITY_RLF_DAMAGE_HTB1, 0) .. " Hhb1 " .. R(hb, ABILITY_RLF_AMOUNT_HEALED_DAMAGED_HHB1, 0))
  out("Hbz1 " .. show(BlzSetAbilityIntegerLevelField(hb, ABILITY_ILF_NUMBER_OF_WAVES, 0, 9)) .. " " .. I(hb, ABILITY_ILF_NUMBER_OF_WAVES, 0)
    .. " zzzz " .. show(BlzSetAbilityRealLevelField(hb, ConvertAbilityRealLevelField(FourCC("zzzz")), 0, 3)) .. " acdn int " .. show(BlzSetAbilityIntegerLevelField(hb, ConvertAbilityIntegerLevelField(FourCC("acdn")), 0, 4)) .. " " .. R(hb, ABILITY_RLF_COOLDOWN, 0))
  out("tip " .. show(BlzSetAbilityStringLevelField(hb, ABILITY_SLF_TOOLTIP_NORMAL, 0, "own tip")) .. " " .. show(BlzGetAbilityStringLevelField(hb, ABILITY_SLF_TOOLTIP_NORMAL, 0))
    .. " type " .. cut(BlzGetAbilityTooltip(FourCC("AHhb"), 0)) .. " other " .. cut(BlzGetAbilityStringLevelField(other, ABILITY_SLF_TOOLTIP_NORMAL, 0)))
  out("tip past " .. show(BlzSetAbilityStringLevelField(hb, ABILITY_SLF_TOOLTIP_NORMAL, 5, "five")) .. " " .. cut(BlzGetAbilityStringLevelField(hb, ABILITY_SLF_TOOLTIP_NORMAL, 5)))
  out("icon " .. show(BlzSetAbilityStringLevelField(hb, ABILITY_SLF_ICON_NORMAL, 0, "x.blp")) .. " " .. show(BlzGetAbilityStringLevelField(hb, ABILITY_SLF_ICON_NORMAL, 0)) .. " type " .. cut(BlzGetAbilityIcon(FourCC("AHhb"))))
  out("int " .. show(BlzSetAbilityIntegerField(hb, ABILITY_IF_BUTTON_POSITION_NORMAL_X, 3)) .. " " .. BlzGetAbilityIntegerField(hb, ABILITY_IF_BUTTON_POSITION_NORMAL_X)
    .. " type " .. BlzGetAbilityPosX(FourCC("AHhb")) .. " levels " .. show(BlzSetAbilityIntegerField(hb, ABILITY_IF_LEVELS, 5)) .. " " .. BlzGetAbilityIntegerField(hb, ABILITY_IF_LEVELS)
    .. " " .. GetUnitAbilityLevel(u, FourCC("AHhb")) .. " " .. SetUnitAbilityLevel(u, FourCC("AHhb"), 5))
  out("bool " .. show(BlzSetAbilityBooleanField(hb, ABILITY_BF_HERO_ABILITY, false)) .. " " .. show(BlzGetAbilityBooleanField(hb, ABILITY_BF_HERO_ABILITY))
    .. " item " .. show(BlzSetAbilityBooleanField(hb, ABILITY_BF_ITEM_ABILITY, true)) .. " " .. show(BlzGetAbilityBooleanField(hb, ABILITY_BF_ITEM_ABILITY)))
  out("name " .. show(BlzSetAbilityStringField(hb, ABILITY_SF_NAME, "Own Light")) .. " " .. show(BlzGetAbilityStringField(hb, ABILITY_SF_NAME))
    .. " " .. GetObjectName(FourCC("AHhb")) .. " real " .. show(BlzSetAbilityRealField(hb, ABILITY_RF_ARF_MISSILE_ARC, 0.5)) .. " " .. BlzGetAbilityRealField(hb, ABILITY_RF_ARF_MISSILE_ARC))
  out("level set " .. SetUnitAbilityLevel(u, FourCC("AHhb"), 2) .. " " .. R(hb, ABILITY_RLF_COOLDOWN, 0) .. " " .. I(hb, ABILITY_ILF_MANA_COST, 1))
  UnitRemoveAbility(u, FourCC("AHhb"))
  UnitAddAbility(u, FourCC("AHhb"))
  local again = BlzGetUnitAbility(u, FourCC("AHhb"))
  out("re-added " .. R(again, ABILITY_RLF_COOLDOWN, 0) .. " " .. I(again, ABILITY_ILF_MANA_COST, 1) .. " old " .. R(hb, ABILITY_RLF_COOLDOWN, 0) .. " "
    .. show(BlzSetAbilityRealLevelField(hb, ABILITY_RLF_COOLDOWN, 0, 1)))
  local df = BlzGetUnitAbility(make("hfoo"), FourCC("Adef"))
  out("Def1 " .. show(BlzSetAbilityRealLevelField(df, ABILITY_RLF_DAMAGE_TAKEN_PERCENT_DEF1, 0, 0.25)) .. " " .. R(df, ABILITY_RLF_DAMAGE_TAKEN_PERCENT_DEF1, 0)
    .. " Def2 past " .. show(BlzSetAbilityRealLevelField(df, ABILITY_RLF_DAMAGE_DEALT_PERCENT_DEF2, 1, 0.25)))
end)

part(0.25, "cooldowns", function()
  local u = make("hfoo")
  local hb = FourCC("AHhb")
  local function cds(f, a, b) local r = {} for l = -1, 3 do r[#r + 1] = show(f(a, b, l)) end return table.concat(r, " ") end
  local function ucds(f) local r = {} for l = -1, 3 do r[#r + 1] = show(f(u, hb, l)) end return table.concat(r, " ") end
  out("lacking " .. ucds(BlzGetUnitAbilityCooldown) .. " mana " .. ucds(BlzGetUnitAbilityManaCost))
  out("type " .. cds(function(_, id, l) return BlzGetAbilityCooldown(id, l) end, nil, hb) .. " mana " .. cds(function(_, id, l) return BlzGetAbilityManaCost(id, l) end, nil, hb))
  out("type Adef " .. BlzGetAbilityCooldown(FourCC("Adef"), 0) .. " zzzz " .. BlzGetAbilityCooldown(FourCC("zzzz"), 0) .. " " .. BlzGetAbilityManaCost(FourCC("zzzz"), 0))
  UnitAddAbility(u, hb)
  out("unit " .. ucds(BlzGetUnitAbilityCooldown) .. " mana " .. ucds(BlzGetUnitAbilityManaCost))
  BlzSetUnitAbilityCooldown(u, hb, 1, 7)
  BlzSetUnitAbilityManaCost(u, hb, 1, 12)
  BlzSetUnitAbilityCooldown(u, hb, 3, 7)
  out("set " .. ucds(BlzGetUnitAbilityCooldown) .. " mana " .. ucds(BlzGetUnitAbilityManaCost) .. " field " .. BlzGetAbilityRealLevelField(BlzGetUnitAbility(u, hb), ABILITY_RLF_COOLDOWN, 1)
    .. " type " .. BlzGetAbilityCooldown(hb, 1) .. " " .. BlzGetAbilityManaCost(hb, 1))
  out("remaining " .. BlzGetUnitAbilityCooldownRemaining(u, hb) .. " " .. BlzGetUnitAbilityCooldownRemaining(u, FourCC("AHtb")) .. " percent " .. BlzGetUnitAbilityCooldownPercent(u, hb))
  BlzStartUnitAbilityCooldown(u, hb, 4)
  out("started " .. BlzGetUnitAbilityCooldownRemaining(u, hb) .. " " .. BlzGetUnitAbilityCooldownPercent(u, hb))
  BlzStartUnitAbilityCooldown(u, FourCC("AHtb"), 4)
  out("lacking started " .. BlzGetUnitAbilityCooldownRemaining(u, FourCC("AHtb")) .. " add " .. show(UnitAddAbility(u, FourCC("AHtb"))) .. " " .. BlzGetUnitAbilityCooldownRemaining(u, FourCC("AHtb")))
  local v = make("hfoo")
  UnitAddAbility(v, FourCC("AHds"))
  BlzStartUnitAbilityCooldown(v, FourCC("AHds"), 10)
  BlzSetUnitAbilityCooldownRemaining(v, FourCC("AHds"), 2)
  out("set remaining " .. BlzGetUnitAbilityCooldownRemaining(v, FourCC("AHds")) .. " " .. BlzGetUnitAbilityCooldownPercent(v, FourCC("AHds")))
  local w = make("hfoo")
  UnitAddAbility(w, FourCC("AHds"))
  BlzSetUnitAbilityCooldownRemaining(w, FourCC("AHds"), 3)
  out("set idle " .. BlzGetUnitAbilityCooldownRemaining(w, FourCC("AHds")) .. " " .. BlzGetUnitAbilityCooldownPercent(w, FourCC("AHds")))
  TimerStart(CreateTimer(), 1.0, false, function()
    out("1 s " .. BlzGetUnitAbilityCooldownRemaining(u, hb) .. " " .. BlzGetUnitAbilityCooldownPercent(u, hb) .. " " .. BlzGetUnitAbilityCooldownRemaining(u, FourCC("AHtb"))
      .. " " .. BlzGetUnitAbilityCooldownRemaining(v, FourCC("AHds")) .. " " .. BlzGetUnitAbilityCooldownRemaining(w, FourCC("AHds")))
    BlzAdjustUnitAbilityCooldownRemaining(u, hb, 2)
    out("adjusted " .. BlzGetUnitAbilityCooldownRemaining(u, hb) .. " " .. BlzGetUnitAbilityCooldownPercent(u, hb))
    BlzSetUnitAbilityCooldownPercent(u, hb, 0.5)
    out("percent " .. BlzGetUnitAbilityCooldownRemaining(u, hb) .. " " .. BlzGetUnitAbilityCooldownPercent(u, hb))
    BlzAdjustUnitAbilityCooldownPercent(u, hb, 0.25)
    out("adjusted percent " .. BlzGetUnitAbilityCooldownRemaining(u, hb) .. " " .. BlzGetUnitAbilityCooldownPercent(u, hb))
    BlzEndUnitAbilityCooldown(u, hb)
    out("ended " .. BlzGetUnitAbilityCooldownRemaining(u, hb) .. " " .. BlzGetUnitAbilityCooldownPercent(u, hb))
    BlzStartUnitAbilityCooldown(u, hb, -1)
    out("negative " .. BlzGetUnitAbilityCooldownRemaining(u, hb))
    UnitRemoveAbility(v, FourCC("AHds"))
    UnitAddAbility(v, FourCC("AHds"))
    out("re-added " .. BlzGetUnitAbilityCooldownRemaining(v, FourCC("AHds")))
  end)
  TimerStart(CreateTimer(), 2.5, false, function()
    out("2.5 s " .. BlzGetUnitAbilityCooldownRemaining(w, FourCC("AHds")) .. " " .. BlzGetUnitAbilityCooldownRemaining(u, FourCC("AHtb")))
  end)
end)
