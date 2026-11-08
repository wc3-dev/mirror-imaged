-- Groups and the unit enumerations (W2-6), on the probe map (custom_v1's data): the group natives and the order a group
-- keeps, ForGroup and FirstOfGroup with units changed, removed and exploded while held, and the enumerations in rect, in
-- range, of player and of type, their order over units made out of order across the map, which units they take
-- (hidden, dead, removed), their filters and counts, and where their edges fall. Each unit is labelled with its user
-- data; a unit is made where no other stands, as the game moves one made on another.
local function L(u) return u == nil and "nil" or tostring(GetUnitUserData(u)) end
local function make(n, id, x, y, p)
  local u = CreateUnit(Player(p or 0), FourCC(id), x, y, 0)
  SetUnitUserData(u, n)
  return u
end
local function at(g)
  local t = {}
  for i = 0, BlzGroupGetSize(g) - 1 do t[#t + 1] = L(BlzGroupUnitAt(g, i)) end
  return table.concat(t, ",")
end
local function each(g)
  local t = {}
  ForGroup(g, function() t[#t + 1] = L(GetEnumUnit()) end)
  return table.concat(t, ",")
end
local function s(v) return tostring(v) end
local function part(time, label, f)
  TimerStart(CreateTimer(), time, false, function()
    out(label)
    local ok, err = pcall(f)
    if not ok then out(label .. " error " .. tostring(err)) end
  end)
end

TimerStart(CreateTimer(), 3, false, guard(function() done() end))

-- The enumerations first, while no other unit stands, players 5 and 6 only.
part(0.05, "enum", function()
  local made = {}
  local spots = {
    { 21, "hfoo", 300, 300, 5 }, { 22, "hfoo", -300, -300, 5 }, { 23, "hfoo", 10, 10, 5 }, { 24, "hfoo", 100, 100, 5 },
    { 25, "hfoo", -300, 300, 5 }, { 26, "hfoo", 300, -300, 5 }, { 27, "hfoo", 600, 10, 5 }, { 28, "hfoo", 10, 600, 5 },
    { 29, "hfoo", 150, 10, 5 }, { 30, "hfoo", -600, -10, 5 }, { 31, "hfoo", 500, 500, 6 }, { 32, "hfoo", -500, -500, 6 },
    { 33, "Hpal", 700, -700, 5 }, { 34, "hpea", -700, 700, 5 }, { 35, "hfoo", -650, -650, 5 }, { 36, "hfoo", 650, 650, 5 },
  }
  local places = {}
  for _, p in ipairs(spots) do
    local u = make(p[1], p[2], p[3], p[4], p[5])
    made[p[1]] = u
    places[#places + 1] = p[1] .. ":" .. math.floor(GetUnitX(u)) .. "," .. math.floor(GetUnitY(u))
  end
  out("places " .. table.concat(places, " "))
  local ids = {}
  for n = 22, 36 do ids[#ids + 1] = GetHandleId(made[n]) - GetHandleId(made[21]) end
  out("ids " .. table.concat(ids, ","))
  ShowUnit(made[34], false)
  KillUnit(made[35])
  RemoveUnit(made[36])
  local g, r = CreateGroup(), Rect(-1000, -1000, 1000, 1000)
  GroupEnumUnitsInRect(g, r, nil)
  out("rect " .. at(g))
  GroupEnumUnitsInRange(g, 0, 0, 1500, nil)
  out("range " .. at(g))
  GroupEnumUnitsOfPlayer(g, Player(5), nil)
  out("player5 " .. at(g))
  GroupEnumUnitsOfPlayer(g, Player(6), nil)
  out("player6 " .. at(g))
  GroupEnumUnitsOfType(g, "footman", nil)
  out("type footman " .. at(g))
  GroupEnumUnitsOfType(g, "Footman", nil)
  out("type Footman " .. at(g))
  GroupEnumUnitsOfType(g, "hfoo", nil)
  out("type hfoo " .. at(g) .. ";")
  GroupEnumUnitsOfType(g, "paladin", nil)
  out("type paladin " .. at(g))
  GroupEnumUnitsOfType(g, "peasant", nil)
  out("type peasant " .. at(g))
  GroupEnumUnitsInRangeOfLoc(g, Location(300, 300), 100, nil)
  out("loc " .. at(g))
  -- Does an enumeration clear its group first?
  GroupClear(g)
  GroupAddUnit(g, made[31])
  GroupEnumUnitsOfPlayer(g, Player(5), nil)
  out("cleared player " .. s(IsUnitInGroup(made[31], g)))
  GroupClear(g)
  GroupAddUnit(g, made[31])
  GroupEnumUnitsInRange(g, -300, -300, 10, nil)
  out("cleared range " .. s(IsUnitInGroup(made[31], g)) .. " " .. at(g))
  GroupClear(g)
  GroupAddUnit(g, made[31])
  GroupEnumUnitsInRect(g, Rect(-310, -310, -290, -290), nil)
  out("cleared rect " .. s(IsUnitInGroup(made[31], g)) .. " " .. at(g))
  GroupClear(g)
  GroupAddUnit(g, made[22])
  GroupEnumUnitsOfType(g, "peasant", nil)
  out("cleared type " .. s(IsUnitInGroup(made[22], g)) .. " " .. at(g))
  -- Filters: the units each is called for, and what the responses read in them
  local calls, inside = {}, nil
  local odd = Filter(function()
    calls[#calls + 1] = L(GetFilterUnit())
    inside = inside or L(GetFilterUnit()) .. "/" .. L(GetEnumUnit()) .. "/" .. L(GetTriggerUnit())
    return GetUnitUserData(GetFilterUnit()) % 2 == 1
  end)
  GroupEnumUnitsInRange(g, 0, 0, 1500, odd)
  out("filter range " .. at(g) .. " calls " .. table.concat(calls, ",") .. " inside " .. inside)
  calls = {}
  GroupEnumUnitsInRangeCounted(g, 0, 0, 1500, odd, 3)
  out("counted filter 3 " .. at(g) .. " calls " .. table.concat(calls, ","))
  GroupEnumUnitsInRangeCounted(g, 0, 0, 1500, nil, 3)
  out("counted range 3 " .. at(g))
  GroupEnumUnitsInRangeCounted(g, 0, 0, 1500, nil, 0)
  out("counted range 0 " .. at(g))
  GroupEnumUnitsInRangeCounted(g, 0, 0, 1500, nil, -1)
  out("counted range -1 " .. at(g))
  GroupEnumUnitsInRectCounted(g, r, nil, 2)
  out("counted rect 2 " .. at(g))
  GroupEnumUnitsOfTypeCounted(g, "footman", nil, 2)
  out("counted type 2 " .. at(g))
  GroupEnumUnitsInRangeOfLocCounted(g, Location(0, 0), 1500, nil, 1)
  out("counted loc 1 " .. at(g))
  calls = {}
  GroupEnumUnitsOfPlayer(g, Player(5), odd)
  out("filter player " .. at(g) .. " calls " .. table.concat(calls, ","))
  calls = {}
  GroupEnumUnitsOfType(g, "footman", odd)
  out("filter type " .. at(g) .. " calls " .. table.concat(calls, ","))
  calls = {}
  GroupEnumUnitsInRect(g, r, odd)
  out("filter rect " .. at(g) .. " calls " .. table.concat(calls, ","))
  out("filter after " .. L(GetFilterUnit()) .. " " .. L(GetEnumUnit()))
  GroupEnumUnitsInRange(g, 0, 0, 1500, Filter(function() return 1 end))
  out("filter one " .. BlzGroupGetSize(g))
  GroupEnumUnitsInRange(g, 0, 0, 1500, Filter(function() return nil end))
  out("filter nil " .. BlzGroupGetSize(g))
  GroupEnumUnitsInRange(g, 0, 0, 1500, Filter(nil))
  out("filter Filter(nil) " .. BlzGroupGetSize(g))
  GroupEnumUnitsInRange(nil, 0, 0, 1500, nil)
  GroupEnumUnitsInRect(g, nil, nil)
  out("nil rect " .. BlzGroupGetSize(g))
  -- A filter that enumerates: the outer filter unit after the inner enumeration
  local inner = CreateGroup()
  calls = {}
  GroupEnumUnitsInRange(g, 300, 300, 10, Filter(function()
    local before = L(GetFilterUnit())
    GroupEnumUnitsInRange(inner, -300, -300, 10, Filter(function() calls[#calls + 1] = "in" .. L(GetFilterUnit()) return true end))
    calls[#calls + 1] = before .. "/" .. L(GetFilterUnit())
    return true
  end))
  out("nested filter " .. table.concat(calls, ",") .. " " .. at(g) .. " " .. at(inner))
  for _, u in pairs(made) do RemoveUnit(u) end
end)

-- Where the edges fall: one footman at (40, 50) against rects, one at (200, 0) against ranges, and the range natives.
part(0.1, "edges", function()
  local u = make(41, "hfoo", 40, 50, 7)
  local g = CreateGroup()
  local function rect(a, b, c, d)
    GroupEnumUnitsInRect(g, Rect(a, b, c, d), nil)
    return BlzGroupGetSize(g)
  end
  out("at " .. GetUnitX(u) .. " " .. GetUnitY(u) .. " collision " .. BlzGetUnitCollisionSize(u))
  out("rect x " .. rect(40, 0, 100, 100) .. rect(0, 0, 40, 100) .. rect(40.01, 0, 100, 100) .. rect(0, 0, 39.99, 100)
    .. rect(33, 0, 100, 100) .. rect(32, 0, 100, 100) .. rect(0, 0, 63, 100) .. rect(0, 0, 64, 100) .. rect(39, 0, 41, 100))
  out("rect y " .. rect(0, 50, 100, 100) .. rect(0, 0, 100, 50) .. rect(0, 50.01, 100, 100) .. rect(0, 0, 100, 49.99)
    .. rect(0, 49, 100, 51) .. rect(100, 100, 0, 0) .. rect(40, 50, 40, 50))
  RemoveUnit(u)
  local v = make(42, "hfoo", 200, 0, 7)
  local w = make(43, "Hpal", 500, 0, 7)
  local function range(x, y, d)
    GroupEnumUnitsInRange(g, x, y, d, nil)
    return BlzGroupGetSize(g)
  end
  out("range " .. range(0, 0, 199.99) .. range(0, 0, 200) .. range(0, 0, 200.01) .. range(0, 0, 169) .. range(0, 0, 0)
    .. range(200, 0, 0) .. range(200, 0, -1) .. range(170, 0, 30) .. range(170, 0, 29.99))
  local function xy(d) return IsUnitInRangeXY(v, 0, 0, d) and "1" or "0" end
  out("xy " .. xy(168) .. xy(168.9) .. xy(169) .. xy(169.1) .. xy(200) .. xy(0) .. xy(-1) .. " "
    .. (IsUnitInRangeXY(v, 200, 0, 0) and 1 or 0) .. (IsUnitInRangeXY(v, 200, 0, -1) and 1 or 0) .. (IsUnitInRangeXY(v, 200, 0, -31) and 1 or 0)
    .. (IsUnitInRangeXY(v, 200, 0, -32) and 1 or 0))
  local here = Location(0, 0)
  out("loc " .. (IsUnitInRangeLoc(v, here, 168.9) and 1 or 0) .. (IsUnitInRangeLoc(v, here, 169) and 1 or 0))
  local function pair(a, b, d) return IsUnitInRange(a, b, d) and "1" or "0" end
  out("pair collision " .. BlzGetUnitCollisionSize(v) .. " " .. BlzGetUnitCollisionSize(w) .. " "
    .. pair(v, w, 236.9) .. pair(v, w, 237) .. pair(v, w, 268.9) .. pair(v, w, 269) .. pair(v, w, 299.9) .. pair(v, w, 300) .. " "
    .. pair(w, v, 236.9) .. pair(w, v, 237) .. pair(w, v, 267.9) .. pair(w, v, 268) .. pair(w, v, 268.9) .. pair(w, v, 269) .. " "
    .. pair(v, v, 0) .. pair(v, v, -1) .. pair(v, nil, 1000) .. pair(nil, v, 1000))
  for _, id in ipairs({ "hpea", "hkni", "hgry", "hbar", "Hpal", "hrif", "nshe" }) do
    local k = make(0, id, -800, -800, 8)
    out(id .. " collision " .. BlzGetUnitCollisionSize(k))
    RemoveUnit(k)
  end
  RemoveUnit(v)
  RemoveUnit(w)
end)

part(0.15, "group", function()
  local u = {}
  for n = 1, 7 do u[n] = make(n, "hfoo", -1200 + n * 150, -1200, 0) end
  local g = CreateGroup()
  out("add " .. s(GroupAddUnit(g, u[3])) .. " " .. s(GroupAddUnit(g, u[1])) .. " " .. s(GroupAddUnit(g, u[4])) .. " "
    .. s(GroupAddUnit(g, u[1])) .. " " .. s(GroupAddUnit(g, nil)) .. " " .. s(GroupAddUnit(nil, u[1])))
  out("size " .. BlzGroupGetSize(g) .. " at " .. at(g) .. " each " .. each(g) .. " first " .. L(FirstOfGroup(g)) .. " at-1 "
    .. L(BlzGroupUnitAt(g, -1)) .. " at3 " .. L(BlzGroupUnitAt(g, 3)) .. " nil " .. BlzGroupGetSize(nil) .. " " .. L(FirstOfGroup(nil)))
  out("in " .. s(IsUnitInGroup(u[1], g)) .. " " .. s(IsUnitInGroup(u[2], g)) .. " " .. s(IsUnitInGroup(nil, g)) .. " "
    .. s(IsUnitInGroup(u[1], nil)))
  out("remove " .. s(GroupRemoveUnit(g, u[1])) .. " " .. s(GroupRemoveUnit(g, u[1])) .. " " .. s(GroupRemoveUnit(g, nil)) .. " at " .. at(g))
  GroupClear(g)
  for n = 1, 6 do GroupAddUnit(g, u[n]) end
  GroupRemoveUnit(g, u[2])
  out("middle " .. at(g))
  GroupRemoveUnit(g, u[1])
  out("first " .. at(g))
  GroupAddUnit(g, u[1])
  out("again " .. at(g))
  local ids = {}
  for i = 0, BlzGroupGetSize(g) - 1 do ids[#ids + 1] = GetHandleId(BlzGroupUnitAt(g, i)) - GetHandleId(u[1]) end
  out("ids " .. table.concat(ids, ",") .. " u7 " .. (GetHandleId(u[7]) - GetHandleId(u[1])))
  -- ForGroup as the callback changes the group: at the first unit visited, the last is taken out and u7 added
  local seen, last = {}, BlzGroupUnitAt(g, BlzGroupGetSize(g) - 1)
  ForGroup(g, function()
    local e = GetEnumUnit()
    seen[#seen + 1] = L(e)
    if #seen == 1 then
      GroupRemoveUnit(g, last)
      GroupAddUnit(g, u[7])
    end
  end)
  out("changed " .. table.concat(seen, ",") .. " then " .. at(g))
  seen = {}
  ForGroup(g, function()
    seen[#seen + 1] = L(GetEnumUnit())
    if #seen == 1 then GroupClear(g) end
  end)
  out("cleared inside " .. table.concat(seen, ",") .. " then " .. BlzGroupGetSize(g))
  for n = 1, 6 do GroupAddUnit(g, u[n]) end
  seen = {}
  ForGroup(g, function()
    seen[#seen + 1] = L(GetEnumUnit())
    GroupRemoveUnit(g, GetEnumUnit())
  end)
  out("emptied " .. table.concat(seen, ",") .. " then " .. BlzGroupGetSize(g))
  for n = 1, 4 do GroupAddUnit(g, u[n]) end
  seen = {}
  local h = CreateGroup()
  GroupAddUnit(h, u[6])
  GroupAddUnit(h, u[7])
  ForGroup(g, function()
    local outer = L(GetEnumUnit())
    if outer == "2" then ForGroup(h, function() seen[#seen + 1] = "in" .. L(GetEnumUnit()) end) end
    seen[#seen + 1] = outer .. "/" .. L(GetEnumUnit())
  end)
  out("nested " .. table.concat(seen, ",") .. " after " .. L(GetEnumUnit()))
  seen = {}
  while true do
    local f = FirstOfGroup(g)
    if f == nil then break end
    seen[#seen + 1] = L(f)
    GroupRemoveUnit(g, f)
  end
  out("first loop " .. table.concat(seen, ","))
  for n = 1, 4 do GroupAddUnit(g, u[n]) end
  GroupClear(h)
  for n = 3, 6 do GroupAddUnit(h, u[n]) end
  out("addfast " .. BlzGroupAddGroupFast(h, g) .. " " .. at(g) .. " source " .. at(h))
  GroupClear(h)
  GroupAddUnit(h, u[2])
  GroupAddUnit(h, u[7])
  GroupAddUnit(h, u[5])
  out("removefast " .. BlzGroupRemoveGroupFast(h, g) .. " " .. at(g))
  out("fast nil " .. BlzGroupAddGroupFast(nil, g) .. " " .. BlzGroupAddGroupFast(h, nil))
  GroupClear(g)
  out("clear " .. BlzGroupGetSize(g) .. " " .. L(FirstOfGroup(g)))
  GroupAddUnit(g, u[1])
  DestroyGroup(g)
  out("destroyed " .. BlzGroupGetSize(g) .. " " .. L(FirstOfGroup(g)) .. " " .. s(GroupAddUnit(g, u[2])) .. " " .. s(IsUnitInGroup(u[1], g)))
  -- A dead unit stays and is visited; removed and exploded units, in the same instant and after it
  local dead, removed, first, exploded = CreateGroup(), CreateGroup(), CreateGroup(), CreateGroup()
  for n = 1, 4 do
    GroupAddUnit(dead, u[n])
    GroupAddUnit(removed, u[n])
    GroupAddUnit(first, u[n])
  end
  local e1, e2 = make(8, "hfoo", -1200, -900, 0), make(9, "hfoo", -1000, -900, 0)
  GroupAddUnit(exploded, e1)
  GroupAddUnit(exploded, e2)
  KillUnit(u[4])
  out("dead " .. at(dead) .. " each " .. each(dead) .. " alive " .. s(UnitAlive(u[4])))
  RemoveUnit(u[2])
  RemoveUnit(u[1])
  SetUnitExploded(e1, true)
  KillUnit(e1)
  out("removed now " .. BlzGroupGetSize(first) .. " " .. at(first) .. " each " .. each(first) .. " first " .. L(FirstOfGroup(first))
    .. " exploded " .. at(exploded) .. " " .. L(FirstOfGroup(exploded)))
  TimerStart(CreateTimer(), 0, false, function()
    out("removed later " .. BlzGroupGetSize(first) .. " " .. at(first) .. " first " .. L(FirstOfGroup(first)) .. " in " .. s(IsUnitInGroup(u[3], first)))
    out("removed each " .. each(first) .. " size " .. BlzGroupGetSize(first) .. " " .. at(first))
    out("exploded later " .. BlzGroupGetSize(exploded) .. " " .. at(exploded) .. " first " .. L(FirstOfGroup(exploded)) .. " each " .. each(exploded)
      .. " " .. BlzGroupGetSize(exploded))
    GroupAddUnit(removed, u[5])
    out("removed add " .. BlzGroupGetSize(removed) .. " " .. at(removed))
    out("removed remove " .. s(GroupRemoveUnit(removed, u[3])) .. " " .. BlzGroupGetSize(removed) .. " " .. at(removed))
    local seen2 = {}
    while true do
      local f = FirstOfGroup(removed)
      if f == nil then break end
      seen2[#seen2 + 1] = L(f)
      GroupRemoveUnit(removed, f)
    end
    out("removed first loop " .. table.concat(seen2, ",") .. " size " .. BlzGroupGetSize(removed))
    GroupClear(removed)
    out("removed clear " .. BlzGroupGetSize(removed))
    out("dead later " .. at(dead))
  end)
end)
