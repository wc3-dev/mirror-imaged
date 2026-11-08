-- The game's Lua build (decisions D33 to D36), for MirrorImaged's number model (W1-8): where a C89 Windows build and a
-- C99 one differ, the Lua 5.2 compatibility functions, whether load takes a binary chunk, integers made floats, and
-- the order pairs gives a fixed set of string keys. A float is given with its bits.
local function bits(x)
    return string.format('%08x', string.unpack('<i4', string.pack('<f', x)))
end
local function show(v)
    if math.type(v) == 'float' then return 'f ' .. bits(v) .. ' ' .. string.format('%.9g', v) end
    if math.type(v) == 'integer' then return 'i ' .. v end
    if type(v) == 'string' then return 's "' .. v .. '"' end
    return type(v) .. ' ' .. tostring(v)
end
local function case(name, f, ...)
    local ok, v = pcall(f, ...)
    out(name .. ' = ' .. (ok and show(v) or 'error ' .. tostring(v)))
end
local function run(source)
    return function()
        local f, problem = load(source)
        if not f then error(problem, 0) end
        return f()
    end
end

case('math.log 8 2', math.log, 8, 2)
case('math.log 3 2', math.log, 3, 2)
case('math.log 1024 2', math.log, 1024, 2)
case('math.log 100 10', math.log, 100, 10)
case('math.log 1000 10', math.log, 1000, 10)
case('0x1.0000011p0', run('return 0x1.0000011p0'))
case('0xA.8p0', run('return 0xA.8p0'))
case('0x.1p4', run('return 0x.1p4'))
case('tonumber 0x1p-2', tonumber, '0x1p-2')
case('format %a 0.1', string.format, '%a', 0.1)
case('format %a 1.0', string.format, '%a', 1.0)
case('format %A -2.5', string.format, '%A', -2.5)
case('date %F', os.date, '%F', 0)
case('date %e', os.date, '%e', 0)
case('date %#x', os.date, '%#x', 0)
case('date %Ey', os.date, '%Ey', 0)

for _, name in ipairs({'math.pow', 'math.ldexp', 'math.frexp', 'math.cosh', 'math.sinh', 'math.tanh', 'math.log10',
        'math.mod', 'bit32', 'unpack', 'loadstring', 'table.maxn', 'table.getn', 'setfenv', 'module', 'string.gfind',
        'utf8', 'table.move', 'math.ult', 'string.dump', 'require', 'rawlen'}) do
    local v = _G
    for part in name:gmatch('[^.]+') do v = type(v) == 'table' and v[part] or nil end
    out('has ' .. name .. ' = ' .. type(v))
end

-- The start of a binary chunk: refused for its mode, or read as bytecode and found cut short.
local binary = '\27Lua\83\0'
case('load binary', run(binary))
case('load binary mode b', function()
    local f, problem = load(binary, 'binary', 'b')
    if not f then error(problem, 0) end
    return f()
end)

for _, n in ipairs({16777217, 16777219, 16777221, 16777223, 33554435, 33554437, 2147483647, -16777219, -2147483647 - 1}) do
    case('int + 0.0 ' .. n, function(i) return i + 0.0 end, n)
end
case('16777219 / 1', function(i) return i / 1 end, 16777219)
case('16777219 == 16777220.0', function(i, f) return tostring(i == f) end, 16777219, 16777220.0)
case('16777219 < 16777220.0', function(i, f) return tostring(i < f) end, 16777219, 16777220.0)
case('math.tointeger 16777220.0', math.tointeger, 16777220.0)

local t, order = {}, {}
for i, key in ipairs({'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'one', 'two', 'three', 'four', 'five', 'alpha', 'beta',
        'gamma', 'x1', 'x2', 'x10', 'Footman'}) do
    t[key] = i
end
for key in pairs(t) do order[#order + 1] = key end
out('pairs ' .. table.concat(order, ' '))
done()
