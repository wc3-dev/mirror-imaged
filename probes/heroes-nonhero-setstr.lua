-- SetHeroStr on a footman, which is no hero (W2-3): a probe of its own, as setting a hero attribute field there is
-- taken to close the game (probe units-attribute, D227).
local f = CreateUnit(Player(0), FourCC("hfoo"), 0, 0, 0)
SetHeroStr(f, 30, true)
out("set " .. GetWidgetLife(f) .. " " .. BlzGetUnitMaxHP(f))
done()
