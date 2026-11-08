-- An ability type's tooltips, icons and button places (W2-7), on the probe map (custom_v1's data): read at levels from
-- -1 past the last, for an ability, a buff, a unit and an unknown id; set at levels in and past the ability's; and
-- whether a unit's ability, made before or after the set, reads the type's.
local function show(v) return v == nil and "nil" or tostring(v) end
local function cut(s) return s == nil and "nil" or (#s .. ":" .. s:sub(1, 24)) end
local function part(at, label, f)
  TimerStart(CreateTimer(), at, false, function()
    out(label)
    local ok, err = pcall(f)
    if not ok then out(label .. " error " .. tostring(err)) end
  end)
end
local function each(get, id, from, to)
  local r = {}
  for l = from or -1, to or 3 do r[#r + 1] = cut(get(FourCC(id), l)) end
  return table.concat(r, " | ")
end
local function one(get, id, from, to)
  local r = {}
  for l = from or -1, to or 3 do r[#r + 1] = show(get(FourCC(id), l)) end
  return table.concat(r, " | ")
end
local spot = 0
local function make(id)
  spot = spot % 225 + 1
  return CreateUnit(Player(0), FourCC(id), (spot % 15) * 200 - 1400, (spot // 15) * 200 - 1400, 0)
end

TimerStart(CreateTimer(), 2.0, false, guard(function() done() end))

part(0.05, "read", function()
  out("tip " .. one(BlzGetAbilityTooltip, "AHhb", -1, 4))
  out("uber " .. each(BlzGetAbilityExtendedTooltip, "AHhb", -1, 3))
  out("research " .. one(BlzGetAbilityResearchTooltip, "AHhb", -1, 2))
  out("research uber " .. each(BlzGetAbilityResearchExtendedTooltip, "AHhb", 0, 1))
  out("activated " .. one(BlzGetAbilityActivatedTooltip, "AHhb", 0, 0) .. " | " .. one(BlzGetAbilityActivatedTooltip, "AHds", -1, 3))
  out("activated uber " .. each(BlzGetAbilityActivatedExtendedTooltip, "AHhb", 0, 0) .. " | " .. each(BlzGetAbilityActivatedExtendedTooltip, "AHds", 0, 0))
  for _, id in ipairs({ "AHhb", "AHds", "Adef", "Aatk", "AHtb" }) do
    local c = FourCC(id)
    out(id .. " icons " .. show(BlzGetAbilityIcon(c)) .. " " .. show(BlzGetAbilityActivatedIcon(c)))
    out(id .. " places " .. BlzGetAbilityPosX(c) .. " " .. BlzGetAbilityPosY(c) .. " " .. BlzGetAbilityActivatedPosX(c) .. " " .. BlzGetAbilityActivatedPosY(c))
  end
  for _, id in ipairs({ "zzzz", "BHbd", "hfoo", "Rhme" }) do
    local c = FourCC(id)
    out(id .. " " .. cut(BlzGetAbilityTooltip(c, 0)) .. " | " .. cut(BlzGetAbilityExtendedTooltip(c, 0)) .. " | " .. cut(BlzGetAbilityResearchTooltip(c, 0))
      .. " | " .. cut(BlzGetAbilityActivatedTooltip(c, 0)))
    out(id .. " " .. cut(BlzGetAbilityIcon(c)) .. " | " .. cut(BlzGetAbilityActivatedIcon(c)) .. " | " .. BlzGetAbilityPosX(c) .. " " .. BlzGetAbilityActivatedPosY(c))
  end
  out("defend " .. one(BlzGetAbilityTooltip, "Adef", 0, 1) .. " | " .. one(BlzGetAbilityActivatedTooltip, "Adef", 0, 1))
  out("defend uber " .. #BlzGetAbilityExtendedTooltip(FourCC("Adef"), 0) .. " " .. #BlzGetAbilityActivatedExtendedTooltip(FourCC("Adef"), 0)
    .. " learn uber " .. #BlzGetAbilityResearchExtendedTooltip(FourCC("AHhb"), 0))
  local E, L = SOUND_TYPE_EFFECT, SOUND_TYPE_EFFECT_LOOPED
  out("sound " .. show(GetAbilitySoundById(FourCC("Arsp"), E)) .. " " .. show(GetAbilitySoundById(FourCC("Arsp"), L)) .. " " .. show(GetAbilitySoundById(FourCC("AHhb"), E))
    .. " " .. show(GetAbilitySoundById(FourCC("zzzz"), E)) .. " " .. show(GetAbilitySoundById(FourCC("Arsp"), nil)) .. " " .. show(GetAbilitySoundById(FourCC("Arsp"), ConvertSoundType(2))))
  out("sound by name " .. show(GetAbilitySound("Arsp", E)) .. " " .. show(GetAbilitySound("Stampede", E)) .. " " .. show(GetAbilitySound("arsp", E))
    .. " " .. show(GetAbilitySound("Bmlc", L)) .. " " .. show(GetAbilitySoundById(FourCC("Bmlc"), L)) .. " " .. show(GetAbilitySound("", E)))
  local u = make("hfoo")
  UnitAddAbility(u, FourCC("Arsp"))
  local a = BlzGetUnitAbility(u, FourCC("Arsp"))
  out("sound field " .. show(BlzGetAbilityStringField(a, ABILITY_SF_EFFECT_SOUND)) .. " " .. show(BlzGetAbilityStringField(a, ABILITY_SF_EFFECT_SOUND_LOOPING)))
  BlzSetAbilityStringField(a, ABILITY_SF_EFFECT_SOUND, "Own")
  out("sound field set " .. show(GetAbilitySoundById(FourCC("Arsp"), E)))
end)

part(0.1, "set", function()
  local before = make("hfoo")
  UnitAddAbility(before, FourCC("AHbz"))
  local b = BlzGetUnitAbility(before, FourCC("AHbz"))
  local bz = FourCC("AHbz")
  BlzSetAbilityTooltip(bz, "t0", 0)
  BlzSetAbilityTooltip(bz, "t2", 2)
  BlzSetAbilityTooltip(bz, "t5", 5)
  out("tip " .. one(BlzGetAbilityTooltip, "AHbz", -1, 5))
  BlzSetAbilityTooltip(bz, "tm", -1)
  out("tip -1 " .. one(BlzGetAbilityTooltip, "AHbz", -1, 3))
  BlzSetAbilityExtendedTooltip(bz, "e1", 1)
  BlzSetAbilityExtendedTooltip(bz, "e3", 3)
  out("uber " .. each(BlzGetAbilityExtendedTooltip, "AHbz", -1, 3))
  BlzSetAbilityResearchTooltip(bz, "r1 %d", 1)
  out("research " .. one(BlzGetAbilityResearchTooltip, "AHbz", -1, 3))
  BlzSetAbilityResearchExtendedTooltip(bz, "re0", 0)
  out("research uber " .. each(BlzGetAbilityResearchExtendedTooltip, "AHbz", -1, 3))
  BlzSetAbilityActivatedTooltip(FourCC("AHds"), "a1", 1)
  BlzSetAbilityActivatedTooltip(bz, "ab0", 0)
  BlzSetAbilityActivatedExtendedTooltip(FourCC("AHds"), "ae0", 0)
  out("activated " .. one(BlzGetAbilityActivatedTooltip, "AHds", -1, 3) .. " | " .. one(BlzGetAbilityActivatedTooltip, "AHbz", 0, 1)
    .. " | " .. one(BlzGetAbilityActivatedExtendedTooltip, "AHds", 0, 1))
  BlzSetAbilityIcon(bz, "icon.blp")
  BlzSetAbilityActivatedIcon(bz, "on.blp")
  BlzSetAbilityPosX(bz, 3)
  BlzSetAbilityPosY(bz, -1)
  BlzSetAbilityActivatedPosX(bz, 7)
  BlzSetAbilityActivatedPosY(bz, 2)
  out("icons " .. BlzGetAbilityIcon(bz) .. " " .. BlzGetAbilityActivatedIcon(bz) .. " places " .. BlzGetAbilityPosX(bz) .. " " .. BlzGetAbilityPosY(bz)
    .. " " .. BlzGetAbilityActivatedPosX(bz) .. " " .. BlzGetAbilityActivatedPosY(bz))
  local after = make("hfoo")
  UnitAddAbility(after, bz)
  local a = BlzGetUnitAbility(after, bz)
  local S, I = BlzGetAbilityStringLevelField, BlzGetAbilityIntegerField
  out("before " .. cut(S(b, ABILITY_SLF_TOOLTIP_NORMAL, 0)) .. " | " .. cut(S(b, ABILITY_SLF_ICON_NORMAL, 0)) .. " | " .. I(b, ABILITY_IF_BUTTON_POSITION_NORMAL_X)
    .. " | " .. cut(S(b, ABILITY_SLF_TOOLTIP_NORMAL_EXTENDED, 1)) .. " | " .. cut(S(b, ABILITY_SLF_TOOLTIP_LEARN, 0)) .. " | " .. cut(BlzGetAbilityStringField(b, ABILITY_SF_ICON_ACTIVATED)))
  out("after " .. cut(S(a, ABILITY_SLF_TOOLTIP_NORMAL, 0)) .. " | " .. cut(S(a, ABILITY_SLF_ICON_NORMAL, 0)) .. " | " .. I(a, ABILITY_IF_BUTTON_POSITION_NORMAL_X)
    .. " | " .. cut(S(a, ABILITY_SLF_TOOLTIP_NORMAL_EXTENDED, 1)) .. " | " .. cut(S(a, ABILITY_SLF_TOOLTIP_LEARN, 0)) .. " | " .. cut(BlzGetAbilityStringField(a, ABILITY_SF_ICON_ACTIVATED)))
  BlzSetAbilityStringLevelField(a, ABILITY_SLF_TOOLTIP_NORMAL, 1, "own")
  out("own " .. one(BlzGetAbilityTooltip, "AHbz", 0, 2))
  BlzSetAbilityTooltip(FourCC("zzzz"), "z", 0)
  BlzSetAbilityIcon(FourCC("zzzz"), "z.blp")
  BlzSetAbilityPosX(FourCC("zzzz"), 2)
  out("unknown " .. cut(BlzGetAbilityTooltip(FourCC("zzzz"), 0)) .. " " .. cut(BlzGetAbilityIcon(FourCC("zzzz"))) .. " " .. BlzGetAbilityPosX(FourCC("zzzz")))
  BlzSetAbilityTooltip(FourCC("Adef"), "d1", 1)
  out("defend " .. one(BlzGetAbilityTooltip, "Adef", 0, 1))
  BlzSetAbilityTooltip(bz, string.rep("x", 600), 1)
  out("long " .. #BlzGetAbilityTooltip(bz, 1))
  BlzSetAbilityTooltip(bz, "", 1)
  out("empty " .. #BlzGetAbilityTooltip(bz, 1))
end)
