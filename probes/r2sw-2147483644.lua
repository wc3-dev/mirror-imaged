-- W1-3's R2SW(1.5, 2147483644, 0), alone, as a width this far out might have closed the game: the text's length, its
-- first 4 bytes and its last 8.
local v = R2SW(1.5, 2147483644, 0)
out('R2SW 1.5 2147483644 0 = ' .. #v .. ' "' .. v:sub(1, 4) .. '...' .. v:sub(-8) .. '"')
done()
