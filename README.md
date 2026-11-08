# MirrorImaged

Runs Warcraft III maps with no game open, fast and the same every time, so maps can be tested automatically.

MirrorImaged loads a `.w3x`, runs its script against C# copies of the game's natives on a simulated clock, and lets a
test give orders, send input, read state and check the results. The copies are built from what the real game was
measured doing: every native says how far it can be trusted, and probes run in the real game check it.

It is a test tool, not a game:

- **No game art.** It never draws models, textures or the game's interface.
- **No playing.** Input comes from tests, recordings and replays.
- **No game files shipped.** Standard data, scripts and maps are read from your own install.

## Progress

Work toward 1.0 in waves. A wave's bar counts its merged tasks; the total weighs each wave by its estimated size.

**Total: 20%**

`████░░░░░░░░░░░░░░░░` 20%

| Wave | What it covers | Progress |
|---|---|---|
| 0. Foundation | Repository, CI, the natives generator, Lua with the game's numbers, loading a map, the game runner | `████████████████████` 100% (6 of 6) |
| 1. Script core | Triggers, threads, timers, hashtables, strings, players, files and game cache, handles, JASS maps | `████████████████████` 100% (10 of 10) |
| 2. Data and units | Map files, the game's data, units, heroes, items, destructables, damage, groups, regions, abilities on units | `█████████░░░░░░░░░░░` 45% (5 of 11) |
| 3. Orders, movement, combat | Orders, path finding, combat, buildings, training, research, harvesting | `░░░░░░░░░░░░░░░░░░░░` 0% |
| 4. Abilities and buffs | The cast flow, ability effects, buffs and auras | `░░░░░░░░░░░░░░░░░░░░` 0% |
| 5. Presentation and interface | Cameras, sounds, effects, text, frames, dialogs, multiboards | `██████████░░░░░░░░░░` 50% (2 of 4) |
| 6. Players, input, visibility | Chat, keys, mouse, selection, sync, leaving, fog of war, the desync check | `░░░░░░░░░░░░░░░░░░░░` 0% |
| 7. AI | `common.ai`, the melee and campaign AI, creep camps | `░░░░░░░░░░░░░░░░░░░░` 0% |
| 8. Test tools | The C# and Lua test APIs, the `mirror` commands, snapshots, recordings | `░░░░░░░░░░░░░░░░░░░░` 0% |
| 9. Viewer | A web page to watch and scrub a recorded run | `░░░░░░░░░░░░░░░░░░░░` 0% |
| 10. Accuracy and speed | The remaining probes, whole-corpus runs, the benchmark, all three platforms | `░░░░░░░░░░░░░░░░░░░░` 0% |
| 11. Docs and release | API docs, the guide, examples, the release | `░░░░░░░░░░░░░░░░░░░░` 0% |

### Natives

Of the 1,835 natives in the installed `common.j` and `common.ai`:

| Level | Meaning | Count | Share |
|---|---|---|---|
| Exact | Matches the game on every case a probe measured | 285 | `███░░░░░░░░░░░░░░░░░` 16% |
| Close | A documented model, not fully measured | 381 | `████░░░░░░░░░░░░░░░░` 21% |
| Recorded | Changes nothing a map can read back; the call is logged | 97 | `█░░░░░░░░░░░░░░░░░░░` 5% |
| Missing | Stops the run with the native's name | 1,072 | `████████████░░░░░░░░` 58% |

Implemented in all (not Missing): 763 of 1,835, `████████░░░░░░░░░░░░` 42%. For 1.0, none is Missing.

## How it will work once finished

### What you do

1. **Install it** as a .NET tool (`mirror`) and, for C# tests, the `MirrorImaged` package. It finds your Warcraft III
   install by itself, or you point it at one.
2. **Check your map:** `mirror check MyMap.w3x` lists every native your map calls and how far each is trusted, before
   anything runs.
3. **Write tests** in C# or Lua. A test loads the map, sets the players and the random seed, moves time forward,
   gives orders, and checks what happened.
4. **Run them** with `dotnet test` or `mirror test`, in your editor or in CI. A test of a whole game takes seconds.
5. **When one fails**, record the run and open it in the viewer to scrub through it, or step back to just before
   the failure.

### An example

Say your tower defence map should send 20 creeps in wave 5, and a Frost Tower should slow them. A C# test could read:

```csharp
using var game = Simulation.Load("TowerDefence.w3x", new() { Seed = 1, Users = [0] });

game.Advance(TimeSpan.FromMinutes(4));              // wave 5 starts at 4:00
Assert.Equal(20, game.Units.Count(u => u.Owner == 11));

var tower = game.Create("h00F", owner: 0, x: 512, y: 0);
game.Advance(TimeSpan.FromSeconds(10));
Assert.All(game.Units.Where(u => u.Owner == 11 && u.DistanceTo(tower) < 600),
    creep => Assert.True(creep.HasBuff("Bfro")));
```

The same test in Lua, run by `mirror test`:

```lua
local game = mirror.load("TowerDefence.w3x", { seed = 1, users = { 0 } })
game:advance(4 * 60)
assert(#game:units({ owner = 11 }) == 20)
```

These calls are the plan's, not the finished API: today only loading a map, advancing time and reading the log
exist (`Simulation.Load`, `Advance`, `Log`).

### What else it does

- **Desyncs:** `mirror desync MyMap.w3x` runs the map once as each player and stops at the first value that differs,
  with the step, the value and the Lua stack.
- **Fuzzing:** `mirror fuzz` sends random legal orders and input for every player and reports script errors and
  desyncs.
- **Replays:** `mirror replay game.w3g` runs a real game's recorded actions through the map.
- **Speed:** a 4-player melee game of 30 minutes in under a minute.
- **Same every time:** the same map, seed and input give the same state on Windows, Linux and macOS.

### How far to trust a result

A test passing in MirrorImaged means the map behaves as the game would, as far as the natives it touched are
trusted. `mirror check` and the run's log say which were Close or Recorded, so you know where the game might differ.
Exact natives are checked against the real game on every release.

## Today

`mirror run MyMap.w3x` runs a map and prints its log, and `mirror coverage` lists every native by level. All 466 maps
shipped with the game load and run their setup with no script error.

## Licence

MIT.
