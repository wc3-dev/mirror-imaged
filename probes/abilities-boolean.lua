-- A unit's ability's boolean level field set (W2-7). On Divine Shield's can-deactivate it closed the game inside the
-- set (abilities-columns' first runs, as the facts' drop-items-on-death did on a Paladin's inventory). Run with a
-- progress file (11-08): past the levels and for a field common.j doesn't name it answers false; on Holy Light, whose
-- column holds a real, true; then reading it back closes the game.
local u = CreateUnit(Player(0), FourCC("hfoo"), 0, 0, 0)
UnitAddAbility(u, FourCC("AHhb"))
UnitAddAbility(u, FourCC("AHds"))
local ds, hb = BlzGetUnitAbility(u, FourCC("AHds")), BlzGetUnitAbility(u, FourCC("AHhb"))
out("past level " .. tostring(BlzSetAbilityBooleanLevelField(ds, ABILITY_BLF_CAN_DEACTIVATE, 9, true)))
out("other field " .. tostring(BlzSetAbilityBooleanLevelField(ds, ConvertAbilityBooleanLevelField(FourCC("zzzz")), 0, true)))
out("real column " .. tostring(BlzSetAbilityBooleanLevelField(hb, ABILITY_BLF_CAN_DEACTIVATE, 0, true)))
out("read " .. tostring(BlzGetAbilityBooleanLevelField(hb, ABILITY_BLF_CAN_DEACTIVATE, 0)))
done()
