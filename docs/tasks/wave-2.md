# Wave 2: data and units

Plan sections: 4 (A1, A2, A6), 6 (map and game data, units and heroes, items and destructables), 9 (wave 2). Needs
wave 1. Facts are in `C:\Users\mdlsvensson\Repo\wc3-cs\docs\game-facts.md`; the sections named below are the source
of the tests, and every native a fact measures is Exact.

Order: W2-1, then W2-2; then W2-3 to W2-7, three at a time; W2-8 after W2-7; W2-9 any time after W2-1; W2-10 after W2-2.

Parallel lanes: decision numbers by range: W2-1 D200-D219, W2-2 D220-D239, W2-3 D240-D249, W2-4 D250-D259,
W2-5 D260-D269, W2-6 D270-D279, W2-7 D280-D289, W2-8 D290-D299, W2-9 D300-D309, W2-10 D310-D319. Generated-file conflicts are resolved by re-running the generator.
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
- Small items from W2-1's last review: the exponent-free real writer loses digits from about 1e-21, not 1e-28
  (fix `GameData.cs`'s comment and D214, or write exact digits); test the two inferred `ReadsTrigstr` entries
  (`DisplayTextToPlayer`, `DisplayTimedTextToPlayer`); list `InvalidDataException` in `Simulation.Load`'s doc.

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
- Left by W2-2 (D221, D227, D228; `probes/differences/units.txt`): a hero's attributes set through the field natives
  change its life, mana and armour (the probe's strength 30 gave the Paladin 850 life); the permanent attribute
  fields (read 1 after a set of 7 and of 0); a hero's proper name drawn at random from its type's and numbered once
  taken ("Zann the Defender XII"), for `GetHeroProperName` and `UNIT_SF_PROPER_NAMES`; and a dead hero stays as
  W2-2 leaves it, for revival. A hero's level set through `UNIT_IF_LEVEL` read one more than set (7 to 8, 3 to 4),
  both from level 1, so adding the value fits too (D227): probe a hero at level 5 set to 3 (4 by the first, 8 by the
  second).
- From W2-2's review: a hero's most life and mana are fixed at creation from the type's strength and intelligence,
  armour uses the type's agility, and the nine attribute fields are stored apart: make one attribute state (base,
  bonus, permanent) that life, mana, armour and the fields derive from. Also `BlzResetUnitTalents` (game-facts) and
  `BlzSetHeroProperName`.

## W2-4 Destructables, trees and doodads

Needs W2-1.

- Destructables and trees: creating (nil for a unit's id), life, restoring, killing, gates, enumerating in a rect,
  trees snapping to the 64 grid, doodad animation natives recorded (facts, "Units" destructables).
- From W2-1's review: doodad variation numbering (D204: whether the file counts variations from 0); bridges and
  walkable destructables in `GetLocationZ` (D212); destructable and doodad names are World Editor string keys
  (`WESTRING_DEST_SUMMER_TREE_WALL`): probe `GetDestructableName` and resolve `UI\WorldEditStrings.txt` if the game
  does; `(1)SilverpineSprint.w3x`'s `war3map.doo` that War3Net can't read (D209) — fix the reader (our own handling or
  the fork's fix), since the game makes `.doo` destructables itself (A5).
- From W2-2's review: `Happening.Unit` is typed `unit` and `GetTriggerWidget` returns it; destructable deaths through
  `TriggerRegisterDeathEvent` need a widget there, with `GetTriggerUnit` and `GetDyingUnit` nil for them.

## W2-5 Damage

Needs W2-2.

- `UnitDamageTarget`, `UnitDamagePoint`, the damage events (`EVENT_UNIT_DAMAGING`, `EVENT_UNIT_DAMAGED`), every
  `BlzGetEvent*`/`BlzSetEvent*` damage native, the attack and armour table and the armour formula from the constants,
  death inside the call (facts, "Damage"). Abilities that change damage (Hardened Skin, Divine Shield and the rest)
  come with wave 4; record them in that wave's file if you see them.
- Left by W2-2 (D234): the weapon field natives `BlzGetUnitWeapon*Field`/`BlzSetUnitWeapon*Field` (the facts: `ua1t`
  reads `ATTACK_TYPE_*`, `ua1g` the target flags, "air,enemies" 65540), and a killer for `GetKillingUnit`.
- From W2-2's review: `IsUnitType`'s attack kinds and `Armed()` read the type's weapon data, not the unit's weapon
  fields once those can be set; armour needs a unit armour state for `BlzSetUnitArmor` and the formula; `FireUnit` reads
  the unit's and its owner's registrations (kept apart since W2-2's fixes), each list once a kind: index them by event
  too if a hit's events show in A10. Also `BlzGetUnitArmor`/`BlzSetUnitArmor`, `BlzGetUnitBaseDamage`, the dice natives,
  `BlzGetUnitAttackCooldown`, `BlzUnitHeal` (game-facts: nil source heals nothing, -30 takes 30).

## W2-6 Groups, rects, regions and locations

Needs W2-2.

- Groups and every enumeration (`GroupEnumUnitsInRange` with its collision rule, in rect, of player, of type,
  selected later), `ForGroup`, `FirstOfGroup` with removed units, rects, regions with their 32-cell rule, enter and
  leave events (including `SetUnitX`/`SetUnitY`), locations and `GetLocationZ` (facts, "Units", "Groups and
  performance").
- Left by W1-4: `DefineStartLocationLoc` and `GetStartLocationLoc`.
- Left by W2-2 (D223, D225): `GetFilterUnit` for the enumerations (a player-unit event's filter never runs), the map's
  playable area for `SetUnitPosition` and for `CreateUnit` off the map (the game put one asked at (5000, -5000) on the
  32 by 32 probe map at (2032, -2000)), and `GetUnitLoc`.
- Left by W5-1 (D505): the camera's location natives, `GetCameraTargetPositionLoc`, `GetCameraEyePositionLoc` and
  `CameraSetupGetDestPositionLoc`, the X and Y natives' values as a new location.
- From W1-2's review (D70): every callback runs in a Lua thread of its own, measured only for timers, conditions,
  actions and `ExecuteFunc`. Probe `ForGroup`, `ForForce`, `Enum*` and filter callbacks: `coroutine.isyieldable`,
  `coroutine.running`, and a wait inside them (in the game a wait may suspend the whole calling action).
- From W2-1's review: `(10)DustwallowKeys.w3m`'s `war3map.w3e` that War3Net can't read (D209), an A4 melee map with no
  terrain without it — fix the reader; and measure the off-map edge rule of `Terrain` (a point off the map reads its
  nearest edge, inferred).
- From W2-2's review: `SetUnitX`/`SetUnitY` write X and Y directly, so enter and leave events need a move hook;
  W2-2 keeps no list of the game's units (its O(n) one was dropped), so the enumerations bring one, in the order made,
  with a removal that isn't O(n). Also `CreateUnitAtLoc`/`SetUnitPositionLoc`, `BlzGetUnitCollisionSize`.

## W2-7 Abilities on units, without casting

Needs W2-2.

- `UnitAddAbility`, `UnitRemoveAbility`, levels, hiding and disabling, permanence, the ability field natives
  `BlzGetAbility*`/`BlzSetAbility*` and the unit's ability fields, tech and upgrades applied as data (facts, "Spells
  and abilities" for what needs no cast). The cast flow and effects are wave 4.
- `SetPlayerAbilityAvailable` (facts, "Version and launching": false takes the ability off the command card).
- Left by W2-2 (D226): the `undefend` order (852056) a unit with Defend gets after an owner change (also a dead one,
  on its old owner's order registration), on `RemoveUnit` (twice) and before its death (game-facts), and `Avul` with
  `SetUnitInvulnerable` if `GetUnitAbilityLevel` reads it (`probes/differences/units.txt`).
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

## W2-9 Every shipped map loads

Needs W2-1. Decisions D300-D309. From W1-7's plan review:

- 131 of the install's 193 campaign maps fail in `MapArchive.Read`: in 130, War3Net refuses a `PlayerFlags` value in
  `war3map.w3i`; in one, `war3map.w3s`. W2-1 counted only `Maps/` (273). Fix the readers (our own handling of the
  unknown enum values, or the fork's fix; a draft for War3Net in `docs/upstream.md`), and add a `Needs=Game` test that
  every map in the install's `Maps/` and `Campaign/` (and the campaigns' `.w3n` files) reads and runs to its first
  Missing native (A4, A5).

## W2-10 The rest of the units

Needs W2-2. Decisions D310-D319. From W2-2's review and `later.md`'s former unnamed units item:

- Timed life and summons (`UnitApplyTimedLife` kills 2.0 s later, game-facts), a neutral hostile unit asleep at night
  (`UnitWakeUp`, `IsUnitSleeping`), `TriggerRegisterUnitStateEvent` and `TriggerRegisterFilterUnitEvent`, unit counts,
  `CreateUnitByName` and `UnitId`, `IsUnit`, `IsUnitIdType`, `UnitAddType`/`UnitRemoveType`, `SetPlayerUnitsOwner`,
  `SetUnitUseFood`, `IsUnitIllusion`, `SetUnitRescuable`/`SetUnitRescueRange`, `SetUnitCreepGuard`,
  `UnitIgnoreAlarm`, `UnitSetUsesAltIcon`, `BlzIsUnitSelectable`, `BlzShowUnitTeamGlow`, and the order events of
  `KillUnit`/`RemoveUnit`/`PauseUnit`/`SetUnitOwner` (game-facts: `undefend` before death, twice on `RemoveUnit`;
  `PauseUnit(true)` fires order 851973, `PauseUnit(false)` gives back the move).
- `CreateCorpse` past 45 s and a body's bone decay (`DecayTime` + `BoneDecayTime`, about 93 s: try a probe, the runner
  allows 2 minutes); `IsUnitType` kinds that wait for wave 4 (stunned to magic immune) stay listed for W2-7/wave 4.
