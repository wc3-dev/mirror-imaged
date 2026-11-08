-- More of W1-3's natives, after the first probes: StringHash beyond ASCII, numbers given to string parameters, R2S,
-- R2SW and S2R over many values, SquareRoot near 0, and hashtables' limit and destroyed handles.
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
    return type(v) .. ' ' .. (tostring(v):match('^[^:]+'))
end
local function case(name, f, ...)
    local ok, v = pcall(f, ...)
    out(name .. ' = ' .. (ok and show(v) or 'error ' .. text(tostring(v))))
end

-- The letters as UTF-8: a umlaut, A umlaut, CJK zhong, y diaeresis, Y diaeresis, sharp s, capital sharp s, euro, not
-- sign, alpha, beta, capital alpha, capital omega, cyrillic a, cyrillic capital a, an emoji; and bytes that aren't UTF-8.
for _, s in ipairs({'\228\184\173', '-', '\195\191', '\197\184', 'x', '\195\159', '\225\186\158', '\226\130\172',
        '\194\172', '\206\177\206\178', '\206\145', '\206\169', '\208\176', '\208\144', '\240\159\152\128', 'a\195\164',
        '\195\164a', '\195\164\195\164', '\128', '\195', '\195a', 'a\255b', '\255\255', '\237\160\128', 'aB/c\\D'}) do
    case('StringHash ' .. text(s), StringHash, s)
end
case('StringLength 12345', StringLength, 12345)
case('StringLength 1.5', StringLength, 1.5)
case('S2I 42', S2I, 42)
case('StringHash true', StringHash, true)

local reals = {1 / 16, 3 / 16, 5 / 16, 7 / 16, 9 / 16, 11 / 16, 13 / 16, 15 / 16, 1 / 32, 3 / 32, 5 / 32, 1 / 8, 0.1,
    0.2, 0.3, 0.7, 1.1, 2.675, 0.0004999, 0.0005, 1.0005, 1.0015, 2.0005, 0.0015, 0.0025, 0.0035, 1000.0, 12345.0,
    99999.9999, 1e6, 1234567.0, 1e7, 16777215.0, 33554434.0, 1e8, 123456789.0, 1e9, 2147483648.0, 4294967296.0, 1e10,
    1e12, 1e15, 123.456, 3.14159274, 2.71828175, 1234.5678, 99.9995, 4096.0005, 8388607.5, -1.5, -0.0005, -123.4565,
    1e-3, 1e-4, 1e-5, 3.4028235e38, 0.999999, 9.9995, 19.9995, 0.0095, 0.9999999}
for _, r in ipairs(reals) do
    case('R2S ' .. bits(r), R2S, r)
end
for _, w in ipairs({-1, 0, 1, 2, 3, 4, 5, 6, 8, 10, 20}) do
    case('R2SW 1.5 ' .. w .. ' 2', R2SW, 1.5, w, 2)
end
for p = 0, 12 do
    case('R2SW 1.23456705 0 ' .. p, R2SW, 1.23456705, 0, p)
end
for _, c in ipairs({{-1.5, 8, 2}, {-1.5, 4, 2}, {12.25, 8, 1}, {1.5, 8, 0}, {100.0, 4, 0}, {100.0, 6, 3},
        {0.0005, 0, 3}, {123456.789, 0, 5}, {1e20, 30, 2}, {0.5, 0, 1}, {0.05, 0, 1}, {0.45, 0, 1}, {0.95, 0, 1},
        {9.5, 0, 0}, {99.5, 0, 0}, {0.0, 5, 2}, {-0.0, 0, 2}, {-0.004, 0, 2}, {-0.006, 0, 2}, {2.5, 3, -1},
        {1.25, 0, -2}, {1234.5, 2, 0}}) do
    case('R2SW ' .. bits(c[1]) .. ' ' .. c[2] .. ' ' .. c[3], R2SW, c[1], c[2], c[3])
end

for _, s in ipairs({'0.25', '0.75', '0.125', '0.3', '0.7', '2.5', '7.2', '1.1', '1.01', '1.001', '1.0001', '1.00001',
        '1.000001', '1.2345', '3.14159', '0.001', '0.0001', '0.000001', '123.456', '0.123456789', '99999.99',
        '1234567.8', '100.01', '-0.5', '-0', '00.5', '1..5', '1.5.5', '.', '-', '-.5', '+-1', '12345678901234567890',
        '4294967296', '4294967297.5', '2147483648', '0.30000001', '1.5x', '2.50000', '2.500000000000', '0.1000000000',
        '25000000000.5', '1e5', '1 ', '0.5555555555555', '9.999999999', '16777216.5', '1048576.25', '0.0000000001',
        '1.10', '1.100', '1.1000', '10.5', '100.5', '1000.5'}) do
    case('S2R ' .. text(s), S2R, s)
end

for _, x in ipairs({0.01, 0.001, 0.0005, 0.0002, 0.00015, 0.00011, 0.000101, 0.0001, 0.00010001, 9e-5, 1e-5, 1e-10}) do
    case('SquareRoot ' .. bits(x), SquareRoot, x)
end
local low, high = 0, string.unpack('<i4', string.pack('<f', 0.25))
while high - low > 1 do
    local middle = (low + high) // 2
    if SquareRoot((string.unpack('<f', string.pack('<i4', middle)))) == 0 then low = middle else high = middle end
end
out(string.format('SquareRoot is 0 up to %08x and not from %08x', low, high))
for _, x in ipairs({1.0, -1.0, 1.00000012, -1.00000012, -2.0}) do
    case('Asin ' .. bits(x), Asin, x)
    case('Acos ' .. bits(x), Acos, x)
end

local d = CreateTimer()
local ht = InitHashtable()
SaveTimerHandle(ht, 1, 1, d)
DestroyTimer(d)
case('LoadTimerHandle after DestroyTimer', LoadTimerHandle, ht, 1, 1)
case('SaveTimerHandle of a destroyed timer', SaveTimerHandle, ht, 2, 2, d)
case('HaveSavedHandle of a destroyed timer saved', HaveSavedHandle, ht, 2, 2)
case('SaveLocationHandle', SaveLocationHandle, ht, 3, 3, Location(1, 2))
case('SaveTextTagHandle', SaveTextTagHandle, ht, 4, 4, CreateTextTag())
local made = {ht}
for i = 2, 300 do
    local h = InitHashtable()
    if h == nil then
        out('InitHashtable gave nil when ' .. (i - 1) .. ' were made')
        break
    end
    made[i] = h
end
FlushParentHashtable(made[10])
case('InitHashtable after one was flushed', InitHashtable)
case('InitHashtable again', InitHashtable)
done()
