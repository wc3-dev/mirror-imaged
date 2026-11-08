-- Which balance set a map reads (facts, "Object data"), on the probe map with the melee flag (DataProbeMaps.Melee):
-- the max life of three units whose life differs between the base tables (oshm 350, ebsh 1000, fapo 400), custom_v0
-- (260, none, none), custom_v1 (335, 1000, none) and melee_v0 (355, none, none); the names of an item and an ability
-- only the base tables have (ckdk, AA12) beside two every set has (phea, AHhb) and the unit fapo, and whether they can
-- be made or added; and pierce damage to a footman (heavy armour) by the gameplay constants (DamageBonusPierce 0.9 in
-- the base file, 1.00 in custom_v1's).
for _, id in ipairs({ "oshm", "ebsh", "fapo" }) do
    local u = CreateUnit(Player(0), FourCC(id), 0, 0, 0)
    out(id .. " " .. BlzGetUnitMaxHP(u))
    RemoveUnit(u)
end
for _, id in ipairs({ "phea", "ckdk", "AHhb", "AA12", "fapo" }) do
    out(id .. " " .. tostring(GetObjectName(FourCC(id))))
end
for _, id in ipairs({ "phea", "ckdk" }) do
    local item = CreateItem(FourCC(id), 0, 0)
    out(id .. " item " .. tostring(item ~= nil) .. " " .. (item and tostring(GetItemName(item)) or "") .. " "
        .. (item and GetItemTypeId(item) == FourCC(id) and "same" or "other"))
end
local a = CreateUnit(Player(0), FourCC("hfoo"), 0, 0, 0)
for _, id in ipairs({ "AHhb", "AA12" }) do
    out(id .. " added " .. tostring(UnitAddAbility(a, FourCC(id))) .. " level " .. GetUnitAbilityLevel(a, FourCC(id)))
end
local b = CreateUnit(Player(PLAYER_NEUTRAL_AGGRESSIVE), FourCC("hfoo"), 300, 0, 0)
local before = GetWidgetLife(b)
UnitDamageTarget(a, b, 100, false, false, ATTACK_TYPE_PIERCE, DAMAGE_TYPE_NORMAL, WEAPON_TYPE_WHOKNOWS)
out("pierce " .. string.format("%.2f", before - GetWidgetLife(b)))
done()
