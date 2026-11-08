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
  written by its place in the library, D39), `loslib.c` (the time functions on the simulated clock, D44).
- **D36. The sandbox** leaves out `math.random` and `math.randomseed`: the game's are its own generator, shared with
  `GetRandomInt` (fact), which the world provides (W0-5). `os.clock`, `os.time` and `os.date` read the real clock, as the
  game's do, until the world replaces them (done in W0-5, D44). Binary chunks are refused by every `load`, whatever its mode: Lua 5.3 never
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

### Running a Lua map (W0-5)

- **D41. A handle in Lua is a full userdata** holding its id and declared type, with one metatable per type whose
  `__name` is the type, so `tostring` writes `timer: <address>`: the facts measure that it names the type the
  returning native declares. While Lua holds it, the same handle as the same type is the same userdata (a weak table
  per type), so `==` and table keys work (`GetLocalPlayer() == Player(0)`); as another type it is another userdata, as
  the facts measure for `unit` and `widget`. One Lua let go of is made again at a new address next time; whether the
  game keeps it is not measured. Its place, so `pairs` and `tostring`, is the same every run (D39). An argument's
  declared type must be the parameter's or below it, else Lua's `bad argument #1 to 'X' (timer expected, got
  player)`, as the facts measure for `GetUnitTypeId`; a destroyed handle reads as null to the natives.
- **D42. A Lua function given to C#** becomes a reference in the registry, made by `sandbox.c`'s trampoline before C#
  runs, since C# must not allocate in Lua there. It is let go of when the C# function returns, unless C# keeps it
  (`LuaFunction.Keep`, then `Dispose`). A native's `code` (D20) is a delegate whose target is the kept function's
  wrapper; a native letting go of a callback disposes the target if it can (`TimerStart` again, `DestroyTimer`, and
  `TimerStart` with no timer, which keeps nothing), so a map starting timers with new closures doesn't grow. An
  argument C# can't read (a table, a coroutine) comes to C# as its type (`LuaOpaque`) rather than failing the read,
  so the native refuses it with D49's message and every function given with it is let go of.
- **D43. A Missing native halts the run** (`LuaState.Halts`): its error is raised at the line of the nearest Lua
  function calling in (in its thread, else the main thread, so `pcall(CreateUnit)` and
  `coroutine.wrap(CreateUnit)()` name a line too), and from then on every Lua instruction in every thread raises it
  again (a count hook on every thread; a new thread takes its creator's), so no `pcall`, `xpcall` handler,
  `coroutine.resume`, `load` reader, `string.gsub` callback or C# that swallows the error carries on. No C# function
  runs once the state has halted, since a finalizer or an `xpcall` handler can call one without a Lua instruction.
  The run reports its first error, whatever `coroutine.wrap` adds to it on the way out. Replacing `pcall`, `xpcall`
  and `coroutine.resume` instead misses the others, and in Lua it would move `pcall(error, "", 2)`'s position
  (facts). A stop leaves the simulation stopped rather than throwing it away, so its log is kept: `Simulation.Load`
  returns it with `Stopped`, a `LuaException` starting with the line (`war3map.lua:4: The native CreateUnit is
  Missing...`) whose inner exception is the `MissingNativeException`, and `Advance` throws that, also when the stop
  happens in it. `mirror run` prints the log, then the stop.
- **D44. os.clock, os.time, os.date and os.difftime read the simulated clock** (`LuaState.Clock`, whole milliseconds,
  in the main thread's extra space so every coroutine reads it). `os.clock` is the run's time, divided by 1000 in the
  game's arithmetic as Lua's `/` divides (`mi_div`); the game's is the process's real time, which runs with game time
  in single player. `os.time` and `os.date` add it to 2026-01-01 00:00:00 UTC, the same every run, and local time is
  UTC (`isdst` false, `%z` +0000, `%Z` UTC). The conversions are written out in `native/mitime.c` (dates to days and
  back, `mktime`'s carrying over, Windows's list of conversions in the "C" locale as its C library writes them, `%#c`
  and `%#x` from its documentation), not the C library's, which reads the machine's zone and differs between
  platforms; `loslib.c` calls them in place of `clock`, `time`, `mktime`, `strftime` and `difftime`. As Windows's C
  library (D33) gives no date before 1970, `os.date` of a time below 0 and `os.time` of a date before 1970 raise Lua's
  "time result cannot be represented in this installation"; `os.time` writes the table's fields back before it raises,
  so they are put right then too, the same every run. Not measured in the game: `%c`, `%x`, `%#c`, `%Z`, and times
  before 1970.
- **D45. The clock's step is 1/1024 s.** The facts measure timers in game time down to 1/256 s: a periodic 1/256 s
  timer and a one-shot started again every 1/256 s each run 256 times a game second. With 1 ms steps the one-shot
  would restart every 4 ms (250 a second); with 1/1024 s, 1/256 s is 4 steps. A timer's expiries are at its start
  plus whole timeouts, exactly in seconds, each run in the step nearest it, so it doesn't drift; the clock reads the
  step during its callback, and a 0 s timer started there runs in the same step (fact). Timers due in one step run in
  the order their expiries were queued: when the timer was started or resumed, or for a periodic timer's next
  expiry, when the one before ran, before its callback. Not measured in the game. A periodic timer runs at most once a
  step, so a periodic 0 s one runs every step (not measured). A timer paused in the step it was due, before it ran,
  has 0 left and runs as soon as it is resumed (not measured). An expiry past the clock's range (2^60 steps, as for a
  timeout of 1e30 or `math.huge`) never comes. The remaining time is the next expiry less the clock, and elapsed the
  timeout less that, each cut toward zero to a single float as the game's arithmetic cuts (D27). Cut toward zero, the
  604800 s timer the facts measure reads ahead by up to 0.0596 s over 100 reads 0.01 s apart, and 84 of 99 reads are
  unchanged; the game drifted up to 0.05 s with 83 of 100 unchanged, and rounding to nearest never drifts more than
  half of 0.0625 s, so it can't. At 3600 s the game drifted 0.0002 s, but here the clock's steps make the remaining
  time exact in a float, so it reads exact. Not matched: the game ran timers due at 0.5, 2 and 3.25 s 0.0001 s early.
  `Game.Advance` jumps to what falls due instead of visiting every step: the same run, faster. `Simulation.Advance`
  moves to the step nearest all the time asked for so far, so 600 advances of 1/60 s reach 10 s.
- **D46. The random generator is SplitMix64** from the run's seed: the game's isn't known, and .NET doesn't promise
  `System.Random`'s seeded numbers across versions. `GetRandomInt`, `GetRandomReal`, `SetRandomSeed`, `math.random` and
  `math.randomseed` share it, as the game's do (fact). `math.random(m, n)` is `GetRandomInt(m, n)` and `math.random()`
  a float from 0 below 1, with Lua 5.3's argument checks ("interval is empty", "interval too large");
  `math.randomseed(x)` seeds with `x` cut to an integer. `GetRandomInt` takes its bounds in either order (replaced by
  D86: from the first bound up by the bounds' distance).
- **D47. Handle ids** count up from 0x100000, the first agent id the game is said to give (claim), and are never
  reused: the game's reuse isn't measured (facts, "Handles and identity"). The 28 players, slots 0 to 27 (fact), are
  made first. `GetHandleId` waits for wave 1, with the rest of the core.
- **D48. The run's log** has an entry for each `print`, each text shown to a player, whichever player, and each Lua
  error, as `<seconds to 3 places> <source>: <text>`, the source `print`, `player <n>` or `error`; text of several
  lines stays one entry with that before each line, so every line printed reads on its own. An error's traceback is
  left out. The game shows text only to the local player and loses what is printed before `main` (facts); the log
  keeps it all, as the test's record. A Lua error in the script, `config`, `main` or a timer goes in the log and the
  run carries on, as the game drops errors in callbacks (fact); a map without `config` or `main` logs "attempt to call
  a nil value (global 'main')".
- **D49. The natives become Lua globals** by reflection over `GameNatives`'s `[Native]` methods, sorted by name, so the
  global table fills the same way every run; a Missing native throws before its arguments are converted, whatever
  they are. An integer parameter takes a whole float (fact) and refuses another with Lua's "number has no integer
  representation"; a real takes an integer; a boolean is Lua's truth; a string, handle or code takes nil as null (for
  a string, replaced by D80: nil is refused);
  anything else, a table or a coroutine too, is Lua's `bad argument #n to 'Name' (<type> expected, got <type>)`. A
  handle is returned as the native's declared type. The game's own Lua functions, Close: `print`, joining `tostring`
  of each argument with tabs, as Lua's does; `FourCC` (facts), a string's bytes big-endian (an object id for 4; other
  lengths not measured); and `__jarray(default)`, which World Editor's scripts make each array variable with
  (`udg_x = __jarray(0)`, with `0`, `""` or `false`): a table whose missing keys read the default through its
  metatable's `__index`, storing nothing. `FourCC` and `__jarray` are Lua, so `FourCC` reads the bytes, which a Lua
  string read in C# loses when it isn't UTF-8. The four maps in `Maps\lua-reference` load their scripts and stop in
  `config` at `SetMapName`. After W0-5: 1 Exact (`GetLocalPlayer`), 14 Close, 2 Recorded, 1,818 Missing.
- **D50.** Probe maps are built with the upstream `War3Net.Build.Core` 6.1.2 from nuget.org, the package of
  `War3Net.Build` that holds `Map`, `MapBuilder` and `MapFactory`, all the builder uses. `War3Net.Build` itself adds
  only the JASS script builder and the transpiler, and their packages. No fork is needed. (W0-4)
- **D51.** A probe map is War3Net's `MapFactory` map at 32 by 32 (info format 28 as World Editor 1.31 wrote it, flat
  ground, the melee flag cleared, one force holding player 1), with `config` and `main` as World Editor writes them for
  one user player, and a black 256x256 `war3mapMap.tga` made in code, since the game closes a map with no minimap image
  (facts) and no game file may be in git. No pathing, shadow or doodad file: the game loads it without. (W0-4)
- **D52.** A probe is plain Lua that calls `out(text)` per line and `done()` at the end. It runs in the first
  zero-second timer, through `guard(f)`, which runs `f` in `pcall` and on an error adds the line
  `error: <message>` (line breaks made spaces, cut to fit as below) and calls `done()`; a probe passes its own timer
  functions through `guard` too. These rules are tested by running a probe map's script in MirrorImaged's Lua with stand-ins for the natives.
  `out` refuses, with a Lua error, a line over 253 bytes or with a line break, the most one `Preload` call keeps whole
  as one line of the file (facts "Files and Preload"); a backslash counts two, as the game writes it doubled (whether
  the game's cut counts it once or twice is not measured, so this is the safe side). `done()` writes the lines to
  `CustomMapData\MirrorImaged\<name>.pld`, each as `")\n` + line + `\n//` (as `docs/tools/gate.ps1` reads them), then
  ends the game. A name is letters, digits and dashes, as it is a file name and a Lua string. (W0-4)
- **D53.** The runner is in `tests/MirrorImaged.GameTests`, with its tests, since what uses it is tests that compare
  with the game; the builder and the probes are in `probes/`. (W0-4)
- **D54.** The runner always puts the game on a hidden desktop: there it runs at full speed and shows nothing (facts).
  Minimised is ruled out (game time stops), and a normal window, which also runs, is not needed, so there is no choice
  to make. The desktop is asked for `DESKTOP_READOBJECTS`, to list its windows, and `DESKTOP_CREATEWINDOW`, without
  which `CreateDesktop` fails with error 8 (seen 11-08). (W0-4)
- **D55.** Every probe map writes `CustomMapData\MirrorImaged\loaded.pld` at the end of `main` and `started.pld` in its
  first zero-second timer, before the probe. While `loaded.pld` is there and `started.pld` isn't, the runner posts a
  space key to the game's window every 3 s (facts: the loading screen waits; a posted key passes it). These marks, not
  `War3Log.txt`, say the map loaded, because the game holds the log back while it runs and loses it when killed
  (facts). A run with no lines is `NoOutput` when the map loaded; `AtLogin` when the game still ran but never logged in
  (it rewrites `War3Preferences.txt` once it has, facts), with its detail starting "needs Sven: ..." and saying what is
  known: it was seen every time the runner ran under Claude Code's Bash tool and never from PowerShell (facts); and
  `Queued` otherwise. What the game shows can't be captured on the hidden desktop (facts), so `AtLogin` is told from the
  preferences file, not from a picture. (W0-4)
- **D56.** The game starts suspended in a job object of its own with `JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE`, and is
  resumed once in it, so everything it starts is in the job (measured: no breakaway). The runner ends the job, never
  closing the game's window, which opens the quit dialog; when the test host ends, however it ends, Windows closes the
  job and the game goes with it. It stops nothing outside the job. (W0-4)
- **D57.** Runs are one at a time on a computer: a run holds the named mutex `MirrorImaged.GameRunner` for its whole
  length, a second waits for it up to 5 minutes (well inside the 10 after which the test host's blame collector ends a
  run) and then fails saying so, and a run refuses to start while any Warcraft III is running, as it couldn't tell that
  game's files from its own. Two runs at once made one fail (facts).
  A test that starts a fake `Warcraft III.exe` holds the mutex while it runs. The run's detail names the installed
  build and warns when it isn't 3.0.1.24371, the version the facts were measured on. (W0-4)
- **D58.** Built maps go to `%LOCALAPPDATA%\MirrorImaged\maps`, as `<name>-<8 random hex digits>.w3x`, new each run and
  deleted after it, since the game loads no map whose path is longer than 173 characters (facts) and the test assembly's
  folder is long already; a longer path is refused before the run. (W0-4)
- **D59.** The runner never writes over any of Sven's files and puts nothing back. The one file of his whose content a
  run changes is the account's `LastReplay.w3g`, which the probe's game replaces (facts). Every launch also rewrites
  `Documents\Warcraft III\Logs\War3Log.txt` (the game does it), which the workspace's `docs/tools/gate.ps1` reads to
  confirm Sven's own test-map runs, so a run between his test map and that read loses it. Before a run each account's
  `LastReplay.w3g` whose SHA-256 isn't one a probe run made is copied to `Replays\Saved by MirrorImaged\LastReplay
  <its write time, yyyy-MM-dd HHmmss>.w3g`, or with the first 8 digits of its hash added when another replay holds that
  name; never over a file, and not again when a copy with its content is there. After the run the hash of each
  `LastReplay.w3g` that changed is added to `%LOCALAPPDATA%\MirrorImaged\probe-replays.txt` (hexadecimal, one a line),
  unless a Warcraft III outside the run's job was seen during it (Sven's own game, or World Editor's Test Map), in which
  case the replay may be his and the next run saves it. This replaced a keep-and-restore of five files, which could put
  a stale copy over a newer file. (W0-4)
- **D60.** A `Needs=Game` test whose probe gives no lines fails with the run's detail, rather than passing or being
  skipped: xUnit 2 can't skip once a test runs, and a pass would hide that nothing was checked. (W0-4)
- **D61.** The hub (one game kept open, handed each probe through a Preload request and `ChangeLevel`, each probe going
  back to a hub built for the next request) was built, tried and measured not to work: the hub read its request, but
  `ChangeLevel` from a `-loadfile` game does nothing or crashes the game (facts). It is removed; with no login needed, a
  launch per probe costs about 15 s. (W0-4)

### The game's globals and blizzard.j (W1-1)

- **D62. common.j's globals are generated with the natives,** as `GameNatives.Globals()` in `Generated/Globals.g.cs`:
  each global's name and its value as C#, sorted by name. A value is an integer (any of JASS's forms, and the products
  such as `MAP_LOCK_SPEED`'s `8192*2`, written out), a boolean, or the call of the native common.j calls, with integer
  arguments (`ConvertPlayerColor(0)`, `GetPlayerNeutralPassive()`), so each handle is made by its native as the game
  makes it, and no JASS is committed. Any other form, and an integer past 32 bits (`2147483648`, `$100000000`), stops
  the generator, naming the global. 3.0.1.24371's common.j has 1,871 globals, all constants: 1,857 made by 82 of the 88
  `Convert*` natives, 10 integers, 2 booleans and the two neutral players. A run sets them as Lua globals in that order,
  after the natives and before the game's Lua functions and blizzard.j. Not measured: whether the game's are plain
  globals a script can change, as they are here.
- **D63. Every handle has a key, and Lua's handles hold it.** A handle's key is its own in its game, given when it is
  made, whatever its type, and never given twice; it is not its id, the number the game shows (`GetHandleId`, W1-6).
  The userdata Lua holds is made from the key and the type the handle is seen as (`LuaHandle(Type, Key)`; D41's "its
  id" is now its key): the same key as the same type is the same userdata, as another type another, and `tostring`
  and the metatable follow the type. A handle passed back is found by its key, so one seen as a parent type, as
  `GetTriggerEventId`'s `eventid` will give `gameevent`, `limitop` and the other event types, is still found, and
  `gameevent` 0 and `limitop` 0, both eventids standing for 0, are two handles; a handle that isn't of the parameter's
  type is refused with D49's bad argument, as the type Lua sees is. Keying by id and type, as the first version did,
  missed a handle seen as its parent, and an id alone is ambiguous between a converted value and an agent.
- **D64. A Convert native gives the handle standing for its value:** one object per type and value, made the first
  time and kept, whose id is the value, with a key of its own and no game id, so the agents' ids are as before (D47).
  So the same value gives the same userdata and `==` holds, and the same value as another type is another handle
  (`ConvertRace(0) ~= ConvertPlayerColor(0)`). All 88 are Close. `ConvertPlayerColor` and `ConvertRace` (facts:
  `GetPlayerColor` equals `ConvertPlayerColor(n)` and `GetPlayerRace` `RACE_*` with `==`), and `ConvertAttackType`,
  `ConvertDamageType` and `ConvertWeaponType` (the Moonwell.Damage gate's types, made once and compared with `==`)
  become Exact in the tasks that add those getters, which the facts compare them with. Not measured: `GetHandleId` of
  one (said to be the value; W1-6), and a value common.j doesn't name, which gives a handle like any other.
- **D65. blizzard.j is transpiled at run time** from the install's `War3.w3mod:scripts/blizzard.j` (the root spells it
  `Scripts/Blizzard.j`; its lookup ignores case, D24), with `common.j` registered first for the natives' return types,
  by the upstream `War3Net.CodeAnalysis.Transpilers` 6.1.1 from nuget.org, rendered by its CSharp.lua with the default
  settings. 6.1.1, not 6.0.2: 6.0.2 brings `War3Net.CSharpLua` 2.0.3, whose `NuGet.Packaging` 5.11.6 and
  `Newtonsoft.Json` 9.0.1 fail the build on their advisories; 6.1.1 brings 2.0.4, which doesn't. No fork: two changes
  are made to the JASS before transpiling, by War3Net's own `JassSyntaxRewriter`. A name that is a Lua keyword and not
  a JASS one gets `_lua` after it, since War3Net writes it as it is and Lua can't load it (`SubStringBJ`'s parameter
  `end`; `docs/upstream.md`), and JASS refuses `end_`. A `debug` statement is left out, Close: War3Net transpiles it as
  if it had no `debug`, and the JASS manual says the game never runs one (claim, not measured; blizzard.j's
  `PlayDialogueFromSpeakerEx` has one). The same two changes are to apply to a map's JASS (W1-7). The Lua runs as the
  chunk `blizzard.lua`, after common.j's globals and the game's Lua functions (its globals use `FourCC` and
  `__jarray`), before `war3map.lua`; an error in a BJ function reads `blizzard.lua:N:`, N the line in the cached file
  (D66); the game's name for it isn't measured. Its globals make 4 timers and 3 groups, the handles after the 28
  players, and call the slot natives (D67); none of its functions runs until a map calls one. They call the natives as
  globals, looked up at each call, so a native a script replaces is what they call (facts). A blizzard.lua that
  doesn't load or run stops the run before the map: `Simulation.Load` throws its `LuaException`
  (`blizzard.lua:N: ...`), or, for a Missing native, comes back `Stopped` (D43). Without an install, `Simulation.Load`
  throws a `DirectoryNotFoundException` ("A run needs blizzard.j from an installed Warcraft III, and there is none in
  ..."), after reading the map, so a missing map is still reported as such. The tests of the runtime itself load their
  maps through an internal seam, `Simulation.Load(path, blizzard, options)`, with `""` in place of blizzard.j, so CI
  runs them on every platform; only the tests that need blizzard.j itself are `Needs=Game`.
- **D66. The transpiled Lua is cached:** a transpile takes about 1.6 s in a new process (0.35 s opening the storage, the
  rest parsing and transpiling; Release build). It is kept once a process, and between processes in
  `%LOCALAPPDATA%\MirrorImaged\cache\blizzard-<hash>.lua`, the hash (SHA-256, its first 32 hexadecimal digits) of the
  game's build key and of the bytes of the assemblies that make the Lua: MirrorImaged's own, War3Net's JASS parser and
  transpiler, and CSharp.lua's renderer. So a change to any of them makes a new file, and nothing else does. A file is
  written whole under another name and moved in place, so no process reads half of one. Each read marks the file used
  (its write time is set to now), and writing one removes the `blizzard-*` files there not used for 7 days, so lanes
  running other builds at once keep theirs. A cache that can't be read or written is passed over, and there is none
  when the user's local application data folder is unknown or an assembly has no file (a single-file app). A
  `mirror run` of a small map then takes about 0.2 s.
- **D67. The slot natives:** `GetBJMaxPlayers` and `GetPlayerNeutralAggressive` 24, `GetBJPlayerNeutralVictim` 25,
  `GetBJPlayerNeutralExtra` 26, `GetPlayerNeutralPassive` 27 (Exact: facts, a new item belongs to neutral passive, slot
  27) and `GetBJMaxPlayerSlots` 28; the rest Close, the values the game has given since it had 24 players, not
  measured here. `CreateGroup` makes a group with the next id, Close; what groups hold comes with the units (wave 2).
- **D68. Left for W1-7,** whose JASS rules reach blizzard.j too, as it goes through the same transpiler: a whole number
  given to a real stays a Lua integer (`bj_MELEE_MINE_SEARCH_RADIUS` is the integer 2000; `docs/upstream.md`), and
  JASS's integer division is Lua's `//`, which floors where JASS cuts toward zero. What the game's own Lua gives for
  these is not measured. After W1-1: 2 Exact, 108 Close, 2 Recorded, 1,723 Missing.

### Triggers and threads (W1-2)

The measurements are four probes, `probes/triggers*.lua`, in `docs/facts.md`, "Triggers and threads", with their lines
in `probes/expected`.

- **D70. The script's callbacks run in Lua threads of their own,** as the game's do (not the main thread,
  `coroutine.isyieldable()` false): a timer's callback, a condition or filter, a trigger's action and `ExecuteFunc`'s
  function each start a thread (`LuaFunction.Start`, sandbox.c's `mirror_start` and `mirror_resume`). The script can't
  yield one: there sandbox.c's `coroutine.yield` raises "attempt to yield across a C-call boundary" and
  `coroutine.isyieldable` is false, while a coroutine the script makes yields as usual. Nor can it run one on:
  `coroutine.resume` of one answers false, "cannot resume non-suspended coroutine" (not measured; the game's threads
  aren't Lua's to resume). A wait is the only yield: a C# function asks for it (`LuaState.Yield`), and the trampoline
  yields the calling thread once C# has returned, so `pcall` and `xpcall` across it keep working (facts). A trigger's
  actions and `ExecuteFunc`'s function are run on after their wait; a timer's callback or a condition that waits is let
  go of, so it never goes on, as measured. The natives know no Lua: a function the script gave them is the target of
  their delegate, an `IScriptFunction` that starts it in a thread; `IScriptThreads` starts a global by name and makes
  the caller wait; an `IScriptThread` waiting is run on or let go of. Not matched, as the game's threads are stacks of
  their own rather than Lua coroutines: a wait past a C function (a `table.sort` comparator in an action) raises here
  and waits in the game, and a wait in a coroutine the script made inside an action yields that coroutine here and ends
  the action in the game. A wait in `main` raises Lua's "attempt to yield from outside a coroutine" (not measured). The
  state now makes a table for its threads and wraps `coroutine.resume`, which moves the addresses `LuaDeterminismTests`
  pins. Left to wave 2 (W2-6, `docs/tasks/wave-2.md`): `ForGroup`, `ForForce`, `Enum*` and filter callbacks, measured
  only as far as timers, conditions, actions and `ExecuteFunc` go; and to wave 7 (`docs/tasks/later.md`): `common.ai`'s
  threads.
- **D71. A wait ends at the first tenth of a second of game time after its timeout has passed,** counted in steps from
  the start; a timeout of 0 or less takes 0.1 s, and one past the clock's range never ends. Close: the game's waits end
  mostly on that grid, at or one tenth after the timeout, but differ between runs (facts). Of the 28 waits measured in
  `triggers` and `triggers-2`'s first run, as call + timeout = the game's end, the rule matches 19: 0.6 + 1 = 1.7, 1.7 +
  0 = 1.8, 2.0 + 0.33 = 2.4, 2.4 + (-1) = 2.5, 2.5 + 0.5 = 3.1, 2.0 + 0.1 = 2.2, 2.2 + 0.1 = 2.4, 2.8 + 0.2 = 3.1, 3.1 +
  0.25 = 3.4, 3.4 + 0.5 = 4.0, 4.0 + 0.55 = 4.6, 4.6 + 0.01 = 4.7, 2.033 + 0.1 = 2.2, 2.2 + 0.2 = 2.5, 2.5 + 0 = 2.6,
  2.6 + 0.5 = 3.2, 2.4 + 0.3 = 2.8, 2.8 + 1 = 3.9, 0.8 + 0.2 = 1.1; and misses 9, the rule's end in brackets: 1.8 + 0.05
  = 2.0 (1.9), 0.8 + 0.5 = 1.3 (1.4), 0.9 + 0.25 = 1.3 (1.2), 2.4 + 0.1 = 2.5 (2.6), 2.5 + 0.15 = 2.8 (2.7), 4.7 + 0.09
  = 4.9 (4.8), 4.9 + 0.11 = 5.125 (5.1), 2.25 + 0 = 2.4 (2.3), 0.6 + 0.5 = 1.1 (1.2). Rounding the timeout to tenths and
  adding one matched 20; the rule is the simplest to state. A wait never ends in the step it started in, so
  `PolledWait`'s loop always moves the clock; `PolledWait(1)` takes 1.1 s, as twice in the game. As wait lengths vary,
  `probes/expected/<probe>.txt` marks the lines they reach: a line starting `# ` is a note; one starting `~ ` varies
  from run to run, each `{t}` in it standing for a number and the rest exact, and a run gives it once, anywhere, as its
  time decides its place; every other line comes in order, exactly; a `~ ` line without a `{t}` is refused. W1-8's
  comparison reads the same format (`docs/tasks/wave-1.md`); `ExpectedLines` in `GameProbeTests.cs` reads it now.
- **D72. `==` holds for the same handle seen as two types:** each handle type's metatable has an `__eq` comparing the
  handles' keys (D63), so `GetTriggerEventId() == EVENT_GAME_TIMER_EXPIRED` and a widget equal to its unit, as the game
  measures; `rawequal` stays false (D41). Called straight from the metatable with anything but two handles, it answers
  false. The game's `==` also holds between different handles with the same id (`GetTriggerEventId()` with id 4 `==`
  `ConvertPlayerColor(4)`; `Condition(nil) == Filter(nil)`), which needs the id in the userdata: left to W1-6
  (`docs/tasks/wave-1.md`), so `GetTriggerEventId`, `Condition` and `Filter` are Close until then.
- **D73. Boolexprs:** `And` and `Or` run their second operand only when needed; a nil operand, `Not(nil)` and a nil
  condition are false; a destroyed `Condition` or `Filter` fails without running, while a destroyed `And`, `Or` or `Not`
  still runs. `Condition(nil)` and `Filter(nil)` each give one handle every call, which passes as a trigger's own
  condition and is false in an `And`, `Or` or `Not`: this matches every measured case but one, `And(true,
  Condition(nil))`, true in the game. In the game the two are one handle; here, as C# types, two. In `triggers` a
  condition destroyed while a trigger had it, then a new one added and removed, still ran the new one through the old
  one's place (`ran removed`; handle id reuse inferred), which isn't modelled: `TriggerAddCondition`,
  `TriggerRemoveCondition`, `DestroyCondition` and `TriggerEvaluate` are Close for it, left to W1-6 with the ids.
- **D74. A run takes the trigger's actions as it starts** (facts), so an action removed while a run goes on, by
  `TriggerRemoveAction`, `TriggerClearActions` or `DestroyTrigger`, is let go of only once the trigger's last running
  thread has ended. `DestroyTrigger` lets go of its events too and lets a waiting run go on: the claim that it breaks
  the game's handle stack isn't modelled.
- **D75. The event responses read the running thread's firing:** its trigger, its event id and its game state, kept
  across its waits. An event sets all three; `TriggerExecute` keeps the caller's event and names its trigger;
  `TriggerEvaluate` names the trigger for its conditions; `ExecuteFunc` keeps the caller's (after its wait the game read
  only the trigger: the event kept there too is the model). Outside an event the event id is `ConvertGameEvent(0)` seen
  as `eventid` (the game's has id 0). Units, players and the other responses join it in their tasks.
- **D76. Events here:** timer events schedule themselves, not through a timer handle, so `GetExpiredTimer` is nil in
  them, as measured; a timeout under 0.01 s is 0.01 s. Among what falls due in one step, a timer event keeps the place
  it got when registered, every time (`Game.At` with a place), which both measured ties fit: a periodic event registered
  before a timer started later ran first when both fell due (`triggers`, at 3.0), and a periodic 0 s event fired 10
  times in 0.1 s, before the 0.1 s timer started after it (`triggers-2`); so it is Exact. Whether timers keep such a
  place too, against D45's queue order, is W1-8's to probe. Variable events never fire, as in the game for a Lua global.
  Integer game states (divine intervention, disconnected; W1-4's `GameState.cs`) fire their events on a change as
  measured, with the event id the comparison's value less 255 (`ConvertGameEvent(-253)` for `EQUAL`). The time of day's
  events are W1-9's, with W1-4's clock: their registrations are kept in `_stateEvents`, for the clock to fire with
  `Fire` and the id 3; until then `TriggerRegisterGameStateEvent` is Close. `TriggerRegisterGameEvent` is W1-9's too.
- **D77. `BlzTriggerIsRunning` and `BlzTriggerInterrupt`** (probe `triggers-4`): a trigger is running while a run of its
  actions hasn't ended, waiting or not; `TriggerEvaluate`'s conditions are no run. An interrupt stops the runs started
  before it: the action running goes on, but no other action runs, and a run that waits stops at its wait's end, so it
  reads running until then. When the action running interrupts its own trigger and then waits, the wait wins: the run
  stops at that wait's end, and the rest of the action never runs (not measured; the probe's action ended without a
  wait). A run started after the interrupt runs as usual (not measured). After W1-2, with W1-3 and W1-4 merged first: 93
  Exact, 265 Close, 7 Recorded, 1,470 Missing.

### Hashtables, strings, maths and randomness (W1-3)

The facts are `docs/facts.md`, "Strings, numbers and hashtables": four probes in `probes/`, kept so W1-8 can compare
them, as every measured line is a test. Each model was fitted against those lines with the Lua library's own float
functions before it was written.

- **D80. A string parameter is read as `luaL_checklstring` reads it,** for every native: nil and booleans are refused
  with Lua's `bad argument #n to 'X' (string expected, got nil)`, a number is written as `tostring` writes it (Lua's own
  `%.7g` through the library, and `.0` after a whole number), and the native gets the string up to its first NUL. This
  replaces D49's "a string takes nil as null". Measured on seven natives (nil), three (numbers) and three (NUL); that
  the game binds every native alike is inferred.
- **D81. The maths natives call the Lua library's float functions** (`native/mifloat.c`, through `MirrorImaged.Lua`'s
  `Native`, which `MirrorImaged.Natives` now references), so they give `math.*`'s bits here as the game's natives give
  the game's `math.*`'s: Close where that model is (D31, D32), the game's value beside the model's in the tests (34 of
  the 91 measured cases are a step or more off). Where the game's natives differ from its `math.*`, they follow the
  game: `SquareRoot` is 0 below 0.001, and `Asin`, `Acos` and `Atan2(0, 0)` give 0, as the model's functions already do.
  `Deg2Rad` and `Rad2Deg` multiply by the floats nearest π/180 and 180/π in the game's arithmetic: Exact on the ten
  measured. The new `math.*` measurements (`math.rad`, `math.deg`, `math.asin(2)`, `math.atan(0, 0)`, `^` of a negative
  base, sines of 1 and 1e6) are left to W1-8, which owns the model.
- **D82. StringHash** is lookup2 of the string's UTF-16 letters, folded to upper case and written as one or two bytes,
  with `""` 0 (all 43 measured strings and 19 pairs). Measured folded: a, b, c, h, w, ä, é, ö, þ, ÷, œ, а, ё and я;
  measured left: ÿ, ß, ł, ą, ž, š, ő, ı, α, β, ա, ａ, ǆ and µ. The folding of the ranges a-z, à-þ, а-я and ѐ-џ is
  inferred from those; every letter outside them and œ is left as it is, measured or not.
  Close: a string that isn't UTF-8, which the game decodes byte by byte without checks, reaches C# with U+FFFD in its
  place (D38), so its hash differs; so does `SubString` cutting inside a letter, where the game gives the lone byte.
  Keeping Lua's bytes across the binding would fix both; it is left until a map needs it.
- **D83. S2R** sums the digits as an integer while they fit (then in the game's float arithmetic), cuts toward zero to a
  float and divides by ten to the decimals in the game's arithmetic: 67 of the 77 measured strings exact, the other 10
  (most with five or more decimals) a float step off, so Close. Tried and dropped: summing in floats from the first
  digit (`123456789` lost its last digit, as the game's addition drops it), in doubles (`4294967296` came out exact,
  not the game's 4294967040), dividing by ten once per decimal, multiplying by the game's 1/10, and dividing in steps of
  10^4 (70 of 77, but two more rules for three strings). Past the float range the model is extrapolated: its sum wraps
  as the game's products do (D28), and of the 13 strings of 39 to 48 characters measured (strings-4) three agree,
  eight are 9 to 14 float steps off, and two are far: 40 nines (the game 0, the model 99) and 40 decimals (the game
  0.123456791, as for 9 decimals; the model -1.814). How the game keeps a long fraction sane wasn't found.
- **D84. R2S and R2SW** write the integer part as an integer and the decimals as the fraction times ten to the
  precision, plus 0.5, cut, in the game's arithmetic, with the game's padding after the sign: 130 of the 141 measured
  cases exact. A precision below 0 writes 6 decimals with the zeros at the end dropped but one, and above 9 writes 9;
  the text is at most 255 bytes, cut there, and `""` when the length the width asks for passes a 32-bit integer, the
  pad and the length wrapping as 32-bit integers do (strings-4 to strings-6 and four probes of one call: 29 cases,
  all exact). Close from 2^31, where the game writes about ten digits of its own and then zeros and the largest
  float as `inf`: there MirrorImaged writes the float's exact digits, unpadded, as the game's algorithm wasn't found
  (dividing by ten repeatedly or by a power of ten made by multiplying, then taking digits, matched none). Close too
  for a number below 0 whose text passes 255 bytes: the game closes (0xC0000409, twice), which a run can't copy, so
  the text is cut as above 0.
- **D85. The hashtable natives** keep each key's integer, real, boolean, string and handle apart, as measured; a
  `Save*Handle` saves nothing and gives false for a null or destroyed handle, and saves a Convert native's value but
  gives false, as for `FOG_OF_WAR_MASKED` (a handle made by a native that makes objects gave true); a handle destroyed
  after its save loads as null; a save to a destroyed table saves nothing and gives false (not measured: from Lua a
  destroyed table reads as null, D41, which was). `FlushParentHashtable` destroys the table and gives its place back
  under the limit of 256. Exact are the natives measured and tested: `InitHashtable`, the integer, real, boolean and
  string saves and loads, the five `HaveSaved*`, `RemoveSavedInteger`, `RemoveSavedHandle`, both flushes, the saves
  of timer, group, agent, player and fogstate, and the loads of timer, group, widget, player and fogstate. Close: the
  location and text tag saves (measured true, but `Location` and `CreateTextTag` are Missing, so no test makes the
  handle), the other handle types' saves and loads (the same code, types not measured), and `RemoveSavedReal`,
  `RemoveSavedBoolean` and `RemoveSavedString`.
- **D86. The random natives go from the first bound up by the bounds' distance,** as measured (`GetRandomInt(5, 1)`
  5 to 9, `GetRandomReal(2, 1)` 2 to 3); this replaces D46's "GetRandomInt takes its bounds in either order", and
  `math.random`, which refuses an empty interval first, is unchanged. `GetRandomInt` counts its numbers in 32 bits that
  wrap, as the four widest pairs measured show (always -2^31 for (-2^31, 2^31-1), whose count wraps to 0; below 0 for
  (2^31-1, 0)). `GetRandomReal` works in doubles and holds at the float range's ends, so finite bounds never give inf
  or nan: the game's spread over (-3e38, 3e38) and (0, 3e38) is matched; its (3e38, -3e38), some numbers from about 0
  to 3.4e38 and a third tiny and below 0, is not (here 3e38 to the largest float). The generator stays SplitMix64,
  Close: the game's numbers for seed 42 are in the facts for whoever finds its algorithm.
- **D87. I2R, R2I, MathRound, I2S, S2I, StringLength, StringCase and the bit natives are Exact:** C#'s own operations
  give every measured case (`R2I` and `MathRound`'s `long` hold at the ends as .NET 9 and later do on every platform),
  with `I2R` cut toward zero as `TimerGetElapsed` cuts (D45). After W1-3, with W1-4 merged first: 65 Exact, 255
  Close, 7 Recorded, 1,508 Missing.

### Players, forces and the game (W1-4)

The players probe (`probes/players.lua`, its game lines in `probes/expected/players.txt`, `docs/facts.md` "Players and
the game") measured what a one-player `-loadfile` game starts with; the choices below follow it where it measured and
say so where not. What W1-4 left is in W1-9 (`docs/tasks/wave-1.md`), W2-1, W2-6 and W2-7 (`wave-2.md`) and
`docs/tasks/later.md`.

- **D90. A player's colour, race, controller and slot state are kept as values** on the `player` handle and given out
  through the Convert handles (D64), so `GetPlayerColor(p) == ConvertPlayerColor(n)` and `GetPlayerRace(p) ==
  RACE_HUMAN` hold with Lua's `==` (facts); `ConvertPlayerColor` and `ConvertRace` are Exact with them. A player's data
  (name, team, start location, alliances, states, handicaps, tech) lives on its handle, as later waves' unit data will.
- **D91. Who plays is seated after `config`, before `main`,** as the lobby does once `config` has run
  (`GameNatives.Seat`). The test chooses with `SimulationOptions.Users` (the slots people play, the first the local
  player) and `Computers`; without them it is a single-player game in the map's first user slot of `war3map.w3i`, with
  the map's computer slots. The options are checked against `war3map.w3i` before any of the map's Lua runs, each
  refusal an `ArgumentException` saying which slot and why: a person only in a slot the map gives a user, a computer in
  one it gives a user or a computer (the lobby lets a computer take an open slot), never a slot the map doesn't have,
  and no person, a slot given twice or one outside 0 to 23 neither. The map's neutral and rescuable slots always play,
  with the controller `config` gave them (inferred: the lobby offers them to no one). The chosen slots play with their
  controller set; the other slots of 0 to 23 are Empty and keep the controller `config` gave them. The one user of a
  single-player game is "Local Player" (facts); with several users each keeps "Player N", as the game's account names
  aren't known. Before seating, in the script's top level and in `config`, the local player is slot 0, the game type
  melee and no map flag set, while the players probe saw `InitGenericPlayerSlots` read melee inside `config`; the lobby
  before `config` is W1-9's. Two options, not three: the first user is the local player, so a test choosing whose game
  the run is orders `Users`. Choosing a selectable slot's race or a computer's difficulty is in `later.md`.
- **D92. Races come from the lobby:** a playing slot whose race is fixed (its preference names a race, and it isn't
  selectable) has that race; a selectable one gets `RACE_PREF_RANDOM` (replaced by D141: not with custom forces and
  fixed player settings, which keep every preference) and, like a random preference, a random race, as do the unused
  slots of 0 to 23 (probe; replaced by D141: an unused slot whose preference names a race has it); the neutral slots
  have race 0. The random races come from a generator of their own, seeded from the run's seed (`Game.RunSeed`), with a
  draw for every slot from 0 to 23 in order, used or not, so a slot's race doesn't depend on who else plays and the
  map's own `GetRandomInt` and `math.random` don't depend on the seating at all. Whether the game's lobby draws from the
  generator the script shares is not known (D46).
- **D93. The starting state is the probe's:** names "Player N" (replaced by D141: "" until the lobby seats the players),
  slots 1 to 23 Empty with `MAP_CONTROL_NONE`, team -1 and start location -1, the neutral slots Playing as
  `MAP_CONTROL_CREEP` on team 0, colour the slot, races selectable; each player giving itself alliance types 0 to 7,
  neutral passive and every player passive to each other (26 to 27 inferred), nothing else; game result 3, food cap
  ceiling 100, bounty from the neutral slots (26 inferred), `PLACED` 1 for a seated slot; handicaps 1; speed normal,
  difficulty easy, placement the map's settings, game type melee. A seated computer's and a named slot's defaults are
  inferred from these, to be settled by W1-9's probe (settled by D141: a computer's are a user's, and it has its
  `war3map.w3i` name). Gold below 0 reads 0 (measured) and so does lumber (inferred); other states keep any value. A
  tech maximum below 0 reads 2147483647; `SetPlayerTechResearched` sets the level, also lower, but not below 0
  (inferred), and an addition stops at 2147483647. An upgrade's level count, which the game's `AddPlayerTechResearched`
  stops at and its maximum reads, needs the game's data (W2-1), so those natives are Close; so is
  `SetPlayerTechResearched` (setting above that count isn't measured) and `GetPlayerTechCount` (a unit type counts the
  player's units, W2-2, and `specificonly` is ignored until then). `SetGameSpeed` refuses `MAP_SPEED_FASTEST`
  (measured); the speed changes nothing else, as game time is simulated. `GetGameTypeSelected` is the map's settings for
  a map with custom forces (inferred from the lobby, which offers no other there; replaced by D141: melee), and custom
  forces with fixed player settings set `MAP_LOCK_ALLIANCE_CHANGES` (facts); `SetMapFlag` is Close, as only that flag
  set false was measured. `war3map.w3i` is read with War3Net's `ReadMapInfo` from `War3Net.Build.Core`, now a package of
  `MirrorImaged.Data` too. Player names from `war3map.w3i` aren't used yet (replaced by D141: a computer's is): they are
  `TRIGSTR_n` until the strings file is read (W2-1), so `GetPlayerName` is Close.
- **D94. Forces are a mask of slots,** so `ForForce` goes in slot order (measured). It runs over the players the force
  holds as it starts: one the callback adds isn't visited and one it removes still is (not measured; the simplest
  reading of a mask copied at the start). The enumerations clear the force first and take the playing players of slots 0
  to 23, not the neutral ones (measured with one user; computers inferred). `ForceEnumAllies` takes the players the
  given player is passive to, itself among them, and `ForceEnumEnemies` the others (the direction isn't measured). A
  filter stops the run as the native being Missing until the boolexprs can be evaluated (W1-9), so a map never gets a
  wrong answer; `GetFilterPlayer` waits with them (replaced by D146: filters run). `ForForce` lets go of its callback
  (D42) and gives the outer enum player back after a nested one. It is Close: besides the copy, the facts' wait that
  never ends after a force was looped with `ForForce` and destroyed isn't modelled (W1-9; replaced by D148: modelled).
- **D95. Pausing stops the clock** (`Game.Paused`): what is due in the step the game paused in still runs, so a pause
  from one callback doesn't hold back the others of its frame, and then the clock stays; `Simulation.Advance` counts no
  paused time, so once the game goes on the clock moves from where it stopped. `Simulation.Paused` shows it; only the
  map can go on, as in the game, so the API has no way to resume. Not measured; the facts say only that the victory and
  defeat boxes pause the game. Until dialogs and input exist (waves 5 and 6), only a callback in the same step can go
  on.
- **D96. The time of day is worked out from the clock,** from where it was last set, at 0.05 hours a second times the
  scale, kept from 0 below 24 (by `%` on doubles, which is exact, so even a huge scale gives an hour of the day) and
  read cut toward zero to a float, so it reads under 24 (from 23.9, 2 s later 23.999998, where the game read
  23.9999962). An infinite or NaN scale is refused and the scale stays (not measured). Setting reads back as set; below
  0 and above 23.99 read 0 (measured: 23.99 kept, 23.995 not). Close: the game adds each frame's share and loses a
  little (0.0999975 for 0.1 after 2 s), reads a set 0.1 a float step lower, and its event at 12 reads a float step under
  12; the scale and suspending are Exact. The workspace's facts saw a 7 read 6.999999 (11-05, a Moonwell check map)
  where the probe read 7 back exactly; the facts' read may have come later than the set, and W1-9's probe is to separate
  them (answered by D149: neither read gives 6.999999). A day is the standard 24 hours of 480 s until the game's data
  (W2-1) gives a map's own (the facts' `DayHours=30` cases).
- **D97. Events wait for the triggers (W1-9):** `TriggerRegisterPlayerEvent` (victory, defeat, leave),
  `TriggerRegisterPlayerStateEvent`, `TriggerRegisterPlayerAllianceChange` and `TriggerRegisterGameStateEvent` stay
  Missing, and with them the events the facts measure inside `RemovePlayer`, `SetPlayerAlliance` and `SetPlayerState`
  and as the time of day passes an hour, so those three natives are Close (replaced by D142 to D144: the events fire,
  and the three natives are Exact). `RemovePlayer` sets the slot, Left for defeat and Playing for any other result
  (victory measured; replaced by D142: Left for all but victory, once), and the player's `PLAYER_STATE_GAME_RESULT` to
  the result given (inferred: the state starts at the neutral result, 3, and is named for it). `EndGame`, which ends the
  game for `CustomVictorySkipBJ`, stays Missing (W1-9; replaced by D147: it ends the run); `CustomVictoryBJ` and
  `CustomDefeatBJ` also need dialogs (wave 5). `DefineStartLocationLoc` and `GetStartLocationLoc` wait for locations
  (W2-6), `SetPlayerAbilityAvailable` for abilities (W2-7).
- **D98. What only the lobby reads is Recorded:** `SetMapName`, `SetMapDescription`, `SetEnemyStartLocPrio`,
  `SetEnemyStartLocPrioCount` and `SetPlayerRaceSkin` change nothing a native reads back, so they are Recorded, each
  with its reason. Their calls aren't written to the run's log: D48's log is what the map shows, and none of these
  shows anything during a run; how tests read Recorded calls in general is the test tools' (wave 8).
  `SetStartLocPrioCount` stays Close: whether `GetStartLocPrio` and `GetStartLocPrioSlot` read the slots past the count
  differently isn't measured, and here they read what was set. The lobby's placement of players on start locations
  isn't simulated, so a player's start location is the one `config` set, or -1.
- **D99. After W1-4:** 25 Exact, 169 Close, 7 Recorded, 1,634 Missing. `config` of the four maps in
  `Maps\lua-reference` runs through, and each stops at the first line of `main`, `GetCameraMargin` (wave 5); `main`'s
  player setup (`InitBlizzard`) comes after the cameras, sounds, regions and units, and its first player function,
  `InitBlizzardGlobals`, needs `Filter` (W1-2) and sounds first.

### Files and game caches (W1-5)

The files probe (`probes/files.lua`, its lines in `probes/expected/files.txt`) and its one-off runs measured what the
workspace's facts left open (`docs/facts.md`, "Files and game cache"); the choices below follow them where they
measured and say so where not.

- **D100. The documents are an object the test holds,** `Documents` (in `MirrorImaged.World`): `CustomMapData`, the
  files by their path in it, `\` between folders, found in any case, which a test reads, fills and empties; and the
  saved game caches, internal. A run takes it from `SimulationOptions.Documents` (a new, empty one by default) and
  shows it as `Simulation.Documents`; the same one given to the next run keeps both. It is in memory, not a folder: a
  run stays the same on every platform and a map can reach nothing outside it; a test that wants a real file copies it
  in.
- **D101. Paths are the game's, as the probe measured them.** A file is written and read only with `.pld` or `.txt` in
  lower case: `.PLD`, `.dat`, `.json`, `.ini`, `.log`, `.w3v` and none were written nowhere, and an outside program's
  `.dat`, `.json` and extensionless files weren't read. `/` is `\`, and `.` and empty folders are left out. `..` makes a
  write go nowhere, while a read goes through it (an outside file read through `sub\..`), never above `CustomMapData`
  (not measured). An absolute path (`C:\...`, measured) is neither written nor read, and neither is one starting with
  a slash (`\x.pld`, `/x.pld`, `\\srv\x.pld`, `//srv/x.pld`) or holding a colon (`C:x.pld`) (not measured). At most
  201 characters: 201 was written and read and 202 not, on a PC whose `CustomMapData` path has 58, so the game's limit
  is likely the whole path's 259 and moves with the player's folder; reads have it too (not measured: the longer files
  weren't there to read). Folders and names are found in any case, and so is a path `Preloader` has read (probe:
  `MIRRORIMAGED\FILES-PROBE\own.pld` gave `own.pld`'s first content). Windows's other refused characters (`<>"|?*`)
  aren't modelled.
- **D102. Preload and Preloader.** Every `Preload` text is kept, its first 259 bytes of UTF-8, until `PreloadGenClear`,
  whether or not `PreloadGenStart` came first and also after `PreloadGenEnd`, which writes them all and keeps them
  (probe). A cut can split a letter (not measured); `Preloader` reads the file as UTF-8, so the split letter comes back
  as U+FFFD, and a Lua string that isn't UTF-8 is already U+FFFD where it isn't when C# reads it (D38). A backslash is
  doubled as the file is written, after the cut: whether the game's cut counts it once or twice isn't measured, and
  D52's lines take the safe side. `PreloadEnd`'s number is written 0.0: 304 of the 311 Preload files on this PC have
  it, those known to be written in one call among them; 7 have 0.1, and a file ended 2.5 game seconds after its
  `PreloadGenStart` had 1.9, 2.0, 2.0 and 1.9 in four runs, so it counts something other than game time, perhaps real
  time, and `PreloadGenStart` and `PreloadGenEnd` are Close.
  `Preloader` reads a path once a game and keeps that content, also after the map writes the path again (probe); a
  path that was missing is read again (not measured). It runs the file at once: the JASS goes through War3Net's
  transpiler and D65's rewriter as blizzard.j does, against declarations of common.j's natives and globals made from
  `GameNatives` by reflection (the transpiler needs a function's declaration to call it inside an expression, and no
  game file is needed). The rewriter now also writes a rawcode as its value: War3Net writes it as `FourCC("...")` with
  its text as it is, so `'ab"c'` breaks out of the string and `'\abc'` reads `\a` as one byte; the value is War3Net's
  reading of the four characters, a negative one in parentheses (`-'éab'` would read as a Lua comment). War3Net reads a
  byte from 0x80 up after the first as signed: `'éab'` (UTF-8 `C3 A9 61 62`) is -1029086878, where its bytes, and the
  old `FourCC`, give -1012309662. Neither that nor the game's reading of a backslash in a rawcode is measured: W1-7's
  JASS probes are to settle rawcodes with high bytes. This reaches blizzard.j's rawcodes too, with the same values for
  its rawcodes, which are all ASCII. The file runs in a Lua thread of its own, as W1-2 runs a callback; a wait in it
  (`TriggerSleepAction`) lets the rest of the file go (not measured). The Lua is loaded as the chunk named by the
  file's path, so an error in it reads `MyMap\save.pld:N:` (N the transpiled Lua's line, as D65's are), and runs with
  those natives and globals as they were before blizzard.j and the map ran, and `__jarray`, which War3Net makes a JASS
  array with. So a native the map replaced isn't what a file calls, and a file's `PreloadFiles` isn't the map's
  (inferred: the game runs a Preload file as JASS, without blizzard.j or Lua, facts). A `Preload` call the file makes
  isn't kept for the map's next file, while what the map kept before and after the `Preloader` call is (probe).
  The game closed on a file with a cut piece and on one naming a blizzard.j global (facts), on a line that isn't JASS,
  and on a file that ends `PreloadFiles` and goes on in Lua, as some maps' save files do (probe); so a file that
  doesn't parse or names anything but those natives and globals stops the run with a `GameClosedException`, as for a
  Missing native (D43), at the line of the map's `Preloader` call, the file's error in the message. An error as the
  file runs (a missing argument) does the same; that one is not measured. The facts' "Lua cannot be run from a
  Preload file (`//!beginusercode` does nothing)" (10-01) didn't record the file: a `//!beginusercode` block of JASS
  comments runs nothing here, and Lua after `endfunction` closes the game as measured. A Missing native the file calls
  stops the run as D43 does, at the file's line (`MyMap\save.pld:N: The native ... is Missing`). JASS's own type
  checks aren't made (a call with an argument too many runs), and neither is its operation limit, so a file that loops
  forever hangs the run; the limit is W1-7's, for transpiled JASS. The game's own Preload files
  (`scripts\HumanMelee.pld`, which the melee BJ functions read) aren't in `CustomMapData` and read as missing; they
  only name files to load. `PreloadStart`, `PreloadEnd`, `PreloadEndEx` and `PreloadRefresh` are Recorded and not in
  the log (D98). The ability-tooltip carrier waits for W2-7 (`BlzSetAbilityTooltip` and its kin, with the facts'
  tooltip cases): until then a file calling it stops the run as Missing. The game-cache and `SetPlayerName` carriers
  work now (probe: the probe reads its files back through a game cache). Comparing the game closing with
  `GameClosedException` is W1-8's.
- **D103. Game caches, as the probe measured them:** one cache a name, every `InitGameCache` of it a new handle to it;
  each type's values apart; missions and keys found in any case (measured with ASCII letters; `ToUpperInvariant`
  here); a missing value reads 0, 0.0, false and an empty string; an empty string, false and 0 are stored values, and
  `StoreString` gives true. `FlushGameCache` removes the cache: its handles then read and store nothing, also once
  `InitGameCache` makes the name again, which gives a new, empty cache. `FlushStoredInteger` takes the integer only;
  the real, boolean and string ones take theirs (Close, not measured). A name is found in any case, as a file is (not
  measured).
- **D104. Saved caches.** A game starts with the caches saved in its documents, taken as the documents are given to
  it, so a save another game makes later doesn't reach it (a value saved in one game read 1 in the next, one stored
  after the save 0); `SaveGameCache` writes every cache, not only the one given (the game's file then held caches the
  probe never saved by name), and gives true, also for a name without `.w3v`; `ReloadGameCachesFromDisk` gives true,
  as in a live game, puts the saved caches back in place, so their handles read them, and drops the caches never
  saved, whose handles then read nothing. These two are Close: they were measured in one run, which no probe may
  repeat, since `SaveGameCache` rewrites the account's `BattleNet\<id>\Campaigns\Classic\Campaigns.w3v` (here
  `Documents` holds them). So caches go from one map of a test to the next through `SaveGameCache`, as from one game
  to the next. Left for later (`docs/tasks/later.md`): a level change in one game (`ChangeLevel`), which may keep
  caches never saved; replays, where `ReloadGameCachesFromDisk` gives false (facts) and Preload files are written
  again; and keeping `Documents` between processes or seeding its saved caches from outside. `SaveGameCache(null)`
  gives false (not measured). The sync natives do nothing: a run is one machine, whose caches are every player's. The
  unit natives (`StoreUnit`, `RestoreUnit`, `HaveStoredUnit`, `FlushStoredUnit`, `SyncStoredUnit`) wait for W2-3;
  the 255-cache limit (a claim) isn't modelled. `Documents` isn't thread-safe: one run at a time uses it.
- **D105. After W1-5,** with W1-2, W1-3 and W1-4 merged first: 112 Exact, 276 Close, 11 Recorded, 1,436 Missing.

### Player and game events (W1-9)

The measurements are three probes, `probes/lobby.lua`, `events.lua` and `forforce-wait.lua`, in `docs/facts.md`,
"Players and the game" and "Player and game events", with their lines in `probes/expected`. What they left is in
`docs/tasks/later.md` (wave 6), W2-1, W1-6 and W1-8 (`docs/tasks/wave-1.md`, `wave-2.md`).

- **D140. A probe map with a computer** (`ProbeMap.Build(..., withComputer: true)`, and the runner's `Run` likewise),
  for the lobby and events probes: a user in slot 0, a computer in 1 and a user slot 2 no one plays, named "Probe User",
  "Probe Computer" and "Probe Empty" in `war3map.w3i`, human with a selectable race, in custom forces with fixed player
  settings (0 and 2 in one force, 1 in the other, not allied). Its `config` first adds what it reads (the game type,
  the lock flag, the local player, the counts, and slots 0 to 2's name, slot state, controller, race and random
  preference) to the global list `ProbeConfig`, one entry a call: so a probe sees what the lobby set before `config`,
  and how often it ran (once, in the map's Lua state). One fixed layout, not options: both probes need the same, and
  the other probes' map stays as it was.
- **D141. The lobby before and after config** (the lobby probe). At the start of `config`, before it sets anything,
  slots 0 to 2 are empty with no controller, named "" and with the random race preference (slots 3 to 23 inferred the
  same, as only 0 to 2 were read), the local player is slot 0, the game type melee, `MAP_LOCK_ALLIANCE_CHANGES` not set
  (read only there, not after `config`'s own calls), 0 players and 24 teams. The lobby seats the players after
  `config` (D91), and then names a computer in a slot the map gives a computer as `war3map.w3i` does and every other
  slot of 0 to 23 "Player N"; puts a slot no one plays on team -1, with the controller `config` gave it; keeps every
  race preference when the map has custom forces with fixed player settings, selectable or not (the same with and
  without `war3map.w3i`'s selectable-race flag), and otherwise makes a playing selectable slot random, as before; gives
  a slot whose preference names a race that race, played or not; and, for custom forces with fixed settings, sets
  `MAP_LOCK_ALLIANCE_CHANGES` and the flag 2^20, which `common.j` doesn't name. So the lock isn't set before `config`,
  against the task's premise, and the seating stays after it. The game type is melee with custom forces too; custom
  forces without fixed settings aren't measured. A computer's alliances, states, handicaps and placed state are a
  user's, as D93 inferred. Not measured, and chosen: a name `config` set is kept (the game crashes the lobby when
  `config` calls `SetPlayerName`, facts), and a computer seated in a slot the map gives a user is "Player N", not that
  slot's `war3map.w3i` name. With `MAP_PLACEMENT_TEAMS_TOGETHER` the lobby put the players on start locations at
  random, different in each run; that isn't simulated (D98, `later.md`), so `GetPlayerStartLocation` stays Close.
  World Editor writes the names as `TRIGSTR_n`, read through the strings file in W2-1; until then a computer's name is
  `war3map.w3i`'s as it is.
- **D142. RemovePlayer takes a player out once** (the events probe and the facts): the result is stored, victory keeps
  the slot Playing and any other result leaves it Left (neutral measured, tie inferred), and a player that isn't a user
  fires its victory or defeat event inside the call, after the change, with itself the trigger player. A user fires
  neither, inside the call or a second later (victory measured here, defeat by the facts' `CustomDefeatBJ`); neutral
  and tie fire nothing (neutral measured); `EVENT_PLAYER_LEAVE` never fires for it. A second call on the same player
  does nothing, whatever the result. Who counts as a user is the controller: another person in a multiplayer game
  isn't measured. Exact.
- **D143. Alliance and player state events** (the events probe). `SetPlayerAlliance` fires only on a change, on the
  giving player's registrations: its `EVENT_PLAYER_ALLIANCE_CHANGED` first, for `ALLIANCE_PASSIVE` only (alliance types
  1 to 9 fire none), then the alliance type's own (`TriggerRegisterPlayerAllianceChange`), each with the event id
  `EVENT_PLAYER_ALLIANCE_CHANGED` and no trigger player, reading the new alliance. The workspace's facts say it fires
  "each time it is set"; setting the same value again fired nothing here, so that means each change. A player's
  alliance with itself doesn't change (passive measured). `SetPlayerState` fires the player's events for that state on
  a change to a value that meets the comparison, the value stored (gold set to -1 fires as 0), with `NOT_EQUAL` a real
  comparison, unlike the integer game states' (any change, D76); the event id is `EVENT_PLAYER_STATE_LIMIT` whatever
  the comparison, `GetTriggerPlayer` the player and `GetEventPlayerState` the state. Outside an event
  `GetEventPlayerState` is the state with id 0 (`PLAYER_STATE_GAME_RESULT`), not nil. `TriggerRegisterPlayerEvent`
  with `EVENT_PLAYER_STATE_LIMIT` never fires; its input events (leave, chat, cinematic, arrows, mouse, keys) register
  and never fire until wave 6 (`later.md`). The three registrations, `GetTriggerPlayer`, `GetEventPlayerState`,
  `SetPlayerAlliance` and `SetPlayerState` are Exact.
- **D144. The time of day's events** (the events probe; triggers-3's reads fit too, so D76's inference holds). A set
  fires inside `SetFloatGameState`, reading the value set, by where the time goes: `GREATER_THAN` and
  `GREATER_THAN_OR_EQUAL` when it goes up from the limit or below to the limit or above, `LESS_THAN` and
  `LESS_THAN_OR_EQUAL` when it goes down from above the limit to it or below (from the limit itself, nothing), `EQUAL`
  when it lands on the limit, `NOT_EQUAL` never. The workspace's facts say a set on from exactly the hour (6 to 18, 18
  to 17) fires the old hour's event too; here 12 to 13 and 12 to 11 fired no `EQUAL` inside the set, so that was the
  frames after (below). As the day runs, it fires a limit's `EQUAL`, `GREATER_THAN_OR_EQUAL` and `GREATER_THAN` events
  as it passes it forward, and its `EQUAL`, `LESS_THAN_OR_EQUAL` and `LESS_THAN` ones backward, the time reading the
  limit exactly; here at the first step at or past it, with the time set to the limit there (2.001 s from 11.9 at
  scale 1; the game's frame came at 1.9999), so the day loses up to a step's share at each pass, and again a day later.
  A set that lands on a limit while the day runs passes it at the next step, also when something else reschedules the
  day in the set's step. Only the nearest pass is queued, so a map setting the time every frame doesn't fill the
  queue. Not modelled, so `TriggerRegisterGameStateEvent` stays Close (W1-8 is to settle or model them): with the day
  stopped, the frame after a set to exactly the limit fired all five again (the facts' "again within the next
  second"); 12 to 13 with the day stopped, after frames at 12, fired nothing, where the same set within one instant
  fired the two upward comparisons; and with the day running, a set that lands on the limit fired `EQUAL` only at the
  next frame, which the model follows by firing `EQUAL` in a set only with the day stopped. The game's order among one
  moment's events (GT, GE, EQ forward; EQ, LE, LT backward; EQ, GT, GE in a set) isn't registration order and isn't
  followed.
- **D145. TriggerRegisterGameEvent never fires in a run:** none of its 16 events fired through the events probe, not
  the timer event for its timers, the state event for its state sets nor the victory event for its victories; that the
  end-level event doesn't fire for `EndGame` is inferred from a file its action would have written and didn't. Close:
  the game fires `EVENT_GAME_LOADED` after a saved game loads (facts, "Initialisation"), which a run doesn't do.
  `GetWinningPlayer` stays Missing, with no event to read it in (`later.md`).
- **D146. Filters** (the events probe). An enumeration's filter runs for each playing player of slots 0 to 23 in slot
  order, with `GetFilterPlayer` that player (nil after), and passes as a trigger's condition does: 1 passes; 0, `"x"`,
  nil, a table and a raising filter fail; `Filter(nil)` passes without running; a `Condition` serves too; `And` runs
  both operands for each player. `ForceEnumAllies` and `ForceEnumEnemies` run it for every playing player, then test
  the alliance; `ForceEnumPlayersCounted` stops once the force holds the count, without running the filter for the
  rest. Inside the filter `GetEnumPlayer` and `GetTriggerPlayer` were nil when the enumeration ran in a timer's
  callback, outside any event or `ForForce`; a filter run inside an event's action or a `ForForce` callback isn't
  measured, and here it reads the outer thread's trigger player and enum player. Not matched: a destroyed filter given
  again ran a condition made after it, through the handle's place (W1-6), where here a destroyed filter reads as nil
  and every player passes; so the four `ForceEnum*` stay Close until W1-6, as `TriggerEvaluate` does for D73. A filter
  runs in a thread of its own, as a condition (D70); W2-6 probes that.
- **D147. EndGame ends the run** (`Simulation.Ended`): the rest of its step runs, the code after the call and a 0 s
  timer started before it among it, then the clock stops for good, a pause the map can't lift, and `Advance` moves
  nothing. No event fires (D145; inferred from a missing file). The evidence is the probe's side files
  (`events-after.pld` and `events-timer.pld` written, `events-end.pld` not), which the events game test now deletes
  before the run and checks after, and W1-8's comparison is to check too. `doScoreScreen` changes nothing: a
  `-loadfile` game skips the score screen (facts). Exact.
- **D148. The lost wait after ForForce** (the forforce-wait probe, which reproduced the facts' three): a trigger's
  wait started right after a force `ForForce` had run over was destroyed never ended, whether the force held a player
  or not; `ForForce` alone, or a force made and destroyed, didn't; the next trigger's wait, 1.5 s later, and a
  `PolledWait` after it ended. Its reach, probed after the code review: a wait started 1 s after the destroy ended, and
  so did a wait already running across it, while a new one started beside it in the same instant was lost. Modelled
  so: destroying a force `ForForce` has run over loses the next wait started in the same step; a later wait, or one
  already running, ends. Not measured: a second new wait in that step. Why the game loses it isn't known (the trigger
  and timer made after the force took new ids, so it isn't an id reused at once). `ForForce` stays Close for the
  force's changes during it (D94).
- **D149. The time of day read back** (the lobby probe, D96's question): with the day stopped, 7, 12, 18, 6 and 7.5
  read back as set at once and in the same frame, and after a frame 7 read 7.00000477 (`40e0000a`), the others the
  same; 0.1 read `3dccccc0` and 23.9 `41bf3332` already at once, where the players probe read `3dcccccc` and `41bf3333`.
  So neither read gives the 11-05 map's 6.999999; the read drifts with the frames by a rule not found (times that are
  binary fractions of 24 hours stay exact), left to W1-8. D96's model stays, reading back as set, Close. After W1-9,
  with W1-5 merged first: 123 Exact, 273 Close, 11 Recorded, 1,428 Missing.
