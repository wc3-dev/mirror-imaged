-- The order of the unit enumerations and of a group (W2-6): 64 footmen on a grid 128 apart (x and y from -448 to 448;
-- label 8 * row + column, row 0 the south), made in a scrambled order (label 37 * k mod 64 for the k-th). For each
-- enumeration, the order its filter is called in and the order the group then holds; and each unit's handle id less
-- the first's, in the group's order.
local made, labels = {}, {}
for k = 0, 63 do
  local n = 37 * k % 64
  local u = CreateUnit(Player(0), FourCC("hfoo"), (n % 8) * 128 - 448, (n // 8) * 128 - 448, 0)
  SetUnitUserData(u, n)
  made[k + 1] = u
  labels[k + 1] = n
end
out("made " .. table.concat(labels, ","))
local function L(u) return u == nil and "nil" or tostring(GetUnitUserData(u)) end
local calls = {}
local filter = Filter(function() calls[#calls + 1] = L(GetFilterUnit()) return true end)
local g = CreateGroup()
local function show(label)
  local held = {}
  for i = 0, BlzGroupGetSize(g) - 1 do held[#held + 1] = L(BlzGroupUnitAt(g, i)) end
  out(label .. " calls " .. table.concat(calls, ","))
  out(label .. " group " .. table.concat(held, ","))
  calls = {}
end
GroupEnumUnitsInRect(g, Rect(-1000, -1000, 1000, 1000), filter)
show("rect")
GroupEnumUnitsInRange(g, 100, -100, 2000, filter)
show("range")
GroupEnumUnitsOfPlayer(g, Player(0), filter)
show("player")
local first = GetHandleId(made[1])
local ids = {}
for i = 0, BlzGroupGetSize(g) - 1 do ids[#ids + 1] = GetHandleId(BlzGroupUnitAt(g, i)) - first end
out("ids " .. table.concat(ids, ",", 1, 32))
out("ids " .. table.concat(ids, ",", 33, 64))
local h = CreateGroup()
for k = 64, 1, -1 do GroupAddUnit(h, made[k]) end
local held = {}
for i = 0, BlzGroupGetSize(h) - 1 do held[#held + 1] = L(BlzGroupUnitAt(h, i)) end
out("added backwards " .. table.concat(held, ","))
-- Moved units: the first made moves to the far corner, and a unit moves within its square
SetUnitPosition(made[1], 448, 448)
SetUnitX(made[2], GetUnitX(made[2]) + 10)
GroupEnumUnitsInRect(g, Rect(-1000, -1000, 1000, 1000), filter)
show("moved")
-- Which square a unit is in, by its size: in one row (y 900 to 1020), a footman (collision 31) at x 400, made first,
-- then a gryphon rider (8) at 270, a peasant (16) at 280 and a footman at 290, each 270 to 290 against a square's edge.
for _, u in ipairs(made) do RemoveUnit(u) end
TimerStart(CreateTimer(), 0.1, false, guard(function()
  local kinds = { { "A", "hfoo", 400, 900 }, { "B", "hgry", 270, 1000 }, { "C", "hpea", 280, 960 }, { "D", "hfoo", 290, 1020 } }
  local names = {}
  for i, k in ipairs(kinds) do
    local u = CreateUnit(Player(0), FourCC(k[2]), k[3], k[4], 0)
    SetUnitUserData(u, i)
    names[i] = k[1]
  end
  local order = {}
  GroupEnumUnitsInRect(g, Rect(-1000, 0, 1000, 2000), Filter(function()
    order[#order + 1] = names[GetUnitUserData(GetFilterUnit())] .. math.floor(GetUnitX(GetFilterUnit()))
    return true
  end))
  out("sizes " .. table.concat(order, ","))
  -- A footman at 100 (Z, made first) and one at 400 (Y), then one at 276 (X): by its place less its size (31) X is in
  -- Z's square, by its place in Y's
  local trio = {}
  for i, k in ipairs({ { "Z", 100 }, { "Y", 400 }, { "X", 276 } }) do
    local u = CreateUnit(Player(0), FourCC("hfoo"), k[2], 1500, 0)
    SetUnitUserData(u, i)
    trio[i] = k[1]
  end
  order = {}
  GroupEnumUnitsInRect(g, Rect(-1000, 1400, 1000, 1600), Filter(function()
    order[#order + 1] = trio[GetUnitUserData(GetFilterUnit())] .. math.floor(GetUnitX(GetFilterUnit()))
    return true
  end))
  out("trio " .. table.concat(order, ","))
  done()
end))
