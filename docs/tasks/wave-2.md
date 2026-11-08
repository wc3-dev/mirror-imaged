# Wave 2: data and units

Plan sections: 4 (A1, A2, A6), 6 (map and game data, units and heroes, items and destructables), 9 (wave 2). Needs
wave 1. Facts are in `C:\Users\mdlsvensson\Repo\wc3-cs\docs\game-facts.md`; the sections named below are the source
of the tests, and every native a fact measures is Exact.

Order: W2-1, then W2-2; then W2-3 to W2-7, three at a time; W2-8 after W2-7.

Parallel lanes: decision numbers by range: W2-1 D200-D219, W2-2 D220-D239, W2-3 D240-D249, W2-4 D250-D259,
W2-5 D260-D269, W2-6 D270-D279, W2-7 D280-D289, W2-8 D290-D299. Generated-file conflicts are resolved by re-running the generator.
W2-1 started before wave 1 finished (11-08): it touches the data loading, which wave 1's open lanes don't.

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
  names in `war3map.w3i` and `war3map.wts` (D141: only a computer reads its `war3map.w3i` name; a user is "Local
  Player" or "Player N", an unplayed slot "Player N"; `Seat` passes a `TRIGSTR_n` name through as it is, so resolve it
  through `war3map.wts`; then `GetPlayerName` can become Exact).
- Like the W1-1 seam for `blizzard.j`: runtime tests and CI must be able to supply their own small data instead of
  the install's (rule 5).
- Tests with maps built in the tests; a `Needs=Game` test that the standard footman, Paladin and Holy Light read the
  values the game's data gives.
- Left by W2-1 for a follow-up (decisions D212): `ParseTags`, with the rules `docs/facts.md` ("The game's data")
  measured; the length of a map's and the game's sound files (plan, section 6).

## W2-2 Units

Needs W2-1.

- Creating, removing and killing units; every property and state native (life, mana, their maxima, position, height
  from the terrain, facing and turning at the turn rate, fly height, owner, colour, scale and vertex colour stored,
  pause, hide, invulnerable, the unit field natives `BlzGetUnit*Field`/`BlzSetUnit*Field`), death, decay and corpses,
  the unit events of this wave (death, owner change), and `SetUnitOwner`'s stop inside the call (facts, "Units",
  "Orders and events").
- Tests from every measured case in the facts' "Units" section that needs no movement.
- From W2-1's review: `GetObjectName` (with the facts' "Default string" for a new buff's or destructable's name in the
  skin files), and which unit fields apply to which units (D205: `useUnit`, `useHero`, `useBuilding`) for the
  `BlzGetUnit*Field` natives.

## W2-3 Heroes and items

Needs W2-2.

- Heroes: experience and levels by the constants' formula (facts on `NeedHeroXP`), attributes and what they give,
  skill points and learning, revival, hero events.
- Items: creating, removing, owners, charges, `UnitAddItem` and `UnitAddItemById` with their events and slots, the
  inventory natives, dropping, powerups, the bag (facts, "Orders and events" items).
- Left by W1-5: the unit game-cache natives `StoreUnit`, `RestoreUnit`, `HaveStoredUnit`, `FlushStoredUnit`,
  `SyncStoredUnit`, with the measured case (game-facts, about line 709) that a Paladin's Gloves of Haste survived a
  store and restore though `StoreUnit` returned false (its attack-speed half needs wave 3). A probe of these never
  calls `SaveGameCache`.
- From W2-1's review: a fixed-order list of a kind's objects in the data model for `ChooseRandomItem`/`Ex` (the facts
  on `pickRandom` and levels), and for `ChooseRandomCreep` and `ChooseRandomNPBuilding` (melee creep drops, A4).

## W2-4 Destructables, trees and doodads

Needs W2-1.

- Destructables and trees: creating (nil for a unit's id), life, restoring, killing, gates, enumerating in a rect,
  trees snapping to the 64 grid, doodad animation natives recorded (facts, "Units" destructables).
- From W2-1's review: doodad variation numbering (D204: whether the file counts variations from 0); bridges and
  walkable destructables in `GetLocationZ` (D212); destructable and doodad names are World Editor string keys
  (`WESTRING_DEST_SUMMER_TREE_WALL`): probe `GetDestructableName` and resolve `UI\WorldEditStrings.txt` if the game
  does; `(1)SilverpineSprint.w3x`'s `war3map.doo` that War3Net can't read (D209) — fix the reader (our own handling or
  the fork's fix), since the game makes `.doo` destructables itself (A5).

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
- From W1-2's review (D70): every callback runs in a Lua thread of its own, measured only for timers, conditions,
  actions and `ExecuteFunc`. Probe `ForGroup`, `ForForce`, `Enum*` and filter callbacks: `coroutine.isyieldable`,
  `coroutine.running`, and a wait inside them (in the game a wait may suspend the whole calling action).
- From W2-1's review: `(10)DustwallowKeys.w3m`'s `war3map.w3e` that War3Net can't read (D209), an A4 melee map with no
  terrain without it — fix the reader; and measure the off-map edge rule of `Terrain` (a point off the map reads its
  nearest edge, inferred).

## W2-7 Abilities on units, without casting

Needs W2-2.

- `UnitAddAbility`, `UnitRemoveAbility`, levels, hiding and disabling, permanence, the ability field natives
  `BlzGetAbility*`/`BlzSetAbility*` and the unit's ability fields, tech and upgrades applied as data (facts, "Spells
  and abilities" for what needs no cast). The cast flow and effects are wave 4.
- `SetPlayerAbilityAvailable` (facts, "Version and launching": false takes the ability off the command card).
- Left by W1-5 (with W2-1's data): the ability-tooltip carrier, `BlzSetAbilityTooltip`, `BlzGetAbilityTooltip`,
  `BlzSetAbilityExtendedTooltip` and its getter, with the "Files and Preload" cases: a tooltip is kept only for the
  ability's levels (3 on Blizzard; on a 100-level copy, levels above 2 read level 2's; with `Tooltip = "-"` levels
  0-99 hold and 100 up read 99's), pieces of 200-213 characters come back whole, tooltips make no handles. Then give
  `probes/files.lua` its tooltip read back.

## W2-8 Tooltip tags and ability columns

Needs W2-1 and W2-7. Decisions D290-D299. From W2-1's review:

- `ParseTags`, with the facts' rules (it reads table columns by name with the map's changes; unit columns don't
  follow one rule: `HP` reads, `spd` doesn't); standard tooltips carry `<AHhb,DataA1>` tags.
- Map changes stored by column, not by field id: ability ids share columns (`Hbz1` read on AHhb gives Holy Light's
  DataA), so a change written under one id must be visible through another id of the same column.
- Which ability fields apply to which ability (D205's `useSpecific`).
