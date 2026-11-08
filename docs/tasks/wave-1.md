# Wave 1: script core

Plan sections: 4 (A1, A2, A6), 5 (script path, time and randomness), 6 (script host, core, files), 9 (wave 1).
Needs wave 0. The facts' sections named below are in `C:\Users\mdlsvensson\Repo\wc3-cs\docs\game-facts.md`; every
measured case there becomes a test, and a native it measures is Exact.

Order: W1-1 first; then W1-2 to W1-6 at once (three at a time); W1-8 after W1-5; W1-7 last.

Parallel lanes, to keep merges painless:
- Decision numbers by range: W1-2 D70-D79, W1-3 D80-D89, W1-4 D90-D99, W1-5 D100-D109, W1-6 D110-D119,
  W1-8 D120-D129, W1-7 D130-D139. Gaps are fine.
- A conflict in `src/MirrorImaged.Natives/Generated/` is resolved by taking either side and re-running the generator
  (D22), never by hand.
- Each lane writes its natives in its own files (`Triggers.cs`, `Hashtables.cs`, `Players.cs`, `Files.cs`,
  `Handles.cs`, ...); shared files (`GameNatives.cs`, `Simulation*.cs`) get only small, additive changes.

## W1-1 The game's globals and `blizzard.j`

- The constants and globals of `common.j` (`PLAYER_COLOR_RED`, `EVENT_PLAYER_UNIT_DEATH`, and the rest, made by the
  `Convert*` natives) exist as Lua globals before the map's script runs, as in the game. Generated as code with the
  natives (names and values only), so no game file is shipped.
- `blizzard.j` is read from the installed game, turned into Lua by War3Net's transpiler and run before the map's
  script, so every BJ function and `bj_` global exists. Without an installed game, the run stops with a clear message.
- Replacing a global native changes what BJ functions call (facts, "Errors and callbacks").
- Tests: constants have the game's values; a BJ function runs; a replaced native is what a BJ function calls.

## W1-2 Triggers, events, conditions and threads

- Triggers, actions, conditions (`Condition`, `Filter`, `And`, `Or`, `Not`), `TriggerEvaluate`, `TriggerExecute`,
  enabling, waits (`TriggerSleepAction`, `PolledWait`) as coroutines on the simulated clock, timer events, game-state
  and variable events, map initialisation order (facts, "Initialisation": the game hasn't started inside `main`).
- The facts' "Errors and callbacks" section: errors in callbacks dropped (but logged by MirrorImaged), a raising
  condition counts as false, `Condition` returns a new handle every call, a timer-expiry trigger fires before the
  timer's own callback, a 0 s timer started in a callback runs in the same frame, `pcall` across waits.

## W1-3 Hashtables, strings, maths and randomness

- Hashtables (every save, load, have and remove native, `StringHash`), the string natives (`SubString`,
  `StringLength`, `StringCase`, `I2S`, `R2S`, `R2SW`, `S2I`, `S2R` and the rest, formatted as the game formats them),
  the maths natives (`SquareRoot` matching `math.sqrt`, facts "Lua runtime"), and the random natives on the seeded
  generator. The game's own random algorithm is not known: Close, with the same seed giving the same numbers.

## W1-4 Players, forces and the game

- Players, slots, names, races, colours, teams, alliances one way, forces, `ForForce`, player state (gold, lumber,
  food), tech limits and research levels, victory and defeat and their events, game speed, pause, time of day as a
  clock (facts "Version and launching", "Time of day").
- The player setup comes from the map's `war3map.w3i` and `config()`, with the test choosing who plays (users and
  computers) through the API.

## W1-5 Files and game cache

- Preload writing (`PreloadGenStart`, `Preload`, `PreloadGenEnd`) and reading (`Preloader`) in a simulated
  `Documents\Warcraft III\CustomMapData` that a test can read, fill and keep between runs (facts, "Files and Preload").
- The game cache natives, kept between maps of a run.

## W1-6 Handles and errors as the game gives them

- Handle ids and `GetHandleId`, `tostring` of a handle naming the declared type, argument type errors on a handle of
  another declared type (facts, "Handles and identity"); a Lua error's message starting `war3map.lua:N: `.

## W1-8 Probes compared with the game

Needs W1-5 (the probes write through Preload) and W0-4's runner.

- One command or test that runs a probe in the game (W0-4's runner, from the PowerShell tool) and in MirrorImaged,
  and compares their lines; a difference names the probe, the line and both values.
- Turn the measured cases of the facts' "Lua runtime" section that W0-3 left Close into probes and settle them: the
  C89 question (`math.log(8, 2)`, `math.log(3, 2)`, `0x1.0000011p0`, `%a`, `os.date('%F')`), division (`1/d` over many
  divisor mantissas, an `n/d` grid), `sqrt` over [1, 4), `sin`/`cos`/`tan` in radians, `exp`, `log`, `atan`, `pow`,
  subtractions that cancel bits, products near the range's ends, integer-to-float conversion, `pairs` order over a
  fixed set of string keys, which Lua 5.2 compatibility functions exist, and whether `load` accepts bytecode. Update
  the model where the probes show it wrong, and the levels.
- Also probe the order of timers due in the same step, and `TimerGetElapsed` at 3600 s (D45).
- This closes wave 0's "one probe matches the game".

## W1-7 JASS maps

Needs W1-1 to W1-6.

- A JASS map's `war3map.j` turned into Lua by War3Net's transpiler and run like a Lua map.
- JASS behaviour the transpiled code must keep, each Close unless a probe measures it: integer overflow wrapping,
  integer division, the operation limit ending a thread, an uninitialised variable stopping the thread.
- Tests: a small JASS map with triggers, timers and hashtables gives the same log as the same map written in Lua.
