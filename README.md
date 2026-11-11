**MirrorImaged** is headless Warcraft III: Reforged for automated testing.

MirrorImaged loads `.w3x` archives (aka. maps), runs the map script against C# copies of the game natives on a simulated clock, and lets tests give orders, input, read state and check outcomes. The copied game is built from in-game probing for which I have not standardized methodology yet.

## Progress `████░░░░░░░░░░░░░░░░` 20%

| #                             | Description                                                                                        | Progress                               |
| ----------------------------- | -------------------------------------------------------------------------------------------------- | -------------------------------------- |
| 0. Foundation                 | Repository, CI, natives gen, Lua, loading maps, main runner                                        | `████████████████████` 100% (6 of 6)   |
| 1. Core                       | Triggers, threads, timers, hashtables, strings, players, files, cache, handles, JASS               | `████████████████████` 100% (10 of 10) |
| 2. Data and units             | Map files, game data, units, heroes, items, destructables, damage, groups, regions, unit abilities | `█████████░░░░░░░░░░░` 45% (5 of 11)   |
| 3. Orders, movement, combat   | Orders, pathfinding, combat, buildings, training, research, harvesting                             | `░░░░░░░░░░░░░░░░░░░░` 0%              |
| 4. Abilities and buffs        | Casting, ability effects, buffs and auras                                                          | `░░░░░░░░░░░░░░░░░░░░` 0%              |
| 5. Presentation and interface | Cameras, sounds, visual effects, text, frames, dialogs, multiboards                                | `██████████░░░░░░░░░░` 50% (2 of 4)    |
| 6. Players, input, visibility | Chat, keys, mouse, selection, sync, leaving, fow, desyncs                                          | `░░░░░░░░░░░░░░░░░░░░` 0%              |
| 7. AI                         | `common.ai`, melee/campaign AI, creeps                                                             | `░░░░░░░░░░░░░░░░░░░░` 0%              |
| 8. Test tools                 | C# API, Lua API, commands, snapshots, recordings                                                   | `░░░░░░░░░░░░░░░░░░░░` 0%              |
| 9. Viewer                     | Webapp for watching/scrubbing recordings                                                           | `░░░░░░░░░░░░░░░░░░░░` 0%              |
| 10. Accuracy and speed        | More probes, whole-corpus runs, benchmarks, linux/macOS                                            | `░░░░░░░░░░░░░░░░░░░░` 0%              |
| 11. Docs and release          | docs, guides, examples, 1.0 release                                                                | `░░░░░░░░░░░░░░░░░░░░` 0%              |

### Natives `████████░░░░░░░░░░░░` 42%

| Level    | Meaning                                       | Count | Share                      |
| -------- | --------------------------------------------- | ----- | -------------------------- |
| Exact    | Every case matched                            | 285   | `███░░░░░░░░░░░░░░░░░` 16% |
| Close    | Documented as matching but not fully measured | 381   | `████░░░░░░░░░░░░░░░░` 21% |
| Recorded | Changes nothing a map can read                | 97    | `█░░░░░░░░░░░░░░░░░░░` 5%  |
| Missing  | Stops testing and names the native for now    | 1,072 | `████████████░░░░░░░░` 58% |

## Licence

MIT.
