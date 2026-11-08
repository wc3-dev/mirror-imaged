-- GetLocationZ on and off the map (W2-6), on the probe map with its ground changed (ProbeComparison.Change): each
-- corner of the 128 grid (x and y from 0 to 32, from -2048) at height x * 8 + y * 32, a plane, so a point's height
-- tells where on the map it was read. The points: inside, at the edges, and stepping off each side.
local function z(x, y) return string.format("%.3f", GetLocationZ(Location(x, y))) end
local points = { { 0, 0 }, { 128, 0 }, { 0, 128 }, { -1000.5, 300.25 }, { 64, 64 }, { -2048, -2048 }, { 2048, 2048 },
  { 2047.9, 0 }, { -2047.9, 0 }, { 3000, 3000 }, { -3000, -3000 }, { 2100, -2100 }, { -2100, 2100 }, { 1e6, 5 }, { -1e6, -1e6 } }
for _, p in ipairs(points) do out(p[1] .. " " .. p[2] .. " " .. z(p[1], p[2])) end
local steps = { 0.1, 0.2, 0.25, 0.3, 0.4, 0.45, 0.5, 0.6, 0.9, 1, 16, 32, 64, 128, 256, 512, 951 }
for _, side in ipairs({ { 1, 0 }, { -1, 0 }, { 0, 1 }, { 0, -1 } }) do
  local read = {}
  for _, d in ipairs(steps) do
    local x, y = side[1] * (2048 + d), side[2] * (2048 + d)
    if side[1] == 0 then x = 100 end
    if side[2] == 0 then y = 100 end
    read[#read + 1] = z(x, y)
  end
  out("side " .. side[1] .. " " .. side[2] .. ": " .. table.concat(read, " ", 1, 9))
  out("side " .. side[1] .. " " .. side[2] .. ": " .. table.concat(read, " ", 10, #read))
end
done()
