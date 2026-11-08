-- W1-3's limits: R2SW's width and precision far out, S2R past the float range, and the random natives' widest bounds.
-- A long string is given by its length, its first 12 bytes and its last 8.
local function bits(x)
    return string.format('%08x', string.unpack('<i4', string.pack('<f', x)))
end
local function show(v)
    if v == nil then return 'nil' end
    if math.type(v) == 'float' then return 'f ' .. bits(v) .. ' ' .. string.format('%.9g', v) end
    if math.type(v) == 'integer' then return 'i ' .. v end
    if type(v) == 'string' then
        return 's ' .. #v .. ' "' .. (#v <= 24 and v or v:sub(1, 12) .. '...' .. v:sub(-8)) .. '"'
    end
    return type(v) .. ' ' .. tostring(v)
end
local function case(name, f, ...)
    local ok, v = pcall(f, ...)
    out(name .. ' = ' .. (ok and show(v) or 'error ' .. tostring(v)))
end
local min, max = -2147483647 - 1, 2147483647

for _, c in ipairs({{1.5, 30, 2}, {1.5, 31, 2}, {1.5, 100, 2}, {-1.5, 100, 2}, {1.5, 255, 2}, {1.5, 256, 2},
        {1.5, 1000, 2}, {1.5, 0, min}, {1.5, 0, max}, {1.5, min, 2}, {1e20, 100, 2}}) do
    case('R2SW ' .. bits(c[1]) .. ' ' .. c[2] .. ' ' .. c[3], R2SW, c[1], c[2], c[3])
end

local nines, zeros = string.rep('9', 40), string.rep('0', 38)
for _, s in ipairs({'1234567890123456789012345678901234567890', '0.1234567890123456789012345678901234567890',
        '1' .. zeros, '3' .. zeros, '4' .. zeros, '1' .. zeros .. '0', nines, '0.' .. string.rep('0', 39) .. '1',
        '0.' .. string.rep('0', 45) .. '1', '1.' .. string.rep('0', 40), '-4' .. zeros, '340282346638528859811704183484516925440',
        '340282356779733661637539395458142568448'}) do
    case('S2R ' .. #s .. ' ' .. s:sub(1, 6) .. ' ' .. s:sub(-4), S2R, s)
end

SetRandomSeed(1)
local function spread(name, f, a, b)
    local low, high, negative = math.huge, -math.huge, 0
    for _ = 1, 1000 do
        local v = f(a, b)
        low, high = math.min(low, v), math.max(high, v)
        if v < 0 then negative = negative + 1 end
    end
    out(name .. ' over 1000 = ' .. show(low) .. ' ' .. show(high) .. ' ' .. negative .. ' negative')
end
spread('GetRandomInt min max', GetRandomInt, min, max)
spread('GetRandomInt max 0', GetRandomInt, max, 0)
spread('GetRandomInt 0 max', GetRandomInt, 0, max)
spread('GetRandomInt max min', GetRandomInt, max, min)
spread('GetRandomReal -3e38 3e38', GetRandomReal, -3e38, 3e38)
spread('GetRandomReal 3e38 -3e38', GetRandomReal, 3e38, -3e38)
spread('GetRandomReal 0 3e38', GetRandomReal, 0, 3e38)
done()
