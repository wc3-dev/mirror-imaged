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

## War3Net.CodeAnalysis.Transpilers: statements after a return

Draft, not posted. In `War3Net.CodeAnalysis.Transpilers` 6.1.1, `JassToLuaTranspiler` writes the statements that follow
a `return` in a block after Lua's `return`, which must end its block, so Lua can't load the file (`'end' expected`).
JASS allows such dead code; a downloaded map, WotLK RPG 17.11b, has a function ending `return` and then a loop. Expected:
the return written as `do return end`, or the dead statements left out. MirrorImaged leaves them out before transpiling
(decisions D139).
