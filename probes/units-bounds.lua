-- Rects, locations and the map's bounds (W2-6), on the probe map (custom_v1's data; 32 by 32 tiles, the world from
-- -2048 to 2048, the playable area -1280 to 1280 across and -1536 to 1024 up): the rect natives, swapped and moved,
-- GetWorldBounds and blizzard.j's playable area, the location natives and the start locations, and where CreateUnit,
-- CreateUnitAtLoc and SetUnitPosition put a unit asked off the map or off the playable area, by its size.
local function f(v) return string.format("%.3f", v) end
local function box(r) return f(GetRectMinX(r)) .. " " .. f(GetRectMinY(r)) .. " " .. f(GetRectMaxX(r)) .. " " .. f(GetRectMaxY(r)) .. " c " .. f(GetRectCenterX(r)) .. " " .. f(GetRectCenterY(r)) end
local function place(u) return u == nil and "nil" or f(GetUnitX(u)) .. " " .. f(GetUnitY(u)) end

local r = Rect(10, 20, 30, 40)
out("rect " .. box(r))
out("swapped " .. box(Rect(30, 40, 10, 20)))
out("odd " .. box(Rect(0.1, 0.2, 0.2, 0.7)) .. " big " .. box(Rect(-3e38, -1, 3e38, 1e38)))
MoveRectTo(r, 0, 0)
out("moved " .. box(r))
MoveRectTo(r, 0.1, -1000.3)
out("moved odd " .. box(r))
MoveRectToLoc(r, Location(500, 500))
out("moved loc " .. box(r))
SetRect(r, 5, 6, 7, 8)
out("set " .. box(r))
SetRect(r, 7, 8, 5, 6)
out("set swapped " .. box(r))
SetRectFromLoc(r, Location(1, 2), Location(3, 4))
out("set loc " .. box(r))
out("from loc " .. box(RectFromLoc(Location(-1, -2), Location(-3, -4))))
MoveRectTo(Rect(30, 40, 10, 20), 0, 0)
local sw = Rect(30, 40, 10, 20)
MoveRectTo(sw, 0, 0)
out("moved swapped " .. box(sw))
RemoveRect(r)
out("removed " .. box(r))
out("nil " .. box(nil))

-- Where a rect's corners are held: the world is -2048 to 2048 each way
out("clamp " .. box(Rect(-2100, -2100, 2100, 2100)) .. " " .. box(Rect(0, 0, 2030, 2017)))
out("clamp edge " .. box(Rect(2020, 2020, 2040, 2040)) .. " " .. box(Rect(-2060, -2060, -2050, -2040)))
local m = Rect(0, 0, 100, 100)
MoveRectTo(m, 5000, 5000)
out("move clamp " .. box(m))
SetRect(m, -5000, -5000, 5000, 5000)
out("set clamp " .. box(m))
out("from loc clamp " .. box(RectFromLoc(Location(-5000, 0), Location(5000, 3000))) .. " loc " .. f(GetLocationX(Location(5000, -5000))))
local wb = GetWorldBounds()
MoveRectTo(wb, 100, 100)
out("world moved " .. box(wb))
SetRect(wb, -2048, -2048, 2048, 2048)
out("world set " .. box(wb))

local w1, w2 = GetWorldBounds(), GetWorldBounds()
out("world " .. box(w1) .. " same " .. tostring(w1 == w2) .. " " .. (GetHandleId(w2) - GetHandleId(w1)))
RemoveRect(w1)
out("world after remove " .. box(GetWorldBounds()) .. " " .. box(w2))
out("playable " .. box(bj_mapInitialPlayableArea))

local l = Location(1.5, -2.5)
out("loc " .. f(GetLocationX(l)) .. " " .. f(GetLocationY(l)) .. " " .. f(GetLocationZ(l)))
MoveLocation(l, -7, 8)
out("moved " .. f(GetLocationX(l)) .. " " .. f(GetLocationY(l)))
RemoveLocation(l)
out("removed " .. f(GetLocationX(l)) .. " " .. f(GetLocationY(l)))
out("nil " .. f(GetLocationX(nil)) .. " " .. f(GetLocationZ(nil)))
local s0, s0b = GetStartLocationLoc(0), GetStartLocationLoc(0)
out("start " .. f(GetLocationX(s0)) .. " " .. f(GetLocationY(s0)) .. " same " .. tostring(s0 == s0b) .. " undefined "
  .. tostring(GetStartLocationLoc(5) == nil) .. " " .. f(GetLocationX(GetStartLocationLoc(5))))
DefineStartLocationLoc(3, Location(123.5, -45.25))
DefineStartLocationLoc(4, nil)
out("defined " .. f(GetStartLocationX(3)) .. " " .. f(GetStartLocationY(3)) .. " " .. f(GetLocationX(GetStartLocationLoc(3))) .. " nil "
  .. f(GetStartLocationX(4)) .. " " .. tostring(GetStartLocationLoc(4) == nil))

-- Placement: each kind asked at points off the world, past the playable area and inside it
local asked = { { 5000, -5000 }, { -5000, 5000 }, { 5000, 5000 }, { -5000, -5000 }, { 1500, 0 }, { 0, 1500 }, { -1500, -1700 },
  { 2040, 0 }, { 0, -2040 }, { 2100, 100 } }
for _, id in ipairs({ "hfoo", "hpea", "Hpal", "hgry", "hbar" }) do
  local made, moved = {}, {}
  for i, p in ipairs(asked) do
    local u = CreateUnit(Player(0), FourCC(id), p[1], p[2], 0)
    made[#made + 1] = place(u)
    RemoveUnit(u)
  end
  out(id .. " made " .. table.concat(made, ", "))
  local u = CreateUnit(Player(0), FourCC(id), 0, 0, 0)
  for i, p in ipairs(asked) do
    SetUnitPosition(u, p[1], p[2])
    moved[#moved + 1] = place(u)
    SetUnitPosition(u, 0, 0)
  end
  RemoveUnit(u)
  out(id .. " position " .. table.concat(moved, ", "))
end
local u = CreateUnitAtLoc(Player(0), FourCC("hfoo"), Location(5000, -5000), 0)
out("at loc " .. place(u) .. " " .. place(CreateUnitAtLoc(Player(0), FourCC("hfoo"), nil, 0)) .. " " .. place(CreateUnitAtLoc(Player(0), FourCC("hfoo"), Location(-1000.5, 800.25), 90)))
SetUnitPositionLoc(u, Location(-300.5, 400.25))
out("position loc " .. place(u))
SetUnitPositionLoc(u, nil)
out("position nil " .. place(u))
SetUnitX(u, 5000)
SetUnitY(u, -5000)
out("x y " .. place(u))
local ul = GetUnitLoc(u)
out("unit loc " .. f(GetLocationX(ul)) .. " " .. f(GetLocationY(ul)) .. " same " .. tostring(ul == GetUnitLoc(u)) .. " nil " .. tostring(GetUnitLoc(nil) == nil))
SetUnitPosition(u, 300, 300)
local v = CreateUnit(Player(0), FourCC("hfoo"), 300, 300, 0)
out("onto unit " .. place(v))
SetUnitPosition(u, 300, 300)
out("position onto unit " .. place(u))
SetUnitX(u, 600)
SetUnitY(u, 600)
SetUnitPosition(v, 600, 600)
out("position onto x y " .. place(v))
done()
