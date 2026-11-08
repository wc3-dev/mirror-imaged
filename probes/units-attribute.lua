-- A hero attribute field read on a unit that is no hero (W2-2): UNIT_IF_AGILITY on a footman closes the game, as the
-- units probe's first runs did.
local u = CreateUnit(Player(0), FourCC("hfoo"), 0, 0, 0)
out("agility " .. BlzGetUnitIntegerField(u, UNIT_IF_AGILITY))
done()
