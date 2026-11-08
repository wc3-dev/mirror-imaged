-- Levels, experience and revival on a footman, which is no hero (W2-3); a probe of its own in case one of them closes
-- the game, as reading a hero attribute field on a footman does (probe units-attribute).
local f = CreateUnit(Player(0), FourCC("hfoo"), 0, 0, 0)
SetHeroLevel(f, 3, false)
out("level " .. GetHeroLevel(f) .. " " .. GetUnitLevel(f) .. " " .. GetHeroXP(f) .. " " .. GetWidgetLife(f))
SetHeroXP(f, 500, false)
out("xp " .. GetHeroLevel(f) .. " " .. GetHeroXP(f))
AddHeroXP(f, 500, false)
out("add " .. GetHeroLevel(f) .. " " .. GetHeroXP(f))
out("strip " .. tostring(UnitStripHeroLevel(f, 1)) .. " " .. GetHeroLevel(f))
KillUnit(f)
out("revive " .. tostring(ReviveHero(f, 0, 0, false)) .. " " .. tostring(UnitAlive(f)))
done()
