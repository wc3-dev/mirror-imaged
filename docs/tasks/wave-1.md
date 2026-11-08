# Wave 1: script core

Plan sections: 4 (A1, A2, A6), 5 (script path, time and randomness), 6 (script host, core, files), 9 (wave 1).
Needs wave 0. The facts' sections named below are in `C:\Users\mdlsvensson\Repo\wc3-cs\docs\game-facts.md`; every
measured case there becomes a test, and a native it measures is Exact.

Order: W1-1 first; then W1-2 to W1-6 at once (three at a time); W1-8 after W1-5; W1-9 after W1-2 and W1-4; W1-10 after W1-8; W1-7 last.

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
- Left by W1-2 (D72, D73; MirrorImaged's facts "Triggers and threads"): `==` holds between different handles with the
  same id, whatever their types (`GetTriggerEventId()` with id 4 equals `ConvertPlayerColor(4)`); `Condition(nil)` and
  `Filter(nil)` are one handle (which also fixes `And(x, Condition(nil))`); after `DestroyCondition` a new condition
  ran in the destroyed one's place (the probe saw `ran removed`; id reuse inferred). Then `GetTriggerEventId`,
  `Condition`, `Filter`, `And`, `TriggerEvaluate`, `TriggerRemoveCondition` and `DestroyCondition` can become Exact.
  Choose knowingly whether `__eq` compares the key (D63) or the game id: a stale handle equal to a new one that reused
  its id is what the game seems to do.
- Note the C side still calls the handle key "id" (`native/sandbox.c`, `Native.cs`): rename it when adding the game id.
- From W1-9's events probe: a destroyed Filter given to `ForceEnumPlayers` after a Condition had taken its place ran
  the new condition for each player (`events.txt` "destroyed c,c>0 1"). With D115's places, a destroyed condition
  given to a native runs its place's current function; then the four `ForceEnum*` become Exact. D146 and
  `docs/facts.md` ("the destroyed handle's id was reused (inferred)") should say "the condition's place". Whichever of
  W1-6 and W1-9 merges second fixes the coverage counts.
- Every native's argument check (D49, D80): a native given nil where it takes a handle, a number for a string, and so
  on, as the game reports it.

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
- W1-3's probes found more `math.*` values the model gets wrong: see MirrorImaged's `docs/facts.md` ("Strings, numbers
  and hashtables"), D81 and the Close rows of `MathNativesTests` (`math.rad`/`math.deg`, `math.asin(2)`,
  `math.atan(0, 0)` = pi/2, `(-8)^(1/3)`, `0^-1`, sin/cos/tan of 0.5, 1, -1 and 1e6). When `math.asin(2)` changes, the
  `Asin`, `Acos` and `Atan2` natives need their own domain check (their Exact rows will catch it).
- W1-2's probes: a periodic trigger event's expiry ran before a timer started later that fell due at the same moment
  (MirrorImaged keeps queue order, D45).
- From W1-9: `TriggerRegisterGameStateEvent` stays Close for three frame effects D144 names (settle or model them),
  and the time-of-day read-back drift D149 leaves open (0.1 reads 3dccccc0 and 23.9 reads 41bf3332 at once while
  suspended; 7 reads 40e0000a after a frame), which keeps `SetFloatGameState`/`GetFloatGameState` Close. The lobby and
  events probes are built with `ProbeMap`'s `withComputer` (D140) in both runs. `EndGame`'s evidence is in side files
  (`events-after.pld`, `events-timer.pld` written, `events-end.pld` not): compare those too.
- From W1-6: probe lines that differ from MirrorImaged by decision, not bug: `handles`' "first" and "blizzard" lines
  and `handles-3`'s two "players" lines (D110, starting ids), the id-reuse lines in `handles-2` and `handles-3` (D111),
  `handles-3`'s "predicted" and `handles`' "after a pass" lines (D114). Mark them in the expected files.
  Also: `triggers.txt:16` and `triggers-2.txt:23` (raw ids, D110), `triggers-3.txt:24` (raw ids and `And(t, cn)`,
  D110 and D114), and `handles-3.txt:32`, the "ran" line after "predicted" (`And(Not(cn), t7)`, D114).
- Every probe's raw game lines live in `probes/expected/<probe>.txt`; the comparison reads them from there. Lines that
  vary from run to run (W1-2's wait lengths, D71) get a tolerance or are skipped, said in the expected file.
- W1-2's two measured cases on `TriggerRegisterTimerEvent` (probe `triggers`: the tie at 3.0, the periodic event ran
  before the timer; `triggers-2`: `periodic zero in 0.1 s` gave 10, MirrorImaged 9) settle its level and the two unit
  tests that stepped around them (`TriggerTests.cs` 1.1 s and 0.105 s). The trigger probes' 1000 s clock reads
  (game 0.6000, 0.5001, 1.2501) are evidence for D45's elapsed model.
- The runner also saves a copy of each account's `Campaigns\*\Campaigns.w3v` and `Campaigns\*\Classic.w3p` before a run,
  by the same never-overwrite rule as replays (D59): every game rewrites `Classic.w3p`, and `SaveGameCache` rewrites
  `Campaigns.w3v`.
- W1-3's six single-call `R2SW` probes (widths 2147483640 and 2147483644, precisions -100 and -2147483647, and -1.5
  at widths 254 and 1000, which close the game) live only as facts prose and test rows: commit them as one-call probes
  with expected files. Also rename `S2R_is_a_float_step_off_the_game_on_these`, which now holds two far-off rows.
- Optional: count the game closing (exit code 1, `BlizzardError`, no lines) as equal to MirrorImaged's
  `GameClosedException`, so W1-5's `files-text` case can be compared.
- This closes wave 0's "one probe matches the game".

## W1-9 Player and game events, filters, the game's end

Needs W1-2 and W1-4. Decisions D140-D149. What W1-4 left for after the triggers (D94, D97):

- The player events: `TriggerRegisterPlayerEvent` (victory, defeat, leave, the others it takes),
  `TriggerRegisterPlayerStateEvent`, `TriggerRegisterPlayerAllianceChange`, and the time-of-day game-state events
  (`TriggerRegisterGameStateEvent` on `GAME_STATE_TIME_OF_DAY`), with their event responses. `RemovePlayer`,
  `SetPlayerAlliance` and `SetPlayerState` fire them as the facts measure ("Version and launching": `RemovePlayer`
  fires the defeat or victory event inside the call), and then become Exact where measured.
- Filters for the `ForceEnum*` natives and `GetFilterPlayer`.
- `EndGame` (a run's end; the facts on `-loadfile` and the score screen).
- The never-ending wait after a force was looped with `ForForce` and destroyed, then a trigger with
  `TriggerSleepAction` ran (facts, "Errors and callbacks", 3 probes): model it, or make `ForForce` Close citing it.
- The lobby before `config`: the game type and `MAP_LOCK_ALLIANCE_CHANGES` are already set inside `config` (the
  players probe: `InitGenericPlayerSlots` saw melee); check the simulation's options before `config` runs.
- A probe with a computer slot and a named slot, to settle D91-D93's inferred defaults (a computer's alliances, placed
  state, handicaps, race when selectable, name from `war3map.w3i`, an empty slot's controller), and the time of day
  read back at once against later (the facts' 11-05 "7 reads 6.999999" against the probe's exact 7).
- From W1-2 (D76): the time-of-day events hook is `_stateEvents`, fired with `Fire` and id 3; MirrorImaged's facts
  on which set fired the event (at 10, the first 12) are inferred, since the probe logged only the value read inside
  the event — the probe here logs which set fired. `TriggerRegisterGameStateEvent` becomes Exact here.
- `TriggerRegisterGameEvent` and its events, and `BlzTriggerInterrupt`/`BlzTriggerIsRunning` if W1-2's fix round
  didn't add them.

## W1-10 The Lua number model fitted to the game

Needs W1-8 (part 1, the comparison). Decisions D150-D159. W1-8 split off this part (its report and
`probes/differences/`):

- Fit `native/mifloat.c` to new probes: division (`1/d` over many divisor mantissas, an `n/d` grid), `sqrt` over
  [1, 4), `sin`/`cos`/`tan` in radians, `exp`, `log`, `atan`, `pow`, subtractions that cancel bits, products near the
  range's ends; and the `math.*` mismatches `probes/differences/strings.txt` lists (rad/deg, `asin(2)`, `atan(0, 0)`,
  pow, trig at 0.5, 1, -1, pi, 100 and 1e6). Give `Asin`, `Acos` and `Atan2` their own domain check when
  `math.asin` changes.
- Find the game's string hash seed (`pairs` over string keys is the same every run, but the seed isn't 0).
- D45's step and same-moment timer order against the `timers` probe (neither made, started nor queued order), and
  the game reading a periodic timer's expiries at exact hundredths where MirrorImaged steps at 1/1024 s.
- W1-2's `TriggerRegisterTimerEvent` cases and the trigger probes, once W1-6's `GetHandleId` lets them run; refresh
  their `probes/differences` files.
- Keep the native library reproducible and the cross-platform pins honest (re-pin on all three platforms through CI).
- Also from W1-8's review: `lua-config.txt`'s `math.log(3, 2)`, `math.log(100, 10)`, `math.log(1000, 10)`;
  `game-time.txt`'s `TimerGetElapsed` (1.01 to 7.01 in the game, 1.00 here); the seed is inferred, not certain (another
  hash function or table growth would also change `pairs` order).
- The BJ stand-ins in `ProbeComparison` (no-op `SetPlayerSlotAvailable`, `InitGenericPlayerSlots`, `InitBlizzard`):
  once `GetHandleId` lands, players and triggers 1-3 hit them (`PolledWait` and `GetPlayersAll` nil, no
  `TeamInitPlayerSlots`/`SetTeams(24)`, no `ConfigureNeutralVictim`/`InitSummonableCaps`, about 40 handles missing so
  ids shift). Those lines involve Exact natives, so they can't just be recorded as differences: run the comparison
  with the installed `blizzard.j` under `Needs=Game` once `InitBlizzard` gets past `IsFogEnabled`, or give the
  stand-ins what those lines need.
- The comparison needs a per-probe build option (`withComputer` for lobby and events, D140) and to read side files
  (`EndGame`'s `events-after.pld`/`events-timer.pld`/`events-end.pld`). W1-9's `TriggerRegisterGameStateEvent` frame
  effects (D144) and the time-of-day read-back drift (D149) are in W1-8's list above; settle them here if W1-8 didn't.
- W1-6's differing-by-decision lines (listed under W1-8) go in `probes/differences/<probe>.txt` (D121), not the
  expected files, which stay the game's raw lines.
- From W1-6's review: probe a condition place taken twice (c1 destroyed, c2 takes its place, c2 destroyed, c3 takes
  it): in the model c1, c2 and c3 all run c3's function, and destroying the stale c1 frees c3's place (D115).
- From W1-8's re-review: `GetPlayersAll` being nil (the stand-ins) ends the lobby and players probes early, so their
  later lines, on Exact natives (`ForceEnum*`, `GetGameTypeSelected`, `IsMapFlagSet`, `SuspendTimeOfDay`,
  `GetGameSpeed`, `GetGameDifficulty`, `GetIntegerGameState`, `GetTimeOfDayScale`, the cheat reads,
  `IsPlayerRacePrefSet`, `VersionGet`), are never compared; settle the stand-ins so they are. And fix
  `docs/facts.md`'s wording that no whole binary chunk "can be made" without `string.dump` (none was made; D126).
- W1-8's D121 already settled `EndGame`'s side files as a game test of their own (`GameProbeTests`): this bullet is
  covered by it (coordinator, 11-08).
- Differences with no task yet, to accept against A6 or plan, in `docs/tasks/later.md` (wave 10): R2S/R2SW from 2^31
  (D84), S2R past the float range (D83), StringHash/SubString on non-UTF-8 bytes (D82, D38), the random generator
  (D46, D86) incl. `GetRandomReal(3e38, -3e38)`'s tiny negatives.

## W1-7 JASS maps

Needs W1-1 to W1-6 and W1-9.

- A JASS map's `war3map.j` turned into Lua by War3Net's transpiler and run like a Lua map.
- JASS behaviour the transpiled code must keep, each Close unless a probe measures it: integer overflow wrapping,
  integer division, the operation limit ending a thread, an uninitialised variable stopping the thread.
- Tests: a small JASS map with triggers, timers and hashtables gives the same log as the same map written in Lua.
- D80 (string parameters refuse nil) is Lua's binding: JASS passes `null` strings to natives, and transpiled JASS
  (maps and `blizzard.j`) goes through the same binding, so decide and probe what a `null` string does in JASS.
