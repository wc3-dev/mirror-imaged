-- Division in the game's floats, for MirrorImaged's number model (decisions D29, W1-10). Every float is given by its
-- bits. "1/d <k>": 1 / d for d = 1 + k / 1024 and the 23 after it in a line. "1/d random": d:quotient for divisors
-- with every mantissa bit varied. "n/d <i>": n / d for n = 1 + i / 16 and d = 1 + j / 16, j 0 to 15. "n/d random":
-- n:d:quotient. "n/d signs and octaves": quotients of numbers of other signs and octaves.
local function bits(x)
    return string.format('%08x', string.unpack('<i4', string.pack('<f', x)))
end
local function float(b)
    return (string.unpack('<f', string.pack('<i4', b)))
end
-- xorshift32, in the game's 32-bit integers
local state = 123456789
local function random()
    state = state ~ state << 13
    state = state ~ state >> 17
    state = state ~ state << 5
    return state
end
local function mantissa()
    return float(0x3f800000 | random() & 0x7fffff)
end

local one = float(0x3f800000)
for k = 0, 1023, 24 do
    local line = {}
    for i = k, math.min(k + 23, 1023) do
        line[#line + 1] = bits(one / float(0x3f800000 + i * 0x2000))
    end
    out('1/d ' .. k .. ' ' .. table.concat(line, ' '))
end
for _ = 1, 24 do
    local line = {}
    for _ = 1, 13 do
        local d = mantissa()
        line[#line + 1] = bits(d) .. ':' .. bits(one / d)
    end
    out('1/d random ' .. table.concat(line, ' '))
end
for i = 0, 15 do
    local line = {}
    for j = 0, 15 do
        line[#line + 1] = bits(float(0x3f800000 + i * 0x80000) / float(0x3f800000 + j * 0x80000))
    end
    out('n/d ' .. i .. ' ' .. table.concat(line, ' '))
end
for _ = 1, 24 do
    local line = {}
    for _ = 1, 8 do
        local n, d = mantissa(), mantissa()
        line[#line + 1] = bits(n) .. ':' .. bits(d) .. ':' .. bits(n / d)
    end
    out('n/d random ' .. table.concat(line, ' '))
end
local line = {}
for _, nd in ipairs({{0x40400000, 0x40e00000}, {0xc0400000, 0x40e00000}, {0x40400000, 0xc0e00000},
        {0x3eaaaaab, 0x3f800000}, {0x47c35000, 0x3dcccccd}, {0x3dcccccd, 0x47c35000}, {0x7f7fffff, 0x3f800001},
        {0x7f7fffff, 0x3f000000}, {0x00800000, 0x40000000}, {0x00800000, 0x3f800001}, {0x3f800000, 0x7f7fffff},
        {0x3f800000, 0x7e800000}, {0x4b7fffff, 0x3f7fffff}, {0x3f7fffff, 0x3f7fffff}, {0x3f800000, 0x3f800001},
        {0x3f800000, 0x3fffffff}, {0x3fffffff, 0x3f800001}, {0x3fffffff, 0x3fffffff}}) do
    line[#line + 1] = bits(float(nd[1]) / float(nd[2]))
end
out('n/d signs and octaves ' .. table.concat(line, ' '))
done()
