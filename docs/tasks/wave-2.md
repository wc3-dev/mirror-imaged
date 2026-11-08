# Wave 2: data and units

Plan sections: 4 (A1, A2, A6), 6 (map and game data, units and heroes, items and destructables), 9 (wave 2). Needs
wave 1. Facts are in `C:\Users\mdlsvensson\Repo\wc3-cs\docs\game-facts.md`; the sections named below are the source
of the tests, and every native a fact measures is Exact.

Order: W2-1, then W2-2; then W2-3 to W2-7, three at a time.

## W2-1 Map files and the game's standard data

- Every map file: `war3map.w3i` (players, forces, settings), `war3map.wts` strings (`TRIGSTR_n`), object data
  (`w3u w3t w3a w3b w3d w3h w3q`), `war3mapMisc.txt` and the gameplay constants, `war3map.w3e` (heights and cliffs,
  read as the facts' "Units" section measures `GetLocationZ`), `war3map.wpm` pathing, `war3map.doo` and
  `war3mapUnits.doo`, regions (`w3r`), cameras (`w3c`), sounds (`w3s`), imports, and the FDF and TOC files.
- The standard data from the installed game (the SLK tables and profile text files for units, items, abilities,
  buffs, upgrades, destructables, doodads, and the gameplay constants), merged with the map's changes as the game
  merges them (facts, "Object data", "World Editor and map files").
- One data model the world reads: an object by its id, each field by its code and level.
- `GetLocalizedString`, `GetLocalizedHotkey` and `ParseTags` (string tables from the game's data; `blizzard.j` calls
  `GetLocalizedString` often), left by W1-3.
- Left by W1-4 for this task: an upgrade's level count (`GetPlayerTechMaxAllowed`'s default and the cap on
  `AddPlayerTechResearched`, D93), the map's day length and hours (`DayHours`, `DayLength`; D96), and the player
  names in `war3map.w3i` and `war3map.wts` (D93: a named slot reads its map name; the score screen showed "The Horde").
- Like the W1-1 seam for `blizzard.j`: runtime tests and CI must be able to supply their own small data instead of
  the install's (rule 5).
- Tests with maps built in the tests; a `Needs=Game` test that the standard footman, Paladin and Holy Light read the
  values the game's data gives.

## W2-2 Units

Needs W2-1.

- Creating, removing and killing units; every property and state native (life, mana, their maxima, position, height
  from the terrain, facing and turning at the turn rate, fly height, owner, colour, scale and vertex colour stored,
  pause, hide, invulnerable, the unit field natives `BlzGetUnit*Field`/`BlzSetUnit*Field`), death, decay and corpses,
  the unit events of this wave (death, owner change), and `SetUnitOwner`'s stop inside the call (facts, "Units",
  "Orders and events").
- Tests from every measured case in the facts' "Units" section that needs no movement.

## W2-3 Heroes and items

Needs W2-2.

- Heroes: experience and levels by the constants' formula (facts on `NeedHeroXP`), attributes and what they give,
  skill points and learning, revival, hero events.
- Items: creating, removing, owners, charges, `UnitAddItem` and `UnitAddItemById` with their events and slots, the
  inventory natives, dropping, powerups, the bag (facts, "Orders and events" items).

## W2-4 Destructables, trees and doodads

Needs W2-1.

- Destructables and trees: creating (nil for a unit's id), life, restoring, killing, gates, enumerating in a rect,
  trees snapping to the 64 grid, doodad animation natives recorded (facts, "Units" destructables).

## W2-5 Damage

Needs W2-2.

- `UnitDamageTarget`, `UnitDamagePoint`, the damage events (`EVENT_UNIT_DAMAGING`, `EVENT_UNIT_DAMAGED`), every
  `BlzGetEvent*`/`BlzSetEvent*` damage native, the attack and armour table and the armour formula from the constants,
  death inside the call (facts, "Damage"). Abilities that change damage (Hardened Skin, Divine Shield and the rest)
  come with wave 4; record them in that wave's file if you see them.

## W2-6 Groups, rects, regions and locations

Needs W2-2.

- Groups and every enumeration (`GroupEnumUnitsInRange` with its collision rule, in rect, of player, of type,
  selected later), `ForGroup`, `FirstOfGroup` with removed units, rects, regions with their 32-cell rule, enter and
  leave events (including `SetUnitX`/`SetUnitY`), locations and `GetLocationZ` (facts, "Units", "Groups and
  performance").
- Left by W1-4: `DefineStartLocationLoc` and `GetStartLocationLoc`.

## W2-7 Abilities on units, without casting

Needs W2-2.

- `UnitAddAbility`, `UnitRemoveAbility`, levels, hiding and disabling, permanence, the ability field natives
  `BlzGetAbility*`/`BlzSetAbility*` and the unit's ability fields, tech and upgrades applied as data (facts, "Spells
  and abilities" for what needs no cast). The cast flow and effects are wave 4.
- `SetPlayerAbilityAvailable` (facts, "Version and launching": false takes the ability off the command card).
