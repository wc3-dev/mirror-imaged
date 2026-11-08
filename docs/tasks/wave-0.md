# Wave 0: foundation

Plan sections: 1, 2, 4 (A1, A3, A7, A15), 5, 8, 9 (wave 0), 10. Order: W0-1 first; then W0-2, W0-3 and W0-4 at once;
then W0-5.

## W0-1 Repository skeleton and CI

- `MirrorImaged.slnx`, `Directory.Build.props` (net10.0, nullable, implicit usings, warnings as errors, package
  metadata: authors `mdlsvensson`, MIT, project and repository URLs of `wc3-dev/mirror-imaged`) and central package
  versions in `Directory.Packages.props`.
- Projects: `src/MirrorImaged.Lua`, `src/MirrorImaged.Data`, `src/MirrorImaged.Natives`, `src/MirrorImaged.World`,
  `src/MirrorImaged` (the public API) and `src/MirrorImaged.Cli` (tool command `mirror`), each with no code yet beyond
  what makes it build; `tests/MirrorImaged.Tests` (xUnit 2, as Moonwell) with one passing test. A
  `tests/Tests.runsettings` that fails a run hanging 10 minutes, as Moonwell's does.
- `.gitignore`, `docs/decisions.md` (heading and the plan's decisions as D1 onwards), `docs/facts.md` (heading),
  `docs/upstream.md` (heading).
- `tools/check-no-game-files.ps1` (PowerShell 7, cross-platform): fails on any tracked file with a game extension
  (`.w3x .w3m .w3n .mpq .mdx .mdl .blp .dds .slk .w3e .doo`), any file named `common.j`, `blizzard.j` or `common.ai`
  (any case), and any tracked file starting with an MPQ, BLP or MDX signature. With a test fixture proving it catches
  each case.
- `.github/workflows/ci.yml`: on pull requests and pushes to `main` and `release/**`; Windows, Ubuntu and macOS;
  .NET 10; build, test (skipping `Needs=Game`), and the game-files check. Actions pinned by commit hash, as
  `wc3-dev/moonwell/.github/workflows/release.yml` does.
- Done when: `dotnet build` and `dotnet test` pass locally, the check script passes on the repository and fails on its
  fixtures.

## W0-2 Natives, trust levels and the generator

Needs W0-1.

- In `MirrorImaged.Natives`: the `Level` enum (Exact, Close, Recorded, Missing) and an attribute on each native giving
  its level, the reason for Recorded, and the probe or fact for Exact.
- In `MirrorImaged.Data`: reading files from the installed game's CASC storage. Port the approach of
  `C:\Users\mdlsvensson\Repo\wc3-cs\wc3-dev\moonwell\src\Moonwell.GameData\CascStorage.cs` and `GameInstall.cs` (same
  author, MIT), with no reference to Moonwell. The install folder is found as there, or from `MIRROR_GAME`.
- `tools/MirrorImaged.NativesGenerator`: reads `common.j` and `common.ai` from the game, parses them with War3Net's JASS
  parser (NuGet `War3Net.CodeAnalysis.Jass`), and writes `src/MirrorImaged.Natives/Generated/*.g.cs`: a C# class per
  handle type, following `extends`, and a stub per native with its typed signature, level Missing, that throws an
  exception naming the native. The generated files are committed so CI builds without the game.
- A coverage report in the library and `mirror coverage`: counts per level, and the list of Missing natives.
- Tests: the parser on small JASS snippets; a `Needs=Game` test that the generated natives match the installed
  `common.j` and `common.ai` one to one. Record the counts in `docs/decisions.md`.

## W0-3 Lua 5.3 with the game's numbers

Needs W0-1.

- Lua 5.3.6 source from lua.org (check the SHA-256 the site lists), vendored under `native/lua` with its MIT licence.
- `luaconf.h` for 32-bit integers and single floats (`LUA_32BITS`).
- The game's sandbox: no `io`, `debug`, `package`, `dofile`, `loadfile`, `collectgarbage`; `os` with only `clock`,
  `date`, `difftime`, `time` (facts, "Lua runtime").
- The game's float arithmetic: route Lua's number operations (`luai_numadd` and the rest in `llimits.h`/`luaconf.h`)
  through C functions that reproduce every measured case in the facts' "Lua runtime" section: the rounding of
  additions and products, `1/0`, `0/0`, `100/0`, overflow and underflow, casts. Where the facts don't pin a model down,
  try candidate models and keep the one that matches all measured cases; record it in `docs/decisions.md`.
- `math.sqrt`, `sin`, `cos`, `exp`, `log`, `tan`, `atan` fitted to the measured errors as far as the facts allow;
  what can't be pinned down is recorded as Close.
- Build the library with `zig cc` (install zig with winget) for win-x64, linux-x64 and osx-arm64, by a script in
  `native/`. The built libraries are committed under `src/MirrorImaged.Lua/runtimes/<rid>/native/` so a plain
  `dotnet build` works without zig; CI rebuilds them and fails if they differ from the committed ones, or, if zig's
  output is not byte-reproducible across machines, checks a hash of the sources the binaries were built from.
- `MirrorImaged.Lua`: a `LuaState` over the C API (`LibraryImport`): run a chunk, register C# functions as globals,
  call a global, read and write values, errors with a Lua traceback.
- Tests: every number in the facts' "Lua runtime" section as a test case; the sandbox; the C# interop.

## W0-4 Probe maps and the game runner

Needs W0-1.

- `probes/`: plain-Lua probe sources and a builder (War3Net, NuGet `War3Net.Build`) that packs each into a playable
  `.w3x` with a small flat map, outside the repository's tracked files. A probe writes its lines to a Preload file and
  ends itself.
- The runner (in `tests/MirrorImaged.GameTests` or a `tools/` project, the implementer's call): launches the game
  with `-launch -windowmode windowed -loadfile <map>`; confirms the load in `War3Log.txt` ("Opening map - <path>" after
  "Activating WebUI", as `C:\Users\mdlsvensson\Repo\wc3-cs\docs\tools\gate.ps1` does); waits for the Preload file
  written after the opening; stops the game process it started; returns the lines.
- Keeping the game out of sight, tried in order and each result recorded in `docs/facts.md`: a hidden Windows desktop
  (`CreateDesktop`, `STARTUPINFO.lpDesktop`); a minimised window that never takes focus. WSL has no distribution on
  this PC, so the Wine route is recorded as not tried.
- The login: a `-loadfile` launch asks for the Battle.net login before the map loads (fact, 11-08). Detect it (no
  "Opening map" within a timeout, or the login window) and report the probe as queued for Sven, never typing anything.
  Build the hub map from the plan (section 10, "The login"): it reads the next probe's name from a Preload file and
  loads it with `ChangeLevel`; each probe returns to the hub. Unit-test what can be tested without the game.
- Run one real attempt with a trivial probe tonight and record exactly what happened.

## W0-5 Load and run a Lua map

Needs W0-2 and W0-3.

- `MirrorImaged.Data`: open a `.w3x`/`.w3m` (War3Net MPQ) and read `war3map.lua`.
- `MirrorImaged.World`: the simulated clock with a fixed step (choose it from the facts on timer precision and
  record it), a seeded random generator, handles with ids.
- The first natives, Exact where a fact measures them, each with its level: `CreateTimer`, `DestroyTimer`,
  `TimerStart`, `TimerGetElapsed`, `TimerGetRemaining`, `TimerGetTimeout`, `PauseTimer`, `ResumeTimer`,
  `GetExpiredTimer`, `Player`, `GetLocalPlayer`, `GetPlayerId`, `DisplayTextToPlayer`, `DisplayTimedTextToPlayer`,
  and the game's Lua extras such as `FourCC` (check the facts for others).
- Runs `config()` and `main()`, then advances time; text shown to players and `print` go into the run's log.
- A Missing native stops the run with its name and the Lua line that called it.
- `MirrorImaged`: `Simulation.Load(path, options)` with a seed, `Advance(TimeSpan)`, `Log`.
- `mirror run <map> --seconds <n> --seed <n>` prints the log.
- Tests with small Lua maps built in the tests: timers fire in order at the right simulated times, a periodic timer
  doesn't drift, the same seed gives the same log, a Missing native stops with its name and line.
- From the wave 0 reviews:
  - Replace all four of `os.clock`, `os.time`, `os.date` and `os.difftime` with versions on the simulated clock
    (the built-in ones depend on the machine's time zone, C library and clock even when fed simulated time).
    `os.date('*t')` returns a table, so it needs a Lua wrapper or table support in `LuaState`.
  - `math.random` and `math.randomseed` were left out of the Lua build: provide them on the same seeded generator as
    `GetRandomInt` and `GetRandomReal`.
  - A Missing native's exception must stop the run even inside a map's `pcall`: a map must not be able to swallow it.
  - Handles become Lua values (userdata or tables): their `pairs` order and `tostring` must be reproducible run to run
    (W0-3's allocator work covers this if it merged; check).
  - The generated handle classes are `partial`; add ids and world members in separate files, never in `Generated/`.
