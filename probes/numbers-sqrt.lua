-- math.sqrt and x ^ 0.5 over the mantissas of [1, 4), for MirrorImaged's number model (decisions D31, W1-10). Every
-- float is given by its bits. "sqrt <k>": the roots of x = 1 + k / 512 (k below 512) and 2 + (k - 512) / 256, and the
-- 23 after it in a line; "pow" the same for x ^ 0.5. "sqrt random": x:root for x with every mantissa bit varied.
local function bits(x)
    return string.format('%08x', string.unpack('<i4', string.pack('<f', x)))
end
local function float(b)
    return (string.unpack('<f', string.pack('<i4', b)))
end
-- xorshift32, in the game's 32-bit integers
local state = 362436069
local function random()
    state = state ~ state << 13
    state = state ~ state >> 17
    state = state ~ state << 5
    return state
end
local function x(k)
    return float(k < 512 and 0x3f800000 + k * 0x4000 or 0x40000000 + (k - 512) * 0x8000)
end

local half = float(0x3f000000)
for _, f in ipairs({{'sqrt', math.sqrt}, {'pow', function(v) return v ^ half end}}) do
    for k = 0, 1023, 24 do
        local line = {}
        for i = k, math.min(k + 23, 1023) do
            line[#line + 1] = bits(f[2](x(i)))
        end
        out(f[1] .. ' ' .. k .. ' ' .. table.concat(line, ' '))
    end
end
for _ = 1, 20 do
    local line = {}
    for _ = 1, 13 do
        local v = float(0x3f800000 + (random() & 0xffffff))
        line[#line + 1] = bits(v) .. ':' .. bits(math.sqrt(v))
    end
    out('sqrt random ' .. table.concat(line, ' '))
end
done()
