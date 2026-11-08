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
- **D27.** Probe maps are built with the upstream `War3Net.Build.Core` 6.1.2 from nuget.org, the package of
  `War3Net.Build` that holds `Map`, `MapBuilder` and `MapFactory`, all the builder uses. `War3Net.Build` itself adds
  only the JASS script builder and the transpiler, and their packages. No fork is needed. (W0-4)
- **D28.** A probe map is War3Net's `MapFactory` map at 32 by 32 (info format 28 as World Editor 1.31 wrote it, flat
  ground, the melee flag cleared, one force holding player 1), with `config` and `main` as World Editor writes them for
  one user player, and a black 256x256 `war3mapMap.tga` made in code, since the game closes a map with no minimap image
  (facts) and no game file may be in git. No pathing, shadow or doodad file: the game loads it without. (W0-4)
- **D29.** A probe is plain Lua that calls `out(text)` per line and `done()` at the end; it runs in the first
  zero-second timer, inside `pcall`, so an error becomes the line `error: <message>` and still ends the probe. `done()`
  writes the lines to `CustomMapData\MirrorImaged\<name>.pld`, each as `")\n` + line + `\n//` (so each is one line of
  the file, as `docs/tools/gate.ps1` reads them; at most 253 bytes, facts "Files and Preload"), then ends the game. A
  name is letters, digits and dashes, as it is a file name and a Lua string. (W0-4)
- **D30.** The runner is in `tests/MirrorImaged.GameTests`, with its tests, since what uses it is tests that compare
  with the game; the builder and the probes are in `probes/`. (W0-4)
- **D31.** The runner always puts the game on a hidden desktop: there it runs at full speed and shows nothing (facts).
  Minimised is ruled out (game time stops), and a normal window, which also runs, is not needed, so there is no choice
  to make. It refuses to start while any Warcraft III runs: it couldn't tell that game's files from its own, and it
  stops only a game it started. (W0-4)
- **D32.** Every probe and hub map writes `CustomMapData\MirrorImaged\loaded.pld` at the end of `main` and
  `started.pld` in its first zero-second timer, before the probe. While `loaded.pld` is newer than `started.pld`, the
  runner posts a space key to the game's window every 3 s (facts: the loading screen waits; a posted key passes it).
  These marks, not `War3Log.txt`, say the map loaded, because the game holds the log back while it runs and loses it
  when killed (facts); the run's detail says whether the log shows `Opening map` all the same. A run with no lines is
  `NoOutput` when the map loaded and `Queued` when it never did. (W0-4)
- **D33.** The runner ends the game by killing its process tree, then any `BlizzardBrowser` or `BlizzardError` started
  since the game was, never by closing its window, which opens the quit dialog (facts). (W0-4)
- **D34.** Built maps go to `maps\` beside the test assembly (under `bin`), each run's under a new name
  (`<name>-<yyyyMMdd-HHmmss>.w3x`) and deleted after the run, so an older `War3Log.txt` never shows this run's map. (W0-4)
- **D35.** A `Needs=Game` test whose probe gives no lines fails with the run's detail, rather than passing or being
  skipped: xUnit 2 can't skip once a test runs, and a pass would hide that nothing was checked. (W0-4)
- **D36.** The hub: the runner writes one numbered request a file (`MirrorImaged\hub\<n>.pld`, written whole and then
  renamed, as the game keeps the content a path had when first read), and each probe goes back to a hub built for the
  next number, so neither the hub nor the runner keeps state between runs. It is kept, as the coordinator asked, though
  `ChangeLevel` doesn't load a probe from a `-loadfile` game on 3.0.1.24371 (facts); with no login needed, the runner
  launches the game once per probe instead. (W0-4)
