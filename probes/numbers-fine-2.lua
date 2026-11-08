-- math.sin, cos, atan, exp and log at close inputs, for MirrorImaged's number model (decisions D32, W1-10). Every
-- float is given by its bits, 24 a line, each line naming the first input's index j. "sin tiny": sin of j * 2^-20;
-- "sin 1": of 1 + j * 2^-20; "sin 0.5": of 0.5 + j * 2^-21; "cos tiny": cos of j * 2^-12; "atan tiny": atan of
-- j * 2^-16; "exp tiny": exp of j * 2^-20; "exp 1": of 1 + j * 2^-20; "log 1": log of 1 + j * 2^-16; "log 1.5": of
-- 1.5 + j * 2^-20.
local function bits(x)
    return string.format('%08x', string.unpack('<i4', string.pack('<f', x)))
end
local function rows(label, f, first, count, step)
    for j = 0, count - 1, 24 do
        local line = {}
        for i = j, math.min(j + 23, count - 1) do
            line[#line + 1] = bits(f(first + i * step))
        end
        out(label .. ' ' .. j .. ' ' .. table.concat(line, ' '))
    end
end
local function pow2(k)
    return (string.unpack('<f', string.pack('<i4', 0x3f800000 + k * 0x800000)))
end

rows('sin tiny', math.sin, 0, 264, pow2(-20))
rows('sin 1', math.sin, 1, 264, pow2(-20))
rows('sin 0.5', math.sin, pow2(-1), 264, pow2(-21))
rows('cos tiny', math.cos, 0, 264, pow2(-12))
rows('atan tiny', math.atan, 0, 144, pow2(-16))
rows('exp tiny', math.exp, 0, 144, pow2(-20))
rows('exp 1', math.exp, 1, 144, pow2(-20))
rows('log 1', math.log, 1, 144, pow2(-16))
rows('log 1.5', math.log, pow2(0) + pow2(-1), 144, pow2(-20))
done()
