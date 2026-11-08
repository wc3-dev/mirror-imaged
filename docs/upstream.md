# Upstream

Bugs found in War3Net, Lua or another library, drafted here and not posted.

## War3Net.CodeAnalysis.Transpilers: a JASS name that is a Lua keyword

Draft, not posted. In `War3Net.CodeAnalysis.Transpilers` 6.0.2 and 6.1.1, `JassToLuaTranspiler` writes every JASS name
as it is, so a name that is a Lua keyword but not a JASS one (`break`, `do`, `end`, `for`, `goto`, `in`, `nil`,
`repeat`, `until`, `while`) gives Lua that doesn't load. The game's own `blizzard.j` (3.0.1.24371) has one:
`function SubStringBJ takes string source, integer start, integer end returns string` becomes
`function SubStringBJ(source, start, end)`, and Lua stops at that line with `<name> or '...' expected near 'end'`.
Expected: such a name renamed, for example to `end_`, which no JASS name can be. MirrorImaged renames it before
transpiling (decisions D65).

## War3Net.CodeAnalysis.Transpilers: a whole number given to a real stays an integer

Draft, not posted. `constant real bj_MELEE_MINE_SEARCH_RADIUS = 2000` in `blizzard.j` becomes
`bj_MELEE_MINE_SEARCH_RADIUS = 2000`, a Lua integer, where JASS holds the real 2000.0. So `math.type` reads
`integer`, and arithmetic on it in the game's 32-bit Lua can wrap where JASS's real wouldn't. Expected: `2000.0`, as the
game's own Lua blizzard.j has it (probe blizzard-numbers). MirrorImaged writes such a number as a real before
transpiling (decisions D131).

## War3Net.Build.Core: three of the install's maps have a file its readers refuse

Draft, not posted. War3Net.Build.Core 6.1.2 reads an enum value it doesn't define as an error, where the game loads the
map. In 3.0.1.24371's install: `Maps/(10)DustwallowKeys.w3m`'s `war3map.w3e` has a cliff type 0 ("Value '0' is not
defined for enum of type CliffType"), `Maps/ForsakenKingdom/Scenario/(1)SilverpineSprint.w3x`'s `war3map.doo` a doodad
state 14 (`DoodadState`), and `Maps/FrozenThrone/(4)Roundabout.w3x`'s `war3mapUnits.doo` a random unit mode 641
(`RandomUnitDataMode`). Expected: the value kept as read. MirrorImaged leaves such a file out and names it in
`MapArchive.Unread` (decisions D209); the other 270 maps read whole. It reads a `war3map.doo` itself when War3Net
refuses a state, keeping it as written (decisions D258): SilverpineSprint's is format 13 with states 10 and 14, bit 3
being the one `DoodadState` doesn't define.

## War3Net.Build.Core: an object's id is kept in the reverse byte order of the game's

Not a bug, a trap, noted for whoever reads War3Net's object data: `SimpleObjectModification.OldId` and the field ids
keep an id's first letter in the lowest byte (`"hfoo".FromRawcode()`), where the game's `FourCC` keeps it in the
highest. A map built with ids in the game's order (`'h' << 24 | ...`) loads without error, but its objects are
`oofh` and none of its changes act (probe `data`'s first runs, 11-08). MirrorImaged reverses them as it reads (D204).

## War3Net.CodeAnalysis.Transpilers: statements after a return

Draft, not posted. In `War3Net.CodeAnalysis.Transpilers` 6.1.1, `JassToLuaTranspiler` writes the statements that follow
a `return` in a block after Lua's `return`, which must end its block, so Lua can't load the file (`'end' expected`).
JASS allows such dead code; a downloaded map, WotLK RPG 17.11b, has a function ending `return` and then a loop. Expected:
the return written as `do return end`, or the dead statements left out. MirrorImaged leaves them out before transpiling
(decisions D139).
