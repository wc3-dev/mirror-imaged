-- GetHeroStr on a footman, which is no hero (W2-3): a probe of its own, as reading a hero attribute field there closes
-- the game (probe units-attribute).
local f = CreateUnit(Player(0), FourCC("hfoo"), 0, 0, 0)
out("str " .. GetHeroStr(f, false) .. " " .. GetHeroStr(f, true) .. " agi " .. GetHeroAgi(f, false) .. " int " .. GetHeroInt(f, true))
done()
