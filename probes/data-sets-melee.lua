-- Which balance set a map reads (facts, "Object data"): the max life of three units whose life differs between the
-- base tables (oshm 350, ebsh 1000, fapo 400), custom_v0 (260, none, none), custom_v1 (335, 1000, none) and melee_v0
-- (355, none, none), on the probe map with the melee flag (DataProbeMaps.Melee).
for _, id in ipairs({ "oshm", "ebsh", "fapo" }) do
    local u = CreateUnit(Player(0), FourCC(id), 0, 0, 0)
    out(id .. " " .. BlzGetUnitMaxHP(u))
    RemoveUnit(u)
end
done()
