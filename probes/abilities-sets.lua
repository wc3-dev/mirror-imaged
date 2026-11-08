-- More of a unit's abilities (W2-7), on the probe map (custom_v1's data): a column set through another type's field,
-- string sets out of range, which plain fields take a set, the level count set lower, cooldown percent, the abilities
-- every kind of unit starts with in their order, and an upgrade raising an ability's level.
local function show(v) return v == nil and "nil" or tostring(v) end
local function name(id) return id == 0 and "0" or string.pack(">I4", id) end
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
local function with(id)
  local u = make("hfoo")
  UnitAddAbility(u, FourCC(id))
  return u, BlzGetUnitAbility(u, FourCC(id))
end
local function lv(u, id) return GetUnitAbilityLevel(u, FourCC(id)) end
local function list(u)
  local ids = {}
  for i = 0, 20 do
    local a = BlzGetUnitAbilityByIndex(u, i)
    if a == nil then break end
    ids[#ids + 1] = name(BlzGetAbilityId(a))
  end
  return table.concat(ids, ",")
end

TimerStart(CreateTimer(), 3.0, false, guard(function() done() end))

part(0.1, "plain sets", function()
  local u, a = with("AHbz")
  local ints = { ABILITY_IF_LEVELS, ABILITY_IF_REQUIRED_LEVEL, ABILITY_IF_LEVEL_SKIP_REQUIREMENT, ABILITY_IF_PRIORITY,
    ABILITY_IF_BUTTON_POSITION_NORMAL_X, ABILITY_IF_BUTTON_POSITION_NORMAL_Y, ABILITY_IF_BUTTON_POSITION_ACTIVATED_X,
    ABILITY_IF_BUTTON_POSITION_ACTIVATED_Y, ABILITY_IF_BUTTON_POSITION_RESEARCH_X, ABILITY_IF_BUTTON_POSITION_RESEARCH_Y,
    ABILITY_IF_MISSILE_SPEED, ABILITY_IF_TARGET_ATTACHMENTS, ABILITY_IF_CASTER_ATTACHMENTS }
  local r = {}
  for _, f in ipairs(ints) do r[#r + 1] = show(BlzSetAbilityIntegerField(a, f, 2)) .. BlzGetAbilityIntegerField(a, f) end
  out("ints " .. table.concat(r, " "))
  out("bools " .. show(BlzSetAbilityBooleanField(a, ABILITY_BF_CHECK_DEPENDENCIES, false)) .. show(BlzGetAbilityBooleanField(a, ABILITY_BF_CHECK_DEPENDENCIES))
    .. " " .. show(BlzSetAbilityBooleanField(a, ABILITY_BF_HERO_ABILITY, true)) .. " " .. show(BlzSetAbilityBooleanField(a, ABILITY_BF_ITEM_ABILITY, false)) .. show(BlzGetAbilityBooleanField(a, ABILITY_BF_ITEM_ABILITY)))
  local s = {}
  for _, f in ipairs({ ABILITY_SF_NAME, ABILITY_SF_ICON_ACTIVATED, ABILITY_SF_ICON_RESEARCH, ABILITY_SF_EFFECT_SOUND, ABILITY_SF_EFFECT_SOUND_LOOPING }) do
    s[#s + 1] = show(BlzSetAbilityStringField(a, f, "s")) .. BlzGetAbilityStringField(a, f)
  end
  out("strings " .. table.concat(s, " "))
  out("other types " .. show(BlzSetAbilityIntegerField(a, ConvertAbilityIntegerField(FourCC("amac")), 2)) .. " " .. BlzGetAbilityIntegerField(a, ConvertAbilityIntegerField(FourCC("amac")))
    .. " " .. show(BlzSetAbilityIntegerField(a, ConvertAbilityIntegerField(FourCC("acdn")), 2)) .. " " .. BlzGetAbilityIntegerField(a, ConvertAbilityIntegerField(FourCC("anam")))
    .. " " .. show(BlzSetAbilityIntegerField(a, ConvertAbilityIntegerField(FourCC("zzzz")), 2)))
  local v, b = with("AHbz")
  SetUnitAbilityLevel(v, FourCC("AHbz"), 3)
  out("levels lower " .. show(BlzSetAbilityIntegerField(b, ABILITY_IF_LEVELS, 1)) .. " " .. lv(v, "AHbz") .. " inc " .. IncUnitAbilityLevel(v, FourCC("AHbz"))
    .. " set " .. SetUnitAbilityLevel(v, FourCC("AHbz"), 3) .. " dec " .. DecUnitAbilityLevel(v, FourCC("AHbz")) .. " field " .. BlzGetAbilityIntegerLevelField(b, ABILITY_ILF_NUMBER_OF_WAVES, 2))
  local w, c = with("AHbz")
  BlzSetAbilityIntegerField(c, ABILITY_IF_LEVELS, 5)
  out("levels 5 " .. SetUnitAbilityLevel(w, FourCC("AHbz"), 9) .. " field " .. BlzGetAbilityIntegerLevelField(c, ABILITY_ILF_NUMBER_OF_WAVES, 4) .. " "
    .. show(BlzSetAbilityIntegerLevelField(c, ABILITY_ILF_NUMBER_OF_WAVES, 4, 3)) .. " " .. BlzGetAbilityIntegerLevelField(c, ABILITY_ILF_NUMBER_OF_WAVES, 4)
    .. " cooldown " .. BlzGetUnitAbilityCooldown(w, FourCC("AHbz"), 4) .. " tip " .. BlzGetAbilityStringLevelField(c, ABILITY_SLF_TOOLTIP_NORMAL, 4))
  local x = make("hfoo")
  UnitAddAbility(x, FourCC("AHds"))
  BlzStartUnitAbilityCooldown(x, FourCC("AHds"), 8)
  BlzSetUnitAbilityCooldownPercent(x, FourCC("AHds"), 0.25)
  out("percent 0.25 " .. BlzGetUnitAbilityCooldownRemaining(x, FourCC("AHds")) .. " " .. BlzGetUnitAbilityCooldownPercent(x, FourCC("AHds")))
  BlzSetUnitAbilityCooldownPercent(x, FourCC("AHds"), 1.5)
  out("percent 1.5 " .. BlzGetUnitAbilityCooldownRemaining(x, FourCC("AHds")) .. " " .. BlzGetUnitAbilityCooldownPercent(x, FourCC("AHds")))
  BlzStartUnitAbilityCooldown(x, FourCC("AHds"), 8)
  BlzAdjustUnitAbilityCooldownRemaining(x, FourCC("AHds"), -10)
  out("adjusted below 0 " .. BlzGetUnitAbilityCooldownRemaining(x, FourCC("AHds")) .. " " .. BlzGetUnitAbilityCooldownPercent(x, FourCC("AHds")))
  BlzStartUnitAbilityCooldown(x, FourCC("AHds"), 8)
  BlzStartUnitAbilityCooldown(x, FourCC("AHds"), 2)
  out("started again " .. BlzGetUnitAbilityCooldownRemaining(x, FourCC("AHds")) .. " " .. BlzGetUnitAbilityCooldownPercent(x, FourCC("AHds")))
  local y = make("hfoo")
  UnitAddAbility(y, FourCC("AHds"))
  BlzSetUnitAbilityCooldownPercent(y, FourCC("AHds"), 0.5)
  BlzAdjustUnitAbilityCooldownPercent(y, FourCC("AHds"), 0.5)
  BlzAdjustUnitAbilityCooldownRemaining(y, FourCC("AHds"), 3)
  out("idle " .. BlzGetUnitAbilityCooldownRemaining(y, FourCC("AHds")) .. " " .. BlzGetUnitAbilityCooldownPercent(y, FourCC("AHds")))
end)
