# Facts

Game behaviour measured for MirrorImaged, in the style of the workspace's `docs/game-facts.md`. **Seen** = a run showed
it; **inferred** = reasoned from what was seen.

## The game runner

All on 3.0.1.24371, one PC (Windows 11), single player, with the Battle.net app running and nobody at the keyboard
(Sven was watching from 02:20 to 02:35), maps started with `-launch -windowmode windowed -loadfile "<map>"`, 11-08
(W0-4) unless marked.

- Login: a `-loadfile` launch logs in to Battle.net by itself, with nothing typed (Sven, 11-08, had seen a login prompt
  that morning). Seen in the pictures of every run in a normal window (`PrintWindow`, 2 so far): a door, then a "Login
  Queue" box ("The Blizzard servers are busy right now! You're position 1 in the queue (estimated time: 5s).", with a
  Cancel button) for 2 to 5 s, then the map's loading screen, "Loading...", then "Press any key to continue". In every
  run whose `War3Log.txt` was written out (about 15), `[CLoginCallbacks] LoginDoorClose called` came 5 to 6 s after the
  start, before `Activating WebUI`. Once logged in, 10 to 14 s after the start, the game rewrites `War3Preferences.txt`
  and the account's `BattleNet\<account>\Campaigns\Classic\Classic.w3p` and `Reforged\Reforged.w3p`, before the map
  loads. The Battle.net app was running in every run; not tried with it closed.
- What the game shows can be captured only from a window on the visible desktop: `PrintWindow` with
  `PW_RENDERFULLCONTENT` of the game's window on a hidden desktop gives its frame and title bar and a black inside (3
  runs), while on the visible desktop it gives the game's picture, also with the display switched off by power saving
  (2 runs).
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
- Once a map has loaded, its `main` has run but the game waits on its loading screen (Sven: for a click): a zero-second
  timer started in `main` ran only after input. A space key posted to the game's window (`PostMessage`, `WM_KEYDOWN`
  then `WM_KEYUP`, `VK_SPACE`) gets past it without the window being in front, minimised or on a hidden desktop: in
  every run that logged it (over 20), the game started within 0.7 s of a posted key, the first one 3 s after `main` in
  most. A left click posted the same way at (100, 100), 3 s after `main`, did not (3 runs). `SendInput` was not needed
  and not tried. One run with no input posted (minimised, while Sven was watching) gave its lines 23 s after launch;
  whether he clicked is not known, so it is not known whether the screen ever goes by itself.
- Game time and the window (probe `game-time`: a 1-second periodic timer for 20 ticks, each with a timer's elapsed
  time and `os.time()`): on a hidden desktop (over 10 runs) and in a normal window kept in front (1 run), 20 game
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
  the other's game started but gave none in 100 s (1 pair). Inferred: the plan reviewer's two failed runs of 02:53 to
  02:57, where the game stopped before rewriting the account's files, may have overlapped with the code reviewer's; they
  were not reproduced in about 25 later single runs.
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
