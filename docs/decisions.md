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
- **D16.** The natives are parsed with the upstream `War3Net.CodeAnalysis.Jass` 6.0.2 from nuget.org: it parses the
  installed 3.0.1.24371 `common.j` and `common.ai` whole, so no fork is needed. (W0-2)
- **D17.** The counts in 3.0.1.24371: `common.j` declares 139 handle types and 1,712 natives, `common.ai` no types and
  123 natives, none of them in both. With `handle` that makes 140 classes and 1,835 natives, all Missing after W0-2.
  The plan's 1,243 natives were 1.32.9's. (W0-2)
- **D18.** The natives are instance methods of one class, `GameNatives`, so each simulation has its own. It is not
  called `Natives`, which would clash with its namespace `MirrorImaged.Natives`. (W0-2)
- **D19.** The handle classes keep `common.j`'s names (`unit`, `@event`, `equipmentType`), so a signature reads as
  `common.j` declares it and the Lua layer and the checks match by name. Their file turns off CS8981, the warning for
  lower-case type names. They are `partial`, so later tasks add members (ids, the world) in their own files. (W0-2)
- **D20.** JASS types in C#: `integer` is `int`, `real` is `float` (the game's single floats), `boolean` is `bool`, and
  `string`, the handles and `code` are nullable, since JASS and Lua pass null for any of them. `code` is `Action?`,
  except where the game reads what the function returns: `Func<bool>?` for `Condition` and `Filter`, and `Func<int>?`
  for `common.ai`'s `SetHeroLevels`. In 3.0.1.24371's storage, `War3.w3mod:Scripts/human.ai`, `orc.ai`, `undead.ai`
  and `elf.ai` each call `StandardAI(function SkillArrays, ...)`; `common.ai` declares `SkillArrays takes nothing
  returns integer` and `StandardAI` passes that `code` to `SetHeroLevels`, as `CampaignAI` passes its own. (W0-2)
- **D21.** The generator writes `Types.g.cs`, `CommonJNatives.g.cs` and `CommonAiNatives.g.cs`, each sorted by name
  (ordinal), with `\n` line endings whatever the generator's own checkout has, and a header naming the game version,
  so a re-run on the same install gives the same files. `.gitattributes` keeps `*.g.cs` at LF in every checkout. A
  `Needs=Game` test checks the committed files are what the generator writes. `* text=auto` was left out: it changes
  nothing where `core.autocrlf` is set, and it would reach every other task's files. (W0-2)
- **D22.** A native is filled in by writing it by hand in `MirrorImaged.Natives`, outside `Generated`, in the partial
  `GameNatives`, and re-running the generator: it leaves out every method marked `[Native(...)]` right before its
  `public` in the project's other files, passing over comments and strings. The one-to-one test reads all the natives
  by reflection, so it holds as they move out of the stubs. (W0-2)
- **D23.** The Exact native's probe or fact is the attribute's `Fact`. That a Recorded native names a `Reason` and an
  Exact one a `Fact` is a test over every native, since an attribute can't require a named argument. (W0-2)
- **D24.** The CASC reader is Moonwell's, reading a file by its root path (`War3.w3mod:scripts/common.j`), without
  Moonwell's graphics tiers and locales, which the scripts don't need, and without its lock: nothing here reads from
  several threads. A failed open closes what it opened; an I/O error comes out as it is, anything else wrapped in an
  `IOException` naming the folder. The install is the folder given, else `MIRROR_GAME`, else
  `C:\Program Files (x86)\Warcraft III`. (W0-2)
- **D25.** `mirror coverage` prints the count at each level and in all, then every Missing native, one a line, sorted.
  (W0-2)
- **D26.** The melee AI scripts are stored three times in 3.0.1.24371: `War3.w3mod:Scripts/` (`human.ai` 22,453 bytes),
  `War3.w3mod:_Balance/Melee_V0.w3mod:Scripts/` (17,331) and `War3.w3mod:_Balance/Custom_V0.w3mod:Scripts/` (17,267),
  each with `orc.ai`, `undead.ai` and `elf.ai`; `common.ai` and `common.j` are only in `War3.w3mod:Scripts/`. Which
  copy a melee game loads is not known: wave 7 (the AI) must find out, by a probe, before it runs one. (W0-2)

### The game's Lua numbers (W0-3)

The measurements are the facts' "Lua runtime" section and the two probe outputs behind it (`docs/facts.md`, "Lua
numbers"). Each model was found by trying candidates against every measured case in Python first; `native/mifloat.c`
is the result. A float is read in the IEEE layout throughout: the game's `string.pack('<f')` gives those bits.

- **D27. Addition and subtraction** align the smaller number with one guard bit and cut the result toward zero; a number
  23 or more octaves smaller (under two float steps) is dropped, a zero or subnormal operand too. Matches every measured
  case: the N1 and N7 sums and ties, the ten "adding k float steps" rows, the ten loops, the twenty sums of up to a
  million additions to the last bit, the four stalls with their exact counts, and `1 - cos(0)` = 2^-24. Rejected:
  plain truncation (`2^24 + 3` gives 2^24 + 2), pre-shifting both operands (`33000.01 + 0.004` loses a step), aligning
  the smaller to two steps (adding 3 steps gives 2), no guard bit (`1 - cos(0)` gives 2^-23; for additions one guard bit
  and none always agree). Not measured: subtractions that shift out two or more bits, sums past the range.
- **D28. Multiplication** keeps the top 24 bits of the 48-bit product (toward zero); a zero or subnormal operand gives +0.
  A result's biased exponent is kept from 1 to 256 and packed as it is, so 255 and 256 carry into the inf, nan and sign
  bits; anything else is +0. Measured: 256 kept (`1e38 * 10` = -5.5e-39, `(1 / 0) * 4` = -0), 259 and 260 zero
  (`1e20 * 1e20`, `100 / 0`); 257 and 258 aren't, and fall on the zero side. Matches all of N7's products, nine r*r
  products, the `10 * 0.7` family and the limits.
- **D29. Division** is the dividend times a reciprocal, R = ⌊(2^47 + 0xCC0000) / m⌋ with m the divisor's 24-bit mantissa
  without its lowest bit, cut toward zero, with no zero check: zero reads as 1.0 × 2^-127, so `0/0` is 1, `1/0` is
  2^127 and `100/0` is past the range, 0. Matches the twelve measured quotients, `3/5`, `7.2 % 0.1` = 0, 21 of
  the 23 roots after a correction step, and the 140 wrong casts of 4,200 in `n * (k / 100)`. Rejected: exact
  truncation (`1/3`), rounding up (`1/5`), Newton-Raphson reciprocals in the game's arithmetic from 2- to 12-bit tables
  (no quotient set matched), a table with one exact Newton step plus a bias. **Close:** `(int)(n / d)` for n 1-2000, d
  2-100 is wrong 268 times in the game and 102 in the model, `Ceiling` 893 and 956; the correction step after the roots
  of 3 and 1,000,000 is one step lower than the game's.
- **D30. floor, ceil and fmod.** floor is exact. ceil reads a zero exponent as a fraction, so `math.ceil(0.0)` and
  `math.ceil(-0.0)` are 1 as in the game. fmod is `a - trunc(a / b) * b` in the game's arithmetic (exact fmod gives
  0.0999997 for 7.2 and 0.1; the game 0), and Lua's float `%` uses it.
- **D31. sqrt** is the exact root of the mantissa reduced to [1, 4) (the error repeats every two octaves), times
  1 + E(m), with E linear between the errors measured at ten mantissas (seventeen roots) and 0 at 1 and 4. Every
  measured root is bit for bit the game's, powers of 4 are exact and a larger value never gives a smaller root.
  **Close** between the measured mantissas: the game's worst is 1.93e-5 high in both octaves and 0.09e-5 low, the
  model's 1.93e-5 (at 2.25) and 1.34e-5 below 2, never low, and `(int)Sqrt(n * n)` for n 1-4096 is wrong 64 times in
  the game and never here. The algorithm wasn't found: the error jumps between near mantissas (3 gives 0.2e-5, 3.0625
  gives 1.67e-5), which neither a Newton step from straight lines over [1, 2], [2, 3], [3, 4] nor a table and a Newton
  step reproduce.
- **D32. sin, cos, tan, asin, acos, atan, exp, log and pow are Close.** They are computed in doubles with + - × ÷ only
  (no C library), then cut to a float toward zero after a nudge of 2^-45, so that exact results (2^10, 10^3) stay exact.
  sin and cos go through a 31-bit fraction as the game's do (cos 0 = 1 - 2^-24, sin 90° the same), and tan is sin over
  cos, as the game's `tan(0.5)` is. Exact on `log(10)`, `asin(0.5)`, `acos(0.5)`, `2^10`, `10^2`, `10^3`, `1.5^2`,
  `2^0.5`, cos 0, sin 90°, `1 - cos(0)`. Off: sin, cos and tan of 0.5 by up to 7.4e-6 (the game's angle is about
  5.6e-6 rad off there), `exp(1)`, `log(2)` and `atan(1)` by one or two float steps, `log10(1000)` by 8.1e-6, `x^0.5`
  by the game's own up to 1.35e-5 low, sin π (2^-31 in the game, -8.7e-8 here), and the 720 whole-degree sines and
  cosines by up to 5.3e-6 (18 the same). Tried for sin: an angle in 2^14 to 2^20 units a turn, floored or rounded, into
  an exact sine; at most 19 of the 720 matched. `math.log(x, 2)` is `log(x) / log(2)` in the game's arithmetic (D33),
  so `math.log(8, 2)` is 3.00000024. Outside their domains the functions give 0, as `math.sqrt(-1)` does; `log(0)` is
  -inf; a nan result is 0.
- **D33. One configuration on every platform: Windows's.** Lua's `luaconf.h` gives a Windows build `LUA_USE_C89` and
  others C99 paths, which differ in what a script sees: `math.log(x, 2)`, hex numerals, `%a` and `os.date`'s options.
  Every platform is built as a stock Windows build is (`-DLUA_USE_C89` and Windows's `os.date` options), chosen to
  match the likely Windows build of the game; to be settled by a probe of `math.log(8, 2)` (here 3.00000024),
  `math.log(3, 2)` (1.58496273), `0x1.0000011p0` (1: Lua's own reader adds the digits in floats), `string.format('%a',
  0.1)` (`0xc.ccccdp-7`, Lua's own writer) and `os.date('%F')` (refused; `%#x` is taken). `LUA_USE_POSIX` stays on
  Linux and macOS, where it only makes the time functions thread-safe.
- **D34. Number text and parsing.** `tostring` is `%.7g` (Lua's default for floats; the game's `1/0` reads
  1.701412e+38). inf and nan are written by the library, as the C library writes them (sign or `+` or space, capitals
  for `%G` and the like, padded to the width) but the same on every platform; the game writes `nan` for `MaxValue * 2`.
  Decimal numerals are parsed with `strtod` and cast to a float, since `strtof` differs between C libraries. An integer
  becomes a float by the C cast (to nearest); not measured.
- **D35. The Lua build.** Lua 5.3.6 without `LUA_COMPAT_5_2`, so no `math.pow`, `bit32` or `unpack`: no fact shows them
  in the game, and a probe can settle it. Changed in the vendored sources, and nowhere else: `luaconf.h` (`LUA_32BITS`,
  the include of `mifloat.h`, Windows's `os.date` options, a fixed `table.sort` pivot instead of the clock, pointers
  written as arena offsets), `lstate.c` (the string hash seed is 0 instead of the time and addresses; the game's seed
  is unknown), `lmathlib.c` (`math.log` with a base, `math.modf`, `math.rad` and `math.deg` use the game's arithmetic
  instead of C operators), `ldo.c` (binary chunks refused, D36), `lapi.c` and `ltable.c` (a C function is hashed and
  written by its place in the library, D39).
- **D36. The sandbox** leaves out `math.random` and `math.randomseed`: the game's are its own generator, shared with
  `GetRandomInt` (fact), which the world provides (W0-5). `os.clock`, `os.time` and `os.date` read the real clock, as the
  game's do, until the world replaces them. Binary chunks are refused by every `load`, whatever its mode: Lua 5.3 never
  checks bytecode, so a map could crash the process with one. Whether the game loads them isn't measured;
  `string.dump` stays, harmless without them.
- **D37. The native build** uses zig 0.17.0 (`native/build.ps1`) for win-x64 (MinGW), linux-x64 (glibc 2.17) and
  osx-arm64, with `-DLUA_USE_C89` (D33), `-ffp-contract=off` (arm64 would fuse multiply-adds and x64 not) and `-s`
  (without it the DLL holds a hash of the folder it was built in). The output is the same bytes from any folder and
  with CRLF sources (checked), so CI rebuilds it on Windows and fails if it differs from the committed libraries. A
  determinism test hashes every number operation over 20,000 float pairs, nan and infinity patterns, hex numerals, `%a`
  and `os.date` included, and runs on all three platforms.
- **D38. LuaState** passes nil, booleans, integers (`int`), floats (`float`) and strings; a Lua string is read as UTF-8,
  so bytes that aren't UTF-8 come back as U+FFFD; reading a table or a function throws until a task needs them.
  Anything that can raise a Lua error runs in C under `lua_pcall` (`sandbox.c`): getting and setting globals, calling
  one, registering a function, and pushing a C# function's results, which a C trampoline does after C# returns them in
  a buffer. So Lua's `longjmp` never crosses a managed frame, even when a `__gc` fails or `_G` has a strict metatable.
  The values C# writes are pushed with the collector held, before anything else runs: a finalizer calling C# would
  otherwise write over them mid-push. To keep the collector's pace, the hold is Lua's own flag (`gcrunning`), not
  `lua_gc`'s stop and restart, which forget the collector's debt; what the push allocated is added to the debt, and the
  collector takes its step once the values are in. A loop that only calls a C# function returning a 100 KB string
  commits 1 MB, as the same loop in Lua does, and a C# call per step gives 717 collection cycles where Lua's own
  function gives 647. The other choice, a fresh buffer for each nested call, was left: it needs C to tell C# when a
  push is over. Errors carry Lua's traceback, and a chunk's name is given with `@`, so messages
  read `war3map.lua:3:`. The state is a `SafeHandle`, counted in use for the whole of every call, so neither `Dispose`
  (even from inside a callback) nor the finalizer frees it under a running call. A function is added to the state's list
  before its global is set, since a `__newindex` on `_G` can call it then, and stays there if the setting fails, since
  the `__newindex` may have kept it: an id is never given to two functions.
- **D39. Addresses are the same every run.** Each state allocates from its own arena of 64 GB of address space,
  aligned to its size and filled the same way every run (size classes, freed blocks reused last in first out), so an
  object's offset in it is the same every run and, as CI checks with a pinned workload, on every platform. Lua hashes
  an object by the low 32 bits of its address, which are the offset's, so `pairs` visits table, function and coroutine
  keys in the same order, and `tostring` writes the offset (16 hex digits, as Windows writes `%p`). The exception is a C
  function from the library (`print`, `math.sqrt`): it is hashed and written by its place in the library, the same
  every run but not between platforms or builds, so `tostring(print)` and `pairs` over such keys are left out of the
  cross-platform check. The game's order isn't reproduced: the facts say it differs between machines. Memory: each state
  reserves 64 GB of address space (128 GB for a moment while it is placed), commits memory as it is used and gives none
  back until it is closed, so about 2,000 states fit at once (2,008 measured on Windows 11, whose processes have 128 TB
  of address space).
- **D40. Facts not tested here.** The "Lua runtime" facts that measure Moonwell's or CSharp.lua's own Lua code on top of
  these numbers (`Round(2.567, 2)`, `F2` and `F0` text, `Normalize`, `Wrap`, `Lerp`, vectors, rays, slides, bounces,
  circle and wall hits, straight lines, distances away from the origin, easing ends but `InSine(0)`, `-7 / 2` and
  `-7 % 2` as .NET, CoreSystem's integer forms): those libraries aren't part of this repository; the arithmetic under
  them is tested, and their probes can be compared when a C# map runs here. The clock facts (`os.clock`'s steps,
  `os.time` as the PC's seconds, `os.date` in local time, long calls not stopped, slow ticks made up): they are about
  the real clock and the game loop, which belong to the world (W0-5); only `os.time` being an integer and `os.date`'s
  options are tested. `math.random` sharing the game's generator: W0-5.
- **D41.** Probe maps are built with the upstream `War3Net.Build.Core` 6.1.2 from nuget.org, the package of
  `War3Net.Build` that holds `Map`, `MapBuilder` and `MapFactory`, all the builder uses. `War3Net.Build` itself adds
  only the JASS script builder and the transpiler, and their packages. No fork is needed. (W0-4)
- **D42.** A probe map is War3Net's `MapFactory` map at 32 by 32 (info format 28 as World Editor 1.31 wrote it, flat
  ground, the melee flag cleared, one force holding player 1), with `config` and `main` as World Editor writes them for
  one user player, and a black 256x256 `war3mapMap.tga` made in code, since the game closes a map with no minimap image
  (facts) and no game file may be in git. No pathing, shadow or doodad file: the game loads it without. (W0-4)
- **D43.** A probe is plain Lua that calls `out(text)` per line and `done()` at the end. It runs in the first
  zero-second timer, through `guard(f)`, which runs `f` in `pcall` and on an error adds the line
  `error: <message>` (line breaks made spaces, cut to fit as below) and calls `done()`; a probe passes its own timer
  functions through `guard` too. These rules are tested by running a probe map's script in MirrorImaged's Lua with stand-ins for the natives.
  `out` refuses, with a Lua error, a line over 253 bytes or with a line break, the most one `Preload` call keeps whole
  as one line of the file (facts "Files and Preload"); a backslash counts two, as the game writes it doubled (whether
  the game's cut counts it once or twice is not measured, so this is the safe side). `done()` writes the lines to
  `CustomMapData\MirrorImaged\<name>.pld`, each as `")\n` + line + `\n//` (as `docs/tools/gate.ps1` reads them), then
  ends the game. A name is letters, digits and dashes, as it is a file name and a Lua string. (W0-4)
- **D44.** The runner is in `tests/MirrorImaged.GameTests`, with its tests, since what uses it is tests that compare
  with the game; the builder and the probes are in `probes/`. (W0-4)
- **D45.** The runner always puts the game on a hidden desktop: there it runs at full speed and shows nothing (facts).
  Minimised is ruled out (game time stops), and a normal window, which also runs, is not needed, so there is no choice
  to make. The desktop is asked for `DESKTOP_READOBJECTS`, to list its windows, and `DESKTOP_CREATEWINDOW`, without
  which `CreateDesktop` fails with error 8 (seen 11-08). (W0-4)
- **D46.** Every probe map writes `CustomMapData\MirrorImaged\loaded.pld` at the end of `main` and `started.pld` in its
  first zero-second timer, before the probe. While `loaded.pld` is there and `started.pld` isn't, the runner posts a
  space key to the game's window every 3 s (facts: the loading screen waits; a posted key passes it). These marks, not
  `War3Log.txt`, say the map loaded, because the game holds the log back while it runs and loses it when killed
  (facts). A run with no lines is `NoOutput` when the map loaded; `AtLogin` when the game still ran but never logged in
  (it rewrites `War3Preferences.txt` once it has, facts), with its detail starting "needs Sven: ..." and saying what is
  known: it was seen every time the runner ran under Claude Code's Bash tool and never from PowerShell (facts); and
  `Queued` otherwise. What the game shows can't be captured on the hidden desktop (facts), so `AtLogin` is told from the
  preferences file, not from a picture. (W0-4)
- **D47.** The game starts suspended in a job object of its own with `JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE`, and is
  resumed once in it, so everything it starts is in the job (measured: no breakaway). The runner ends the job, never
  closing the game's window, which opens the quit dialog; when the test host ends, however it ends, Windows closes the
  job and the game goes with it. It stops nothing outside the job. The game gets the user's own environment as a new
  logon builds it (`CreateEnvironmentBlock` without inheriting), not the test host's, so the caller's shell (Git Bash's
  `HOME`, `MSYSTEM` and `PATH`) never reaches it; that alone did not end the stalls under the Bash tool (facts). (W0-4)
- **D48.** Runs are one at a time on a computer: a run holds the named mutex `MirrorImaged.GameRunner` for its whole
  length, a second waits for it up to 5 minutes (well inside the 10 after which the test host's blame collector ends a
  run) and then fails saying so, and a run refuses to start while any
  Warcraft III is running, as it couldn't tell that game's files from its own. Two runs at once made one fail (facts).
  A test that starts a fake `Warcraft III.exe` holds the mutex while it runs. The run's detail names the installed
  build and warns when it isn't 3.0.1.24371, the version the facts were measured on. (W0-4)
- **D49.** Built maps go to `%LOCALAPPDATA%\MirrorImaged\maps`, as `<name>-<8 random hex digits>.w3x`, new each run and
  deleted after it, since the game loads no map whose path is longer than 173 characters (facts) and the test assembly's
  folder is long already; a longer path is refused before the run. (W0-4)
- **D50.** The runner never writes over any of Sven's files and puts nothing back. The one file a run changes is the
  account's `LastReplay.w3g`, which the probe's game replaces (facts). So before a run each account's
  `LastReplay.w3g` whose SHA-256 isn't one a probe run made is copied to `Replays\Saved by MirrorImaged\LastReplay
  <its write time, yyyy-MM-dd HHmmss>.w3g`, never over a file that is there; after the run the hash of each
  `LastReplay.w3g` that changed is added to `%LOCALAPPDATA%\MirrorImaged\probe-replays.txt` (hexadecimal, one a line),
  unless a Warcraft III outside the run's job was seen during it (Sven's own game, or World Editor's Test Map), in which
  case the replay may be his and the next run saves it. This replaced a keep-and-restore of five files, which could put
  a stale copy over a newer file. (W0-4)
- **D51.** A `Needs=Game` test whose probe gives no lines fails with the run's detail, rather than passing or being
  skipped: xUnit 2 can't skip once a test runs, and a pass would hide that nothing was checked. (W0-4)
- **D52.** The hub (one game kept open, handed each probe through a Preload request and `ChangeLevel`, each probe going
  back to a hub built for the next request) was built, tried and measured not to work: the hub read its request, but
  `ChangeLevel` from a `-loadfile` game does nothing or crashes the game (facts). It is removed; with no login needed, a
  launch per probe costs about 15 s. (W0-4)
