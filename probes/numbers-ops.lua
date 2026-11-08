-- Subtractions that shift out bits and cancel, and sums and products near the float range's ends, for MirrorImaged's
-- number model (decisions D27, D28, W1-10). Every float is given by its bits. "sub <s>" and "add <s>": a:b:result for
-- a in [1, 2) and b in [2^-s, 2^(1-s)), each with every mantissa bit varied, of a - b and a + b; "sub near": a - b
-- for b a few float steps under a; then sums and products of the floats listed, as a:b:result.
local function bits(x)
    return string.format('%08x', string.unpack('<i4', string.pack('<f', x)))
end
local function float(b)
    return (string.unpack('<f', string.pack('<i4', b)))
end
-- xorshift32, in the game's 32-bit integers
local state = 521288629
local function random()
    state = state ~ state << 13
    state = state ~ state >> 17
    state = state ~ state << 5
    return state
end

for _, s in ipairs({1, 2, 3, 4, 5, 8, 12, 20, 22, 23, 24, 25}) do
    for _, op in ipairs({{'sub', function(a, b) return a - b end}, {'add', function(a, b) return a + b end}}) do
        local line = {}
        for _ = 1, 8 do
            local a = float(0x3f800000 | random() & 0x7fffff)
            local b = float(0x3f800000 - s * 0x800000 | random() & 0x7fffff)
            line[#line + 1] = bits(a) .. ':' .. bits(b) .. ':' .. bits(op[2](a, b))
        end
        out(op[1] .. ' ' .. s .. ' ' .. table.concat(line, ' '))
    end
end
for _, d in ipairs({1, 2, 3, 5, 100, 0x7fffff}) do
    local line = {}
    for _ = 1, 8 do
        local a = float(0x3f800000 | random() & 0x7fffff)
        local b = float(string.unpack('<i4', string.pack('<f', a)) - d)
        line[#line + 1] = bits(a) .. ':' .. bits(b) .. ':' .. bits(a - b)
    end
    out('sub near ' .. d .. ' ' .. table.concat(line, ' '))
end

local function cases(label, f, list)
    for i = 1, #list, 8 do
        local line = {}
        for j = i, math.min(i + 7, #list) do
            local a, b = float(list[j][1]), float(list[j][2])
            line[#line + 1] = bits(a) .. ':' .. bits(b) .. ':' .. bits(f(a, b))
        end
        out(label .. ' ' .. table.concat(line, ' '))
    end
end
cases('add ends', function(a, b) return a + b end, {{0x7f7fffff, 0x7f7fffff}, {0x7f7fffff, 0x7f000000},
    {0x7f000000, 0x7f000000}, {0x7f400000, 0x7f400000}, {0x7f7fffff, 0x73800000}, {0x7f7fffff, 0x73000000},
    {0xff7fffff, 0xff7fffff}, {0x00800000, 0x80800001}, {0x00800001, 0x80800000}, {0x00c00000, 0x80800000},
    {0x01000000, 0x80800001}, {0x00800000, 0x00800000}, {0x00ffffff, 0x80800000}, {0x7f800000, 0x3f800000},
    {0x7f800000, 0xff800000}, {0x7f800000, 0x7f800000}})
cases('mul ends', function(a, b) return a * b end, {{0x7f000000, 0x40000000}, {0x7f000000, 0x40800000},
    {0x7f000000, 0x41000000}, {0x7f000000, 0x41800000}, {0x7f000000, 0x42000000}, {0x7f000000, 0x42800000},
    {0x7f400000, 0x3fc00000}, {0x7f400000, 0x40400000}, {0x7f400000, 0x40c00000}, {0x7f400000, 0x41400000},
    {0x7f7fffff, 0x3f800001}, {0x7f7fffff, 0x3fffffff}, {0x00800000, 0x3f000000}, {0x00800000, 0x3f7fffff},
    {0x00800000, 0x3f800001}, {0x00ffffff, 0x3f000000}, {0x01000000, 0x3f000000}, {0x00800000, 0x3e800000},
    {0x01800000, 0x3e7fffff}, {0x1f800000, 0x1f800000}, {0x20000000, 0x1f800000}, {0x20000000, 0x20000000},
    {0x80800000, 0x3f000000}, {0xff000000, 0x40800000}})
cases('div ends', function(a, b) return a / b end, {{0x7f000000, 0x3f000000}, {0x7f000000, 0x3e800000},
    {0x7f000000, 0x3e000000}, {0x7f000000, 0x3d800000}, {0x7f7fffff, 0x3f7fffff}, {0x00800000, 0x40000000},
    {0x00800000, 0x40800000}, {0x00800000, 0x3fffffff}, {0x3f800000, 0x7f000000}, {0x3f800000, 0x7f7fffff},
    {0x40000000, 0x00000000}, {0x40800000, 0x00000000}, {0x41000000, 0x00000000}, {0x3f000000, 0x00000000},
    {0x00000000, 0x3f800000}, {0x80000000, 0x00000000}})
done()
