-- math.asin, acos, atan, exp, log, ^, rad and deg, for MirrorImaged's number model (decisions D32, W1-10). Every
-- float is given by its bits, 24 a line, each line naming its inputs: "k/64 <k>" for x = k / 64 from that k on, and
-- the like; "bits" lines take their inputs from the lists below, in order.
local function bits(x)
    return string.format('%08x', string.unpack('<i4', string.pack('<f', x)))
end
local function float(b)
    return (string.unpack('<f', string.pack('<i4', b)))
end
local function rows(label, f, inputs)
    for i = 1, #inputs, 24 do
        local line = {}
        for j = i, math.min(i + 23, #inputs) do
            line[#line + 1] = bits(f(inputs[j]))
        end
        out(label(i) .. ' ' .. table.concat(line, ' '))
    end
end
local function steps(first, last, step)
    local t = {}
    for k = first, last do t[#t + 1] = k * step end
    return t
end
local function floats(list)
    local t = {}
    for i, b in ipairs(list) do t[i] = float(b) end
    return t
end

local sixtyFourth, sixteenth = float(0x3c800000), float(0x3d800000)
local unit = steps(-64, 64, sixtyFourth)
rows(function(i) return 'asin k/64 ' .. (i - 65) end, math.asin, unit)
rows(function(i) return 'acos k/64 ' .. (i - 65) end, math.acos, unit)
local outside = floats({0x3f800001, 0xbf800001, 0x40000000, 0xc0000000, 0x501502f9, 0x7f7fffff})
rows(function() return 'asin bits' end, math.asin, outside)
rows(function() return 'acos bits' end, math.acos, outside)
rows(function(i) return 'atan k/16 ' .. (i - 65) end, math.atan, steps(-64, 64, sixteenth))
rows(function() return 'atan bits' end, math.atan, floats({0x42c80000, 0x461c4000, 0x501502f9, 0x7f7fffff,
    0x3a83126f, 0x358637bd, 0xc2c80000}))
local grid = floats({0xc0000000, 0xbf800000, 0xbf000000, 0x00000000, 0x3f000000, 0x3f800000, 0x40000000})
local pairs_ = {}
for _, y in ipairs(grid) do
    for _, x in ipairs(grid) do pairs_[#pairs_ + 1] = {y, x} end
end
rows(function(i) return 'atan2 grid ' .. (i - 1) end, function(p) return math.atan(p[1], p[2]) end, pairs_)

rows(function(i) return 'exp k/16 ' .. (i - 161) end, math.exp, steps(-160, 160, sixteenth))
rows(function() return 'exp bits' end, math.exp, floats({0x42b00000, 0x42b17218, 0x42b20000, 0x42c80000,
    0xc2ae0000, 0xc2ce0000, 0xc2d00000, 0xc2dc0000, 0x33800000, 0xb3800000, 0x3dcccccd, 0xbdcccccd}))
rows(function(i) return 'log 1+k/256 ' .. (i - 1) end, math.log, steps(256, 511, float(0x3b800000)))
rows(function(i) return 'log 2^k ' .. (i - 21) end, math.log, floats((function()
    local t = {}
    for k = -20, 20 do t[#t + 1] = 0x3f800000 + k * 0x800000 end
    return t
end)()))
rows(function() return 'log bits' end, math.log, floats({0x40400000, 0x41200000, 0x42c80000, 0x447a0000,
    0x49742400, 0x3dcccccd, 0x3a83126f, 0x7f7fffff, 0x00800000, 0x00000000, 0x80000000, 0xbf800000}))
local whole = steps(1, 48, 1)
for _, base in ipairs({10, 2, 3}) do
    rows(function(i) return 'log k ' .. base .. ' ' .. i end, function(x) return math.log(x, base) end, whole)
end

local bases = floats({0x3e800000, 0x3f000000, 0x3fc00000, 0x40000000, 0x40400000, 0x40a00000, 0x41200000,
    0x41c80000, 0x42c80000, 0x447a0000, 0xc1000000, 0xc0000000, 0x00000000})
local powers = floats({0xc0000000, 0xbf800000, 0xbf000000, 0x3f000000, 0x3eaaaaab, 0x3fc00000, 0x40000000,
    0x40200000, 0x40400000, 0x41200000, 0x00000000})
local pow = {}
for _, x in ipairs(bases) do
    for _, y in ipairs(powers) do pow[#pow + 1] = {x, y} end
end
rows(function(i) return 'pow grid ' .. (i - 1) end, function(p) return p[1] ^ p[2] end, pow)

rows(function(i) return 'rad k*5 ' .. (i - 1) end, math.rad, steps(0, 72, 5))
rows(function(i) return 'deg k/8 ' .. (i - 1) end, math.deg, steps(0, 56, float(0x3e000000)))
done()
