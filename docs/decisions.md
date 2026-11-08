# Decisions

Each decision has a number, the choice and the reason. D1 to D11 are the plan's (section 2); those marked *(Sven)* are
his, the rest the agents' and open to his review.

## From the plan

- **D1.** MirrorImaged is a new repository in `wc3-dev`, using War3Net as a package to read map files. *(Sven)*
- **D2.** The name is MirrorImaged. *(Sven)*
- **D3.** Tests are written in C# and Lua, both in 1.0. *(Sven)*
- **D4.** JASS maps run, turned into Lua by War3Net's transpiler at load time. *(Sven)*
- **D5.** Everything in the plan is 1.0; nothing is held back for a later version. *(Sven)*
- **D6.** The approach is a copy of the game's rules, not the real game run hidden as warcraftsim does. *(Sven, 11-08)*
- **D7.** It is built autonomously, by subagents, as `docs/tasks/how-tasks-run.md` sets out. *(Sven)*
- **D8.** The command-line tool is `mirror`. Short, and the word the name starts with.
- **D9.** The licence is MIT, as Moonwell's, so code can move between the two.
- **D10.** The library and the command line run on Windows, Linux and macOS; the comparison against the game runs on
  Windows, the only platform the game runs on here.
- **D11.** .NET is net10.0, the version Moonwell uses.

## Made by the tasks

- **D12.** The game-files check's test fixtures are git repositories the tests make in a temporary folder, not tracked
  files: a tracked fixture with a game extension or signature would fail the check on this repository. (W0-1)
- **D13.** The game-files check also fails on a file starting with `HM3W`, the header a map puts before its MPQ
  archive, so a map renamed to another extension is caught too. (W0-1)
- **D14.** The game-files check also fails on the sound extensions `.wav`, `.mp3`, `.flac` and `.ogg`, since the rules
  keep the game's sounds out of git and a sound has no signature the check looks for. (W0-1)
- **D15.** The game-files check reads each file's content from git's index, not the disk, so it checks what a commit
  holds. A path git can't give the content of (a submodule, a missing object) fails, and so does a Git LFS pointer,
  whose real content git doesn't hold: the repository uses neither. (W0-1)

### The game's Lua numbers (W0-3)

The measurements are the facts' "Lua runtime" section and the two probe outputs behind it (`docs/facts.md`, "Lua
numbers"). Each model was found by trying candidates against every measured case in Python first; `native/mifloat.c`
is the result. A float is read in the IEEE layout throughout: the game's `string.pack('<f')` gives those bits.

- **D16. Addition and subtraction** align the smaller number with one guard bit and cut the result toward zero; a number
  23 or more octaves smaller (under two float steps) is dropped, a zero or subnormal operand too. Matches every measured
  case: the N1 and N7 sums and ties, the ten "adding k float steps" rows, the ten loops, the twenty sums of up to a
  million additions to the last bit, the four stalls with their exact counts, and `1 - cos(0)` = 2^-24. Rejected:
  plain truncation (`2^24 + 3` gives 2^24 + 2), pre-shifting both operands (`33000.01 + 0.004` loses a step), aligning
  the smaller to two steps (adding 3 steps gives 2), no guard bit (`1 - cos(0)` gives 2^-23; for additions one guard bit
  and none always agree). Not measured: subtractions that shift out two or more bits, sums past the range.
- **D17. Multiplication** keeps the top 24 bits of the 48-bit product (toward zero); a zero or subnormal operand gives +0.
  A result's biased exponent is kept from 1 to 256 and packed as it is, so 255 and 256 carry into the inf, nan and sign
  bits; anything else is +0. Measured: 256 kept (`1e38 * 10` = -5.5e-39, `(1 / 0) * 4` = -0), 259 and 260 zero
  (`1e20 * 1e20`, `100 / 0`); 257 and 258 aren't, and fall on the zero side. Matches all of N7's products, nine r*r
  products, the `10 * 0.7` family and the limits.
- **D18. Division** is the dividend times a reciprocal, R = ⌊(2^47 + 0xCC0000) / m⌋ with m the divisor's 24-bit mantissa
  without its lowest bit, cut toward zero, with no zero check: zero reads as 1.0 × 2^-127, so `0/0` is 1, `1/0` is
  2^127 and `100/0` is past the range, 0. Matches the twelve measured quotients, `3/5`, `math.fmod(7.2, 0.1)` = 0, 21 of
  the 23 roots after a correction step, and the 140 wrong casts of 4,200 in `n * (k / 100)`. Rejected: exact
  truncation (`1/3`), rounding up (`1/5`), Newton-Raphson reciprocals in the game's arithmetic from 2- to 12-bit tables
  (no quotient set matched), a table with one exact Newton step plus a bias. **Close:** `(int)(n / d)` for n 1-2000, d
  2-100 is wrong 268 times in the game and 102 in the model, `Ceiling` 893 and 956; the correction step after the roots
  of 3 and 1,000,000 is one step lower than the game's.
- **D19. floor, ceil and fmod.** floor is exact. ceil reads a zero exponent as a fraction, so `math.ceil(0.0)` and
  `math.ceil(-0.0)` are 1 as in the game. fmod is `a - trunc(a / b) * b` in the game's arithmetic (exact fmod gives
  0.0999997 for 7.2 and 0.1; the game 0), and Lua's float `%` uses it.
- **D20. sqrt** is the exact root of the mantissa reduced to [1, 4) (the error repeats every two octaves), times
  1 + E(m), with E linear between the errors measured at ten mantissas (seventeen roots) and 0 at 1 and 4. Every
  measured root is bit for bit the game's, powers of 4 are exact and a larger value never gives a smaller root.
  **Close** between the measured mantissas: the game's worst is 1.93e-5 high in both octaves and 0.09e-5 low, the
  model's 1.93e-5 (at 2.25) and 1.34e-5 below 2, never low, and `(int)Sqrt(n * n)` for n 1-4096 is wrong 64 times in
  the game and never here. The algorithm wasn't found: the error jumps between near mantissas (3 gives 0.2e-5, 3.0625
  gives 1.67e-5), which neither a Newton step from straight lines over [1, 2], [2, 3], [3, 4] nor a table and a Newton
  step reproduce.
- **D21. sin, cos, tan, asin, acos, atan, exp, log and pow are Close.** They are computed in doubles with + - × ÷ only
  (no C library), then cut to a float toward zero after a nudge of 2^-45, so that exact results (2^10, log2 8) stay
  exact. sin and cos go through a 31-bit fraction as the game's do (cos 0 = 1 - 2^-24, sin π/2 the same), and tan is
  sin over cos, as the game's `tan(0.5)` is. Exact on `log(10)`, `asin(0.5)`, `acos(0.5)`, `2^10`, `10^2`, `10^3`,
  `1.5^2`, `2^0.5`, cos 0, sin π/2, `1 - cos(0)`. Off: sin, cos and tan of 0.5 by up to 7e-6 (the game's angle is
  about 5.6e-6 rad off there), `exp(1)`, `log(2)` and `atan(1)` by one or two float steps, `log10(1000)` by 8e-6, `x^0.5`
  by the game's own 1.35e-5 low, sin π (2^-31 in the game). Tried for sin: an angle in 2^14 to 2^20 units a turn,
  floored or rounded, into an exact sine; at most 19 of the 720 whole-degree values matched. Outside their domains the
  functions give 0, as `math.sqrt(-1)` does; `log(0)` is -inf; a nan result is 0.
- **D22. Number text and parsing.** `tostring` is `%.7g` (Lua's default for floats; the game's `1/0` reads
  1.701412e+38). inf and nan are written by the library as `inf`, `-inf`, `nan`, `-nan` on every platform (the game
  writes `nan` for `MaxValue * 2`); `%a` is still the C library's. Numbers are parsed with `strtod` and cast to a float,
  since `strtof` differs between C libraries. An integer becomes a float by the C cast (to nearest); not measured.
- **D23. The Lua build.** Lua 5.3.6 without `LUA_COMPAT_5_2`, so no `math.pow`, `bit32` or `unpack`: no fact shows them
  in the game, and a probe can settle it. Changed in the vendored sources, and nowhere else: `luaconf.h` (`LUA_32BITS`
  and the include of `mifloat.h`), `lstate.c` (the string hash seed is 0 instead of the time and addresses, so `pairs`
  visits string keys in the same order every run; the game's seed is unknown), `lmathlib.c` (`math.log` with a base,
  `math.modf`, `math.rad` and `math.deg` use the game's arithmetic instead of C operators).
- **D24. The sandbox** leaves out `math.random` and `math.randomseed`: the game's are its own generator, shared with
  `GetRandomInt` (fact), which the world provides (W0-5). `os.clock`, `os.time` and `os.date` read the real clock, as the
  game's do, until the world replaces them.
- **D25. The native build** uses zig 0.17.0 (`native/build.ps1`) for win-x64 (MinGW), linux-x64 (glibc 2.17) and
  osx-arm64, with `-ffp-contract=off` (arm64 would fuse multiply-adds and x64 not) and `-s` (without it the DLL holds a
  hash of the folder it was built in). The output is the same bytes from any folder and with CRLF sources (checked), so
  CI rebuilds it on Windows and fails if it differs from the committed libraries. A determinism test hashes every number
  operation over 20,000 float pairs, nan and infinity patterns included, and runs on all three platforms.
- **D26. LuaState** passes nil, booleans, integers (`int`), floats (`float`) and strings; reading a table or a function
  throws until a task needs them. A C# function is called through a C trampoline that raises the Lua error itself, at
  the calling line, so Lua's `longjmp` never crosses a managed frame. Errors carry Lua's traceback, and a chunk's name is
  given with `@`, so messages read `war3map.lua:3:`.
