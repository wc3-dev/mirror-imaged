-- A unit's ability fields where a column is shared (W2-7), on the probe map (custom_v1's data): a field set through a
-- field of another type on the same column, string level sets out of range, fields with lists, missile and
-- attachment fields, and Defend's extended tooltip on this balance set. Setting a boolean level field closes the game
-- (probe abilities-boolean), so it is only read here.
local function show(v) return v == nil and "nil" or tostring(v) end
local spot = 0
local function make(id)
  spot = spot % 225 + 1
  return CreateUnit(Player(0), FourCC(id), (spot % 15) * 200 - 1400, (spot // 15) * 200 - 1400, 0)
end
local function with(id)
  local u = make("hfoo")
  UnitAddAbility(u, FourCC(id))
  return u, BlzGetUnitAbility(u, FourCC(id))
end

local R, I, B, S = BlzGetAbilityRealLevelField, BlzGetAbilityIntegerLevelField, BlzGetAbilityBooleanLevelField, BlzGetAbilityStringLevelField
local _, hb = with("AHhb")
local _, bz = with("AHbz")
local _, ds = with("AHds")
out("int on real " .. show(BlzSetAbilityIntegerLevelField(hb, ABILITY_ILF_NUMBER_OF_WAVES, 0, 9)) .. " " .. I(hb, ABILITY_ILF_NUMBER_OF_WAVES, 0) .. " " .. I(hb, ABILITY_ILF_NUMBER_OF_WAVES, 1))
out("then real " .. R(hb, ABILITY_RLF_AMOUNT_HEALED_DAMAGED_HHB1, 0) .. " " .. R(hb, ABILITY_RLF_AMOUNT_HEALED_DAMAGED_HHB1, 1) .. " Htb1 " .. R(hb, ABILITY_RLF_DAMAGE_HTB1, 0))
out("real back " .. show(BlzSetAbilityRealLevelField(hb, ABILITY_RLF_AMOUNT_HEALED_DAMAGED_HHB1, 0, 7.5)) .. " " .. R(hb, ABILITY_RLF_AMOUNT_HEALED_DAMAGED_HHB1, 0) .. " int " .. I(hb, ABILITY_ILF_NUMBER_OF_WAVES, 0))
out("real on int " .. show(BlzSetAbilityRealLevelField(bz, ABILITY_RLF_AMOUNT_HEALED_DAMAGED_HHB1, 0, 2.5)) .. " " .. R(bz, ABILITY_RLF_AMOUNT_HEALED_DAMAGED_HHB1, 0))
out("then int " .. I(bz, ABILITY_ILF_NUMBER_OF_WAVES, 0) .. " Hbz2 " .. R(bz, ABILITY_RLF_DAMAGE_HBZ2, 0))
out("string past " .. show(S(bz, ABILITY_SLF_TOOLTIP_NORMAL, 0)))
out("string past set " .. show(BlzSetAbilityStringLevelField(bz, ABILITY_SLF_TOOLTIP_NORMAL, 5, "five")))
out("string past read " .. S(bz, ABILITY_SLF_TOOLTIP_NORMAL, 0) .. " | " .. S(bz, ABILITY_SLF_TOOLTIP_NORMAL, 2))
out("string 2 " .. show(BlzSetAbilityStringLevelField(bz, ABILITY_SLF_TOOLTIP_NORMAL, 2, "two")) .. " " .. S(bz, ABILITY_SLF_TOOLTIP_NORMAL, 2) .. " | " .. S(bz, ABILITY_SLF_TOOLTIP_NORMAL, 3) .. " | " .. S(bz, ABILITY_SLF_TOOLTIP_NORMAL, -1))
out("string -1 " .. show(BlzSetAbilityStringLevelField(bz, ABILITY_SLF_TOOLTIP_NORMAL, -1, "minus")) .. " " .. S(bz, ABILITY_SLF_TOOLTIP_NORMAL, 0) .. " | " .. S(bz, ABILITY_SLF_TOOLTIP_NORMAL, 2))
out("string off " .. show(BlzSetAbilityStringLevelField(bz, ABILITY_SLF_TOOLTIP_TURN_OFF, 0, "off")) .. " " .. S(bz, ABILITY_SLF_TOOLTIP_TURN_OFF, 0))
out("string effect " .. show(BlzSetAbilityStringLevelField(bz, ABILITY_SLF_EFFECT, 0, "e.mdl")) .. " " .. S(bz, ABILITY_SLF_EFFECT, 0))
for _, f in ipairs({ "areq", "arqa", "ata0", "alig", "aani" }) do
  out(f .. " " .. show(BlzGetAbilityStringLevelField(bz, ConvertAbilityStringLevelField(FourCC(f)), 0)))
  out(f .. " array " .. show(BlzGetAbilityStringLevelArrayField(bz, ConvertAbilityStringLevelArrayField(FourCC(f)), 0, 0)) .. " "
    .. BlzGetAbilityIntegerLevelArrayField(bz, ConvertAbilityIntegerLevelArrayField(FourCC(f)), 0, 0))
end
for _, id in ipairs({ "AHtb", "Aroc", "Asp2", "Asph" }) do
  local _, a = with(id)
  out(id .. " " .. BlzGetAbilityIntegerField(a, ABILITY_IF_MISSILE_SPEED) .. " " .. BlzGetAbilityRealField(a, ABILITY_RF_ARF_MISSILE_ARC) .. " "
    .. BlzGetAbilityIntegerField(a, ABILITY_IF_TARGET_ATTACHMENTS) .. " " .. BlzGetAbilityIntegerField(a, ABILITY_IF_CASTER_ATTACHMENTS))
end
out("anam " .. show(BlzSetAbilityStringLevelField(bz, ConvertAbilityStringLevelField(FourCC("anam")), 0, "n")))
out("Adef extended " .. #BlzGetAbilityExtendedTooltip(FourCC("Adef"), 0) .. " research " .. #BlzGetAbilityResearchExtendedTooltip(FourCC("AHhb"), 0))
out("bool read " .. show(B(ds, ABILITY_BLF_CAN_DEACTIVATE, 0)) .. " " .. show(B(ds, ABILITY_BLF_CAN_DEACTIVATE, 1)))
done()
