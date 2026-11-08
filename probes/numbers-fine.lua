-- Square roots, quotients and zeros at close inputs, for MirrorImaged's number model (decisions D29, D31, W1-10).
-- Every float is given by its bits, 24 a line, each line naming the first input's index. "sqrt 2^-16 <j>": math.sqrt
-- of 1 + j * 2^-16; "sqrt ulp <j>": of 1 + j * 2^-23; "1/d 2^-16 <j>" and "1/d ulp <j>": 1 / d for d the same; "1/d
-- 1.5 <j>": for d = 1.5 + j * 2^-16; "zeros": the quotients and products of signed zeros listed below.
local function bits(x)
    return string.format('%08x', string.unpack('<i4', string.pack('<f', x)))
end
local function float(b)
    return (string.unpack('<f', string.pack('<i4', b)))
end
local function rows(label, f, first, count, step)
    for j = 0, count - 1, 24 do
        local line = {}
        for i = j, math.min(j + 23, count - 1) do
            line[#line + 1] = bits(f(float(first + i * step)))
        end
        out(label .. ' ' .. j .. ' ' .. table.concat(line, ' '))
    end
end

local one = float(0x3f800000)
rows('sqrt 2^-16', math.sqrt, 0x3f800000, 528, 0x80)
rows('sqrt ulp', math.sqrt, 0x3f800000, 264, 1)
rows('1/d 2^-16', function(d) return one / d end, 0x3f800000, 528, 0x80)
rows('1/d ulp', function(d) return one / d end, 0x3f800000, 264, 1)
rows('1/d 1.5', function(d) return one / d end, 0x3fc00000, 264, 0x80)
local zero, negative = float(0x00000000), float(0x80000000)
local line = {}
for _, v in ipairs({zero / zero, negative / zero, zero / negative, negative / negative, one / negative, -one / zero,
        zero / one, negative / one, zero / -one, negative / -one, zero * one, negative * one, zero * -one, one * negative,
        zero - zero, negative - zero, zero + negative, negative + negative, float(0x00000001) / zero,
        float(0x00400000) / one, one / float(0x00400000), float(0x00400000) * one}) do
    line[#line + 1] = bits(v)
end
out('zeros ' .. table.concat(line, ' '))
done()
