-- The string, conversion, maths and random natives (W1-3): each case's answer, a float with its bits, a string with its
-- length and its bytes outside ASCII as <xx>. The maths natives are compared with Lua's math functions.
local function bits(x)
    return string.format('%08x', string.unpack('<i4', string.pack('<f', x)))
end
local function text(s)
    return (s:gsub('[^%w%p ]', function(c) return string.format('<%02x>', c:byte()) end))
end
local function show(v)
    if v == nil then return 'nil' end
    local t = math.type(v)
    if t == 'float' then return 'f ' .. bits(v) .. ' ' .. string.format('%.9g', v) end
    if t == 'integer' then return 'i ' .. v end
    if type(v) == 'string' then return 's ' .. #v .. ' "' .. text(v) .. '"' end
    return type(v) .. ' ' .. tostring(v)
end
local function case(name, f, ...)
    local ok, v = pcall(f, ...)
    out(name .. ' = ' .. (ok and show(v) or 'error ' .. text(tostring(v))))
end
local function both(name, native, lua, ...)
    local a, b = native(...), lua(...)
    out(name .. ' = ' .. show(a) .. (math.type(b) == math.type(a) and bits(a) == bits(b) and ' same' or ' lua ' .. show(b)))
end

for _, s in ipairs({'', 'a', 'A', 'abc', 'ABC', '/', '\\', 'a/b', 'a\\b', 'hello world', 'Hello World', 'abcdefghijk',
        'abcdefghijkl', 'abcdefghijklm', 'abcdefghijklmnopqrstuvwx', 'abcdefghijklmnopqrstuvwxy', 'war3map.lua',
        'Units\\Human\\Footman', 'z{|}~', '\195\164', '\195\132', '\195\169', 'a\0b', '0', '123', '\255'}) do
    case('StringHash ' .. text(s), StringHash, s)
end
case('StringHash nil', StringHash, nil)

for _, s in ipairs({'', 'abc', '\195\164', '\228\184\173', 'a\0b'}) do
    case('StringLength ' .. text(s), StringLength, s)
end
case('StringLength nil', StringLength, nil)

for _, s in ipairs({'aBc1!', '\195\164\195\150\195\169', 'z{@[`', ''}) do
    case('StringCase ' .. text(s) .. ' true', StringCase, s, true)
    case('StringCase ' .. text(s) .. ' false', StringCase, s, false)
end
case('StringCase nil true', StringCase, nil, true)

for _, n in ipairs({0, -5, 2147483647, -2147483647 - 1}) do
    case('I2S ' .. n, I2S, n)
end
for _, n in ipairs({0, 16777217, 16777219, -16777219, 2147483647}) do
    case('I2R ' .. n, I2R, n)
end
for _, r in ipairs({1.9, -1.9, 0.99999994, -0.0, 2147483520.0, 2147483648.0, 1e10, -1e10, math.huge}) do
    case('R2I ' .. bits(r), R2I, r)
end
for _, r in ipairs({0.5, 1.5, 2.5, -0.5, -1.5, -2.5, 0.49999997, 1e10}) do
    case('MathRound ' .. bits(r), MathRound, r)
end
for _, s in ipairs({'123', '-45', '+7', ' 12', '\t5', '12abc', 'abc', '', '2147483647', '2147483648', '-2147483648',
        '-2147483649', '99999999999', '1.9', '0x10', '1e3', '--5', '0012', '- 5'}) do
    case('S2I ' .. text(s), S2I, s)
end
case('S2I nil', S2I, nil)
for _, s in ipairs({'1.5', '-2.25', 'abc', '', '1e3', '1.5e2', ' 2.5', '3.0x', '.5', '5.', '0x10', '1e40', '-1e40',
        '1e-50', 'inf', 'nan', '0.1', '16777217', '16777219', '123456789', '1,5', '+.5e1'}) do
    case('S2R ' .. text(s), S2R, s)
end
case('S2R nil', S2R, nil)
case('BlzBitAnd -1 5', BlzBitAnd, -1, 5)
case('BlzBitOr 5 3', BlzBitOr, 5, 3)
case('BlzBitXor 5 3', BlzBitXor, 5, 3)
case('BlzBitXor -1 2147483647', BlzBitXor, -1, 2147483647)

for _, x in ipairs({0.0, 0.25, 2.0, 3.0, 25.0, 100.0, 1e6, 0.0001, -1.0}) do
    both('SquareRoot ' .. bits(x), SquareRoot, math.sqrt, x)
end
for _, x in ipairs({0.0, 0.5, 1.0, -1.0, math.pi, 100.0, 1e6}) do
    both('Sin ' .. bits(x), Sin, math.sin, x)
    both('Cos ' .. bits(x), Cos, math.cos, x)
    both('Tan ' .. bits(x), Tan, math.tan, x)
end
for _, x in ipairs({0.0, 0.5, -0.5, 1.0, 2.0}) do
    both('Asin ' .. bits(x), Asin, math.asin, x)
    both('Acos ' .. bits(x), Acos, math.acos, x)
end
for _, x in ipairs({0.0, 1.0, -1.0, 0.5, 100.0}) do
    both('Atan ' .. bits(x), Atan, math.atan, x)
end
for _, c in ipairs({{1.0, 1.0}, {0.0, 0.0}, {0.0, -1.0}, {-1.0, -1.0}, {1.0, 0.0}, {3.0, 4.0}}) do
    both('Atan2 ' .. bits(c[1]) .. ' ' .. bits(c[2]), Atan2, math.atan, c[1], c[2])
end
for _, c in ipairs({{2.0, 10.0}, {2.0, 0.5}, {25.0, 0.5}, {10.0, 3.0}, {1.5, 2.0}, {-2.0, 3.0}, {-8.0, 1 / 3}, {0.0, 0.0},
        {0.0, -1.0}, {2.0, -1.0}, {0.5, 0.5}}) do
    both('Pow ' .. bits(c[1]) .. ' ' .. bits(c[2]), Pow, function(x, y) return x ^ y end, c[1], c[2])
end
for _, x in ipairs({180.0, 90.0, 1.0, 45.0, -30.0, 360.0}) do
    both('Deg2Rad ' .. bits(x), Deg2Rad, math.rad, x)
end
for _, x in ipairs({math.pi, 1.0, 0.5, -2.0}) do
    both('Rad2Deg ' .. bits(x), Rad2Deg, math.deg, x)
end

SetRandomSeed(42)
local draws = {}
for i = 1, 5 do draws[i] = GetRandomInt(0, 99) end
out('GetRandomInt 0 99 after seed 42 = ' .. table.concat(draws, ' '))
SetRandomSeed(42)
for i = 1, 3 do draws[i] = bits(GetRandomReal(0, 1)) end
out('GetRandomReal 0 1 after seed 42 = ' .. table.concat(draws, ' ', 1, 3))
local low, high = math.huge, -math.huge
for _ = 1, 1000 do
    local v = GetRandomInt(5, 1)
    low, high = math.min(low, v), math.max(high, v)
end
out('GetRandomInt 5 1 over 1000 = ' .. low .. ' ' .. high)
low, high = math.huge, -math.huge
for _ = 1, 1000 do
    local v = GetRandomReal(2, 1)
    low, high = math.min(low, v), math.max(high, v)
end
out('GetRandomReal 2 1 over 1000 = ' .. show(low) .. ' ' .. show(high))
case('GetRandomInt 3 3', GetRandomInt, 3, 3)
case('GetRandomReal 1 1', GetRandomReal, 1, 1)

for _, r in ipairs({0.0, -0.0, 1.5, 0.0625, 0.1875, 2.0625, -0.0625, 0.0005, 0.9995, 0.99951, 1 / 3, 2 / 3, 123456.789,
        0.1, -0.0001, 1e-10, 16777217.0, 1e20, -1e20, 3.4e38, math.huge, -math.huge}) do
    case('R2S ' .. bits(r), R2S, r)
end
for _, c in ipairs({{1.5, 8, 2}, {1.5, 0, 0}, {2.5, 0, 0}, {0.5, 0, 0}, {-1.5, 0, 0}, {0.0625, 1, 3}, {-1.5, -8, 2},
        {1.5, 0, -1}, {0.1, 0, 12}, {123.456, 2, 1}, {1.5, 3, 30}, {1e20, 0, 2}}) do
    case('R2SW ' .. bits(c[1]) .. ' ' .. c[2] .. ' ' .. c[3], R2SW, c[1], c[2], c[3])
end
for _, c in ipairs({{'abcdef', 0, 3}, {'abcdef', 2, 2}, {'abcdef', 4, 2}, {'abcdef', 2, 100}, {'abcdef', 6, 7},
        {'abcdef', 10, 12}, {'abcdef', -1, 3}, {'abcdef', -3, -1}, {'abcdef', 0, -1}, {'a\195\164b', 1, 2},
        {'a\195\164b', 1, 3}, {'a\195\164b', 2, 4}, {'', 0, 0}, {'a\0bc', 0, 4}, {'a\0bc', 2, 4}}) do
    case('SubString ' .. text(c[1]) .. ' ' .. c[2] .. ' ' .. c[3], SubString, c[1], c[2], c[3])
end
case('SubString nil 0 1', SubString, nil, 0, 1)
done()
