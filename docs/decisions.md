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
  lower-case type names. (W0-2)
- **D20.** JASS types in C#: `integer` is `int`, `real` is `float` (the game's single floats), `boolean` is `bool`, and
  `string`, the handles and `code` are nullable, since JASS and Lua pass null for any of them. `code` is `Action?`,
  except where the game reads what the function returns: `Func<bool>?` for `Condition` and `Filter`, and `Func<int>?`
  for `common.ai`'s `SetHeroLevels`, whose function gives a hero skill per level in the melee AI scripts (known from
  older versions; those scripts are not in 3.0.1's storage under `scripts/`). (W0-2)
- **D21.** The generator writes `Types.g.cs`, `CommonJNatives.g.cs` and `CommonAiNatives.g.cs`, each sorted by name
  (ordinal), with `\n` line endings and a header naming the game version, so a re-run on the same install gives the
  same files. A `Needs=Game` test checks the committed files are what the generator writes. (W0-2)
- **D22.** A native is filled in by writing it by hand in `MirrorImaged.Natives`, outside `Generated`, in the partial
  `GameNatives`, and re-running the generator: it leaves out every native named by a public method in the project's
  other files. The one-to-one test reads all of them by reflection, so it holds as natives move out of the stubs. (W0-2)
- **D23.** The Exact native's probe or fact is the attribute's `Fact`. That a Recorded native names a `Reason` and an
  Exact one a `Fact` is a test over every native, since an attribute can't require a named argument. (W0-2)
- **D24.** The CASC reader is Moonwell's, reading a file by its root path (`War3.w3mod:scripts/common.j`), without
  Moonwell's graphics tiers and locales, which the scripts don't need. The install is the folder given, else
  `MIRROR_GAME`, else `C:\Program Files (x86)\Warcraft III`. (W0-2)
- **D25.** `mirror coverage` prints the count at each level and in all, then every Missing native, one a line, sorted.
  (W0-2)
