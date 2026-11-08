# Facts

Game behaviour measured for MirrorImaged, in the style of the workspace's `docs/game-facts.md`.

## The game runner

All on 3.0.1.24371, one PC, single player, maps started with `-launch -windowmode windowed -loadfile "<map>"`, seen
11-08 (W0-4) unless marked.

- A `-loadfile` game needs no Battle.net login (Sven, 11-08; this replaces the workspace fact of the same day that it
  asks for one). In every run whose `War3Log.txt` was written out, `[CLoginCallbacks] LoginDoorClose called` came 5 to
  6 s after the start, before `Activating WebUI`, with nobody logging in, and the game wrote the campaign profiles in
  its account folder (`BattleNet\<account>\Campaigns\...\*.w3p`) just before the map's game began.
- A map without a minimap image (`war3mapMap.blp` or `war3mapMap.tga`) closes the game as the map starts: War3Net's `MapFactory` map (info format 28, terrain, `war3map.lua`) exited with code 0 about 8 s after the game
  made `TempReplay.w3g` (4 runs: twice on a hidden desktop, twice minimised); World Editor's own new map
  (`e10-probe.w3m`) packed again by War3Net without its `war3mapMap.blp` exited with 0, and with a few script lines
  added crashed (0xC0000005, 3 runs); the same repack with its `war3mapMap.blp` kept running. With a 256x256 black
  32-bit `war3mapMap.tga` added, the `MapFactory` map loads and runs its Lua. No `war3map.wpm`, `war3map.shd` or
  `war3map.doo` is needed. The game left no crash report for the exits with 0.
- `War3Log.txt` is held back while the game runs: after the start-up scan of `Documents\Warcraft III\Maps` nothing more
  reached the file while a game ran (read every 250 ms for up to 150 s). The rest is written when the game ends normally
  or crashes (the crash report holds a copy), and is lost when the process is killed. So `Opening map - <path>` can't
  confirm a load while the game runs, nor after a kill.
- Once a map has loaded, its `main` has run but the game waits on its loading screen (Sven: for a click): a zero-second
  timer started in `main` ran only after input. A space key posted to the game's window (`PostMessage`, `WM_KEYDOWN`
  then `WM_KEYUP`, `VK_SPACE`) gets past it without the window being in front, minimised or on a hidden desktop: in the
  5 runs that logged it, the game started within 0.7 s of a posted key, the first one 3 s after `main` in 3 of them. A
  left click posted the same way at (100, 100), 3 s after `main`, did not (3 runs). `SendInput` was not needed and not
  tried. One run with no input posted (minimised, while Sven was watching) gave its lines 23 s after launch; whether he
  clicked is not known, so it is not known whether the screen ever goes by itself.
- Game time and the window (probe `game-time`: a 1-second periodic timer for 20 ticks, each with a timer's elapsed time
  and `os.time()`): on a hidden desktop and in a normal window kept in front, 20 game seconds took 20 real seconds and
  the timer read 1.01 to 20.00. Minimised (started with `SW_SHOWMINNOACTIVE`, never in front), the game got past the
  loading screen and ran its zero-second timer, but the 1-second timer never fired in 100 s: game time stops. Behind
  other windows it runs slower (8.73 s counted as 3.19 s; workspace fact of 10-02); not measured here.
- On a Windows desktop made with `CreateDesktop` and named in `STARTUPINFO.lpDesktop`, the game makes its window
  (`OsWindow "Warcraft III"`) and the WebUI's `BlizzardBrowser` windows there, nothing shows on the visible desktop, and
  game time runs at full speed. From launch to the probe's lines: the `hello` probe 17 s, `game-time` 36 s.
- Closing the game's window (`WM_CLOSE` posted to it) opens the game's quit dialog, which waits for a person (Sven had
  to press X). Killing the game's process with its process tree ends it with nothing left running.
- A game that exits by itself leaves its `BlizzardBrowser` processes running, and a crash leaves `BlizzardError`'s
  dialog open ("The application encountered an unexpected error. ...", with Send to Blizzard, Repair, Reopen and Close)
  and writes a report in `Documents\Warcraft III\Errors`.
- The hub's read works: `Preloader` of a file written by the runner in the game's Preload format, holding
  `call BlzSetAbilityTooltip('AHbz', "<path>", 0)`, gave the 160-character path back from
  `BlzGetAbilityTooltip(FourCC("AHbz"), 0)` on the first poll, in the same call.
- `ChangeLevel` from a `-loadfile` game does not load a custom map: given the map's full path
  (`C:\Users\...\maps\1-hello.w3x`), nothing happened in 76 s and the hub kept running; given a path under
  `Documents\Warcraft III` (`Maps\MirrorImaged\target.w3x`), the log shows the game opening that map, then it crashed
  (ACCESS_VIOLATION reading 0x298, the log listing every player as `<Unused>` and `Played (null)`), 2 runs. So the hub
  cannot hand a probe to the game this way; with no login needed, a launch per probe is the way.
- A probe's `EndGame(false)` in a `-loadfile` game ends the game and the game writes `LastReplay.w3g` (1 run).
