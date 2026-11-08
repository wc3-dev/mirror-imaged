-- The hero natives on a footman, which is no hero (W2-3): the getters and the natives that change no attribute. The
-- ones that may change a hero's attributes are probes of their own (heroes-nonhero-level, heroes-nonhero-str,
-- heroes-nonhero-setstr), as reading a hero attribute field on a footman closes the game (probe units-attribute).
local function show(v) return v == nil and "nil" or tostring(v) end
local f = CreateUnit(Player(0), FourCC("hfoo"), 0, 0, 0)
out("get " .. GetHeroLevel(f) .. " " .. GetHeroXP(f) .. " " .. GetHeroSkillPoints(f) .. " " .. tostring(IsSuspendedXP(f)) .. " "
  .. show(GetHeroProperName(f)) .. " " .. GetUnitLevel(f))
SuspendHeroXP(f, true)
out("suspend " .. tostring(IsSuspendedXP(f)))
out("modify " .. tostring(UnitModifySkillPoints(f, 3)) .. " " .. GetHeroSkillPoints(f))
SelectHeroSkill(f, FourCC("AHhb"))
out("select " .. GetUnitAbilityLevel(f, FourCC("AHhb")))
BlzSetHeroProperName(f, "Probe Name")
out("name " .. show(GetHeroProperName(f)) .. " " .. show(GetUnitName(f)))
done()
