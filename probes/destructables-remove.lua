-- RemoveDestructable(GetTriggerDestructable()) in the destructable's own death action with no guard, as World Editor's
-- "Destructible dies -> Remove (Dying destructible)" writes it: how many times the death event fires, for a tree killed
-- and for one removed, whether the code after the call runs, and the error a pcall around the removal catches.
local function count(start)
    local n = 0
    local t = CreateTrigger()
    local tree = CreateDestructable(FourCC("LTlt"), 0, 0, 270, 1, 0)
    TriggerRegisterDeathEvent(t, tree)
    TriggerAddAction(t, function()
        n = n + 1
        RemoveDestructable(GetTriggerDestructable())
    end)
    start(tree)
    return n, tree
end
local killed, tree = count(KillDestructable)
out("killed " .. killed .. " type " .. GetDestructableTypeId(tree))
local removed = count(RemoveDestructable)
out("removed " .. removed)

local n, problem = 0, nil
local t = CreateTrigger()
local guarded = CreateDestructable(FourCC("LTlt"), 0, 0, 270, 1, 0)
TriggerRegisterDeathEvent(t, guarded)
TriggerAddAction(t, function()
    n = n + 1
    local ok, e = pcall(RemoveDestructable, GetTriggerDestructable())
    if not ok and problem == nil then problem = tostring(e):sub(1, 120) end
end)
KillDestructable(guarded)
out("pcall " .. n .. " " .. tostring(problem))
TimerStart(CreateTimer(), 1, false, guard(function()
    out("a second later " .. killed .. " " .. removed .. " " .. n)
    done()
end))
