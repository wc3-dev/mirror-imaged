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
