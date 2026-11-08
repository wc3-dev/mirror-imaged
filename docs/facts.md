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
  ...) and 1.92324328 high and 0.087634936 low in the odd ones, the same in every octave from 2^-6 to 2^26.
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
  here it came at once. Not separated (W1-9's probe).
