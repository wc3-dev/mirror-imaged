-- W1-3's R2SW(-1.5, 254, 2), alone: its text, with the sign, passes the 255 bytes the game writes at most, and the game
-- closes (0xC0000409) before the line.
local v = R2SW(-1.5, 254, 2)
out('R2SW -1.5 254 2 = ' .. #v)
done()
