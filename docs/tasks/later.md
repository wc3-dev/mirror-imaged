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
