-- Destructables, trees and doodads (W2-4): where CreateDestructable puts each type, life, death events, restoring,
-- gates, invulnerability, occluder height, names, which rects EnumDestructablesInRect finds a destructable in, and
-- GetLocationZ on a bridge's deck. The probe map has no war3map.doo: one written by War3Net closed the game (facts).
local function show(v) return v == nil and "nil" or tostring(v) end
local function n(x) return string.format("%.3f", x) end
local id = FourCC
local function at(d) return n(GetDestructableX(d)) .. " " .. n(GetDestructableY(d)) end
local function life(d) return n(GetDestructableLife(d)) .. "/" .. n(GetDestructableMaxLife(d)) end

-- Each death event says which destructable died, with what life, as the event saw it.
local names = {}
local deaths = CreateTrigger()
TriggerAddAction(deaths, function()
    local d = GetTriggerDestructable()
    out("  death " .. show(names[GetHandleId(d)]) .. " life " .. n(GetDestructableLife(d)) .. " widget " .. tostring(GetTriggerWidget() == d)
        .. " event " .. GetHandleId(GetTriggerEventId()))
end)
local function make(name, type, x, y, face)
    local d = CreateDestructable(id(type), x or 0, y or 0, face or 270, 1, 0)
    names[GetHandleId(d)] = name
    TriggerRegisterDeathEvent(deaths, d)
    return d
end

-- Creating: nil for a unit's id and for 0; a variation past the type's count, or -1, makes one.
out("hfoo " .. show(CreateDestructable(id("hfoo"), 0, 0, 270, 1, 0)) .. " zero " .. show(CreateDestructable(0, 0, 0, 270, 1, 0)))
for _, v in ipairs({ 15, -1 }) do
    local d = CreateDestructable(id("LTlt"), 0, 0, 270, 1, v)
    out("variation " .. v .. " " .. tostring(d ~= nil))
    RemoveDestructable(d)
end

-- Where each type is put: fixed or free rotation, pathing textures of each size, none.
local points = { { 1000, 1000 }, { 0, 0 }, { -10, -10 }, { 10, -40 }, { 63, 63 }, { 64, -64 }, { -96, 96 }, { 1536, -1536 },
    { -1000, 1200 }, { 31.9, 32.1 }, { -10.1, -0.3 }, { 100.6, -200.9 } }
for _, type in ipairs({ "LTlt", "LTbr", "YT16", "LT06", "LTg1", "XTmx", "OTsp", "YTc1" }) do
    local line = {}
    for _, p in ipairs(points) do
        local d = CreateDestructable(id(type), p[1], p[2], 270, 1, 0)
        line[#line + 1] = d == nil and "nil" or GetDestructableX(d) .. "," .. GetDestructableY(d)
        RemoveDestructable(d)
    end
    out("snap " .. type .. " " .. table.concat(line, " "))
end
for _, face in ipairs({ 0, 30, 45, 60, 90, 135, 180, 300, 360, -90, 0.5, 179.9, 270, 540, -180 }) do
    local line = {}
    for _, p in ipairs({ { 0, 0 }, { 1000, 1000 } }) do
        local d = CreateDestructable(id("XTmx"), p[1], p[2], face, 1, 0)
        line[#line + 1] = GetDestructableX(d) .. "," .. GetDestructableY(d)
        RemoveDestructable(d)
    end
    out("facing XTmx " .. face .. " " .. table.concat(line, " "))
end
local small = CreateDestructable(id("LTlt"), -10, -10, 270, 1, 0)
out("zero place " .. n(GetDestructableX(small)) .. " " .. n(GetWidgetY(small)) .. " less 0 " .. (GetDestructableX(small) - 0)
    .. " name of nil " .. tostring(pcall(GetDestructableName, nil)) .. " [" .. show(select(2, pcall(GetDestructableName, nil))) .. "]")
RemoveDestructable(small)
local z = CreateDestructableZ(id("LTlt"), 1000, 1000, 300, 270, 1, 0)
out("z " .. at(z) .. " " .. life(z))
RemoveDestructable(z)

-- Life: a new tree has its type's; a fraction is kept, a set is held to the most life, below 0.405 it dies.
local tree = make("tree", "LTlt")
out("new " .. life(tree) .. " widget " .. n(GetWidgetLife(tree)) .. " " .. n(GetWidgetX(tree)) .. " type " .. GetDestructableTypeId(tree))
for _, v in ipairs({ 20.5, 999, 0.405 }) do
    SetDestructableLife(tree, v)
    out("set " .. v .. " " .. life(tree))
end
out("set 0.4")
SetDestructableLife(tree, 0.4)
out("after " .. life(tree))
SetDestructableLife(tree, 20)
out("dead set 20 " .. life(tree))
KillDestructable(tree)
out("killed again " .. life(tree))
KillDestructable(tree)
out("killed twice " .. life(tree))
for _, v in ipairs({ 0.1, 0, -5 }) do
    local d = make("set" .. v, "LTlt")
    SetDestructableLife(d, v)
    out("set " .. v .. " " .. life(d))
    RemoveDestructable(d)
end

-- The most life: whole numbers, and what happens to the life.
for _, case in ipairs({ { 50, 80.7 }, { 50, 80.5 }, { 50, 80.4 }, { 50, 10 }, { 50, 0 }, { 50, -5 }, { 20, 100 }, { 20, 25 }, { 20, 10 }, { 0, 70 } }) do
    local d = make("max", "LTlt")
    if case[1] == 0 then KillDestructable(d) else SetDestructableLife(d, case[1]) end
    SetDestructableMaxLife(d, case[2])
    out("max " .. case[1] .. " " .. case[2] .. " " .. life(d))
    RemoveDestructable(d)
end

local zero = make("zero", "LTlt")
SetDestructableMaxLife(zero, 0)
SetDestructableMaxLife(zero, 50)
out("max 0 then 50 " .. life(zero))
KillDestructable(zero)
out("max 0 killed " .. life(zero))
RemoveDestructable(zero)

-- Restoring: on a dead one only, held between 0.5 and the most, 0 giving the most.
for _, v in ipairs({ 30, 0, 999, 0.3, 0.5, -5 }) do
    local d = make("restore", "LTlt")
    KillDestructable(d)
    DestructableRestoreLife(d, v, v == 30)
    out("restore " .. v .. " " .. life(d))
    RemoveDestructable(d)
end
local living = make("living", "LTlt")
SetDestructableLife(living, 20)
DestructableRestoreLife(living, 40, true)
out("restore living " .. life(living))
KillDestructable(living)
DestructableRestoreLife(living, 50, false)
out("restored, killed again")
KillDestructable(living)

-- Removing fires the death event with the life it had, then everything reads 0.
SetDestructableLife(living, 0)
DestructableRestoreLife(living, 20, false)
out("remove living")
RemoveDestructable(living)
out("removed " .. GetDestructableTypeId(living) .. " " .. life(living) .. " " .. at(living))

-- Made dead.
local dead = CreateDeadDestructable(id("LTlt"), 0, 0, 270, 1, 0)
names[GetHandleId(dead)] = "dead"
TriggerRegisterDeathEvent(deaths, dead)
out("dead " .. life(dead))
KillDestructable(dead)
DestructableRestoreLife(dead, 0, false)
out("dead restored " .. life(dead))
RemoveDestructable(dead)

-- The widget natives on a destructable.
local w = make("widget", "LTbr")
SetWidgetLife(w, 10.5)
out("widget " .. n(GetWidgetLife(w)) .. " " .. n(GetWidgetX(w)) .. " " .. n(GetWidgetY(w)) .. " " .. life(w))
SetWidgetLife(w, 0)
out("widget killed " .. life(w))
RemoveDestructable(w)

-- Invulnerable, killed and set.
local guarded = make("guarded", "LTlt")
out("invulnerable " .. tostring(IsDestructableInvulnerable(guarded)))
SetDestructableInvulnerable(guarded, true)
out("invulnerable " .. tostring(IsDestructableInvulnerable(guarded)))
SetDestructableLife(guarded, 0)
out("invulnerable set 0 " .. life(guarded))
DestructableRestoreLife(guarded, 50, false)
KillDestructable(guarded)
out("invulnerable killed " .. life(guarded))
RemoveDestructable(guarded)

-- Occluder height, names, and the natives that only change what is drawn.
for _, type in ipairs({ "LTlt", "LTbr", "YT16", "OTsp" }) do
    local d = CreateDestructable(id(type), 0, 0, 270, 1, 0)
    local before = GetDestructableOccluderHeight(d)
    SetDestructableOccluderHeight(d, 100.7)
    out("occluder " .. type .. " " .. n(before) .. " " .. n(GetDestructableOccluderHeight(d)) .. " name " .. show(GetDestructableName(d)))
    SetDestructableOccluderHeight(d, -5)
    out("occluder " .. type .. " -5 " .. n(GetDestructableOccluderHeight(d)))
    RemoveDestructable(d)
end
local shown = make("shown", "LTlt", -512, -512)
ShowDestructable(shown, false)
SetDestructableAnimation(shown, "death")
QueueDestructableAnimation(shown, "stand")
SetDestructableAnimationSpeed(shown, 2)
SetDestructableColor(shown, ConvertPlayerColor(1))
SetDestructableVertexColor(shown, 255, 0, 0, 128)
SetDoodadAnimation(0, 0, 500, id("LTlt"), false, "death", false)
SetDoodadAnimationRect(Rect(-100, -100, 100, 100), id("LTlt"), "stand", true)
local found = 0
EnumDestructablesInRect(Rect(-600, -600, -400, -400), nil, function() found = found + 1 end)
out("hidden " .. life(shown) .. " found " .. found)
RemoveDestructable(shown)

-- A gate as ModifyGateBJ opens and closes it: killed with "death alternate", restored with "stand".
local gate = make("gate", "LTg1", 512, -512)
KillDestructable(gate)
SetDestructableAnimation(gate, "death alternate")
out("gate open " .. life(gate))
DestructableRestoreLife(gate, GetDestructableMaxLife(gate), true)
SetDestructableAnimation(gate, "stand")
out("gate closed " .. life(gate))
RemoveDestructable(gate)

-- Which rects find a destructable: centred, and with one edge at each distance from it.
for _, type in ipairs({ "LTlt", "LTbr" }) do
    local d = CreateDestructable(id(type), -1024, -1024, 270, 1, 0)
    local x, y = GetDestructableX(d), GetDestructableY(d)
    local function count(x1, y1, x2, y2)
        local r, c = Rect(x + x1, y + y1, x + x2, y + y2), 0
        EnumDestructablesInRect(r, nil, function() c = c + 1 end)
        RemoveRect(r)
        return c
    end
    local line = {}
    for _, h in ipairs({ 1, 16, 31, 32, 33, 48, 63, 64, 65, 96, 128, 129 }) do line[#line + 1] = h .. ":" .. count(-h, -h, h, h) end
    out("centred " .. type .. " " .. table.concat(line, " "))
    line = {}
    for _, a in ipairs({ -128, -65, -64, -63, -32, -1, 0, 1, 32, 63, 64, 65, 128 }) do line[#line + 1] = a .. ":" .. count(-300, -300, a, a) end
    out("below " .. type .. " " .. table.concat(line, " "))
    line = {}
    for _, a in ipairs({ -128, -65, -64, -63, -32, -1, 0, 1, 32, 63, 64, 65, 128 }) do line[#line + 1] = a .. ":" .. count(a, a, 300, 300) end
    out("above " .. type .. " " .. table.concat(line, " "))
    line = {}
    for _, a in ipairs({ -64, -32, 0, 32, 64 }) do line[#line + 1] = a .. ":" .. count(a, -300, a + 1, 300) .. "/" .. count(-300, a, 300, a + 1) end
    out("strip " .. type .. " " .. table.concat(line, " "))
    RemoveDestructable(d)
end

-- The enumeration's order and filter.
local order = {}
for i, p in ipairs({ { 1, 128, 0 }, { 2, -128, 0 }, { 3, 0, 128 }, { 4, 0, -128 }, { 5, 0, 0 } }) do
    order[i] = make("order" .. p[1], "LTlt", 1280 + p[2], -1280 + p[3])
end
local seen, filtered = {}, {}
EnumDestructablesInRect(Rect(1000, -1600, 1600, -1000), Filter(function()
    local d = GetFilterDestructable()
    filtered[#filtered + 1] = names[GetHandleId(d)]
    return d ~= order[3]
end), function()
    local filter = GetFilterDestructable()
    seen[#seen + 1] = names[GetHandleId(GetEnumDestructable())] .. (filter == nil and "" or filter == GetEnumDestructable() and "=filter" or "+filter")
end)
out("filtered " .. table.concat(filtered, " ") .. " enum " .. table.concat(seen, " ") .. " after " .. show(GetEnumDestructable()) .. " "
    .. show(GetFilterDestructable()))
seen = {}
EnumDestructablesInRect(Rect(1000, -1600, 1600, -1000), nil, function()
    seen[#seen + 1] = names[GetHandleId(GetEnumDestructable())] .. (GetFilterDestructable() == nil and "" or "+filter")
end)
out("no filter " .. table.concat(seen, " "))
for _, d in ipairs(order) do RemoveDestructable(d) end

-- A destructable off the 32 grid: whether a rect finds it by its place or by its 32 cell.
local portal = CreateDestructable(id("OTsp"), -1000.5, -1000.5, 270, 1, 0)
local hits = {}
for _, r in ipairs({ { -1000.75, -960.5 }, { -1030, -990 }, { -1001, -968.5 }, { -1001, -968.75 }, { -1000.5, -900 } }) do
    local c = 0
    EnumDestructablesInRect(Rect(r[1], r[1], r[2], r[2]), nil, function() c = c + 1 end)
    hits[#hits + 1] = r[1] .. ".." .. r[2] .. ":" .. c
end
out("portal " .. at(portal) .. " " .. table.concat(hits, " "))
RemoveDestructable(portal)

-- GetLocationZ on bridges made on flat ground: the decks, when one is killed, 3 s later, and 3 s after restoring.
local where = Location(0, 0)
local function heights(x, y, offsets)
    local line = {}
    for _, o in ipairs(offsets) do
        MoveLocation(where, x + o[1], y + o[2])
        line[#line + 1] = string.format("%.1f", GetLocationZ(where))
    end
    return table.concat(line, " ")
end
local across = { { 0, 0 }, { 0, 128 }, { 0, 256 }, { 0, -256 }, { 0, 512 }, { 0, -640 }, { 128, 0 }, { 256, 0 }, { -256, 0 }, { 300, 0 } }
local bridge = make("bridge", "YT16", 0, -1280)
local side = make("side", "LT06", -1280, 0)
out("deck YT16 " .. at(bridge) .. " " .. heights(GetDestructableX(bridge), GetDestructableY(bridge), across))
out("deck LT06 " .. at(side) .. " " .. heights(GetDestructableX(side), GetDestructableY(side), across))
KillDestructable(bridge)
out("deck killed " .. heights(GetDestructableX(bridge), GetDestructableY(bridge), across))
TimerStart(CreateTimer(), 3, false, guard(function()
    out("deck 3 s " .. heights(GetDestructableX(bridge), GetDestructableY(bridge), across))
    DestructableRestoreLife(bridge, 2500, false)
    TimerStart(CreateTimer(), 3, false, guard(function()
        out("deck restored 3 s " .. heights(GetDestructableX(bridge), GetDestructableY(bridge), across))
        done()
    end))
end))
