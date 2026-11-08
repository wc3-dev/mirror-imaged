# Later waves: items left by earlier tasks

Each item names the wave whose file it goes into when that file is written.

- Wave 6 (players and input) and wave 7 (AI): the test API can't choose a selectable slot's race (only through the
  seed) or a computer's difficulty; A4 needs computer players on Normal (W1-4's plan review).
- Wave 8 (test tools): the plan says a Recorded native's call is logged, but D98 keeps lobby-only Recorded calls out
  of the map's log (which is what the map shows). Give Recorded calls their own record a test can read, and make
  `Level.Recorded`'s doc and D98 agree (W1-4's re-review).
- Wave 7 (AI): `IScriptThreads.Yield` and `Start(string)` go to the simulation's one Lua state, and the wait rule is
  private to `RunThread`. `common.ai`'s `Sleep` and `StartThread`, and AI scripts with an instance per player, need
  the yield routed to the running thread's state and a wait length per native (W1-2's review).
- Wave 8 (ChangeLevel): whether game caches never saved survive a level change; measure with a probe that doesn't
  call `SaveGameCache` (W1-5).
- Wave 8 (replays, A13): in a replay `ReloadGameCachesFromDisk` gives false, Preload files are written again, and
  `Preloader` reads the files on disk as they are while watching (data-probe-2; D104 says not simulated) (W1-5).
- Wave 8 (test tools): the CLI and the Lua test API can't keep `Documents` from one process to the next, and the
  saved caches are internal, so they can't be saved or seeded from outside (W1-5).
- Wave 6 (players and input): `TriggerRegisterPlayerEvent`'s input events register but never fire (D143):
  `EVENT_PLAYER_LEAVE` when a player leaves (never for `RemovePlayer`, facts), chat, end cinematic, the arrow keys,
  the mouse and the keys (W1-9).
- Wave 6: `GetWinningPlayer` is Missing (D145): no run fires `EVENT_GAME_VICTORY`; probe it outside an event and make
  it at least Close (A1) (W1-9).
- Wave 6: the lobby's placement of players on start locations isn't simulated (D98, D141: teams-together placement
  moved slots 0 and 1 at random), so `GetPlayerStartLocation` is Close; place them from the run's seed and let a test
  choose (W1-9).
- Wave 6 (players, first ids): the game makes one handle before `blizzard.j`'s globals (0x100001 on) and each player
  when first asked for (`Player(0)` in `config` at 0x100008, `Player(25)` next, 1-24, then 26 and 27 later; probes
  `handles`, `handles-3`). Make players when first asked for and find that first handle, so the raw-id lines in
  `handles`, `handles-3` and `triggers` 1-3 compare (D110, D47).
- Wave 10 (id reuse): an id comes back once its handle is destroyed and every userdata of it is collected, 0.1 to
  0.2 s later, last freed first, plus one unexplained extra id (`handles-2` lines 8 and 13, `handles-3` lines 10 and
  18). Model it from MirrorImaged's own deterministic collector (D39's arena keeps A7), or take A2's exception for
  `GetHandleId` to Sven. Until then, maps that forget to flush data keyed by `GetHandleId` behave differently (D111).
  With it, let go of a destroyed `Condition` or `Filter` once Lua has collected its userdata: it now stays in the game
  for good (D115; the `shortcut:` in `Triggers.cs`'s `Destroy`).
- Wave 10 (the nil condition): probe further for `Condition(nil)`'s value inside `And`/`Or`/`Not` (the 6 mismatched
  cases in D114); `Condition`, `Filter`, `And`, `Or` and `Not` stay Close until it fits every measured case.
- Wave 10 (accuracy): accept against A6, or fit, these standing differences: R2S/R2SW from 2^31 (D84), S2R past the
  float range (D83), StringHash/SubString on bytes that aren't UTF-8 (D82, D38), the game's random generator (D46,
  D86). And binary `load`: MirrorImaged refuses binary chunks on purpose (D36, D126: bytecode isn't verified, so a
  map could corrupt the test process), which keeps `lua-config` different for good; that A6 exception is the
  coordinator's call, listed for Sven's review at the end.
- Wave 5 (presentation): sound file lengths (`GetSoundDuration`, from the install's sound files), and a map's own
  FDF/TOC string lists (D211, `BlzLoadTOCFile`) for `GetLocalizedString` (W2-1).
- Wave 7 (AI): the balance sets hold their own AI scripts (`_Balance/Custom_V0.w3mod`, `Melee_V0.w3mod`:
  `Scripts/human.ai` etc. and `*Melee.pld`); take them from the set `StandardData.Sets` picks (W2-1).
- Wave 3 (orders and movement): the game moves a unit made where others stand off them (D223); `SetUnitPosition`;
  what `PauseUnit` and `BlzPauseUnitEx` hold back; `SetUnitFacing`'s turning eased into its end and on the game's
  frames, and the facing of angles past a turn (D224); orders beyond `SetUnitOwner`'s stop (W2-2).
- Wave 5 (presentation): the unit animation natives and `SetUnitTimeScale` (W2-2).
- Wave 10 (speed): a cross-process cache of the standard data (about 1.2-1.7 s a process) if the benchmark or the
  corpus runs need it; it must live outside the repo and packages (A15) (W2-1).
- Wave 10 (the number model, W1-10's rest): find the game's `exp`, `log` and `atan` (tables on straight lines, by the
  fine probes; `numbers-fine-2` has runs to start from) and with them `asin`, `acos` and powers that aren't whole; the
  reciprocal between its knots, a step off for about 1 in 5 divisors (D151; `numbers-div` and `numbers-fine` hold 2,376
  measured); and the game's integer root in `math.sqrt`, a step higher for six of `numbers-squares`' roots (D152), which
  keeps `(int)Sqrt(n * n)` going wrong 70 times here against the game's 64.
- Wave 10 (timers, W1-10's rest): the game's clock starts a hair past 0 and timers' ends drop it, so reads come a float
  step past whole times, and a timer paused and resumed at 0.25 for 0.75 ran before those due with it (D153); model the
  clock as the game's floats, or accept the reads against A2. Also W1-9's time of day: the frame effects of a set
  (D144) and the read-back drift with the day stopped (D149), not started here.
- Wave 10 (JASS): functions with more than 200 locals don't load (D139; Lua's limit). No shipped map has one (most
  is 24); two maps in Sven's Documents do (Gaia's ORPG, Survival Chaos Reborn) (W1-7).
- Wave 10 (JASS): a rawcode with a backslash (`'a\bc'`) loads here but the game refuses the map (D137) (W1-7).
