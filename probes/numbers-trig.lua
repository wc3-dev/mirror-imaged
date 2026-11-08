-- math.sin, math.cos and math.tan in radians, for MirrorImaged's number model (decisions D32, W1-10). Every float is
-- given by its bits, 24 a line. "<f> <k>": f of x = k / 64 and the 23 after it; "<f> small": of 2^-1 to 2^-40;
-- "<f> near 0": of k * 2^-14 for k 1 to 48; "<f> large": of the floats in largeBits below.
local function bits(x)
    return string.format('%08x', string.unpack('<i4', string.pack('<f', x)))
end
local function float(b)
    return (string.unpack('<f', string.pack('<i4', b)))
end
local function rows(label, f, inputs, first, last)
    for i = first or 1, last or #inputs, 24 do
        local line = {}
        for j = i, math.min(i + 23, last or #inputs) do
            line[#line + 1] = bits(f(inputs[j]))
        end
        out(label(i) .. ' ' .. table.concat(line, ' '))
    end
end

local steps, small, near, large = {}, {}, {}, {}
for k = -64, 447 do steps[#steps + 1] = k * float(0x3c800000) end
for k = 1, 40 do small[#small + 1] = float(0x3f800000 - k * 0x800000) end
for k = 1, 48 do near[#near + 1] = k * float(0x38800000) end
local largeBits = {0x41200000, 0x42c80000, 0x447a0000, 0x461c4000, 0x47c35000, 0x49742400, 0x4b189680, 0x4cbebc20,
    0x4e6e6b28, 0x40490fdb, 0x40c90fdb, 0x41490fdb, 0x41c90fdb, 0x42490fdb, 0x42c90fdb, 0x43490fdb, 0x43c90fdb,
    0x3fc90fdb, 0x4096cbe4, 0x40fb53d1, 0x45800000, 0x46000000, 0x46800000, 0x47000000, 0x47800000, 0x48000000,
    0x49000000, 0x4a000000, 0x4b000000, 0x4c000000, 0x4d000000, 0x4e000000, 0x4f000000, 0x4f800000, 0x50000000,
    0x5f000000, 0x7f7fffff, 0xc0490fdb, 0xc2c80000, 0xc9742400}
for _, b in ipairs(largeBits) do large[#large + 1] = float(b) end

for _, f in ipairs({{'sin', math.sin}, {'cos', math.cos}}) do
    rows(function(i) return f[1] .. ' ' .. (i - 65) end, f[2], steps)
end
rows(function(i) return 'tan ' .. (i - 65) end, math.tan, steps, 1, 192)
for _, f in ipairs({{'sin', math.sin}, {'cos', math.cos}, {'tan', math.tan}}) do
    rows(function() return f[1] .. ' small' end, f[2], small)
    rows(function() return f[1] .. ' near 0' end, f[2], near)
    rows(function() return f[1] .. ' large' end, f[2], large)
end
done()
