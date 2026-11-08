-- The game's own blizzard.j as a Lua map runs it: ModuloInteger's integer division (floored as Lua's // or cut toward
-- zero as JASS's /), and a real global blizzard.j gives a whole number (a Lua integer or a float; decisions D68).
out("modulo " .. ModuloInteger(7, -3) .. " " .. ModuloInteger(-7, 3) .. " " .. ModuloInteger(-7, -3) .. " " .. ModuloInteger(7, 3))
out("types " .. math.type(bj_MELEE_MINE_SEARCH_RADIUS) .. " " .. tostring(bj_MELEE_MINE_SEARCH_RADIUS) .. " " .. math.type(bj_cineFadeContinueRed) .. " " .. math.type(bj_MELEE_CLEAR_UNITS_RADIUS))
out("modulo zero " .. tostring(pcall(ModuloInteger, 1, 0)))
done()
