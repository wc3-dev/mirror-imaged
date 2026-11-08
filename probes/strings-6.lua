-- R2SW after strings-4 and strings-5: a negative precision, and widths at the 255 bytes the game writes at most. A long
-- string is given by its length, its first 4 bytes and its last 8. A first version that also wrote -1.5 at width 254
-- closed the game (0xC0000409), as -1.5 at width 254 and at 1000 did, each alone in a probe of its own.
local function show(v)
    return #v .. ' "' .. (#v <= 24 and v or v:sub(1, 4) .. '...' .. v:sub(-8)) .. '"'
end
for _, c in ipairs({{1.25, 0, -1}, {1.23456705, 0, -3}, {2.0, 0, -1}, {0.1, 0, -1}, {1.5, 4, -2}, {0.0625, 0, -1},
        {123.456, 0, -2}, {1.5, 253, 2}, {1.5, 254, 2}, {-1.5, 253, 2}, {1 / 3, 0, -1}, {1.23456705, 0, -1}}) do
    local ok, v = pcall(R2SW, c[1], c[2], c[3])
    out('R2SW ' .. c[1] .. ' ' .. c[2] .. ' ' .. c[3] .. ' = ' .. (ok and show(v) or 'error ' .. tostring(v)))
end
done()
