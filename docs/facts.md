# Facts

Game behaviour measured for MirrorImaged, in the style of the workspace's `docs/game-facts.md`. **Seen** = a run showed
it; **inferred** = reasoned from what was seen.

## Lua numbers

The values below the workspace's "Lua runtime" facts summarise, read again from the two probes' own output: the numbers
probe after Moonwell's 0.3 gate (10-27, `CustomMapData\moonwell-0.3-numbers.txt`, values with their float bits) and the
maths probe (11-01, `moonwell-maths-numbers.txt`, printed with `%.9g`). 3.0.1.24371, single player. The tests in
`tests/MirrorImaged.Tests/Lua` use them.

- Adding a number of k float steps to 16384, 16384 plus one step, -16384, 1, 1 plus one step and 1000.1, and
  subtracting it from 16386, -16384, 1.5 and 1000.1, moves the result by 0, 2, 3, 4, 5, 6, 7 and 8 steps for k = 1 to 8.
  `1 - 2^-25`, `1 - 3 * 2^-25`, `1 + 2^-24` and `1 + 3 * 2^-24` are 1; `-(2^24) - 3` is -2^24.
- 7200.017 (`45e10023`) plus 0.0002, 0.000244, 0.00025, 0.0003 or 0.000488; 20000.75 (`469c4180`) plus 0.0009, 0.00097,
  0.00098, 0.001, 0.0015, 0.0019 or 0.00195; and 33000.01 (`4700e803`) plus 0.001, 0.0019, 0.002 or 0.0039 stay where
  they were, as with the larger additions in the facts.
- `1 - 0.9` is 0.100000024 and `(1000 + 0.1) - 1000` 0.0999755859. `1 - cos(0)` is 5.96046448e-08 (2^-24): Moonwell's
  `Ease.InSine(0)`.
- Products: `0.1 * 0.1` is `3c23d70a`, `20000.75 * 1000` `4b9897f7`, `33000.01 * 1000` `4bfbc525`, `100 * 0.07` 7,
  `100 * 0.29` 28.9999981, `100 * 0.57` 56.9999962, `1000 * 0.7` 699.999939, `3 * 0.1 * 10` cast 2. The square of
  each root below: 5.00006676 gives `41c8015e`, 10.0001335 `42c8015e`, 100.001854 `461c417b`, 200.003708 `471c417b`,
  1.41421354 `3fffffff`, 1.73205435 `40400033`, 1000.01501 `497425e0`, 0.010000092 `38d1b814`; `120^2 + 160^2` is
  40000.
- Quotients: `2/3` is `3f2aaaab`, `7/7` 1, `10000/100` 100, `(1/3) * 3` 1, `-1/0` -1.70141183e+38, `1e38 * 10`
  -5.5173927e-39 exactly, `1e-38 / 1000` and `1e-30 * 1e-30` 0, `-1e30 * 1e30` +0, `(1e20 * 1e20) / 1e20` 0,
  `float.MaxValue + 1` is MaxValue. `7.0 // 2` is the float 3. `(3, 4)` normalised has x 0.600000024.
- Square roots with their bits: 25 `40a0008c`, 100 `4120008c`, 10000 `42c800f3`, 40000 `434800f3`, 2 `3fb504f3`, 3
  `3fddb3f5`, 1000000 `447a00f6`, 0.0001 `3c23d76d`; and 9 is 3.0000577, 36 6.00011539, 49 7.00011635, 81 9.00006676,
  144 12.0002308, 625 25.0004635, 2500 50.000927, 0.00999999978 0.100000285; 1, 4, 16, 64, 1024, 0.25 and 2^24 are
  exact. `x ^ 0.5` gives 4 `3ffffffd`, 25 `409ffff6`, 100 `411ffff4`, 10000 `42c7fff1`, 40000 `4347fff0`, 2^24
  `457fffec`, 0.25 `3f000002`, 2 `3fb504f3`, 3 `3fddb3d1`, 1000000 `4479ff75`, 0.0001 `3c23d708`.
- One correction step `(s + x / s) / 2` after `math.sqrt` gives 2 1.41421366, 3 1.7320509, 625 25.0000019, 2500
  50.0000038, 10000 100.000008, 40000 200.000015, 1000000 1000.00006, 0.00999999978 0.100000001, 0.0001 0.00999999978,
  and the exact root for 1, 4, 9, 16, 25, 36, 49, 64, 81, 100, 144, 1024, 2^24 and 0.25.
- By octave, `math.sqrt`'s worst is 1.92854142 parts in 100,000 high and 0.0773249492 low in the even octaves (2^0, 2^2,
  ...) and 1.92324328 high and 0.087634936 low in the odd ones, the same in every octave from 2^-6 to 2^26. That sweep
  was coarser than every float: `numbers-fine` ("The number model's probes") measured 2.04 high just above 1.
- Sums of a float added to 0 again and again: 0.001 10,000 times is 9.99838257, 100,000 times 99.9489441, 1,000,000
  times 981.466797; 0.01: 99.971489, 996.854736, 9799.05859; 0.03: 299.988312, 2989.46997, 29441.9434; 0.1: 999.804871,
  9973.49512, 97000.0703; 0.37: 3699.40674, 36906.2461, 357990.594; 1.7: 16995.1406, 169506.906, 1664565. 0.03 added
  120,000 times is 3585.1731. Adding stops after 8,383,586 additions of 0.001 (at 8192), 7,546,324 of 0.01, 5,557,738 of
  0.1 and 8,388,608 of 1.
- Loops `t < limit` and `t <= limit` from 0: by 0.01 to 1 run 101 and 101, by 0.1 to 2 21 and 21, by 0.3 to 3 11 and
  11, by 0.1 to 100 1001 and 1001, by 0.25 to 1 4 and 5, by 0.03125 to 5 160 and 161.
- `(int)(n * (k / 100))` for n 1-1000 and k 1-99 is wrong 140 times, all among the 4,200 whole results, and never with
  0.001 added first. `(int)(n / d)` for n 1-2000 and d 2-100 is wrong 268 times of 198,000, and `Ceiling` 893.
- C#'s `5.5 % 2` is 1.5 and `-5.5 % 2` -1.5: CSharp.lua's `System.modf`, Lua's `%` with the sign put right, as is the
  facts' `7.2 % 0.1` = 0. `math.floor(3e9)` is the float `4f32d05e`.
- `Log(10)` is 2.30258489, `Asin(0.5)` 0.523598731, `Acos(0.5)` 1.04719746, `Atan(1)` 0.785398245, `Pow(2, 10)`
  1024, `Pow(10, 2)` 100, `Pow(10, 3)` 1000, `Pow(1.5, 2)` 2.25, `Pow(2, 0.5)` 1.41421354, `Sin(PI)` and `Cos(PI / 2)`
  4.65661287e-10 (2^-31).
- `float.Epsilon` reads 1.40129846e-45: a subnormal literal is read and printed as it is.
- `math.pi` is `40490fdb` and `math.huge` `7f800000` (printed inf); `0.1` is `3dcccccd`, `0.2` `3e4ccccd`, and
  `tostring(0.1)` is 0.1.

## The game runner

All on 3.0.1.24371, one PC (Windows 11), single player, with the Battle.net app running and nobody at the keyboard
(Sven was watching from 02:20 to 02:35), maps started with `-launch -windowmode windowed -loadfile "<map>"`, 11-08
(W0-4) unless marked.

- Login: a `-loadfile` launch logs in to Battle.net by itself, with nothing typed (Sven, 11-08, had seen a login prompt
  that morning). Seen in the pictures of runs in a normal window (`PrintWindow` every 2 s, 18 runs from 03:58 to 04:54,
  3 looked at): a door, then a "Login Queue" box ("The Blizzard servers are busy right now! You're position 1 in the
  queue (estimated time: 5s).", with a Cancel button) for 2 to 5 s, then the map's loading screen, "Loading...", then
  "Press any key to continue"; each of the 18 runs saved a picture of the queue box's size. In every
  run whose `War3Log.txt` was written out (about 15), `[CLoginCallbacks] LoginDoorClose called` came 5 to 6 s after the
  start, before `Activating WebUI`. Once logged in, 10 to 14 s after the start, the game rewrites `War3Preferences.txt`
  and the account's `BattleNet\<account>\Campaigns\Classic\Classic.w3p` and `Reforged\Reforged.w3p`, before the map
  loads. The Battle.net app was running in every run; not tried with it closed.
- What the game shows can be captured only from a window on the visible desktop: `PrintWindow` with
  `PW_RENDERFULLCONTENT` of the game's window on a hidden desktop gives its frame and title bar and a black inside (3
  runs), while on the visible desktop it gives the game's picture, also with the display switched off by power saving
  (18 runs).
- A map without a minimap image (`war3mapMap.blp` or `war3mapMap.tga`) closes the game as the map starts. Seen:
  War3Net's `MapFactory` map (info format 28, terrain, `war3map.lua`) exited with code 0 about 8 s after the game made
  `TempReplay.w3g`, 4 runs (two on a hidden desktop, two minimised); World Editor's own new map (`e10-probe.w3m`) packed
  again by War3Net without its `war3mapMap.blp` exited with 0 (1 run), and with a few script lines added crashed
  (0xC0000005, 3 runs); the same repack with its `war3mapMap.blp` kept running (1 run, 40 s). With a 256x256 black
  32-bit `war3mapMap.tga` added, the `MapFactory` map loads and runs its Lua (every run since). No `war3map.wpm`,
  `war3map.shd` or `war3map.doo` is needed. The exits with 0 left no crash report.
- A `-loadfile` map whose full path is longer than 173 characters never loads: the game logs `Opening map - <path>` (3
  times, as for a map that loads) and then stays there, its window up and `main` never run, until stopped. Seen: paths
  of 172 and 173 characters loaded, 174, 175, 180 and 187 did not (1 run each; 187 in 7 runs). All ASCII; the path's
  folders and the file name were varied, and only the length mattered (a 42-character name loaded from a short
  folder).
- `War3Log.txt` is held back while the game runs: after the start-up scan of `Documents\Warcraft III\Maps` nothing more
  reached the file while a game ran (read every 250 ms, for up to 150 s, in about 20 runs). The rest is written when the
  game ends normally or crashes (the crash report holds a copy), and is lost when the process is killed. So
  `Opening map - <path>` can't confirm a load while the game runs, nor after a kill.
- Once a map has loaded, its `main` has run but the game waits on its loading screen (Sven; the picture says "Press
  any key to continue"): a zero-second timer started in `main` ran only after input. A space key posted to the game's
  window (`PostMessage`, `WM_KEYDOWN` then `WM_KEYUP`, `VK_SPACE`) gets past it without the window being in front,
  minimised or on a hidden desktop: in every run that logged it (over 70), the game started within 0.7 s of a posted
  key, the first one 3 s after `main` in most. A left click posted the same way at (100, 100), 3 s after `main`, did
  not (3 runs). `SendInput` was not needed and not tried. One run with no input posted (minimised, while Sven was
  watching) gave its lines 23 s after launch; whether he clicked is not known, so it is not known whether the screen
  ever goes by itself.
- Game time and the window (probe `game-time`: a 1-second periodic timer for 20 ticks, each with a timer's elapsed
  time and `os.time()`): on a hidden desktop (over 40 runs) and in a normal window kept in front (1 run), 20 game
  seconds took 20 real seconds and the timer read 1.01 to 20.00. Minimised (started with `SW_SHOWMINNOACTIVE`, never in
  front; 1 run), the game got past the loading screen and ran its zero-second timer, but the 1-second timer never fired
  in 100 s: game time stops. Behind other windows it runs slower (8.73 s counted as 3.19 s; workspace fact of 10-02);
  not measured here.
- On a Windows desktop made with `CreateDesktop` and named in `STARTUPINFO.lpDesktop`, the game makes its window
  (`OsWindow "Warcraft III"`) and the WebUI's `BlizzardBrowser` windows there, nothing shows on the visible desktop, and
  game time runs at full speed. From launch to the probe's lines: `hello` 16 to 19 s, `game-time` 36 to 37 s.
- The game started suspended in a Windows job object and then resumed runs as it does outside one, and the 4
  `BlizzardBrowser` processes it starts for the WebUI stay in the job (no breakaway): a WMI listing of the game's
  descendants during a run matched the job's process list (1 run). Ending the job ends them all.
- Closing the game's window (`WM_CLOSE` posted to it) opens the game's quit dialog, which waits for a person (Sven had
  to press X). Seen in 3 diagnostic runs: the X key posted after it quits the game normally, writing
  `War3Log.txt` out.
- A game that exits by itself leaves its `BlizzardBrowser` processes running, and a crash leaves `BlizzardError`'s
  dialog open ("The application encountered an unexpected error. ...", with Send to Blizzard, Repair, Reopen and Close)
  and writes a report in `Documents\Warcraft III\Errors` (2 runs).
- Files a run rewrites in Sven's documents (hashed before and after 1 run of `hello`): `War3Preferences.txt` and the
  account's `Classic.w3p` and `Reforged.w3p` are rewritten with the same bytes; `Replays\LastReplay.w3g` is replaced by
  the probe's game (it ends with `EndGame(false)`). Other accounts' folders were not touched.
- Two runs started at the same moment both launched a game (the check for a running game raced): one gave its lines,
  the other's game started but gave none in 100 s (1 pair, before runs took turns).
- A game started from Claude Code's Bash tool never logs in, and one started from its PowerShell tool always does.
  Seen: the reviewers' runs (2 at 02:53 to 02:57, 3 at 03:45 to 03:51, 3 at 05:03 to 05:09, all from Git Bash) and
  this task's runs from the Bash tool (the 3 game tests at 05:12 to 05:18, 5 runs of `hello`, 1 game in a normal window,
  and 4 runs of the 3 game tests at 05:31 to 06:03 with the game given the user's own environment) all ended with the
  map never loaded and `War3Preferences.txt` and the account's files never rewritten, the job holding up to 11
  `BlizzardBrowser` processes, against 4 or 5 in a run that logs in. The one game
  from the Bash tool that was pictured, in a normal window, stayed black for its 60 s, where a game started from
  PowerShell shows the door and the login queue; the reviewer saw its stuck game holding a TCP connection to a
  Battle.net server (port 1119). Every run from the PowerShell tool logged in, about 80 since 03:56, also with Git
  Bash's `PATH` (1 run), with Git Bash's other variables (`HOME`, `MSYSTEM`, `TERM`, `SHELL` and the rest; 1 run) and
  inside a job of its own with no limits (1 run). Starting the game with a clean environment built from the user's
  profile (`CreateEnvironmentBlock`, not inherited) did not help from the Bash tool (2 runs). A process the Bash tool
  starts is in a Windows job (no UI or limit flags set) and one the PowerShell tool starts is not; both have the same
  token (medium integrity) and the same process mitigations. The connections differ (1 run each, sampled every second):
  from both, the game reaches an HTTPS server (136.110.153.89:443) and then Battle.net (port 1119), and neither talks
  to the Battle.net app's or Agent's local ports; from PowerShell the game's browser then talks only to the game and the
  game goes on to another HTTPS server (34.90.35.110:443) and opens the account, while from the Bash tool the browser
  starts a second set of its processes and loads pages from outside HTTPS servers (IPv6 and IPv4, port 443) and the
  game never opens the account. Inferred: under the Bash tool the game doesn't use its stored login and falls back to
  Battle.net's web login, which waits for a person; which part of the Bash tool's sandbox causes it is not known (the
  sandbox could not be turned off to check). The agent's launch for which Sven saw a login prompt (01:16) fits this.
  Not separated: a game Git Bash started through PowerShell's `Start-Process` (1 run) did log in and load the map. The
  browser count alone says nothing: runs that logged in had up to 10 for a few seconds. The Battle.net app's log
  (`%LOCALAPPDATA%\Battle.net\Logs`, in UTC) shows only that a game ran.
- The hub's read works: `Preloader` of a file written by the runner in the game's Preload format, holding
  `call BlzSetAbilityTooltip('AHbz', "<path>", 0)`, gave the 150-character path back from
  `BlzGetAbilityTooltip(FourCC("AHbz"), 0)` on the first poll, in the same call (1 run).
- `ChangeLevel` from a `-loadfile` game does not load a custom map: given the map's full path
  (`C:\Users\...\maps\1-hello.w3x`, 150 characters, within the limit above), nothing happened in 76 s and the hub kept
  running; given a path under `Documents\Warcraft III` (`Maps\MirrorImaged\target.w3x`), the log shows the game opening
  that map, then it crashed (ACCESS_VIOLATION reading 0x298, the log listing every player as `<Unused>` and
  `Played (null)`), 2 runs. So a hub can't hand a probe to the game this way; with no login needed, the runner launches
  the game once per probe.
- Not tried: the Wine and Xvfb route under WSL2 (the plan's second way to hide the game), as WSL has no distribution on
  this PC.

## Triggers and threads

Four probes, `probes/triggers.lua` to `triggers-4.lua`, run by the runner above on 3.0.1.24371, single player, 11-08;
`triggers` and `triggers-2` twice, with the same lines both times but `triggers-2`'s wait lengths. Their lines are in
`probes/expected`, the lines that vary from run to run marked there. Each part of a probe runs in its own timer
callback; handle ids are read with `GetHandleId`, times with a 1000 s timer's elapsed time, to 4 places.

- Threads: in a timer's callback, a trigger's condition (from an event or `TriggerEvaluate`), its actions and
  `ExecuteFunc`'s function, `coroutine.isyieldable()` is false and `coroutine.running()` is not the main thread.
- A wait (`TriggerSleepAction`) in a timer's callback never ends: nothing after it runs, and a `pcall` around it never
  returns. In a condition the same, and the condition counts as false: `TriggerEvaluate` returns false at once. In an
  action, inside a `coroutine.wrap` the script made, it never ends either, and the action never goes on.
- A wait in a trigger's action ends: the caller of `TriggerExecute` goes on at once, the trigger's next action runs only
  after the first has ended, waits and all, and `GetTriggeringTrigger` is the same after it. A `pcall` around a wait
  returns after it, with an error raised after the wait.
- Wait lengths, as the call's time, the timeout and the time it ended: in `triggers` (both runs) 0.6 + 1 at 1.7, 1.7 + 0
  at 1.8, 1.8 + 0.05 at 2.0, 2.0 + 0.33 at 2.4, 2.4 + (-1) at 2.5, 2.5 + 0.5 at 3.1, and 0.8 + 0.5 and 0.9 + 0.25 at
  1.3. In `triggers-2`, a thread from 2.0 sleeping 0.1, 0.1, 0.1, 0.15, 0.2, 0.25, 0.5, 0.55, 0.01, 0.09 and 0.11 ended
  at 2.2, 2.4, 2.5, 2.8, 3.1, 3.4, 4.0, 4.6, 4.7, 4.9 and 5.125, and in the second run at 2.2, 2.4, 2.6, 2.8, 3.0, 3.4,
  4.0, 4.625, 4.725, 4.925 and 5.125; from 2.033, 0.1, 0.2, 0 and 0.5 ended at 2.2, 2.5, 2.6 and 3.2 (second run 2.2,
  2.4, 2.5, 3.0); from 2.25, 0, 0.3 and 1 at 2.4, 2.8 and 3.9 (3.8). So a wait ends mostly on a tenth of a second of
  game time, at or one tenth after its timeout, and one of 0 or less took 0.1 s 5 times of 6, but not the same way every
  run: inferred to follow the real clock or the game's turns.
- `PolledWait(1)` took 1.2 s in `triggers` and 1.1 s in both runs of `triggers-2`; `PolledWait(0.5)` 0.6 s and
  `PolledWait(3)` 3.025 and 3.125 s.
- `TriggerExecuteWait`, from a trigger's action, doesn't wait for the trigger's waits, also after
  `TriggerWaitOnSleeps(t, true)`, which `IsTriggerWaitOnSleeps` reads back.
- A new trigger is enabled, `IsTriggerWaitOnSleeps` is false and both counts are 0. `TriggerEvaluate` counts nothing,
  with no, passing or failing conditions. `TriggerExecute` counts an evaluation and a run, before the actions run
  (`GetTriggerExecCount` reads 1 in the first). An event counts an evaluation, also while the trigger is disabled, then
  a run when its conditions pass. `ResetTrigger` sets both to 0.
- A disabled trigger's events run no condition and no action; `TriggerExecute` and `TriggerEvaluate` run it as usual.
- `TriggerEvaluate` with no condition is true. Every condition runs, in order, also after one has failed (w3ts's claim
  in the workspace's facts); one that raises fails, and the next runs. A condition passes on `true` and on a number
  other than 0 (1, 0.5, -1), and fails on nothing, nil, false, 0, `""`, `"x"`, `"1"` and a table; only the first result
  counts (`return false, true` fails).
- Actions run in the order added; an error ends only its own action. A run takes the actions the trigger has as it
  starts: one removed or cleared during it, by itself or an earlier action, still runs in it, and one added during it,
  by an action or while the run waits, doesn't run in it (the next run wasn't looked at). `TriggerRemoveCondition` and
  `TriggerClearConditions` take a condition out. An action that removes itself runs that time; so does a condition
  (the workspace's facts, "Errors and callbacks", 09-29, for both; the action also in `triggers`).
- `Condition(f)` gives a new handle every call, as the workspace's facts say of `Condition` and `Filter` (10-12; only
  `Condition` here). `Condition(nil)` and `Filter(nil)` give one handle, the same for both and every call, typed as
  each declares (`conditionfunc: `, `filterfunc: `, `==` between them true).
- `And` runs its second operand only when the first passes, `Or` only when the first fails. A nil operand is false:
  `And(nil, x)` doesn't run x, `Or(nil, x)` does. `Not(nil)`, `And(nil, nil)` and `TriggerAddCondition(t, nil)` fail.
  `Condition(nil)` alone passes, `Not(Condition(nil))` passes and `Not(Not(Condition(nil)))` fails; `And(Condition(nil),
  x)` fails whatever x, `Or(Condition(nil), false)` fails, and `And(true, Condition(nil))` passes.
- A destroyed `Condition` or `Filter` no longer runs and fails where it was added; as `Not`'s operand, `Not` passes. A
  destroyed `And` still runs both operands and passes. In `triggers`, a condition destroyed while a trigger had it, then
  a new `Condition` added to the trigger and taken out again with `TriggerRemoveCondition`: the trigger still ran the
  new one, through the old one's place, as if the destroyed handle had been reused (inferred).
- `GetTriggeringTrigger` is the trigger in its conditions and actions, nil in a timer's callback, and the outer trigger
  again after a `TriggerExecute` inside an action. `GetTriggerEventId` outside an event is a handle with id 0, typed
  `eventid`, equal (`==`) to `EVENT_GAME_VICTORY`; in a timer event or a timer's expiry event it has id 4 and equals
  `EVENT_GAME_TIMER_EXPIRED`. `TriggerExecute` keeps its caller's event and names the trigger it runs; `ExecuteFunc`
  keeps both, the trigger and the event, and after a wait in it the trigger (the event wasn't read there).
- `==` between two handles holds when they have the same id, whatever their types: a unit and `GetTriggerWidget()` in
  its death event (`widget: ` and `unit: `, not `rawequal`), and `GetTriggerEventId()` with id 4 and
  `EVENT_GAME_TIMER_EXPIRED`, `ConvertGameEvent(4)`, `ConvertLimitOp(4)` and `ConvertPlayerColor(4)`.
- `TriggerRegisterTimerEvent` fires after its timeout, and at least 0.01 s: 0, 0.001, 0.005 and 0.01 each fired 0.01 s
  after; periodic with 0 it fired 10 times in 0.1 s. In it `GetExpiredTimer` is nil. Events due together fire in the
  order registered, and a trigger registered twice fires twice. A periodic event registered at 1.0 for 0.5 s and a timer
  started at 2.0 for 1 s, both due at 3.0: the event ran first.
- `TriggerRegisterTimerExpireEvent` fires before the timer's callback (as the workspace's facts say), in the order
  registered, twice for a trigger registered twice, with `GetExpiredTimer` the timer.
- `TriggerRegisterVariableEvent(t, "MirrorProbeVar", GREATER_THAN, 5)` never fired when the script set the global from 0
  to 10.
- `GetIntegerGameState` of `GAME_STATE_DIVINE_INTERVENTION` and `GAME_STATE_DISCONNECTED` is 0 at the start and reads
  back what `SetIntegerGameState` set. A set that changes the value to one that passes the comparison fires the state's
  event inside the call; setting the same value again fires nothing; `NOT_EQUAL` fires on every change, whatever the
  value. In it `GetTriggerEventId` has id -255 for `LESS_THAN` up to -250 for `NOT_EQUAL` (the comparison less 255), so
  it isn't `EVENT_GAME_STATE_LIMIT`, and `GetEventGameState` is the state, typed `gamestate`.
- The time of day (suspended, limit 12), set to 20, then 10, 11, 12, 12, 13, 12, 11, 10 and 14: the events read the
  time inside them as `LESS_THAN` and `LESS_THAN_OR_EQUAL` 10.0 and 12.0, `EQUAL` 12.0 twice, `GREATER_THAN_OR_EQUAL`
  and `GREATER_THAN` 12.0, 13.0 and 14.0, and `NOT_EQUAL` fired never; which sets fired them is inferred from those
  reads (at 10 and the first 12, and so on), as the probe logged no set; W1-9's events probe logged them and bore the
  inference out ("Player and game events"). The event's id is 3, `EVENT_GAME_STATE_LIMIT`.
- `BlzTriggerIsRunning` is false for a new trigger, true in its action and while a run of it waits, also for two runs at
  once, and false once they have ended (read 1 s later), and in its condition under `TriggerEvaluate`.
  `BlzTriggerInterrupt` on a trigger whose run waits: that run never goes on, nor does its next action, and
  `BlzTriggerIsRunning` still reads true right after (false 1 s later). Called in the trigger's own action: the rest of
  that action runs, and still reads it running, but the next action doesn't run. On a trigger with no run it does
  nothing seen.

## Strings, numbers and hashtables

The probes `strings`, `hashtables`, `strings-2` to `strings-6` in `probes/`, their game lines in
`probes/expected/<probe>.txt`, and six probes of one `R2SW` call each, given below, all run by the game runner from the
PowerShell tool, 3.0.1.24371, single player, 11-08 (W1-3). A float is given with its bits. Every measured line is a case
in `tests/MirrorImaged.Tests/StringNativesTests.cs`, `MathNativesTests.cs`, `HashtableNativesTests.cs` and
`RandomNativesTests.cs`, but for `SaveLocationHandle` and `SaveTextTagHandle`, whose handles no test can make while
`Location` and `CreateTextTag` are Missing, and the `math.*` values left to W1-8.

- The string parameters of `StringHash`, `StringLength`, `StringCase`, `S2I`, `S2R`, `SubString` and `SaveStr` are read
  as Lua's `luaL_checklstring` reads them (that every native's are is inferred): `StringHash(nil)`, `StringLength(nil)`,
  `StringCase(nil, true)`, `S2I(nil)`, `S2R(nil)`, `SubString(nil, 0, 1)` and `SaveStr(ht, 7, 8, nil)` raise `bad
  argument #n to 'X' (string expected, got nil)`, `StringHash(true)` the same with `got boolean`, and a number is taken
  as `tostring` writes it: `StringLength(12345)` is 5, `StringLength(1.5)` 3, `S2I(42)` 42, and `SubString(v, 0, 20)`
  gives `1e+20`, `3.0`, `-0.0`, `0.1`, `123456789`, `2.147484e+09` and `0.3333333` for `1e20`, `3.0`, `-0.0`, `0.1`, the
  integer 123456789, `2 ^ 31` and `1 / 3`. A native reads a string up to its first NUL: `StringLength('a\0b')` is 1,
  `StringHash('a\0b')` is `StringHash('a')`, `SubString('a\0bc', 0, 4)` is `a`.
- `StringLength` counts bytes (`ä` 2, `中` 3). `SubString(s, start, end)` takes bytes: `abcdef` gives `abc` for 0 3,
  nothing for 2 2, `ef` for 4 2, `cdef` for 2 100, nothing for 6 7, 10 12, -1 3 and -3 -1, and `abcdef` for 0 -1; `aäb`
  1 2 gives the lone byte c3 and 2 4 the bytes a4 62. `StringCase` changes ASCII letters only (`äÖé` and `ёЁ` stay).
- `StringHash` is Bob Jenkins's lookup2 (1996, seed 0) of the string's letters, each folded to upper case, `/` made `\`,
  and written as one byte below 256 and as two bytes, low first, from there; `""` is 0 (lookup2 gives -1119235827).
  Matched on 43 strings: ASCII of 0 to 25 bytes (`a` -1587459251, `hello world` -1563733934, `Units\Human\Footman`
  736755776), `ä` and `Ä` 159638553 (the byte c4), `中` -234949316 (2d 4e), an emoji -1471725602 (U+FFFD, fd ff).
  Measured folded: a, b, c, h and w (against A, B, C, H and W), ä, é, ö, þ, ÷ (to ×), œ, а, ё and я; measured left as
  they are: ÿ (not to Ÿ), ß, ł, ą, ž, š, ő, ı (not to I), α and β (`αβ` hashes b1 03 b2 03), ա, ａ, ǆ and µ (not to Μ);
  `가` hashes -611326302. That the ranges a-z, à-þ, а-я and ѐ-џ fold is inferred from these. Bytes that aren't UTF-8 are
  decoded without checks: a lone 80 is U+0080 (-366431040), c3 61 is U+00A1 (-578007203), a sequence cut short by the
  end is one 0 (c3, ff and ff ff all hash as the byte 0, 1843378377; `a`, ff, `b` -2135237383), ed a0 80 is U+D800, 00
  d8 (73716403).
- `I2S` writes the integer. `S2I` skips spaces and tabs, takes one sign and the digits after it, and holds at the
  integer's ends: `12abc` 12, ` 12` 12, `+7` 7, `--5` and `- 5` 0, `2147483648` 2147483647, `-2147483649` -2147483648,
  `1.9` 1, `0x10` 0, `1e3` 1.
- `S2R` takes no space before (` 2.5` is 0), one sign, digits and one point, and stops at anything else (`1e3` and
  `1e40` are 1, `1,5` 1, `1..5` 1, `1.5.5` 1.50000012, `+-1` 0, `inf` and `nan` 0, `-0` +0). The digits are summed
  (`123456789` 123456784 and `4294967296` 4294967040, cut toward zero) and divided by ten to the decimals in the game's
  arithmetic: `1.5` is 1.50000012 (3fc00001), `0.1` 0.100000009 (3dccccce), `0.7` 3f333334, `3.0x` 3.00000024, `100.5`
  100.500008, while `2.5`, `10.5`, `0.25` and `-2.25` are exact. 77 strings measured. Past the float range (strings-4):
  `1` and 38 zeros is 9.99999765e+37 (7e967697), `3` and 38 zeros 7f61b1e2, `4` and 38 zeros 7f967697 (a nan, as a
  product past the range packs), `1` and 39 zeros -5.5173899e-39 (803c143d), the 40 digits `1234567890...7890` 80683245,
  40 nines 0, the digits of the largest float and of 2^128 both 7f7ffffa; `0.` with 39 or 45 zeros and a 1 is 2^127
  (7f000000), as 1/0 is; `1.` and 40 zeros 1; and `0.1234567890...7890` (40 decimals) 0.123456791 (3dfcd6ea), as
  `0.123456789` is.
- `I2R` cuts toward zero: 16777219 gives 16777218, 2147483647 gives 2147483520. `R2I` cuts toward zero and holds at the
  integer's ends: 1.9 1, -1.9 -1, 0.99999994 0, 1e10 and `math.huge` 2147483647, -1e10 -2147483648. `MathRound` rounds
  half away from zero (0.5 1, 2.5 3, -0.5 -1, 0.49999997 0) through a 64-bit integer: 1e10 gives 1410065408.
- `R2S(r)` is `R2SW(r, 0, 3)`. `R2SW(r, width, precision)` writes a `-` for a number below 0 (not for -0), then spaces
  to make up `width` less the decimals written less the integer's digits (`R2SW(1.5, 8, 2)` is `     1.50`, `R2SW(-1.5,
  4, 2)` `- 1.50`, `R2SW(1.5, 3, 2)` `1.50`), the integer part, a point and the decimals: the fraction times ten to the
  precision, plus 0.5, cut, in the game's arithmetic (0.0625 `0.063`, 0.0025 as 0.00249999994 `0.002`, 99.9995 as
  99.9994965 `99.999`, 9.9995 as 9.99950027 `10.000`). The precision is at most 9 (`R2SW(0.1, 0, 12)` `0.100000000`,
  `R2SW(1.23456705, 0, 9)` `1.234567040`, `R2SW(1.5, 0, 2147483647)` `1.500000000`), and 0 writes `.0` (`R2SW(1.5, 0,
  0)` `2.0`). Below 0 it writes 6 decimals with the zeros at the end dropped but one (strings-6 and single probes):
  `R2SW(1.25, 0, -1)` `1.25`, `R2SW(1.23456705, 0, -3)` and `-1` `1.234567`, `R2SW(2.0, 0, -1)` `2.0`, `R2SW(0.0625, 0,
  -1)` `0.0625`, `R2SW(123.456, 0, -2)` `123.456001`, `R2SW(1 / 3, 0, -1)` `0.333333`, `R2SW(1.5, 4, -2)` `  1.5`, and
  `1.5` for -100 and -2^31 and `1.25` for -2147483647. The text is at most 255 bytes, the rest cut: width 253 gives 254
  bytes, 254 gives 255, 255 gives 255 ending `1.5`, 256 ending `1.`, 1000, 5000, 100000, 2147483640 and (precision 0)
  2147483644 give 255 spaces; width 2147483647 and -2^31 give `""`, the length past a 32-bit integer. `R2SW(-1.5, 253,
  2)` gives 255 bytes, but -1.5 at width 254 and at 1000, each alone in a probe, closed the game (0xC0000409, a buffer
  overrun). Below 2^31 the integer part is exact (`123456792.000`, `1000000000.000`). From 2^31 the game writes about
  ten digits of its own and then zeros, unpadded: `2147483840.000` for 2^31, `10000000640.000` for 1e10,
  `100000057600000000000.000` for 1e20 (also at width 30), 340000512 and 30 zeros for 3.4e38; the largest float
  (7f7fffff) gives `inf`, as `math.huge` does; width 100 pads nothing there either. 141 cases in strings and strings-2.
- `BlzBitAnd(-1, 5)` 5, `BlzBitOr(5, 3)` 7, `BlzBitXor(5, 3)` 6, `BlzBitXor(-1, 2147483647)` -2147483648.
- The maths natives give `math.*`'s numbers (the same bits on 60 inputs) but for: `SquareRoot` is 0 below 0.001 (0 up to
  3a83126e and a root from 3a83126f, the float nearest 0.001, by bisection), `Asin` and `Acos` are 0 past ±1 (also for
  ±1.00000012) where `math.asin(2)` is π/2 (3fc90fdb), and `Atan2(0, 0)` is 0 where `math.atan(0, 0)` is π/2.
  `Deg2Rad(x)` is x times 0.0174532924 (3c8efa35) and `Rad2Deg(x)` x times 57.2957802 (42652ee1), cut toward zero:
  `Deg2Rad(180)` 3.1415925 (40490fda), `Rad2Deg(math.pi)` 180; the game's own `math.rad(180)` is 40490fd9 and
  `math.deg(1)` 42652ee3, so these natives differ from them.
- New `math.*` values on the way, for the Lua number model (W1-8): `sin`, `cos` and `tan` of 1 are 0.841473639,
  0.540297925 and 1.55742562; of -1 -0.841470361, 0.540302992 and -1.55740499 (not symmetric); of 100 -0.506368101,
  0.862317204 and -0.587217867; of 1e6 -0.471396714, 0.881921232 and -0.534511149 (the true sine is -0.349994);
  `sin(math.pi)` 2^-31, `tan(math.pi)` -4.65661343e-10, `cos(math.pi)` -0.99999994; `atan` of 1, 0.5 and 100 3f490fdc,
  3eed6339 and 3fc7c82f; `atan(y, x)` for (-1, -1), (3, 4), (1, 0) and (0, -1) c016cbe4, 3f24bc7e, 3fc90fdb and
  40490fdb; `asin(1)` 3fc90fdb, `acos(-1)` 40490fdb, `acos(0)` 3fc90fda, `acos(-0.5)` 40060a91; `(-8) ^ (1 / 3)`
  1.99999964, `0 ^ -1` 0, `2 ^ -1` 0.500000119, `0.5 ^ 0.5` 0.707106888; `math.sqrt(0.001)` 3d018707.
- Hashtables: a key holds an integer, a real, a boolean, a string and a handle apart (`RemoveSavedInteger` leaves the
  real). A missing value loads as 0, 0.0, false, `""` (not nil) or nil, and so with a nil table, to which a save does
  nothing (`SaveStr` and `SaveTimerHandle` give false). `SaveStr` gives true; `""`, false and 0 are values that are had.
  One handle a key, whatever its type: `LoadGroupHandle` and `LoadWidgetHandle` of a saved timer are nil, a saved group
  replaces it, `SaveAgentHandle` of a timer loads with `LoadTimerHandle`, `LoadPlayerHandle` of a saved fogstate is nil.
  A `Save*Handle` gives true (timer, group, agent, player, location, text tag); `SaveTimerHandle` of nil gives false and
  leaves what the key held; of a destroyed timer it gives false and saves nothing; `SaveFogStateHandle` of
  `FOG_OF_WAR_MASKED` gives false but saves it (it loads back, and `==` holds). A timer destroyed after its save loads
  as nil, while `HaveSavedHandle` stays true. `FlushChildHashtable(ht, 1)` empties parent key 1 only (child keys -2^31
  and 2^31-1 are apart). `FlushParentHashtable` destroys the hashtable: a save after it does nothing, and another
  hashtable keeps its values. At most 256 hashtables at once: the 257th `InitHashtable` gives nil, and after one was
  flushed one more could be made, then nil again.
- The random natives: `GetRandomInt(5, 1)` gave 5 to 9 over 1,000 draws and `GetRandomReal(2, 1)` 2.0016 to 2.9992, so
  they go from the first bound up by the bounds' distance. `GetRandomInt(3, 3)` is 3 and `GetRandomReal(1, 1)` 1. After
  `SetRandomSeed(42)`, `GetRandomInt(0, 99)` gave 63 11 34 15 77, and after it again `GetRandomReal(0, 1)` 3f71d256,
  3e8b35bc and 3e2bf0d8. The widest bounds, 1,000 draws each (strings-4): `GetRandomInt(-2^31, 2^31-1)` always gave
  -2^31 and `GetRandomInt(2^31-1, -2^31)` always 2^31-1; `GetRandomInt(2^31-1, 0)` gave -2146973498 to -50177, all below
  0, and `(0, 2^31-1)` 3970624 to 2146773448: the count of numbers wraps in 32 bits. `GetRandomReal(-3e38, 3e38)` gave
  -2.9997e38 to 2.989e38 (522 below 0), `(0, 3e38)` 4.7e35 to 2.9996e38, and `(3e38, -3e38)` -3.79e-39 to 3.40207e38,
  355 of them below 0, all tiny.

## Players and the game

Probe `probes/players.lua`, one run on 3.0.1.24371, 11-08 (W1-4): MirrorImaged's probe map (D51: one user slot, no
custom forces, a selectable race, `config` calling `InitGenericPlayerSlots` and `main` calling `InitBlizzard`) started
with `-loadfile` on a hidden desktop, single player; the probe ran in the first zero-second timer. The run's lines
are in `probes/expected/players.txt`, as the game gave them.

- Slot 0 is "Local Player", Playing, `MAP_CONTROL_USER`, team 0, start location 0, colour 0. Its race was
  `RACE_UNDEAD` although `config` set `RACE_PREF_HUMAN` with the race selectable: afterwards
  `IsPlayerRacePrefSet(p, RACE_PREF_RANDOM)` is true and `RACE_PREF_HUMAN` false, so the lobby made it random.
- Slots 1 to 23, which the map doesn't use, are "Player 2" to "Player 24", Empty, `MAP_CONTROL_NONE`, team -1, start
  location -1, colour their slot, and each has one of the four races (human, orc, undead, night elf), seemingly at
  random. Slots 24 to 27 are "Player 25" to "Player 28", Playing, `MAP_CONTROL_CREEP` all four, team 0, start
  location -1, colour their slot, and race 0 (none of the `RACE_*` constants; `GetHandleId` 0). No slot is an
  observer, and every slot's race reads selectable (`GetPlayerSelectable`), the 23 unused ones and the neutral ones
  too.
- Alliances, as a mask of `ALLIANCE_*` types 0 to 9: each player to itself 255 (types 0 to 7; slots 0, 24 and 27
  read); neutral passive (27) and slots 0 and 24, both ways, 1 (passive); after `InitBlizzard`'s
  `ConfigureNeutralVictim`, neutral victim (25) to 0 and to 24 and 24 to 25 1, 0 to 25 0; slots 0 and 1, 1 and 2,
  0 and 24 both ways, 0 to 26: 0. `IsPlayerAlly` follows type 0 and `IsPlayerEnemy` is its opposite in all of them.
- Player states 0 to 16 and 25 of slots 0, 1, 24, 25 and 27: `PLAYER_STATE_GAME_RESULT` 3 (`PLAYER_GAME_RESULT_NEUTRAL`)
  and `PLAYER_STATE_FOOD_CAP_CEILING` 100 for all; `PLAYER_STATE_GIVES_BOUNTY` 1 for 24 and 27 (25's had been set to 0
  by `ConfigureNeutralVictim`); `PLAYER_STATE_PLACED` 1 for slot 0 only; the rest 0. Gold set to -5 reads 0, to
  2,000,000 reads 2,000,000; food cap set to 150 reads 150, past the ceiling; `PLAYER_STATE_GOLD_GATHERED` stays 0.
  All four handicaps read 1.
- `GetPlayerTechMaxAllowed` reads 2147483647 for `hfoo` and for an unknown id (`zzzz`), 3 for `Rhme` (Iron Forged
  Swords, 3 levels); a maximum set to -5 reads 2147483647. `GetPlayerTechCount` of `Rhme` reads 0 at the start, then
  2 after `SetPlayerTechResearched` 2, 1 after setting 1 (it lowers), 3 after `AddPlayerTechResearched` 5 (stopped at
  the upgrade's 3 levels), 2 after `BlzDecPlayerTechResearched` 1; `zzzz` set to 4 reads 4. `GetPlayerTechResearched`
  is false at 0 and true at 2.
- Forces: a force holding slot 5, given to `ForceEnumPlayers` with no filter, holds only slot 0 afterwards: the
  enumeration clears the force first and takes the one playing player of slots 0 to 23, not the neutral slots, which
  play too. `ForceEnumAllies` of slot 0 gives 0, `ForceEnumEnemies` of 0 nothing, `ForceEnumPlayersCounted` of 1 gives
  0; `GetPlayersAll()` holds 0. `ForForce` over a force given 3, 1 and 2 in that order runs 1, 2, 3, and
  `GetEnumPlayer()` is nil after it.
- The game: `GetGameSpeed` `MAP_SPEED_NORMAL`, `GetGameDifficulty` `MAP_DIFFICULTY_EASY`, `GetGamePlacement`
  `MAP_PLACEMENT_USE_MAP_SETTINGS` (as `config` set it), `GetGameTypeSelected` `GAME_TYPE_MELEE`, `GetPlayers` 1,
  `GetTeams` 24 (`InitGenericPlayerSlots` under melee sets 24), `VersionGet` `VERSION_FROZEN_THRONE`,
  `IsNoVictoryCheat` and `IsNoDefeatCheat` false, no map flag of 1 to 2^20 set, both integer game states 0, start
  location 0 at (0, 0) as defined and the undefined 5 at (0, 0). `SetGameSpeed(MAP_SPEED_FASTEST)` leaves it at
  normal; `MAP_SPEED_SLOWEST` reads slowest.
- The time of day is 0 at the start with scale 1. Set and read back at once (with the float's bits): 7, 12, 18, 6, 0.5,
  23.9 (`41bf3333`), 23.95, 23.98 and 23.99 (`41bfeb85`) read as set; 23.995 and 23.999 read 0; 0.1 reads 0.099999994
  (`3dcccccc`, a float step under 0.1's `3dcccccd`). Set to 23.9, 2 s later it reads 23.9999962 (`41bffffe`); set to
  0, 2 s later 0.0999975204 (`3dcccb80`) and 20 s later 0.999865532 (`3f7ff730`), under 0.1 and 1. Inferred: the game
  adds each frame's share in floats and loses a little each time; adding fixed steps of 1/1024 to 1/4 s in the game's
  arithmetic gave none of the three values, and neither did any of ten conversions tried for 0.1 (`x * 20 / 20`,
  `x / 24 * 24` and the like). Against the workspace's facts: on a Moonwell check map (11-05) a time set to 7 read
  6.999999, where here 7 read back exactly; that read may have come later, after the game had moved the time on, while
  here it came at once. W1-9's lobby probe separated them (below): neither read gives 6.999999.

Probe `probes/lobby.lua`, five runs on 3.0.1.24371, 11-08 (W1-9), on the probe map with a computer (decisions D140:
a user in slot 0, a computer in 1 and a user slot 2 no one plays, named "Probe User", "Probe Computer" and "Probe
Empty" in `war3map.w3i`, human and selectable, custom forces with fixed player settings, placement teams together),
single player, `-loadfile`. The second run without `war3map.w3i`'s selectable-race flag gave the same lines. The lines
are in `probes/expected/lobby.txt`.

- `config` ran once, in the map's Lua state (a global it set was there in the probe). At its start, before it set
  anything: `GetGameTypeSelected` melee, `MAP_LOCK_ALLIANCE_CHANGES` false, the local player slot 0, `GetPlayers` 0,
  `GetTeams` 24, and slots 0 to 2 each named "" (empty), Empty, `MAP_CONTROL_NONE`, race 0, with
  `IsPlayerRacePrefSet(p, RACE_PREF_RANDOM)` true.
- After: slot 0 "Local Player", Playing, user, team 0; slot 1 "Probe Computer" (its `war3map.w3i` name), Playing,
  `MAP_CONTROL_COMPUTER`, team 1, `PLACED` 1; slot 2 "Player 3", Empty, `MAP_CONTROL_USER` (as `config` set it), team -1
  though `config` set 0, `PLACED` 0. All three human, with the human preference set and the random one not, though
  selectable (in the players probe, without custom forces, the same `config` gave a random race). The neutral slots
  had the random preference. In the first two runs, whose probe also printed slot 3 (the committed probe no longer
  does), the unused slot 3 had a random race (orc, then undead) and the random preference.
- The lobby moved the start locations: `config` gave slots 0, 1 and 2 locations 0, 1 and 2; slots 0 and 1 read 2 and 0
  in the first run, 1 and 2 in the second (the later runs' weren't kept); slot 2 kept 2.
- The computer's alliances are the user's: none with 0 or 2 either way (different forces, not allied), 255 to itself,
  passive with neutral passive both ways, 25 to it passive and it to 25 not, none with 24 or 26. Its states and
  handicaps are a user's (game result 3, food cap ceiling 100, `PLACED` 1, handicaps 1). `ForceEnumPlayers` held 0 and
  1, the allies of 1 were 1 and its enemies 0.
- After the lobby: game type still melee (custom forces too), placement teams together, 3 players, 2 teams, and the
  map flags 1024 (`MAP_LOCK_ALLIANCE_CHANGES`) and 1048576 (2^20, no constant in `common.j`) set.
- The time of day, suspended, each set read at once, in a 0 s timer, 0.1 s and 1 s later (float bits): 7 read 7, 7,
  then 7.00000477 (`40e0000a`) twice; 12, 18, 6 and 7.5 read as set all four times; 0.1 read 0.0999999046
  (`3dccccc0`) and 23.9 read 23.8999977 (`41bf3332`) all four times. Running at scale 1, a 7 read 7 at once and in a 0
  s timer. So a read later, after a frame, can differ from the read at once, but not as the 11-05 map's 6.999999.

## Files and game cache

Probe `probes/files.lua`, six runs on 3.0.1.24371, 11-08 (W1-5), the probe map started with `-loadfile` on a hidden
desktop, single player; the files each run wrote were looked at on disk afterwards. Runs 1 to 3 read files back through
`AHbz`'s tooltip, as the workspace's facts did, and run 1 also saved and reloaded game caches (below). Runs 4 to 6, the
committed probe, read them back through a game cache instead (`StoreString` into `files-probe-read.w3v` from the file,
`GetStoredString` after), so it can run here too; run 4 also read a file that goes on in Lua, which closed the game,
and runs 5 and 6, without it, gave the same lines, run 6's in `probes/expected/files.txt`. Two one-off runs read files
an outside program wrote and tried more paths (`files-paths`), and read a file that isn't JASS (`files-text`).

- What a file holds: `Preload` before `PreloadGenStart` is written; without `PreloadGenClear` the next file holds the
  earlier texts too; `PreloadGenEnd` twice writes the same file twice; a `Preload` after `PreloadGenEnd` is in the next
  file; after `PreloadGenClear` a file has only its start and end. So every `Preload` text is kept until
  `PreloadGenClear`. A file's own `Preload` call, run by `Preloader`, isn't kept: `PreloadGenClear`, `Preload("kept
  before")`, `Preloader` of a file holding `call Preload( "")` and a `StoreString`, `Preload("kept after")` and
  `PreloadGenEnd` wrote the two texts and nothing for the file's call (run 6; run 5, without the two texts, wrote an
  empty file).
- The bytes: `function PreloadFiles takes nothing returns nothing\n\r\n\tcall PreloadStart()\r\n`, each text as
  `\tcall Preload( "<text>" )\r\n`, then `\tcall PreloadEnd( 0.0 )\r\n\nendfunction\n\n\r\n`. `PreloadEnd`'s number
  was 1.9, 2.0, 2.0 and 1.9 in runs 1, 3, 5 and 6 for a file whose `PreloadGenEnd` came 2.5 game seconds after its
  `PreloadGenStart`, in a timer; 0.0 for every file written in one call. Of the 311 Preload files in this PC's
  `CustomMapData`, 304 have 0.0 and 7 have 0.1 (Moonwell's balance probe's, written in one call, and one of another
  map's).
- Paths: `.txt` and `.pld` were written and read back; `.dat`, `.json`, no extension, `.ini`, `.log`, `.w3v` and `.PLD`
  were written nowhere, and an outside program's `.dat`, `.json` and extensionless files (valid Preload JASS) read back
  nothing, nor did an existing `own.pld` read as `own.PLD`. Folders written in capitals (`MIRRORIMAGED\FILES-PROBE\`)
  and a name in another case (`Mixed.pld` as `mixed.pld`) were read. `/` worked as `\` for writing and reading;
  `files-probe\.\dot.pld` was written as `dot.pld`. `files-probe\sub\..\dots.pld` was written nowhere, but an outside
  file read through `sub\..` came back. `..\MirrorImaged-files-probe-escape.pld` (into `Documents\Warcraft III`) and an
  absolute path (`C:\Users\...\abs.pld`) were written nowhere and read nothing. Paths relative to `CustomMapData` of
  150, 200 and 201 characters were written and read back, 202 and 230 not; `CustomMapData`'s own path here has 58
  characters, so 201 makes a whole path of 259.
- `Preloader` reads a path once a game: written with one piece, read, written again with another and read, it gave the
  first piece both times, and so did the path with its folders in capitals (`MIRRORIMAGED\FILES-PROBE\own.pld`, runs 5
  and 6); the next game read the second.
- `Preloader` of a file whose line is `not jass` (wrapped as the probes' lines are) closed the game: it exited by
  itself with code 1, a `BlizzardError` process started, and the probe's lines never came (`files-text`). So did a
  file whose first text was a carrier line then `endfunction`, `//!beginusercode` and `FilesProbeLua={}--[[`, and
  whose second was `]]FilesProbeLua[1]=1--[[`, the shape of the Lua save files some maps write (run 4: the files
  before it were written, none after, exit code 1).
- Game caches: `InitGameCache` of one name twice gives two handles (`~=`) to one cache (5 stored through the first
  read 5 through the second); another name read 0. A missing value reads 0, 0.0, false and the empty string (a
  string, not nil). An integer and a real stored under one mission and key are both kept, and `HaveStored*` is true
  for those two types only; `FlushStoredInteger` then takes the integer and leaves the real. `StoreString` gives true,
  also for an empty string, which `HaveStoredString` then finds; a stored false and a stored 0 are found too. Missions
  and keys are found in any case (`Case`/`Key` read as `case`/`key` and `CASE`/`KEY`). `FlushStoredMission` takes the
  mission's real, string and boolean and leaves another mission. After `FlushGameCache` its handle read 0, and so did a
  new `InitGameCache` of the name; a value stored through the old handle then read 0 through it and by name, and
  `SyncStoredInteger` changed nothing. A `Preload` file whose line is
  `call StoreInteger(InitGameCache("files-probe.w3v"), "pre", "k", 42)` made `GetStoredInteger` of a new handle of
  that name read 42 after `Preloader`; the `StoreString` carrier of runs 4 to 6 gave every piece back.
- Saving (run 1 only): `SaveGameCache` gave true, also for a name without `.w3v`, and `ReloadGameCachesFromDisk` true.
  After a value was saved and another stored, a reload read the saved one and 0 for the other, through the old handle
  and a new one; a cache never saved read 0 after a reload. The game wrote the account's
  `BattleNet\<id>\Campaigns\Classic\Campaigns.w3v`, which then held every cache the game had then (two the probe never
  saved by name among them) and not the one dropped by the reload. The next game (`files-paths`) read the saved value,
  1, and 0 for the one stored after the save. A probe that saves a cache rewrites that file of the user's account, so
  the committed probe doesn't; run 1 rewrote it once (it held no other cache afterwards). Runs 3 to 6 left it as run 1
  wrote it (still 177 bytes, written 07:24:50, after runs at 07:46, 08:11, 08:12 and 08:13). `Classic.w3p` in the same
  folder is rewritten by every game, saving or not.

## Handles and errors

The probes `handles`, `handles-2`, `handles-3` and `arguments` in `probes/`, their game lines in
`probes/expected/<probe>.txt`, run by the game runner from the PowerShell tool, 3.0.1.24371, single player, 11-08
(W1-6): `handles` four times (the first with an older identity part), `handles-2` six times, `handles-3` five times
and `arguments` three times, with the same lines but where said below. Ids are read with `GetHandleId`, most as a
difference from a timer made just before. The tests are `tests/MirrorImaged.Tests/HandleTests.cs` and the places and nil cases in
`TriggerTests.cs`.

- Every handle a native makes takes the next id, one each, whatever its type: a timer, trigger, group, force, hashtable,
  `Condition`, `Filter`, `And`, `Not`, and also a trigger's action (`triggeraction`), condition (`triggercondition`) and
  event. `TimerStart` (with or without a callback), `ForForce`, `ExecuteFunc`, `TriggerExecute` and `TriggerEvaluate`
  take none.
- A Convert handle's id is its value, also for a value `common.j` doesn't name (`ConvertRace(5)` 5,
  `ConvertPlayerColor(100)` 100, `ConvertPlayerColor(-1)` -1, `ConvertGameEvent(4)` 4, `PLAYER_COLOR_RED` 0,
  `RACE_HUMAN` 1), and it takes none. `GetHandleId(nil)`, `GetHandleId()` and the event id outside an event are 0.
- The first ids: blizzard.j's globals are 0x100001 to 0x100007 (1048577 to 1048583), in the order blizzard.j makes them
  (`bj_volumeGroupsTimer` first, `bj_lastStartedTimer` last), so the game made one handle before them (inferred). The
  players come after: `Player(0)` is 1048584, `Player(25)` 1048585, `Player(1)` to `Player(24)` 1048586 to 1048609,
  `Player(26)` 1048644 and `Player(27)` 1048646; inferred, from that order, that a player is made when first asked for
  (`config` asks for `Player(0)` first). The probe's own first timer was 1048690.
- A destroyed handle keeps its id (`GetHandleId` reads it), and no new handle takes the id while Lua holds the destroyed
  one: three timers destroyed and kept took none of the next four ids; a destroyed trigger, force, hashtable, filter,
  `And` and condition the same. Nor right after Lua has collected the destroyed handle (a weak table emptied), in the
  same callback. A timer let go of but not destroyed keeps its id after Lua collected it.
- The ids come back later. Three timers destroyed and collected at 0.1 s: a second later four new timers took the three
  in their order and one other id (six runs), which came first in two runs and last in one; the three later runs give
  only the freed ones and the count. Three destroyed at 0.1 s, let go of and collected at 1.2 s: a second later the
  three in their order and one other id (six runs), last in the two that showed where. One destroyed and collected at
  3.0 s was not given at 3.01 or 3.05 s, and was at 3.2 s (five runs). Two timers a (made first) and b, destroyed at 0.1
  s, b collected then and a at 1.1 s: four timers at 2.1 s took a, another id, b and a new one (two runs; the three
  later runs give the freed ones only, a then b, and two others). So the last freed comes first, a batch freed together
  in its order, and another id was freed with some batches (whose, not found). Inferred: an id is freed once its handle
  is destroyed and Lua has collected every userdata of it, at a later step of the game (0.1 to 0.2 s), and given again
  last freed first.
- A destroyed condition's place is taken by the next `Condition` or `Filter` made, and whatever still holds the
  destroyed one runs the new one. A trigger whose condition was destroyed ran nothing and failed, and once a new
  condition was made ran it, with its result; the same when the new one was a `Filter`, and for a destroyed `Filter` and
  a new `Condition`. Two destroyed (a, then b) and two new (x, y): the trigger with a and b ran x, then y. A timer and a
  trigger made between the destroy and the new condition changed nothing; inside a `Not` the same. The new condition has
  an id of its own, not the destroyed one's, and doesn't equal it. A destroyed `And` isn't taken by an `Or` made after
  it: it still runs its own operands. Inferred: a condition's function lives in a place of its own that the next
  condition reuses, and this is the `ran removed` of `triggers`. W1-9's probe `events` gave a destroyed `Filter` to
  `ForceEnumPlayers` after a `Condition` had taken its place: it ran the new condition for each player.
- `==` between two handles compares their ids, whatever their types: `ConvertRace(1) == ConvertPlayerColor(1)`,
  `ConvertPlayerColor(n) == t` for a timer of id n, `ConvertRace(GetHandleId(Player(0))) == Player(0)` and
  `GetTriggerEventId() == ConvertPlayerColor(0)` outside an event are true; `ConvertRace(1) == ConvertRace(2)`, `t == u`
  and `t == Player(0)` false. With an agent first and a handle that isn't an agent second it raises: `t ==
  ConvertPlayerColor(n)` and `Player(0) == RACE_HUMAN` give `war3map.lua:N: bad argument #2 to '__eq' (agent
  expected)`, N the comparing line; `RACE_HUMAN == Player(0)` is false. `t == {}` and `t == nil` are false.
  `getmetatable` of a handle is `false`.
- The same handle as the same type is the same userdata: `rawequal` holds for `Player(0)` and `GetLocalPlayer()`, two
  `ConvertRace(1)`, `RACE_HUMAN` and `ConvertRace(1)`, two `Condition(nil)`, and `GetExpiredTimer()` and its timer. So
  it is one table key; an equal handle of another type is another key (`keys[ConvertPlayerColor(1)]` nil where
  `keys[ConvertRace(1)]` was set). A timer whose userdata Lua had collected came back from a hashtable as another
  userdata (its `tostring` differs); two loads then are the same one.
- `tostring` of a handle is its declared type, `: ` and 16 hexadecimal digits (`timer: ` and 16 is 23 characters;
  `player`, `race`, `eventid`, `conditionfunc`, `filterfunc`, `boolexpr` and `triggeraction` alike).
- `Condition(nil)` and `Filter(nil)` are one handle: the same id, `==` true, `rawequal` false (`conditionfunc` and
  `filterfunc`). In an `And`, `Or` or `Not` its value depends on what ran before it. With t and u conditions that pass,
  f one that fails, x any, and cn the nil handle: alone as a trigger's condition cn passes; `Not(cn)` passes,
  `Not(Not(cn))` fails, `And(cn, x)` fails without running x, `Or(cn, f)` fails, `And(t, cn)` passes, `And(t, Not(cn))`
  fails, `Not(And(t, cn))` fails, `And(t, And(cn, x))` fails without running x, `And(Or(f, t), cn)` fails, `Or(f, cn)`
  fails, a trigger with the conditions t and `Not(cn)` passes, `And(t, Or(cn, f))` fails and runs f, `And(t,
  Not(Not(cn)))` fails, `Or(f, Not(cn))` passes, `And(Not(f), cn)` fails, `And(t, And(u, cn))` passes, `And(Not(cn), t)`
  fails without running t, and `And(And(t, u), Not(cn))` fails. No rule tried fits them all (the last condition's
  result, in the evaluation or at each depth); inferred: it reads a value left in memory. A destroyed condition in an
  `And` after a passing one fails.
- Arguments (probe `arguments`, each call straight in `pcall`, so with no position): a handle parameter takes nil and
  nothing (`TimerGetTimeout(nil)` and `TimerGetTimeout()` 0.0, `GetHandleId()` 0) and refuses anything else with `bad
  argument #n to 'X' (<type> expected)`, with no `got`: a handle of another type, a converted handle, a parent type
  (`eventid` for `limitop`), a number, a string, a boolean, a table, a table whose metatable's `__name` is the type, a
  function and a thread. A function parameter takes nil and nothing and refuses a number, string or table with
  `(function expected)`. An integer parameter takes a numeral string as Lua's own functions do (`I2S("5")` 5, `"0x10"`
  16, `"2.0"` 2), refuses `"1.5"` and 1.5 with `(number has no integer representation)`, and a word, nil, nothing, a
  boolean and a table with `(number expected, got string)` and the like, nothing as `got no value`, and a handle as its
  declared type (`got timer`, `got race`, `got conditionfunc`). A real the same (`R2S("1.5")` 1.500, `R2S(2)` 2.000).
  A string parameter given nothing: `got no value`. A boolean parameter takes Lua's truth: nil and nothing false; 0, 1,
  `"false"` and a table true. An extra argument is ignored (`I2S(7, 8)` 7).
- Errors' positions: a native's error inside a function of the script names the calling line, `war3map.lua:N: bad
  argument ...`, also in a tail call (`return I2S("x")`); `error("raised")` gives `war3map.lua:N: raised`, level 2 the
  caller's line, level 0 no position; `error({})` keeps the table; indexing nil, calling a missing global and
  arithmetic on a table give Lua's own messages with the line.

## Player and game events

Two probes on 3.0.1.24371, 11-08 (W1-9), single player, `-loadfile`: `probes/events.lua` on the probe map with a
computer (decisions D140), five runs, and `probes/forforce-wait.lua` on the usual probe map, six runs (its last two
cases in the last four). Their lines are
in `probes/expected`; the first run of `events` called RemovePlayer in another order, as below. Each part ran in the
probe's zero-second timer unless timed; times are a 1000 s timer's elapsed time.

- Filters (each enumeration run in the probe's zero-second timer, outside any event or `ForForce`):
  `ForceEnumPlayers` with a filter called it for slots 0 and 1 (the playing players of 0 to 23), in that
  order, with `GetFilterPlayer` the player, `GetEnumPlayer` and `GetTriggerPlayer` nil, and `GetFilterPlayer` nil
  after. The force was cleared first (slot 5 gone). A filter returning true, false or "is player 1" gave 0 1, nothing
  and 1. `ForceEnumPlayersCounted(f, filter, 1)`: with a filter passing all, called for 0 only and held 0; with one
  refusing 0, called for 0 and 1 and held 1. `ForceEnumAllies` and `ForceEnumEnemies` of 0 called it for 0 and 1 and
  held 0, and 1. A filter returning 1 passed; 0, "x", nil and a table failed; one that raised failed for both. A
  `Condition` as the filter worked; `And(filter(true), filter(false))` ran both for 0, then both for 1, and held
  nothing; `Filter(nil)` held 0 and 1 without running anything. A filter destroyed before a `Condition` was made, then
  given to `ForceEnumPlayers`, ran that condition's function, twice, and held 0 and 1: the condition had taken the
  destroyed filter's place ("Handles and errors").
- Alliances: `SetPlayerAlliance(0, 1, ALLIANCE_PASSIVE, true)` fired, inside the call, player 0's
  `EVENT_PLAYER_ALLIANCE_CHANGED` registration first and then its `TriggerRegisterPlayerAllianceChange` for passive
  (registered before it), both with event id 12 (`EVENT_PLAYER_ALLIANCE_CHANGED`) and `GetTriggerPlayer` nil, the
  alliance reading true inside; setting it true again fired nothing; false fired both again, false again nothing.
  1 to 0 fired player 1's, not player 0's. Shared vision on and off fired only its own registration. Of alliance types
  0 to 9 set on and off, only 0 (passive) fired `EVENT_PLAYER_ALLIANCE_CHANGED`. `SetPlayerAlliance(0, 0, PASSIVE,
  false)` fired nothing and the alliance still read true. Against the workspace's facts (11-06), which say the
  alliance-type event fires "each time it is set": setting the same value again fired nothing here, so that means each
  change.
- Player states: `TriggerRegisterPlayerStateEvent` on player 0's gold with limit 2 and each comparison, gold set from 0
  to 5, 0, 1, 2, 2, 3, 2, 1, 0, 4, -1, 0, read inside: `LESS_THAN` 0, 1, 1, 0, 0; `LESS_THAN_OR_EQUAL` 0, 1, 2, 2, 1,
  0, 0; `EQUAL` 2, 2; `GREATER_THAN_OR_EQUAL` 5, 2, 3, 2, 4; `GREATER_THAN` 5, 3, 4; `NOT_EQUAL` 5, 0, 1, 3, 1, 0, 4, 0
  (no 2). So it fires on a change to a value that meets the comparison, -1 counting as the 0 stored, the second 0 not.
  The event id was 11 (`EVENT_PLAYER_STATE_LIMIT`) for all. Lumber over 10.5 set to 10, 11, 12, 10 fired at 11 and
  12, with `GetTriggerPlayer` 0 and `GetEventPlayerState` the lumber state (typed `playerstate`). Player 1's gold
  registration didn't fire for player 0's gold; `TriggerRegisterPlayerEvent(t, p, EVENT_PLAYER_STATE_LIMIT)` never
  fired; outside an event `GetEventPlayerState` was the state with id 0.
- The time of day, suspended, set to 0, a trigger registered with limit 12, then set to 20, 10, 11, 12, 12, 13, 12, 11,
  10, 14, 11.5, 12.5, 11.5 (sets 1 to 13), each comparison in its own pass; the sets that fired, with the time read
  inside: `LESS_THAN` and `LESS_THAN_OR_EQUAL` 2 (10), 7 (12), 11 (11.5), 13 (11.5); `EQUAL` 4 (12), 7 (12);
  `GREATER_THAN_OR_EQUAL` and `GREATER_THAN` 1 (20), 4 (12), 6 (13), 10 (14), 12 (12.5); `NOT_EQUAL` none. Inside, the
  event id 3, `GetEventGameState` the time of day, `GetTriggerPlayer` nil.
- The time of day passing 12, with a trigger for each comparison: set to 11.9 (from 6) and resumed at scale 1 at
  0.0001, `GREATER_THAN`, `GREATER_THAN_OR_EQUAL` and `EQUAL` fired at 1.9999 in that order, reading exactly 12. At
  2.5001 scale -1 and set to 12.1: `EQUAL`, `LESS_THAN_OR_EQUAL` and `LESS_THAN` at 4.5048, reading 12. At 5.0001
  suspended, scale 1 and set to 12: inside the set `EQUAL`, `GREATER_THAN` and `GREATER_THAN_OR_EQUAL`, then at
  5.0048 all five but `NOT_EQUAL`, then nothing for 2.5 s. At 7.5001 set to 13: nothing, then nothing. At 10.0000
  resumed and set to 12 (from 13): inside `LESS_THAN_OR_EQUAL` and `LESS_THAN`, at 10.0043 `EQUAL`, `GREATER_THAN` and
  `GREATER_THAN_OR_EQUAL`. The first two runs gave the same times; the later runs' weren't kept. Against the
  workspace's facts (11-05), which say setting the time on from exactly the hour (6 to 18, 18 to 17) fires the old
  hour's event too: sets from exactly 12 (12 to 13 and 12 to 11, above) fired no `EQUAL` inside the set, and 12 to 13
  after frames at 12 fired nothing in the frames after either; whether the 11-05 firing came from a running day's
  frames isn't separated.
- RemovePlayer, with `EVENT_PLAYER_VICTORY`, `DEFEAT`, `LEAVE` and `END_CINEMATIC` registered for players 0 and 1:
  on the computer (1, Playing) victory fired its `EVENT_PLAYER_VICTORY` inside the call (event id 14, trigger player
  1, slot Playing and game result 0 already); it stayed Playing with result 0. Then victory again, tie, defeat and
  neutral fired nothing and changed nothing. In the first run, neutral first left it Left with result 3 and fired
  nothing, and tie, victory, defeat, defeat and victory after it fired and changed nothing. On the user (0), victory
  fired nothing, inside or 1 s later, and left it Playing with result 0.
- `TriggerRegisterGameEvent` for every `gameevent` (0 to 10, 256 to 259, 310): none fired in the whole probe, through
  its timers, state sets, the victories and `EndGame` (no `EVENT_GAME_VICTORY` either).
- `EndGame(false)`, the last call of the probe's `done`: the code after the call ran (`events-after.pld`) and so did a
  0 s timer started before it (`events-timer.pld`), in all five runs (the last two's files deleted before them and
  checked after by the events game test). That no `EVENT_GAME_END_LEVEL` fired is inferred: its action would have
  written `events-end.pld`, which never came.
- `forforce-wait`: steps 1.5 s apart, each then executing a trigger whose action waits 0.5 s: none, a force made and
  destroyed, `ForForce` over a new empty force, over one holding player 0, each ended; `ForForce` over an empty force
  then destroyed, and over one holding 0 then destroyed, each never ended; the next step's wait (no force) and a
  `PolledWait(0.5)` after it ended. The force destroyed had id 1048708, the trigger made after it 1048709, the timer
  1048711. Then, in the last four runs: after `ForForce` over a force holding 0 and its destroy, a trigger's wait
  started 1 s later ended, and so did the next step's; a trigger waiting 1.5 s, started 0.5 s before such a destroy,
  ended, while a new trigger's wait started beside the destroy in the same instant never did; the next step's ended.

## Probes compared

The probes run again by W1-8's comparison (`docs/decisions.md`, D120), 3.0.1.24371, single player, 11-08, by the game
runner from the PowerShell tool. Each probe's lines are in `probes/expected`, and where MirrorImaged differs, in
`probes/differences`.

- `game-time`: a 100000 s timer and a 1 s periodic timer started in the same callback; at the periodic timer's first 7
  ticks the clock's `TimerGetElapsed` printed with `%.2f` read 1.01 to 7.01, and from the 8th to the 20th 8.00 to 20.00
  (1 run; W0-4's runs read 1.01 to 20.00 too). Near 99999 a float's step is 0.0078125, so 1.01 is 1.0078125: the
  remaining time was a step under 99999, as if the tick came a little after the whole second, and from 8 s on it didn't.
  Inferred, since the game's elapsed time is the timeout less the remaining time, cut toward zero (D45).
- The six one-call `R2SW` probes of W1-3 (`probes/r2sw-*.lua`), each run once more, gave what the facts above say:
  widths 2147483640 and 2147483644 255 bytes of spaces and the number cut off, precisions -100 and -2147483647 `1.5` and
  `1.25`, and -1.5 at widths 254 and 1000 closed the game before the probe's line. So did `files-text`, `Preloader` of
  a file whose line is `not jass` (now `probes/files-text.lua`).
- `hashtables`: `SaveFogStateHandle(ht, 6, 6, FOG_OF_WAR_MASKED)` gave true, where W1-3's run gave false; the fogstate
  loaded back with `==` holding both times, and every other line was the same. So that answer isn't the same every run.
- `players`: slots 0 to 23 had other races than in W1-4's run (slot 0 human, where it was undead), every other line the
  same: the lobby's races are random each game.

Two new probes, each run twice with the same lines:

- `lua-config`, the Lua build: `math.log(8, 2)` 3.00000024 (`40400001`), `math.log(3, 2)` 1.58496189 (`3fcae008`),
  `math.log(1024, 2)` 10.000001, `math.log(100, 10)` 1.99999964 (`3ffffffd`), `math.log(1000, 10)` 2.99999189; the
  numerals `0x1.0000011p0` 1, `0xA.8p0` 10.5, `0x.1p4` 1, `tonumber('0x1p-2')` 0.25; `%a` of 0.1 `0xc.ccccdp-7`, of 1.0
  `0x8p-3`, `%A` of -2.5 `-0XAP-2`; `os.date` with `%F`, `%e` and `%Ey` raises "bad argument #1 to 'os.date' (invalid
  conversion specifier '%F')" and the like, and `%#x` of time 0 is `Thursday, January 01, 1970`. None of `math.pow`,
  `math.ldexp`, `math.frexp`, `math.cosh`, `math.sinh`, `math.tanh`, `math.log10`, `math.mod`, `bit32`, `unpack`,
  `loadstring`, `table.maxn`, `table.getn`, `setfenv`, `module`, `string.gfind`, `string.dump` and `require` exists;
  `utf8`, `table.move`, `math.ult` and `rawlen` do. `load` of the start of a binary chunk (`'\27Lua\83\0'`) gives
  "truncated precompiled chunk", with mode `b` too: the chunk reached Lua's bytecode reader, so the game reads
  bytecode (inferred; no whole chunk was loaded: none was made, though a map can write one by hand without
  `string.dump`). An integer made a float (`i + 0.0`, `i / 1`) rounds to nearest, ties to even: 16777217 is 16777216,
  16777219 and 16777221 are 16777220, 16777223 is 16777224, 33554435 and 33554437 are 33554436, 2147483647 and -2^31 are
  ±2^31; `16777219 == 16777220.0` is false and `16777219 < 16777220.0` true. `pairs` over the 20 string keys `a` to `h`, `one` to `five`, `alpha`, `beta`, `gamma`,
  `x1`, `x2`, `x10` and `Footman`, set in that order, visits them as `three four one f e d c b a five x10 two Footman
  x2 x1 beta gamma h g alpha` in both runs; Lua 5.3 with string hash seed 0 visits them otherwise. Inferred: the
  game's seed is fixed and isn't 0 (another hash function or table growth would change the order too); W1-10's
  `string-order` found it, `0x1c46318e` (below).
- The three closing probes (`r2sw-negative-254`, `r2sw-negative-1000`, `files-text`), run again: each wrote its
  `started.pld` before the game closed (on disk after two of the runs; the runner's check after the third).
- `timers`: six timers due at 1.0 ran as resumed (started at 0 for 1 s, paused and resumed at 0.25), first (started
  at 0), periodic (0.25 s from 0, its fourth expiry), second (started at 0 after first), late (started at 0.5 for 0.5)
  and restarted (started at 0 for 2 s, started again at 0.5 for 0.5); they were made in the order late, restarted,
  periodic, resumed, second, first. A 3600 s timer read every 0.01 s from a 0.01 s periodic timer started with it gave
  elapsed times 0.01001, 0.02002, ..., 0.24023, 0.25024, 0.26001, ..., 0.50024, ..., 0.75000, ..., 1.00000 (with
  `%.5f`), and its remaining time after 100 reads 3599.00000.

## The game's data

Probes `probes/data.lua`, `data-objects.lua`, `data-tags.lua`, `data-names.lua` and `data-sets-melee.lua`,
`data-sets-tft.lua` and `data-sets-fk.lua`, on 3.0.1.24371, 11-08 (W2-1): MirrorImaged's probe map (D51: info format
28, which has no data version, the Default data set, the melee flag cleared) started with `-loadfile` on a hidden
desktop, single player, each changed by `DataProbeMaps` (`tests/MirrorImaged.GameTests`). For `data`, `data-objects`
and `data-tags`: slot 0 named `TRIGSTR_001` and a computer slot 2 named `TRIGSTR_002` in `war3map.w3i`, the computer
seated in `config` (`SetPlayers(2)`, its controller, colour, race and start location, as World Editor writes them),
six strings in `war3map.wts` (0 "Zero String", 1 "Probe Alpha", 2 "The Horde", 3 "Probe Footman", 4 "Fourth string",
5 "42"), `war3map.w3u` renaming hfoo `TRIGSTR_003` and hkni "Main Knight" and making h000 from hfoo,
`war3mapSkin.w3u` renaming hkni "Skin Knight", `war3map.w3a` giving Holy Light's level 1 250 healing, and
`war3map.w3q` giving Rhme 5 levels and making R000 from Rhme. For `data-names`, W1-9's map with a computer (D140,
custom forces with fixed player settings) with its computer named `TRIGSTR_001` and string 1 "The Horde". For
`data-sets-melee` the melee flag set; for `data-sets-tft` that in info format 31 with The Frozen Throne's data
version; for `data-sets-fk` info format 31 with Forsaken Kingdom's, not melee. The runs' lines are in
`probes/expected`, as the game gave them; the data, tags and set probes ran two or three times as they grew, the
lines they kept the same each time.

- Names: slot 0, the one person, is "Local Player" and the seated computer "Computer (Normal)", the strings
  `LOCAL_PLAYER` and `COMPUTER_NORMAL`; neither reads its `war3map.w3i` name, and an unseated slot is "Player N". On
  the map with fixed player settings the computer named `TRIGSTR_001` reads "The Horde", its string (`data-names`),
  as W1-9's lobby probe read "Probe Computer", written plainly; custom forces without fixed settings aren't measured.
  Without the `config` lines the computer slot of `war3map.w3i` isn't seated at all: `MAP_CONTROL_NONE`, Empty,
  "Player 3" (first run).
- Object data (`data-objects`): `GetObjectName` reads hfoo "Probe Footman" (the `TRIGSTR_` resolved), hkni "Skin
  Knight" (the skin file's change over the main file's) and h000 "Footman". `GetPlayerTechMaxAllowed` reads Rhme 5,
  R000 3 (its standard base's, not the map's change to Rhme) and Rhar 3; `AddPlayerTechResearched` of 9 stops each at
  those (`data`). Each object of a format 3 file has one set flag, 0, as World Editor writes it; the first runs, whose
  ids were in War3Net's byte order reversed (`oofh`), loaded and changed nothing, and one of them with no set flag
  closed the game with `BlizzardError` as it loaded (not separated which of the two closed it).
- `GetLocalizedString`: `TRIGSTR_001`, `TRIGSTR_1`, `TRIGSTR_0001` and `TRIGSTR_001 ` (a space after) give string 1,
  and `TRIGSTR_`, `TRIGSTR_abc` and `TRIGSTR_000` string 0, as C's `atoi` reads the rest; `TRIGSTR_999`, which the
  file doesn't have, `TRIGSTR_-1` and `trigstr_001` come back as they are. `PLAYER` gives "Player", `player` "player"
  (a key is found in its case); `GAMEOVER_CONTINUE` gives "|CFFFFFFFFC|Rontinue", `KEY_HOTKEYS` "|CffffffffH|Rotkeys",
  `AGILITY` "Agility" (`InfoPanelStrings.fdf`), `ERROR_ID_INVALIDPARAMS` "The parameters provided are invalid."
  (`NetworkStrings.fdf`), `MONTH_01` "January" (`DateStrings.fdf`); `WESTRING_APPNAME`, a World Editor string,
  `NO_SUCH_KEY_ZZ` and `""` come back as they are.
- `GetLocalizedHotkey` reads `UI\MiscUI.txt`'s `[Hotkeys]` (`GAMEOVER_CONTINUE='C'`), in any case: `GAMEOVER_CONTINUE`
  and `gameover_continue` 67, `GAMEOVER_QUIT_GAME` 81, `GAMEOVER_OK` 79, `KEY_DELETE` 68; `MinimapTerrain`, which has
  only a `:deDE` line, 0, and so `PLAYER`, `ACCEPT`, `KEY_HOTKEYS` (a coloured letter in its string but no hotkey line),
  `TRIGSTR_001` and `""`.
- A `TRIGSTR_` given to other natives: `SetPlayerName(Player(5), "TRIGSTR_002")` then reads "The Horde",
  `StringCase("TRIGSTR_001", false)` gives "probe alpha" and `StringCase` of `TRIGSTR_`, `TRIGSTR_abc` and
  `TRIGSTR_000` "zero string", of `TRIGSTR_-1` "trigstr_-1"; `StringLength("TRIGSTR_")` is 11, "Zero String"'s. But
  `StringHash("TRIGSTR_001")` (-896862224) isn't `StringHash("Probe Alpha")` (577683974), `S2I("TRIGSTR_005")` is 0 and
  `S2R` of it 0 where string 5 is "42", `SubString("TRIGSTR_001", 0, 5)` is "TRIGS", and a string saved with `SaveStr`
  loads back as `TRIGSTR_001`. `ParseTags("TRIGSTR_001")` gives "TRIGSTR_001".
- `ParseTags` replaces each `<id,column>` by the object's value in that column of its table, as a whole number, the
  map's change included: `<AHhb,DataA1>` 250 (the map's), `<AHhb,DataA2>` 400, `<AHhb,Cool1>` 5, `<AHhb,levels>` 3,
  `<AHhb,DataA4>` 600, `<AHtb,Dur1>` 5, `<Rhme,base1>` 1, `<hfoo,HP>` and `<hfoo,realHP>` 420; `,%` after the column
  multiplies by 100 (`<AHhb,DataA1,%>` 25000, `<Aslo,DataA1,%>` 60, `<Aslo,DataB1,%>` 25) and anything else there is
  ignored (`<AHhb,DataA2,x>` 400); a fraction is cut (`<Aslo,DataA1>` 0, from 0.6). 0 for an unknown object
  (`<zzzz,DataA1>`), an id or column in another case (`<ahhb,dataa2>`), a text column (`<AHhb,Name>`), a `-` cell
  (`<Aslo,Area1>`), and for hfoo's `spd`, `dmgplus1` and `cool1` and Hpal's `STR` (not separated why `HP` reads and
  these don't). `<AHhb>` gives "", an unclosed `<AHhb,DataA2` drops the rest of the text, `<<AHhb,DataA2>>` gives
  "0>", and text outside tags, `%d` included, stays as it is.
- Balance sets: the probe map (format 28, Default, not melee) reads oshm 335, ebsh 1000 and fapo 0, `custom_v1`
  (`data-objects`), so format 28 reads The Frozen Throne's data; with the melee flag it reads 350, 1000 and 0, the base
  tables' values with `custom_v1`'s objects, as Melee with The Frozen Throne does (game-facts), and so does format 31
  with The Frozen Throne's data and the melee flag. So the Default data set reads as Melee on a melee map and as Custom
  on another. On both melee maps the item ckdk and the ability AA12, which only the base tables have, can't be made
  (`CreateItem` gives nil, `UnitAddAbility` false and level 0), while phea and AHhb can; on the Forsaken Kingdom map
  (base tables) all four can, and fapo has 400. `GetObjectName` reads every one's name on every map, fapo's
  "Apothecary" too where it doesn't exist, so a name doesn't show that an object exists.
- Constants by balance set: 100 pierce damage (`UnitDamageTarget`, `ATTACK_TYPE_PIERCE`, `DAMAGE_TYPE_NORMAL`) to a
  footman (heavy armour, 2) took 89.29 on the `custom_v1` map and 80.36 on both melee maps and the Forsaken Kingdom map:
  `DamageBonusPierce`'s heavy entry is 1.00 in `custom_v1`'s `MiscGame.txt` and 0.9 in the base file's, so Melee with
  The Frozen Throne takes the base file's constants, as it takes the base tables' values.
- The install's 273 maps: War3Net 6.1.2 reads every file of 270 of them; `(10)DustwallowKeys.w3m`'s `war3map.w3e`,
  `(1)SilverpineSprint.w3x`'s `war3map.doo` and `(4)Roundabout.w3x`'s `war3mapUnits.doo` it refuses (an enum value it
  doesn't know; `docs/upstream.md`). 257 read the base tables' values with `custom_v1`'s objects (melee maps), 14 the
  base tables (Forsaken Kingdom's data) and 2 `custom_v1`. Read 11-08.
- The install's text files have sections with words after the bracket (`[Hleo] leonid`, `[Scth] Scarlet Crusade
  Captain Hero`, `[AHw2] - T2 - Spell Blade`). Read 11-08.
- The install's 193 campaign maps (`Campaign/Classic` and `Campaign/Reforged`, 97 and 96, no `.w3n`; read 11-08): 130
  have a format-39 `war3map.w3i` of World Editor 6117 or 6127 (2.0.4.23839) with no race HUD in its players, where
  6140 and later put one after each player's race (both have the map's own after the loading screen number). 15
  sounds in 4 of Reforged's maps (Human03 1, Human09 1, Undead05 12, OrcX01_02 1) are named `g_snd_X` in their
  `war3map.w3s` and repeated `gg_snd_X` (`g_snd_H09Arthas16`, `gg_snd_H09Arthas16`), the repeated name the one their
  scripts make the sound in; the paths repeat as they are. That the game loads all of them is assumed, as they ship
  with it; not measured here. With decisions D300 and D301 every file of every one reads to its end.

## The number model's probes

Eleven probes run twice each by the game runner from the PowerShell tool, with the same lines, 3.0.1.24371, single
player, 11-08 (W1-10), their lines in `probes/expected`. Floats are given by their bits.

- Subtraction (`numbers-ops`): a - b for a in [1, 2) and b 1 to 22 octaves smaller, both with every mantissa bit
  varied, 72 cases: always the exact difference cut toward zero. With b 23 to 25 octaves smaller, a. a - b for b 1, 2,
  3, 5, 100 and 2^23 - 1 float steps under a: exact. Additions of the same 96 pairs: the exact sum cut toward zero.
- Signed zeros (`numbers-fine`): `0 - 0`, `-0 - 0`, `0 + -0` and `-0 + -0` are -0. `0/0` and `-0/-0` are 1, `-0/0` and
  `0/-0` are +0, as are `0/1`, `-0/1`, `0/-1`, `-0/-1` and every product with a zero. `1/-0` and `-1/0` are -2^127.
  2^-149 / 0 and a subnormal (`00400000`) / 1 are 0; 1 / `00400000` is `7eaaaaab` (the subnormal read as 1.5 * 2^-127).
- The range's ends (`numbers-ops`): `MaxValue + MaxValue` is `7fffffff`, `2^127 + 2^127` `7f800000`, `2^-126 -
  (2^-126 + 2^-149)` `f5000000` and its negative the same, `2^-125 - (2^-126 + 2^-149)` `007ffffe`, `inf + 1` inf,
  `inf + -inf` 0, `inf + inf` -0. Products: `2^127 * 2` `7f800000`, `* 4` `80000000`, `* 8` and up 0; `1.5 * 2^127 *
  6` `80900000`, `* 12` 0; `-2^127 * 4` `80000000`; `2^-126 * 0.99999994` 0, `* 1.0000001` `00800001`; `2^-125 * 0.5`
  `00800000`. Quotients: `2^127 / 0.5` `7f800000`, `/ 0.25` `80000000`, `/ 0.125` 0; `MaxValue / 0.99999994`
  `7f800000`; `2 / 0` `7f800000`, `4 / 0` `80000000`, `8 / 0` 0, `0.5 / 0` `7e800000`.
- Division (`numbers-div`, `numbers-fine`): 1/d for the 1024 divisors 1 + k/1024 is `floor(2^47 / (m - 1))` of the
  divisor's mantissa m, each one (1/1 is 1). Between them, by the 312 random divisors and the runs of 1 + j * 2^-16
  and 1 + j * 2^-23: up to 5 steps above the exact reciprocal near 1 and 0.5 near 2, near straight lines between the
  knots; the lines MirrorImaged draws are a fit, a step off for about 1 in 5 of the random divisors (decisions D151). n/d is n times that reciprocal: 240 of the 244 grid quotients equal n times the
  game's 1/d.
- `math.sqrt` (`numbers-sqrt`, `numbers-fine`), over 1 + k/512, 2 + k/128, 4 + k/64 and 260 random mantissas, and every
  float from 1 to 1 + 263 steps: the root is the same over runs of inputs (1 for inputs up to 1 + 18 steps, then
  `3f8000b5` up to 1 + 263 steps and beyond) and steps by 2.16e-5, and the error depends on the mantissa only, the
  same in [1, 2), [2, 4) and [4, 8). Every one of these is one more than the integer root of the mantissa times 2^8,
  mapped to 1 + 46341 (r - 46341) / 2^31, times `3fb504f3` for an odd exponent. `numbers-squares`, the roots of n * n
  for n 1 to 4096 in plain Lua: `math.floor` takes 64 of them below n, as the facts' C# probe found; that rule gives
  4090 of the 4096 and 70 below n, the game's integer root being a step higher for n 181, 362, 724, 1448, 2896 and 2327
  (m * 2^8 49 and 68 below a square; at 1 + 18 float steps, 25 below, it isn't).
- `math.sin` and `math.cos` (`numbers-trig`, `numbers-fine-2`), over k/64 for k -64 to 447, 2^-1 to 2^-40, k * 2^-14,
  40 large inputs, and runs of j * 2^-20, 1 + j * 2^-20 and 0.5 + j * 2^-21: the sine of j * 2^-20 stays the same over
  runs of six or seven inputs (0, then 12918, 25836, 38755, ... in 2^-31), a step of 2^-20 turns. A model fits 2,333
  of the 2,336 and 716 of the facts' 720 whole degrees (decisions D152): the angle is x times `3e22f983` in the game's
  arithmetic, cut toward zero to 2^-20 turns; the sine is a table of sin(pi k / 2048) in 2^-31
  (cut), each 256 steps of the turn between two values on a straight line divided into 255, negated past half a turn,
  the cosine a quarter turn on. sin(pi) is 2^-31 and sin(2 pi) -2^-31 (the table's last is 1); cos(0) is 1 - 2^-24.
  `sin(-x)` isn't `-sin(x)`, as the angle is cut toward zero. 1e6 is 0.921875 turns past whole ones, so its sine is
  -0.4714. `math.tan` is the sine over the cosine in the game's division.
- `math.rad(x)` is `x * pi / 180` and `math.deg(x)` `x * 180 / pi` in the game's arithmetic, all 130 measured
  (`numbers-functions`): `math.deg(pi)` is 180 and `math.deg(1)` `42652ee3`.
- `math.asin` of 1.0000001, 2, 5e9 and MaxValue is pi/2 (`3fc90fdb`), of their negatives -pi/2; `math.acos` of them 0
  and pi. `math.atan(0, 0)` is pi/2, `math.atan(0, x)` pi for x below 0 and 0 above, `math.atan(y, 0)` +-pi/2.
  `math.atan` near 0 is x (1 - 2^-20) (`377ffff0` for 2^-16).
- `math.exp` (`numbers-functions`, `numbers-fine-2`): exp(-x) is 1 over exp(x) in the game's division (k/16, k 1 to
  160: 135 of 160 to the bit, the rest a step off as the division model is); `exp(-103)`, `-104` and `-110` are 2^127,
  `exp(88)` `7ef881f1`, `exp(88.7228)` `7f7fff35`, `exp(89)` `7fa8e0dd` (packed past the range), `exp(100)` 0. Over
  1 + j * 2^-20, exp steps by 11 float steps but every 8th by 10 (a slope of 87/8), and is 1.4 to 2.4 steps low.
- `math.log`: right near 1 (`log(1 + 2^-16)` is `377fff80`), but off from 1.5 up: `log(1.5 + j * 2^-20)` about 5e-7
  low, `log(3)` 5, `log(100)` 3, `log(1000)` 41 (6.9077358), `log(1e6)` 16, `log(MaxValue)` 4. `log(0)` and `log(-0)`
  are -88.0296860 (`c2b00f33`, 2^-127's), `log(-1)` 0. `math.log(x, 10)` is `log10`, so `math.log(1000, 10)` 2.99999189.
- `^` (`numbers-functions`, 13 bases by 11 powers): a whole power from 1 up is exact as far as squaring as it goes in the
  game's arithmetic gives (`1000^10` `7149f2c9`, `(-8)^3` -512, `(-2)^3` -8, `25^10` `56ad78ea`); any other goes through
  exp and log, the sign dropped (`(-8)^-1` 0.125000045, `(-8)^0.5` 2.82842636, `(-8)^(1/3)` 1.99999964, `2^-1`
  0.500000119, `0.25^-2` 15.9999933); 0 to any power but 0 is 0, `x^0` 1.
- `pairs` (`string-order`): 24 keys of 8, 14, 22, 30, 38 and 60 letters, set in order, came in an order that, with
  `lua-config`'s 20 keys, only Lua 5.3.6's string hash and table code with the seed `0x1c46318e` give, of all 2^32.
- Timers (`timers-2`): read in the instant they were started, a 2 s timer's elapsed time is 2^-23 and its remaining
  time 2 - 2^-23, a 3600 s timer's elapsed 2^-12 and a 100000 s timer's 2^-7: each one float step of its timeout. At
  each of 96 expiries of a 0.01 s periodic timer started with them, the 2 s timer's elapsed and remaining time add up
  to 2, and the elapsed time is 0.01 times the count but for a drift of up to 2.2e-7 (exactly 0.25 at the 25th);
  the 3600 s timer reads a step past 0.25 at the 25th. At each whole second up to 6 the 3600 and 100000 s timers read a
  float step past it. Due at 2.0: a1 (started at 0), a2 (every 1 s from 0, its second), a3 (at 0, after a2), a4 (at
  1.0 for 1 s) and a5 (at 1.5 for 0.5 s) ran in that order; due at 4.0: b1 (at 0), b4 (at 0, after b3), b2 (at 0,
  paused and resumed at 1.0), b3 (at 0 for 3 s, paused at 1.0 and resumed at 2.0), b5 (at 3.0); due at 6.0: c2, c3 (at
  0), c1 (at 0, started again at 3.0 for 3 s).
- A condition's place taken twice (`condition-places`): c1 destroyed and taken by c2, c2 destroyed and taken by c3:
  the triggers holding c1, c2 and c3 each ran c3; destroying the stale c1 freed the place (all three ran nothing and
  failed), the next condition c4 took it (all three ran c4), destroying the stale c2 freed it again and c5 took it.
  `And(nil, nil)`, `Or(nil, nil)`, `Not(nil)`, `Condition(nil)` and `Filter(nil)` made after a destroy took no place;
  the next `Condition` did.

## JASS maps

Thirteen JASS probes and one Lua probe on 3.0.1.24371, 11-08 (W1-7), single player, `-loadfile`: the
`probes/jass-*.j` maps, whose `war3map.j` the game runs in its JASS machine (`ProbeMap.BuildJass`, decisions D130),
and `probes/blizzard-numbers.lua`, two runs each with the same lines, but `jass-integers` and
`jass-executefunc-null` and `jass-threads`' later loops, one run each (after the code review); and a JASS map with a
backslash in a rawcode, which never loaded. Their lines are
in `probes/expected`. A real is written as its IEEE bits, worked out in JASS with exact steps (halving, doubling,
subtracting 1 in [1, 2), `R2I` of a whole number).

- Integers wrap at 32 bits: 2147483647 + 1 is -2147483648, -2147483648 - 1 is 2147483647, 2147483647 × 2 is -2,
  65536 × 65536 is 0, 46341 × 46341 is -2147479015, 2147483647 × 2147483647 is 1, -(-2147483648) is -2147483648.
  Division cuts toward zero: 7 / 2 = 3, -7 / 2 = -3, 7 / -2 = -3, -7 / -2 = 3, -1 / 3 = 0; blizzard.j's
  `ModuloInteger` body gives 1 for (7, -3), 2 for (-7, 3), -4 for (-7, -3). An integer divided by zero (in a thread
  `ExecuteFunc` started) ended that thread, and its caller went on; -2147483648 / -1 closed the game (exit code 1,
  `BlizzardError`, no lines). A decimal literal past 32 bits wraps: 4294967295 is -1, 2147483648 and -2147483648 are
  -2147483648, 4294967296 is 0, 99999999999 is 1215752191; the map loaded.
- An integer given to a real is cut toward zero: 16777219 is 16777218, -16777219 is -16777218, 2147483647 is
  2147483520, 33554435 is 33554432, alike set to a real variable, given to a real parameter, passed to `I2R`, added to
  0.0 and written out as a literal (`set r = 16777219`). A function `returns real` that returns an integer gives the
  integer's bits: 16777219 gave 0x01000003, -16777219 0xfefffffd, `i + 1` 0x01000004, a literal 16777219 0x01000003;
  `return 1` gave a value that, doubled, never reached 1 (the thread stopped at the operation limit), so a denormal.
  `return i + 0.5` gave 16777218 (a real sum).
- A real literal: 0.1 reads 0x3dccccce, 0.2 0x3e4cccce, 0.7 0x3f333334 and 0.9 0x3f666667, each a float step above
  the nearest, as `S2R` of the same text; 0.3 0x3e99999a and 0.001 0x3a83126f, the nearest; 16777217.0 16777216; a
  literal of 39 digits 0. With a whole part it can differ from `S2R`: 1.001 0x3f8020c4 (`S2R` c5), 1.0001 0x3f800346
  (47), 1.0000001 0x3f800000 (`S2R` 0x3f800002), 2.9 0x40399999 (9a), 1000000.0625 0x49742400 (01), 3.14159
  0x40490fcf (d1), 123456.789 0x47f12064 (65); 0.123456789 0x3dfcd6ea both. All fit the whole part plus the decimals,
  each read as `S2R` reads it, added in the game's float arithmetic (inferred from MirrorImaged's model of both,
  decisions D134). A first version of the probe with exponent literals (`3.4e38`, `1.0e-3`, `3.0e9`) never loaded.
- Real arithmetic: 1.0 / 3.0 gave 0x3eaaaaab, 2.0 / 3.0 0x3f2aaaab, the literals above summed and multiplied as the
  game's Lua does (0.1 + 0.2 0x3e99999a, 3 × 0.1 0x3e99999a, 10 × 0.7 0x40e00001, 1 - 0.9 0x3dccccc8). 1.0 - 0.001
  gave 0x3f7fbe76 and 1000.0 - 0.001 0x4479ffef, the exact differences cut toward zero; MirrorImaged's model of the
  game's Lua gives 0x3f7fbe77 and 0x4479fff0. 1 - 2^-30, 1 - 2^-25, 1 - 2^-24 - 2^-26 and 2^24 - 1.5 gave the larger
  number, and 1 + 0.001 0x3f8020c4. A real divided by zero (7.0 / 0) ended the thread.
- `==` on reals holds within a margin. With a the base and the largest k for which a == a + k × u held, u a float step
  there: a = 0, u = 2^-30: 1073741 both ways (so |a - b| up to 0.00099999923 equal, 0.00100000016 not); a = 1: 8388
  steps of 2^-23 above, 16777 of 2^-24 below; 0.25: 33554 of 2^-25, 67108 of 2^-26; 4: 2097 of 2^-21, 4194 of 2^-22;
  ±1000: 16 of 2^-14 both ways; 2^20: 1 of 2^-3 above, 3 of 2^-4 below; and for a from 2^n to 1.875 × 2^n in eighths:
  n = 12, 2 steps of 2^-11 above (4 of 2^-12 below 2^12, else 2), n = 20 and 23, 1 step above (3 half steps below 2^n,
  else 1). The same with the operands swapped. So |a - b| < 0.001, or under two float steps of the larger number,
  holds. `!=` compares exactly (1.0 == 1.0001 and 1.0 != 1.0001 both true), and so do `<`, `>`, `<=` and `>=`; an
  integer 1 == 1.0005 held, 1 == 1.002 didn't.
- The operation limit: a thread ran `loop set n = n + 1 endloop` 428571 times before it stopped, with a call of an
  empty function or of `GetHandleId(null)` each time 300000, `set k = k + 1` and `set n = k` with a local 333333, and
  `exitwhen n > m` then `set n = n + 1` 230769 times: 3,000,000 operations of 7, 10, 10, 9 and 13. With `set n = n +
  1` and `set q = n / 3` it ran 230769 times (13), with an `if r == 0.5 then endif` 250000 (12), with `set s = t + "a"`
  230769 (13), with `set r = 1.0 + n` 214286 (14), and `set n = n + k` after an `if` that sets k, without an else and
  with one, 187499 each (16). A thread that ran
  six tenths of the limit, then `ExecuteFunc` of one that ran six tenths, then three tenths more, finished both; a
  trigger action that ran six tenths, waited `TriggerSleepAction(0)`, and ran six tenths again finished.
- A variable never set, read, ended its thread: a local integer added to, given to `I2S` or copied, a local real
  added to, a local string joined or given to `StringLength`, a local boolean as a condition, a local timer compared
  with null or given to `GetHandleId`, a global integer added to and a global timer compared with null; one set first
  read 5. An integer array's unset element read 0 (global and local), a string array's null (`== null` true, `== ""`
  false, length 0) and a timer array's null.
- Null strings: `StringLength(null)` 0, `"a" + null` "a", `null + "b"` "b", `null + null` null (not ""),
  `null == null` true, `null == ""` false, `"" == null` false, `StringCase(null, true)` and `SubString(null, 0, 1)`
  null, `S2I(null)` 0, `S2R(null)` 0.000, `StringHash(null)` 0, `SaveStr(t, 1, 2, null)` true, then
  `HaveSavedString` true and `LoadStr` null; `LoadStr` of a key never saved null too (`== null` true, `== ""` false).
  None ended its thread. `ExecuteFunc(null)` and `ExecuteFunc("NoSuchFunction")` ran nothing and ended nothing: the
  lines after each and the caller's came.
- A `debug` statement (`debug call out(...)`, `debug set n = 2`) didn't run.
- Rawcodes: 'hfoo' 1751543663, 'A0z~' 1093696126, 'a' 97; four bytes holding a UTF-8 letter read each byte after the
  first as signed: 'éab' -1029086878, 'aéb' 1623370082, 'abé' 1633796777 (their bytes unsigned would give
  -1012309662, 1640212834, 1633862569); 'ab"c' 1633821283. A map with `'a\bc'` never loaded.
- `blizzard-numbers` (a Lua map): `ModuloInteger(7, -3)` -5, `(-7, 3)` 2, `(-7, -3)` -4, `(7, 3)` 1, so the game's own
  Lua blizzard.j floors as Lua's `//`; `bj_MELEE_MINE_SEARCH_RADIUS` is the float 2000.0, `bj_cineFadeContinueRed` and
  `bj_MELEE_CLEAR_UNITS_RADIUS` floats; `ModuloInteger(1, 0)` raised.

## Destructables

Probe `probes/destructables.lua` on 3.0.1.24371, 11-08 (W2-4): MirrorImaged's probe map (D51, flat ground at 0, no
`war3map.doo`) started with `-loadfile` on a hidden desktop, single player; six runs as it grew, the lines they shared
the same each time. Its lines are in `probes/expected/destructables.txt`.

- Creating: `CreateDestructable` of a unit's id (`hfoo`) and of 0 gives nil; LTlt at variation 15 (it has 10) and -1
  makes one. `CreateDestructableZ` at (1000, 1000, z 300) stands at (960, 960) with 50 of 50 life.
- Where it stands, asked at (1000, 1000), (0, 0), (-10, -10), (10, -40), (63, 63), (64, -64), (-96, 96), (1536, -1536),
  (-1000, 1200), (31.9, 32.1), (-10.1, -0.3) and (100.6, -200.9), facing 270: LTlt (Summer tree, fixed rotation 270,
  pathing texture 4x4) and LTg1 (gate, 270, 20x4) at (960, 960), (0, 0), (0, 0), (0, 0), (0, 0), (64, -64), (-64, 64),
  (1536, -1536), (-960, 1152), (0, 0), (0, 0), (64, -192): the 64 grid toward 0. LTbr (barrel, free rotation, 2x2) at
  (992, 992), (32, 32), (-32, -32), (32, -32), (32, 32), (96, -96), (-96, 96), (1568, -1568), (-992, 1184), (32, 32),
  (-32, -32), (96, -224): the 64 grid toward 0, then 32 away from 0, 0 counting as above. YT16 (Long Stone Bridge, fixed
  0, 32x18) is a barrel across and a tree up; LT06 (fixed 90, the same texture) and XTmx (free, 8x2) a tree across and
  a barrel up. OTsp and YTc1 (no pathing texture) stay where asked but cut down to a quarter: 31.9 is 31.75, 32.1 is 32,
  -10.1 is -10.25, -0.3 is -0.5, 100.6 is 100.5, -200.9 is -201. XTmx at (0, 0) and (1000, 1000) by facing: at 0 and
  180 a barrel across and a tree up, at 30, 45, 60, 90, 135, 300, 360, -90, 0.5, 179.9, 270, 540 and -180 a tree across.
  So half the texture's cells each way decides, odd giving the 32, and the texture's sides swap at exactly 0 and 180.
  The deck probe's placements at facing 270 (Moonwell's `Decks.txt`: 162 walkable types with their textures' sizes)
  fit the same rule.
- A place of 0 reads `0.000` (`GetDestructableX`, `GetWidgetY` of a tree asked at -10, -10), and the place less 0 in
  Lua `-0.0` (the game's subtraction, D151).
- Life: a new LTlt 50 of 50 (`bhps`), `GetWidgetLife` 50; `SetDestructableLife` 20.5 reads 20.5, 999 reads 50, 0.405
  lives at 0.405; 0.4, 0.1, 0 and -5 kill it, its death event (`TriggerRegisterDeathEvent`) inside the call reading 0,
  `GetTriggerWidget() == GetTriggerDestructable()` true, the event id 89 (`EVENT_WIDGET_DEATH`). On the dead tree 20
  reads 20 and `KillDestructable` fires a second death; a third fires nothing. `RemoveDestructable` fires the death
  event again for a dead one (reading 0), and for one at 20 reading 20; afterwards its type id, life, most life and
  place read 0.
- Most life: from 50 of 50, `SetDestructableMaxLife` 80.7 gives 81 of 81, 80.5 81 of 81, 80.4 80 of 80, 10 10 of 10, 0
  0 of 0 and -5 0 of -5; from 20 of 50, 100 gives 40 of 100, 25 10 of 25, 10 4 of 10: the life keeps its share, and
  nothing died. A dead one (0 of 50) set to 70 is 0 of 70. A tree set to 0 then 50 is 0 of 50, and
  `KillDestructable` then fires nothing: it is dead though it never died.
- `DestructableRestoreLife` on a dead LTlt: 30 (with birth) 30, 0 50, 999 50, 0.3, 0.5 and -5 0.5; on a living one at
  20, asked 40 with birth, still 20. Killed, restored with 50 and killed again, it fires its death again.
- `CreateDeadDestructable` of LTlt: 0 of 50; `KillDestructable` fires nothing; restored with 0, 50 of 50.
- On a barrel (20), `SetWidgetLife` 10.5 reads 10.5 by `GetWidgetLife` and `GetDestructableLife`, `GetWidgetX`/`Y` read
  its place, and `SetWidgetLife` 0 kills it with its death event inside the call.
- `IsDestructableInvulnerable` is false for a new tree and true once set; the invulnerable tree still dies to
  `SetDestructableLife` 0 and, restored, to `KillDestructable`, each firing its death.
- `GetDestructableOccluderHeight` reads the type's `boch`: LTlt 230, LTbr, YT16 and OTsp 0; set to 100.7 it reads
  100.7, to -5 -5.
- `GetDestructableName` reads "Summer Tree Wall" (LTlt), "Barrel" (LTbr), "Long Stone Bridge" (YT16, editor suffix
  "Vertical" left off) and "Shimmering Portal" (OTsp), the strings of `UI\WorldEditGameStrings.txt` (enUS) for the
  types' `WESTRING_` names; `UI\WorldEditStrings.txt` doesn't hold them. `GetDestructableName(nil)` gives "".
- A tree hidden with `ShowDestructable(false)`, its animation set, queued and sped up, its colour and vertex colour set,
  and `SetDoodadAnimation`/`SetDoodadAnimationRect` called near it, kept 50 of 50 and was still found by
  `EnumDestructablesInRect`.
- A gate (LTg1, 500) opened as `ModifyGateBJ` does (`KillDestructable`, then the animation "death alternate") fires
  its death and reads 0 of 500; closed (`DestructableRestoreLife` to its most with birth, animation "stand") 500.
- `EnumDestructablesInRect` around a tree at (-1024, -1024) and a barrel at (-1056, -1056), both on the 32 grid: a
  centred rect of half-size 1, 16 and 31 finds nothing, 32 to 129 finds it; a rect from 300 south-west to `a` past it
  finds it for `a` from 32 up and not for 1 down; one from `a` to 300 north-east finds it for `a` from -1 down and not
  for 0 up; strips 1 wide across it find nothing. An OTsp at (-1000.5, -1000.5): rects (-1000.75, -960.5) and (-1001,
  -968.5) don't find it, (-1030, -990) does. So a rect finds a destructable when the 32 cell holding its place lies in
  the rect with its west and south edges strictly inside, not by the place itself.
- Five trees made at (1408, -1280), (1152, -1280), (1280, -1152), (1280, -1408) and (1280, -1280), in that order, are
  given to the filter as the 4th, 2nd, 5th, 3rd, 1st. Five made at (704, 576), (576, 704), (640, 640), (576, 576) and
  (704, 704), all in one square of 256, are listed 5th, 4th, 3rd, 2nd, 1st, whatever their places. Sixteen made in a
  grid at -320, -64, 64 and 320 each way, in an order unlike their places, are listed row by row from the south, west
  to east, and a tree at (0, 0), made long before, comes after the one at (64, 64), in its square. So the order is by
  squares of 256 world units, in rows from the south, west to east, the last made first in a square (squares of 128 or
  512 would give other orders). The callback came for those the filter passed, in that order, with
  `GetFilterDestructable` the destructable it was called for, as `GetEnumDestructable`; without a filter
  `GetFilterDestructable` is still set in the callback. Both are nil after.
- `RemoveDestructable` of a destructable inside its own death event fires the event again there, nested: a tree killed
  and a tree removed, each removed again in its death event while a count stayed under 3, fired three deaths each
  (reading 0 for the killed one, 50 for the removed one), and then read type id 0 and were no longer listed. In a
  destructable's death event `GetTriggerUnit` and `GetDyingUnit` are nil and `GetTriggerWidget() ==
  GetTriggerDestructable()`. A tree killed with a trigger that waits 1 s and restores it to its most life with birth
  read 50 of 50 3 s later.
- With no guard, `RemoveDestructable(GetTriggerDestructable())` in the destructable's own death action (World Editor's
  "Destructible dies -> Remove (Dying destructible)"), probe `destructables-remove`, run from the probe's first timer
  inside its `pcall`, two runs: in a Lua map the event fired 98 times for a tree killed and 98 for one removed, the code
  after the call ran and the tree read type id 0; with the removal inside a `pcall` in the action, 65 times, and the
  `pcall` caught no error; nothing more a second later. 98 and 65 fit Lua's 200 C calls, two a level and three with the
  `pcall`: the next callback doesn't start, and the game drops that. In a JASS map (`destructables-remove-jass`, each
  case in a thread of its own) the game closed before the probe's lines, two runs.
- `GetLocationZ` on flat ground at 0 around a YT16 made at (0, -1280), standing at (32, -1280): 286.5 at its place,
  277.5, 255.0, 247.1, 198.8 and 158.2 at 128, 256, -256, 512 and -640 north, 288.9 at 128 east, 0 at 256 east and
  west and 300 east: it runs north-south. An LT06 made at (-1280, 0), standing at (-1280, 32): 274.2 at its place and
  128 north, 0 at 256 north and south, 269.3, 251.2, 250.3 and 243.5 at 128, 256, -256 and 300 east: east-west.
  Killed, the YT16's deck reads the same in that instant, and 0 at all ten points 3 s later; restored without birth,
  as before 3 s later.
- A probe map with a `war3map.doo` written by War3Net 6.1.2 (format 8, subversion 11, the new layout), with destructables
  in each state or with no doodads at all, closed the game as it loaded (`ACCESS_VIOLATION` writing 0x0), 6 runs: with
  and without a `war3map.wpm`, with an empty `war3mapUnits.doo`, and with the terrain's corner count made to match its
  tiles; with a `war3map.shd` of 32 by 32 by 16 bytes the game said "Invalid shadow map file". The file's bytes match a
  World Editor file's layout. Not separated; the probe runs without a `war3map.doo`.

Read from the install's 273 maps (11-08):

- In `war3map.w3d`, the per-variation fields (`dvr1`, `dvg1`, `dvb1`) are changed at variations 1 to 10 (448 changes
  at 1, 18 at 10) and never at 0; every other field at 0. So World Editor numbers a doodad's variations from 1.
- `war3map.doo` is format 8 (subversion 11, the new layout) in most maps and 13 in some (Forgotten Hollow, Silverpine
  Sprint, GnollWood S2, WarChasers); states 0, 2, 3, 4, 6 and 7 occur, and SilverpineSprint's also 10 and 14 (bit 3),
  which War3Net refuses: 7,123 of state 2, 5,184 of 6, 6 of 10 and 785 of 14 among its 13,098 doodads.
- A destructable's pathing texture (`bptx`) is a TGA whose width and height are cells: `4x4Default` 4 by 4, `2x2Default`
  2 by 2, `CityBridgeLarge0` 32 by 18, `Gate1Path` 20 by 4, `8x2Default` 8 by 2; 16 types (OTsp, YTc1, Volc among them)
  have none.

## Units

Probes `probes/units.lua`, `units-decay.lua`, `units-attribute.lua` and `units-primary.lua`, on 3.0.1.24371, 11-08
(W2-2): MirrorImaged's probe map (D51: info format 28, the Default data set, not melee, so `custom_v1`'s data; flat
ground at height 0), started with `-loadfile` on a hidden desktop, single player. `units` (in its final form, after
the review added `rawequal` to the death lines) and `units-decay` ran twice each with the same lines, which are in
`probes/expected`; earlier forms of `units` ran six more times, and their lines that the final form keeps were the
same. The two closing probes each closed the game once, as the units probe's runs did at the same call.

- Making: `CreateUnit` gives nil for an id that is no unit's (`zzzz`, the item `phea`, the destructable `LTlt`, the
  ability `AHhb`) and for 0. A unit takes one handle id, a Paladin too (a timer made before and after it is one id off
  each way). Made at (1500.5, -1700.25) facing 30 it reads exactly that, `BlzGetUnitZ` 0 on the flat map. Made on a
  spot where other units stand, it is moved off them (48 and -112 from 0, in the first runs); made at (5000, -5000) on
  the 32 by 32 map, it stands at (2032, -2000). `BlzCreateUnitWithSkin(.., 'hfoo', .., 'hkni')` gives a footman (type
  id `hfoo`, name "Footman", 420 life, `UNIT_RF_SCALING_VALUE` 0.8) whose `BlzGetUnitSkin` is `hkni`. `CreateCorpse`
  gives a dead unit: type id, life 0, not alive, `UNIT_TYPE_DEAD`, most life 420.
- At creation: a footman has 420 life of 420, no mana, level 2, move speed 270 (default 270), turn rate 0.6, fly height
  0, acquisition range 500, propulsion window 1.047197 (`GetUnitPropWindow`, radians: 60 degrees made radians as
  `Deg2Rad` makes them) and 60 (`GetUnitDefaultPropWindow`, degrees), food 2 used, point value 100, race 1, user data
  0. A Paladin has 650 life (100 + 22 strength x 25), 255 mana of 255 (100 + 17 intelligence x 15, at most 255), level
  1, name "Paladin". A barracks reads level 0, move speed 0, turn rate 0.001 (its data has none;
  `GetUnitDefaultTurnSpeed` 0), acquisition range 0 (default 900, its sight: the data has none); a gryphon rider fly
  height 240 (default 240), level 5; a farm `hhou` food made 6. `GetUnitRace`: human 1, orc 2, undead 3, night elf 4,
  demon 5 (`ninf`), other 7 (`ncop`), creeps 8 (`nmrl`, `nfrl`), commoner 9 (`nvil`), critters 10 (`nshe`), naga 11
  (`nmyr`). `IsHeroUnitId` is true for `Hpal`, `Hzzz`, `Ozzz` and `Nngs`, false for `hfoo`.
- Facing: made facing 400 reads 40.00002, -90 270, 720 0, 359.9 359.9, -0.5 359.5, 1e9 0, 100000 280.0085, -100000
  79.99146, 3600.5 0.5002212. `BlzSetUnitFacingEx` 123.5 reads 123.5 at once, -30 330. `SetUnitFacing` reads the old
  facing at once and turns the short way round, half a turn going up: read every 1/16 s from the call, a footman (turn
  rate 0.6) from 0 to 180 read 0, 12.0, 35.0, 57.9, 80.8, 103.7, 126.6, 149.5, 172.5, 179.6, 179.9, 179.9, 180; a knight
  (0.5) 0, 6.9, 22.3, 39.5, then 17.2 a read, easing from 125 to 178.3 at 1 s; a gryphon rider (0.4) 0, 5.2, 16.0,
  then 11.5 a read to 179.1 at 1 s. Steady, that is 6.4, 4.8 and 3.2 radians a second. A footman whose turn rate was
  set to 0.2 turned exactly as at 0.6.
- Life: `SetWidgetLife` and `SetUnitState(UNIT_STATE_LIFE)` keep a fraction (100.5, 200.25) and stop at the most life
  (500 reads 420). The life left reads as the most life less the damage, in floats: 0.41 reads 0.4100037 on a footman
  of 420 and 0.4099731 on a Paladin of 650, 1.1 reads 1.100006 and 1.099976. Below 0.405 after that the unit dies:
  0.405 kills the footman (420 less 419.595 is under 0.405) but not the Paladin (0.4050293); 0.4049 and 0.4 kill both.
  `SetUnitState(UNIT_STATE_MAX_LIFE)` changes nothing; `BlzSetUnitMaxHP` 150 cuts the life to 150, 600 then keeps 150,
  0 kills, -5 reads -5; a life of 0.5 stays 0.5 when the most goes to 1000. A dead unit set to 100 reads 100 and stays
  dead; its most set to 50 cuts it to 50; -5 reads 0.
- Mana: a footman (no mana) set to 10 reads 0; given a most of 50 by `BlzSetUnitMaxMana`, its mana stays 0, 70 reads
  50, 20.5 then a most of 10 reads 10, -5 reads 0; `SetUnitState(UNIT_STATE_MAX_MANA)` changes nothing. A Paladin set
  to 300 reads 255, 100.5 reads 100.5.
- Flags: `PauseUnit` true twice and false once leaves `IsUnitPaused` false; `BlzPauseUnitEx(u, true)`, once or twice,
  never makes it true. `ShowUnit(u, false)` makes `IsUnitHidden` true and keeps the unit's place and life.
  `SetUnitInvulnerable` true and false read back through `BlzIsUnitInvulnerable`, with no crash (against w3ts's
  claim). `SetUnitUserData` reads back. `BlzSetUnitName` renames the one unit (`GetObjectName` and a new footman keep
  "Footman"). `SetUnitScale(2.5, 1, 1)` leaves `UNIT_RF_SCALING_VALUE` 0.8; `SetUnitVertexColor(300, -50, 100, 128)`
  leaves the tint fields 255. `SetUnitX`/`SetUnitY` to (5096.5, -300.25) read back, the unit alive.
- Speeds: `SetUnitMoveSpeed` 1000 reads 400, 5 and -10 150 (the constants' `MaxUnitSpeed` and `MinUnitSpeed`); a
  barracks keeps 0 whatever is set. `SetUnitTurnSpeed` keeps an angle from 0 below 2 pi, at least 0.001: 5 reads 5, -1
  5.283185, 7 0.7168145, 0 0.001. `SetUnitAcquireRange` 0, 7.5 and 50 aren't taken; 55 to 100 and 300.5 on a footman
  and 100 to 600 on a rifleman are, 30 on a rifleman and -5 aren't; a barracks and a sheep (no attack) keep 0.
  `SetUnitPropWindow(1)` reads 1. `SetUnitFlyHeight` reads the new height at once, on a footman (200) too, and with a
  rate of 100 (a gryphon rider at 100 set to 300 read 300 at once and 0.5 to 2.5 s later).
- `IsUnitType` (types 0 to 26): footman ground, attacks ground, melee; Paladin hero and the same; barracks structure
  and mechanical (not ground); gryphon rider flying, attacks flying and ground, ranged; peasant the footman's and
  peon; sheep ground only; ghoul the footman's and undead; mortar team ground, attacks ground, ranged; tauren the
  footman's and tauren; haunted gold mine structure, undead and mechanical; wisp ground and peon; a dead footman also
  dead.
- `GetObjectName` reads `hfoo` "Footman", `Hpal` "Paladin", `phea` "Potion of Healing", `AHhb` "Holy Light", `Rhme`
  "Iron Forged Swords", and "Default string" for the buffs `Bdef` and `BHbd`, the destructables `LTlt` and `YTlb`,
  `ZPfw`, `zzzz` and 0.
- Death: `KillUnit` fires, inside the call, the unit's `TriggerRegisterDeathEvent` events first (event id 89,
  `GetTriggerUnit` and `GetTriggerWidget` the unit, the widget `==` the unit but not `rawequal` to it, `GetDyingUnit`
  nil, `GetTriggerPlayer` nil), then its owner's
  `EVENT_PLAYER_UNIT_DEATH` events (20; `GetTriggerWidget` nil, `GetDyingUnit` the unit, `GetTriggerPlayer` the owner),
  then its `EVENT_UNIT_DEATH` events (53, as the player's), each kind in the order registered, though they were
  registered unit, player, widget, unit; life 0, not alive, `GetKillingUnit` nil in all. A player-unit event's filter
  never ran, and one returning false didn't hold the event back. Another player's registration didn't fire. A second
  `KillUnit` fires nothing. `SetWidgetLife(u, 0.3)`, `SetUnitState(u, UNIT_STATE_LIFE, -5)` and `BlzSetUnitMaxHP(u,
  0)` each fire the death inside the call, life 0.
- Decay (`units-decay`, polled every 0.25 s from a kill at 0.1): a footman's decay event came at 3.14 (its death time
  3.04 later), the owner's `EVENT_PLAYER_UNIT_DECAY` (21) before the unit's `EVENT_UNIT_DECAY` (54), `GetDecayingUnit`
  and `GetTriggerUnit` the unit, `GetTriggerPlayer` the owner; a rifleman's at 1.80 (1.7). A barracks fired no decay
  and was gone at 32.50 (2.34 + `StructureDecayTime` 30); a gryphon rider, whose death type doesn't decay, fired none
  and was gone at 2.00 (1.67 later). An exploded footman (`SetUnitExploded`) and a dead one removed were gone at the
  first poll. A Paladin, a footman whose decay was suspended after its death (its decay event still came at 3.14) and
  a `CreateCorpse` corpse (no decay event) were still there at 45 s, dead, life 0. Decayable and raisable follow the
  death type (`deathType` 3 both, 2 decays, 0 neither).
- Owner (`SetUnitOwner(u, Player(1), true)` on a living footman): its stop (851972) fires first, the owner's
  `EVENT_PLAYER_UNIT_ISSUED_ORDER` (38) then the unit's `EVENT_UNIT_ISSUED_ORDER` (75), reading the old owner, with
  `GetOrderedUnit` the unit; then the change, the new owner's `EVENT_PLAYER_UNIT_CHANGE_OWNER` (270) then the unit's
  `EVENT_UNIT_CHANGE_OWNER` (287), `GetChangingUnit` the unit, `GetChangingUnitPrevOwner` the old owner and
  `GetTriggerPlayer` the new; then the footman's `undefend` (852056, its Defend) on the new owner's and the unit's
  order events. Given to the owner it has, nothing fires. A dead footman given away fires no stop and its change; the
  `undefend` line before it is the footman's death (its `KillUnit` came after the player's order registration and
  before its own; read so in W2-10, against W2-2's reading).
- Removal: a removed unit reads, in the same callback, its type, life 420, alive and owner; a dead one removed reads
  dead. After a 0 s timer the dead one reads type 0. A living unit removed and then killed in the same callback fires
  its death (unit and widget events) and stays: after the 0 s timer it reads its type id, life 0, not alive, its new
  owner (given away after the removal) and its name.
- Fields read on a footman, Paladin, barracks and gryphon rider (and set on the first three to 7, 7.5, the other
  boolean, or "Probe"): integer fields give the words of the data as common.j's constants (armour type: Metal 2, Stone
  5, Flesh 1; defence type: large 2, hero 5, fort 3, small 0; regeneration: always 1, none 0; move type: foot 1, fly 2,
  none 0; targeted as: ground 2, structure 8, air 4; classification: mechanical 8; placement prevented by: the
  barracks' `preventPlace` unbuildable, 8), and a set reads back, but for the tint fields (255) and the primary
  attribute (1, also for the Paladin set to 2), which a set doesn't change; a hero's level set to 7 reads 8 and to 3
  reads 4; the gold bounty, formation rank, elevation points and looping fade rates read the data. Real fields: life
  (`UNIT_RF_HP`), mana, move speed, turn rate, acquisition range and fly height are the unit's (a set as their natives
  set them: speed 7.5 reads 150, turn rate 7.5 1.216815; a footman's fly height set to 50 stays 0, a gryphon rider's
  set to 100 reads 100, and its most fly height set to 50 then makes it 90, its least); armour (`UNIT_RF_DEFENSE`) 2
  for the footman and 3.9 for the Paladin (2 - 2 + 13 agility x 0.3); sight 800, 800, 600, 900 (the night sight: the
  game starts at midnight); the gains per level read 0 and refuse a set (false) on a unit that isn't a hero; the most
  pitch and roll read 0 and refuse every set; the least attack range refuses the barracks (no attack); the shadow's
  size and the sight take a set (true) but keep their value. Boolean fields read the data (a neutral building's minimap
  icon true where the data has none) and every set reads back but hiding on the minimap (stays false). String fields:
  names; the Paladin's proper name is one of its type's drawn at random and numbered ("Granis Darkhammer XII", "Zann
  the Defender XII" in two runs); a footman's is "", and a set leaves it "".
- The hero attributes (`UNIT_IF_AGILITY` and the rest, which have no metadata rows) on a Paladin read 13, 17 and 22;
  set, the attribute reads the value, its permanent one reads 1 (after a set of 7, and of 0, which also left agility
  1), and the one with bonus refuses a set; strength set to 30 makes life and most life 850. Reading `UNIT_IF_AGILITY`
  on a footman closes the game (probe `units-attribute` and the units probe's first two runs), and so does setting a
  footman's primary attribute to 3 (`units-primary` and the units probe's third run); 7 is refused there.

## Cameras

Probes `probes/cameras.lua`, `cameras-2.lua`, `cameras-3.lua`, `cameras-4.lua`, `units-camera.lua` and
`units-camera-2.lua`, on 3.0.1.24371, 11-08 (W5-1): MirrorImaged's probe map (D51: 32 by 32, flat ground at height 0, a
start location at (0, 0), and a `main` that sets no camera bounds), started with `-loadfile` on a hidden desktop,
single player. Each ran at least twice (`units-camera` first as `cameras-units`), the first four probes three or four
times; their lines are in `probes/expected`, where the numbers that varied, or that a pan or a change over a time reads
part way, are marked. Angles are read with `%.4f`, in radians.

- The margins: `GetCameraMargin` reads 512 for `CAMERA_MARGIN_LEFT` and `RIGHT`, 256 for `TOP` and `BOTTOM`, and 0 for
  4 and -1. All 466 shipped maps' `main` sets bounds that, with these margins, are the camera bounds of their
  `war3map.w3i` (read from the install).
- The bounds: they read 0 each way until the script sets them, though the probe map's `war3map.w3i` has bounds of
  (-768, -1280) to (768, 768); and while they are 0 the target stays at (0, 0), whatever is set. `GetCameraBoundMinX`
  and the rest read the least and most of the four corners given to `SetCameraBounds`, in any order, a diamond too,
  and they aren't cut to the map (100000 reads 100000).
- The game camera at the start: distance 2000, far z 5300, angle of attack 304 degrees (5.3058), field of view 70
  (1.2217), roll -0, rotation 90 (1.5708), z offset 10, near z 16, the local pitch, yaw and roll -0, the depth of field
  fields 0 and z absolute 0; the target at (0, 0) and height 10, the eye at (0, -1118.3857, 1668.0752). Every field
  reads controlled by input, the camera type 0, the hotkey lock true; each reads back a set.
- Fields read back at once when set without a time, the angles in radians. The limits: distance 0.5 to 10000, far z
  from 100 (100000 kept), the field of view 5 to 120 degrees, the z offset -4096 to 4096, near z 16 to 9500 (9500 at a
  distance of 3000 and at a far z of 2000 too), the depth of field fields from 0, z absolute as set (0.5, -1, 2). An
  angle comes round to a turn: 400 reads 40 degrees, -30 330, 361 1; a whole number of turns reads 2π above 0 (360,
  720, 1080 and 359.9999) and -0 at 0 and below (0, -360, -720); the field of view comes round first, then is held, so
  361 reads 5 degrees. `ResetToGameCamera` puts back every field but z absolute, and leaves the target.
  `AdjustCameraField` adds to the field at once.
- A field set over a time moves in a straight line from where it is, reading the old value in the same instant (a
  distance of 2000 to 3000 over 2 s read 2250.97 at 0.5 s and 2499.93 at 1 s); an angle goes the shorter way round
  (350 to 10 degrees over 1 s read 0.06 degrees at 0.5 s). `ResetToGameCamera` over a time does the same.
- The eye reads as the game last drew it: a rotation set to 45 left the eye where it was in the same instant and in a
  0 s timer, and moved it by 0.05 s, to (-790.8182, -790.8182, 1668.0752). It is the distance back from the target
  along the angle of attack and the rotation (with distance 1650 and z offset 50, (0, -922.6683, 1417.9121)); at 300
  degrees the game's cos reads 0.5000004 (the eye at -500.0004 for a distance of 1000). The target's height is the z
  offset over the flat ground; the eye's z is the target's less the distance times the sine of the angle of attack.
- A position set mid-game lands within 0.05 s (`PanCameraToTimed(.., 0)`), and as the game starts 0.2 to 0.25 s later
  (0 at 0.2 s, 499.9935 at 0.25 s, 500 at 0.4 s). A timed pan moves in a straight line (52 % of the way at 1 s of
  2 s), `PanCameraTo` at about 3000 a second whatever the distance (1100 and 2000 measured), and `StopCamera` stops a
  pan where it is. A position or a pan sets the target's height back to the z offset; `PanCameraToWithZ` and
  `PanCameraToTimedWithZ` move the target's height to the z given (199.9994 of 200 1 s later, 300), while the z offset
  field reads 10; inside bounds of 0, where the target can't move, `PanCameraToWithZ` left its height alone.
- The bounds hold the target in their box: a position outside bounds of 200 each way read (200, 200), (-200, -200),
  (0, 200), (200, 0) at rotations 0, 45, 90 and 120 and distances 1000 to 3000, and a diamond held (900, 900) where its
  box allows it. One case reads otherwise in every run of the cameras probe: after pans with a z made while the
  bounds were 0, then bounds of 200 each way and a position at (1000, 1000) at rotation 120, it read (100, 373.2051),
  200 along the view past the corner; cameras-2 and cameras-3, without the earlier pans, didn't repeat it.
- A camera blocker (`AddCameraBlocker` of a rect from (200, -200) to (600, 200)) keeps the target out: a position set
  at (400, 0) from (0, 0) read (600, 0), past it along the move, and a pan from (0, 0) to (1000, 0) over 1 s stopped
  at (200, 0), where it entered. `EnableCameraBlocker(r, false)` let a position at (400, 0) in.
- Camera setups: a new one reads distance 3000, far z 5400, angle of attack 304, field of view 70, roll 0, rotation
  90, z offset 0, near z 16 and 0 for the rest, in degrees as set; its place (0, 0) and label "". A field reads back as
  set, angles as given (400, -30, 720), the distance, far z, near z and depth of field fields below 0 as 0 (near z 0,
  not 16); the place and label too. `BlzCameraSetupGetCameraType` reads a number of its own each run (1367777792,
  -1842721280, -251714816, ...), the same before and after `BlzCameraSetupSetCameraType`.
- Applying a setup sets every field, each over the time it was set with, `doPan` or not; `doPan` moves the target to
  its place, over the place's time when `panTimed` (0 lands within 0.5 s), else at about 3000 a second. Without
  `doPan` the target stays. `CameraSetupApplyForceDuration` does it all over the time given; with `WithZ` and a time,
  the target's height goes to the z with the pan (125.7 half way, 250), with no time it stays the z offset (0).
  `CameraSetupApplyWithZ` pans as an untimed `CameraSetupApply` and leaves the height at the z offset.
- `SetCameraRotateMode(500, 0, π/2, 2)` with the target at (0, 0): the target goes round (500, 0) counterclockwise to
  (500, -500) in 2 s, at a height of 0 (no z offset), while the rotation field reads 90 degrees; the eye turns with it,
  (1618.3857, -499.9999, 1658.0752) at the end. A position set then and `ResetToGameCamera` in the same instant left
  the target there, at height 10 again. Over 1 s (units-camera-2): a position set after the sweep left the target
  where the turn had it, at height 0; one set half way through stopped the turn there (it moved 0.06 more) and held it;
  `StopCamera` half way did the same, the height staying 0; a unit controller given half way was dropped, the turn
  going on to (500, -500); a timed pan after the sweep moved the target, at height 10 again, and a setup's timed pan
  moved it, at the setup's z offset of 0.
- `CameraSetSourceNoise` and `CameraSetTargetNoise` of 50 move the target and the eye a little and at random: in one
  run the target read 0.1 to 0.3 off (800.12, 399.71, 249.73 for (800, 400, 250)) and the eye 0.3 to 0.6, in the other
  two the target up to 2.8 and the eye up to 5.6. `SetCameraQuickPosition` changes nothing read.
- `CameraSetFocalDistance(500)` and `CameraSetDepthOfFieldScale(2)` read back at once as the depth of field fields,
  500 and 2. `CameraSetSmoothingFactor(1)` left `PanCameraTo`'s target as without it (300 at 0.1 s, 903 at 0.3 s).
- `SetCinematicCamera("Objects\\CinematicCameras\\CameraZoomOut.mdx")` moves the target and the eye to the model's
  camera at once: the target (0.0242, 0, 109.40), the eye (203.93, 0, 116.25), then, moving, the target about
  (0, 0.0242, 119.40) and the eye (0, 213.38, 131.21) at 0.5 s and (0, 251.09, 151.02) at 2.5 s; the fields read as
  they were. `ResetToGameCamera` ends it: the target (0, 0.0242, 10), the eye as the game camera's.
- A pan's height: `PanCameraToTimedWithZ(.., 300, 0.5)`'s target read 300 a second later, and 10 after
  `ResetToGameCamera`; following a unit after such a pan kept it (-600, -600, 300); `StopCamera` half way through one
  to 410 read 210.57 in the same instant and 10 a second later.
- `SetCameraTargetController(u, 100, 50, false)` puts the target at the unit and the offsets from the next frame, and
  it follows the unit's moves a frame later; after `RemoveUnit` it stays where the unit was. `PanCameraToTimed`,
  `ResetToGameCamera` and `SetCameraPosition` at the target end it. `SetCameraOrientController(u, 0, 0)` moved the
  target to the unit at those offsets; neither it nor `inheritOrientation` nor the unit's facing changed the rotation
  field.
- The fog flags: `IsFogEnabled` and `IsFogMaskEnabled` read true at the start, then what was set.
### The rest of the units (W2-10)

Probes `probes/units-more.lua`, `units-more-2.lua`, `units-types.lua`, `units-bones.lua` and `units-gold.lua`, on
3.0.1.24371, 11-08 (W2-10), on the same probe map, started the same way. `units-more` ran five times and `units-more-2`
six, the last two of each the same to the last digit; `units-types` three times, the same; `units-bones` twice, the
same (it takes 94 s of the runner's 2 minutes); `units-gold` three times, the last two the same (the first had one line
less). The players 0 to 15 are made passive to each
other in `units-more` and `units-more-2`: before that, footmen given to Player 1 fought Player 0's and died at 9 to 12 s.

- Names: `UnitId` reads `unitui.slk`'s `name` in any case ("footman", "Footman" and "FOOTMAN" are `hfoo`; "paladin" is
  `Hpal`, the first of the two types with that name in the tables, not `Hssa`); an id ("hfoo"), "" and an unknown name
  give 0. `UnitId2String` gives the name, and "" in Lua for an item (`phea`), an ability (`AHhb`), `zzzz` and 0.
  `CreateUnitByName` makes the unit by that name in any case, where asked and facing as given; nil for "hfoo".
- `IsUnit(u, u)` and `IsUnit(nil, nil)` are true, `IsUnit(u, v)`, `(u, nil)` and `(nil, u)` false. `IsUnitIllusion` is
  false for a footman, a Paladin, a dead footman and nil. `BlzIsUnitSelectable` is true for a living footman, a Paladin,
  a paused footman and a barracks; false for a dead one, a `CreateCorpse` corpse, a hidden one, one removed in the same
  callback, and nil.
- `UnitIgnoreAlarm` on a barracks or a town hall answers true and `UnitIgnoreAlarmToggled` then reads the flag set (true,
  then false again); on a footman it answers false and the toggle stays false; false at first and for nil.
  `SetUnitCreepGuard`, `UnitSetUsesAltIcon`, `BlzShowUnitTeamGlow` and `SetPlayerUnitsOwner(p, 14)` change nothing read:
  the last gave no unit away, living or dead, and fired no order or owner event.
- `IsUnitIdType` (types 0 to 26) reads the type as `IsUnitType` reads a unit, but ground (4) and dead (1) are never true:
  `hfoo` 6 and 7, `Hpal` 0, 6 and 7, `hbar` 2 and 15, `hgry` 3, 5, 6 and 8, `hpea` 6, 7 and 16, `ugho` 6, 7 and 14,
  `hmtm` 6 and 8, `otau` 6, 7 and 20, `ugol` 2, 14 and 15, `ewsp` 16, `hrif` 5, 6 and 8, `nmrl` and `hkni` 6 and 7;
  `nshe`, `zzzz`, `phea` and 0 none, and false for a nil type.
- `UnitAddType` on a footman answers true for giant, summoned, undead, mechanical, peon, sapper, townhall, ancient and
  tauren (9, 10, 14 to 20), which `IsUnitType` then reads and `UNIT_IF_UNIT_CLASSIFICATION` holds (1, 4, 2, 8, 16, 32,
  64, 128, 2048), on that unit only (its type and another footman unchanged); `UnitRemoveType` answers true for them and
  takes them off, a barracks' own mechanical too. Every other type answers false both ways and changes nothing, the ones
  the footman has (ground, attacks ground, melee) too, and so do nil units and types.
- Food (Player 9's states, a player no one plays): a footman made adds 2 to `PLAYER_STATE_RESOURCE_FOOD_USED`, a Paladin
  5; a farm 6 and a town hall 12 to `PLAYER_STATE_RESOURCE_FOOD_CAP` (108 with 18 farms, above the ceiling of 100); each
  change fires the player's state events inside the call. A death gives the food back, before the death events; a
  hero's too. A removed footman keeps its food until it is gone after the step. `CreateCorpse` adds the food and takes it
  back (two events). `SetUnitOwner` moves it to the new owner. `SetUnitUseFood(u, false)` takes the food used back once
  (again does nothing), true adds it; a farm's food made stays. A state set to 50 is added to (52). `GetFoodUsed` of
  `hfoo` 2, `Hpal` 5, 0 0; `GetFoodMade` of `hhou` 6, `htow` 12.
- Counts (Player 11): `GetPlayerUnitCount` counts the living units that aren't structures, a hero, a hidden and a paused
  one too, and a removed one until it is gone; `GetPlayerStructureCount` the living structures; `includeIncomplete`
  changes nothing without construction. `GetPlayerTypedUnitCount` reads the type by `UnitId`'s name ("Footman" too,
  "hfoo" 0), with `includeUpgrades` the types it upgrades to as well ("townhall" 1, with upgrades 3 for a town hall, a
  keep and a castle; "keep" 2). `GetPlayerTechCount` of a unit type counts its units the same way, `specificonly` false
  adding the upgrades (`htow` 1 and 3). A dead unit and a corpse don't count; a unit given away counts for its new owner.
- Timed life: `UnitApplyTimedLife(u, 'BTLF', 1)` kills 1 s later (the death events, killer nil), and the body decays its
  death time later as any other; applied again, the sooner end holds (3 then 1 s, and 1 then 2 s, both 1 s); 0 and -1
  never kill; a dead unit gets nothing; a Paladin dies too; buff 0 and `BHwe` kill too, but `BHwe`'s footman had no decay
  event and got its body's `undefend` at its decay time. It makes the unit summoned: `IsUnitType` and the classification
  (4), also with 0; `UnitRemoveType` takes that off. `UnitPauseTimedLife` true at 0.5 s left and false 1.5 s later: it
  died 0.5 s after; true before any timed life does nothing. `BlzUnitCancelTimedLife` kills at once, paused or not, and
  does nothing without one. A timer due in the step of the deaths ran before them; a removed unit never died.
- Unit state events (`EVENT_UNIT_STATE_LIMIT` 59, `GetTriggerPlayer` the owner, `GetEventUnitState` the state, life 0
  and mana 2; outside an event the state with id 0): a life set or death fires inside the call when the life goes from
  failing the comparison to passing it (`LESS_THAN` 200: 420 to 150 fires, 100 not, 250 then 150 fires again, 420 to
  199.5 fires; `GREATER_THAN` 300: 350 fires; `GREATER_THAN_OR_EQUAL` 420: 400 then 420 fires; a `KillUnit` from 420
  fires `LESS_THAN` 200 reading life 0). `EQUAL` 500 (a set to 500), `NOT_EQUAL` 300, the most life above 700
  (`BlzSetUnitMaxHP` 800) and the most mana below 200 (`BlzSetUnitMaxMana` 150) never fired; nor did a registration made
  while its comparison already passed. Mana (`LESS_THAN_OR_EQUAL` 100, from 255 to 100) fires inside the set and once
  more in the next frame (0.0549 s after a set at 0.0500), also after a later life set, then not again; one set to 100,
  50, 150 and 100 in one callback fired twice inside and once in the frame.
- `TriggerRegisterFilterUnitEvent`: an owner change fires with a nil filter and with one that passes, the filter running
  first; one that fails holds it back. A death never fires, with a nil filter, a passing or failing `Filter` or a
  `Condition`, and the filter doesn't run.
- Order events (a footman has Defend, a rifleman and a Paladin none): `PauseUnit(true)` fires the stunned order 851973
  as a point order (39 then 76), the unit already paused, then `undefend` (38 then 75); paused again, nothing.
  `PauseUnit(false)` fires `undefend` a frame later (0.01 s, twice), the unit unpaused; again, nothing; a rifleman's pause
  fires only the stunned order and its unpause nothing; paused again before that frame, the `undefend` comes at once,
  paused, with no stunned order. A paused footman given away fires no stop and no `undefend`, only its owner change;
  unpaused, it gets a stop (851972) a frame later on its new owner's registration instead of `undefend`. A death
  (`KillUnit`, a life of 0, timed life) fires `undefend` on the owner's and the unit's order events, the unit dead with
  life 0 and unpaused, then the death events; a dead unit's pause and a second kill fire nothing. `RemoveUnit` of a
  living footman fires nothing inside the call; after the callback, in the same frame, `undefend` twice, reading dead,
  life 0, unpaused and its type id; again for a removed paused one; a dead one removed gets it once, an exploded one once
  after the step (and once at its death). `CreateCorpse` fires `undefend` on its owner's registration inside the call,
  and no death event.
- Bones (`units-bones`, polled every 0.25 s, the kills at 0): a footman decays at 3.04 and gets `undefend` at 93.04,
  gone by 93.25; a rifleman decays at 1.70 and is gone by 91.75; a footman whose timed life ended at 1.00 decays at 4.04,
  `undefend` at 94.04; a `CreateCorpse` corpse gets `undefend` at 90.00 and is gone by 90.25 (no decay event); two
  footmen whose decay was suspended right after the kill decayed at 3.04 and went at 93.04 as the others, one of them let
  go only after that. So a body goes `DecayTime` 2 plus `BoneDecayTime` 88 after its decay, and a corpse that long after
  it is made; `UnitSuspendDecay` there changed nothing.
- Rescue: a neutral passive footman `SetUnitRescuable` by Player 0, with a footman of Player 0 made 100 away at 1.40,
  fired `EVENT_PLAYER_UNIT_RESCUED` (19) on its owner's registration (`GetTriggerPlayer` 27, not Player 0's) then
  `EVENT_UNIT_RESCUED` (63) at 1.90, `GetRescuer` Player 0's footman, and stayed neutral passive (blizzard.j's action
  gives it away); one with `SetUnitRescueRange` 50 and one 800 away weren't rescued in 10 s. `GetRescuer` is nil outside.
- Sleep (the game starts at midnight, night until 6): a neutral hostile murloc (`nmrl`, `canSleep` 1) made at night is
  asleep at once (`UnitIsSleeping` and `IsUnitType(UNIT_TYPE_SLEEPING)`), dead, a corpse, hidden or paused too; a
  neutral hostile footman (`canSleep` 0), Player 0's murloc and a neutral passive one never sleep (`UnitCanSleep` true for
  Player 0's murloc). `UnitWakeUp` wakes it at once; it is asleep again by 2.05 s later, not at 2.0. `UnitAddSleep(u,
  false)` wakes it at once and `UnitCanSleep` reads false; true at night puts it to sleep at once. `UnitCanSleepPerm`
  reads false, also after `UnitAddSleepPerm(u, true)`, which changed nothing. `PLAYER_STATE_NO_CREEP_SLEEP` of neutral
  hostile is 0; set to 1 it woke none. Setting the time to 12 wakes every living unpaused one at once (a dead, a corpse
  and a paused one stay asleep), to 23 puts them all to sleep at once, one made at noon too. Running from 17.95, they
  fell asleep 0.2 to 0.25 s after 18 (by 8.75 s at 18.012); from 5.95 they woke as long after 6.
- Gold (`units-gold`): `GetResourceAmount` of a new gold mine (`ngol`, whose `Agld` has Max Gold `Gld1` 12500) is 12500;
  of a haunted (`ugol`) or entangled (`egol`) mine, a footman, a barracks and nil 0. `SetResourceAmount` and
  `AddResourceAmount` keep any amount on a mine (5000, 5250, -750, -5; 2147483647 plus 1 is -2147483648), and 0 doesn't
  kill it (alive 1 s later); a haunted mine keeps 300 and an entangled one 400, a footman and a barracks nothing (0). A
  killed mine reads 12500 in its death event and after, and keeps a set (70).
## Sounds

Probes `probes/sounds.lua` (seven runs) and `sounds-2.lua` (seven runs, the last with a line checking the sound stopped
with a fade), on 3.0.1.24371, 11-08 (W5-2): MirrorImaged's probe map started with `-loadfile` on a hidden desktop from
the PowerShell tool, single player, every volume group set to 0 first (`VolumeGroupSetVolume`) and the music stopped, so
nothing was heard. The lines are in `probes/expected`; the runs gave the same lines but where each says. The paths are
the install's (W5-2 read them from its storage): most sounds are stored three times, a classic file under the language's
folder or none (`_Locales/enUS.w3mod:`, `.ogg` or `.mp3`), a Definitive Edition one (`_DE.w3mod:`, `.flac`) and an HD
one (`_HD.w3mod:`, `.ogg`), of other lengths.

- `GetSoundFileDuration` reads the classic file: `Units\Human\HeroPaladin\HeroPaladinWhat1` 1288 (the classic ogg's
  length; the DE flac and HD ogg are 1788) asked for as `.wav`, `.flac`, `.ogg`, `.mp3` and with no extension, and as
  `units/human/heropaladin/heropaladinwhat1.wav`; `HeroPaladinWhat2.flac` 1184 (DE 1684);
  `Sound\Interface\MouseClick1.flac` 239 (DE 238); `Sound\Interface\QuestNew.wav` 3750 and
  `Sound\Music\mp3Music\Combat1.ogg` 107050 (only a classic ogg); `Abilities\Spells\Human\HeroicSlash\HeroicSlash0.ogg`,
  stored only for DE and HD, 1291, the DE file's. 0 for `Sound\NoSuchFile.wav`, `""`, and the labels `QuestNew` and
  `Comradeship`. An ogg's length is its last page's granule position over its sample rate, in whole milliseconds cut
  toward zero: every ogg read matches.
- An mp3 (the classic music and the classic English speech): `PH1.mp3`, stored only as one, and `PH1.flac` 281600, 10780
  frames, where the file holds 10779; `H01Arthas02` (asked as `.flac`) 4493, 172 frames, the file 171. Of the 44 music
  mp3s and 17 speech mp3s read in `sounds-2`, 27 read one frame more than the file holds, 12 the frames it holds, and 22
  another count: 1 frame fewer (5 files), 2 fewer (3), 5 fewer (Mainscreen), 6 to 8 fewer (Human1 to Human3, NightElf1
  to NightElf3, Orc2, Orc3), 29 to 31 fewer (Orc1, Undead1 to Undead3), 2 more (LichKingTheme). Every file walks frame
  to frame to its end (or to an ID3v1 tag), at one bit rate; why the game's counts differ isn't found.
- A sound takes the next handle id (a timer made before it and one after: 1 and 1). `GetSoundDuration` is its file's
  `GetSoundFileDuration` from when it is made, and stays so after `SetSoundDuration(s, 3000)`, `SetSoundDuration(s, -5)`
  and `SetSoundParamsFromLabel(s, "QuestNew")`, and while and after it plays. `CreateSoundFilenameWithLabel(path, ...,
  "QuestNew")` reads the file's 1288. `CreateSoundFromLabel("QuestNew", ...)` 3750 and `CreateMIDISound("RoosterSound",
  20, 20)` 2935 and `("LordaeronSummerDay", 20, 20)` 117210, their labels' files; a label the tables don't have gives a
  sound of 0, not nil. `HeroPaladinWhat`, whose files are named without their folder in `UnitAckSounds.slk`
  (`HeroPaladinWhat1,...,HeroPaladinWhat4`), read 1184 or 1288 at random, the same six draws in all seven runs (1184
  1288 1184 1184 1288 1288); `HeroPaladinPissed` 2536. A nil sound: 0, not playing, not loading.
- `GetSoundIsLoading` was false in every read, also in the instant of `StartSound`.
- `GetSoundIsPlaying` is false before `StartSound` and true in its instant; never for a sound of a missing file; false
  at once after `StopSound` without a fade. With a fade in `StartSound`'s instant, true in that instant and false 0.6 s
  later (every run of `sounds`); with a fade 0.5 s after `StartSound`, it read playing on to its end, stopping with the
  sound left alone or one poll (0.05 s) before it in all seven runs of `sounds-2`. A looping sound played on.
  `KillSoundWhenDone` on a sound playing made it read not playing at once, and its length still read 1288 4.5 s later;
  on a sound not started it destroyed it (length 0). `StopSound(s, true, false)` kept the length. A second `StartSound`
  in the same instant, or 0.6 s in, didn't change when it stopped reading playing; one after it had stopped played it
  again. The workspace's facts heard otherwise (09-29, by ear): `StartSound` on a playing sound cut it off and nothing
  played. Read here, it plays on to its first end; what is heard then (muted here) isn't separated. A 3D sound at the
  origin, stopping out of range, with no distances set, never read playing.
- How long a sound reads playing isn't steady. In every run of `sounds` whose lines were kept (not the second's before
  2.0 s, nor the fifth's timed reads), a 1.288 s sound read playing at 2.0 s and not at 4.5 s, and one played from 1000
  ms (`SetSoundPlayPosition` after `StartSound`) at 0.6 s; at 2.0 s the one played from 1000 ms read playing in the
  fourth run, the one at pitch 2 in the fourth, sixth and seventh, and neither in the rest. Polled every 0.05 s in
  `sounds-2`, the same sounds stopped all together at 4.95, 5.05, 5.25, 4.90, 4.85, 4.90 and 4.95 s in its seven runs,
  the one played from 1000 ms about 1 s sooner (3.70, 4.05, 4.30, 3.65, 3.65, 3.65 and 3.80 s), the one at pitch 2 0.65
  s sooner (4.30, 4.45, 4.65, 4.30, 4.30, 4.30 and 4.30 s), and QuestNew's 3.75 s and one at pitch 0.5 still played at 6
  s; a play position set before `StartSound` changed nothing. So a sound's start waits a varying while (0.8 to 1.4 s in
  `sounds`, by its reads; 3.6 to 4.0 s in `sounds-2`, from when the sounds stopped), and it then plays its length over
  its pitch; `SetSoundDuration` (3000, 500, 0, -5) changed nothing.
- `TriggerWaitForSound` in a trigger action, from an action started at 6.5 s (`sounds-2`): a 1.288 s sound started in
  the same instant waited 1.5 s, once it had stopped 0.2 s, with offset 0.5 1.0 s, with `SetSoundDuration(s, 3000)` 1.5
  s, a sound never started 0.2 s, with offset 2 0.2 s, nil 0.1 s, and QuestNew started then waited with offset -1 0.2 s;
  the same in its seven runs but the first 1.4 s in the fifth to seventh, offset 0.5's 0.9 s in the third, fifth and
  seventh, and nil's 0.2 s in the fourth. In `sounds`, from 5.0 s: a 1.288 s sound 1.5 s (1.4 in the sixth and seventh
  runs), then offset 1.0 on a new one 0.5, 0.6, 0.5, 0.5, 0.5 and 0.4 s, and a sound never started 0.3, 0.3, 0.3, 0.2,
  0.2 and 0.2 s, in runs one to four, six and seven (the fifth run's first wait read 1.5 s and its others weren't read).
- `GetDialogueSpeakerNameKey` and `GetDialogueTextKey` read "" on a new sound and, after `SetDialogueSpeakerNameKey(s,
  "TRIGSTR_70")` and `SetDialogueTextKey(s, "Some text")`, which give true, what was set; on nil they read "" and the
  sets give false. `SetSoundFacialAnimationLabel`, `SetSoundFacialAnimationGroupLabel` and
  `SetSoundFacialAnimationSetFilepath` give true on a sound and false on nil.
