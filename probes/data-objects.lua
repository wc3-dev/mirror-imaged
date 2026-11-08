-- The data probe's map's object data (DataProbeMaps.Data): war3map.w3u renames hfoo "TRIGSTR_003" and hkni "Main
-- Knight" (war3mapSkin.w3u "Skin Knight") and makes h000 from hfoo. The map (info format 28, Default, not melee) reads
-- custom_v1: three units' life, and pierce damage to a footman (heavy armour) by the gameplay constants
-- (DamageBonusPierce 0.9 in the base file, 1.00 in custom_v1's).
local function show(value)
    return value == nil and "nil" or tostring(value)
end

out("hfoo " .. show(GetObjectName(FourCC("hfoo"))))
out("hkni " .. show(GetObjectName(FourCC("hkni"))))
out("h000 " .. show(GetObjectName(FourCC("h000"))))

for _, id in ipairs({ "oshm", "ebsh", "fapo" }) do
    local u = CreateUnit(Player(0), FourCC(id), 0, 0, 0)
    out(id .. " " .. BlzGetUnitMaxHP(u))
    RemoveUnit(u)
end

local a = CreateUnit(Player(0), FourCC("hfoo"), 0, 0, 0)
local b = CreateUnit(Player(PLAYER_NEUTRAL_AGGRESSIVE), FourCC("hfoo"), 300, 0, 0)
local before = GetWidgetLife(b)
UnitDamageTarget(a, b, 100, false, false, ATTACK_TYPE_PIERCE, DAMAGE_TYPE_NORMAL, WEAPON_TYPE_WHOKNOWS)
out("pierce " .. string.format("%.2f", before - GetWidgetLife(b)))
done()
