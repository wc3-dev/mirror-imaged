-- A hero's primary attribute set on a unit that is no hero (W2-2): 3 (agility) on a footman closes the game, as the
-- units probe's third run did; 7, which no attribute is, is refused there and changes nothing.
local u = CreateUnit(Player(0), FourCC("hfoo"), 0, 0, 0)
out("refused " .. tostring(BlzSetUnitIntegerField(u, UNIT_IF_PRIMARY_ATTRIBUTE, 7)) .. " " .. BlzGetUnitIntegerField(u, UNIT_IF_PRIMARY_ATTRIBUTE))
BlzSetUnitIntegerField(u, UNIT_IF_PRIMARY_ATTRIBUTE, 3)
out("primary " .. BlzGetUnitIntegerField(u, UNIT_IF_PRIMARY_ATTRIBUTE))
done()
