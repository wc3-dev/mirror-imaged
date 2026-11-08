-- math.sqrt of the whole squares n * n for n 1 to 4096, in plain Lua (decisions D152, W1-10's fix round): the facts'
-- (int)Sqrt(n * n) went wrong for 64 of them through C#. "sqrt n*n <n>": the bits of the roots of n * n from that n on,
-- 24 a line; "wrong": how many roots math.floor takes below n.
local function bits(x)
    return string.format('%08x', string.unpack('<i4', string.pack('<f', x)))
end

local wrong = 0
for n = 1, 4096, 24 do
    local line = {}
    for i = n, math.min(n + 23, 4096) do
        local r = math.sqrt(i * i)
        line[#line + 1] = bits(r)
        if math.floor(r) ~= i then wrong = wrong + 1 end
    end
    out('sqrt n*n ' .. n .. ' ' .. table.concat(line, ' '))
end
out('wrong ' .. wrong)
done()
