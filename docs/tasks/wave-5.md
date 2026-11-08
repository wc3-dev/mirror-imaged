# Wave 5: presentation and interface

Plan sections: 4 (A1, A4, A5, A6), 6 (presentation, interface), 9 (wave 5). Needs wave 1. Started early (11-08)
because every shipped map stops at `main`'s first line, `GetCameraMargin` (W2-9's D302): these natives unblock the
corpus runs (A4, A5).

Order: W5-1 first (it unblocks `main`); then W5-2 to W5-4, three at a time.

Parallel lanes: decision numbers by range: W5-1 D500-D519, W5-2 D520-D539, W5-3 D540-D559, W5-4 D560-D599.
Generated-file conflicts are resolved by re-running the generator.

The plan's target for presentation is Recorded, with every getter reading back what was set; for frame layout,
Close. Recorded natives keep their calls where a test can read them (later.md, wave 8).

## W5-1 Cameras, and the rest of what `main` needs first

- Every camera native: margins and bounds (`GetCameraMargin`, `SetCameraBounds`, `GetCameraBound*`), camera setups
  (`CreateCameraSetup`, `CameraSetupSetField`/`GetField`, `CameraSetupApply*`, the World Editor's `gg_cam_` code),
  the per-player camera state (`SetCameraPosition`, `PanCameraTo*`, `SetCameraField`, `GetCameraField`,
  `GetCameraTargetPosition*`, `GetCameraEye*`, `SetCameraTargetController`, `ResetToGameCamera`), camera noise and
  quick positions — Recorded with getters, the local player's camera only (multiplayer per-player state: wave 6).
- Then run W2-9's shipped-map test (`ShippedMapTests`, `Needs=Game`) and keep going down `main` and `InitBlizzard`:
  implement each next Missing native that belongs to this wave's presentation area or is a simple flag or getter
  (`IsFogEnabled` and the fog flags as stored state — fog of war itself is wave 6), until the maps stop at natives of
  other waves. Record the new distribution of first stops in a decision.
- Probe what the facts don't settle (facts: "Presentation", "Initialisation"; the camera's measured values).

## W5-2 Sounds and music

- Sounds (`CreateSound`, `CreateSoundFromLabel`, `CreateMIDISound`, `SetSound*`, `StartSound`, `StopSound`,
  `KillSoundWhenDone`, `GetSoundDuration` from the installed sound files' lengths (later.md), `GetSoundIsPlaying`,
  `GetSoundIsLoading`, 3D sound attachment), music (`PlayMusic`, `PlayMusicEx`, `StopMusic`, `ResumeMusic`,
  `PlayThematicMusic`, `SetMapMusic`, `SetMusicVolume`), sound volume groups — Recorded with getters (facts:
  "Presentation" sound and music bullets).

## W5-3 Effects, text tags, lightning, weather, minimap, cinematics

- Special effects (`AddSpecialEffect*`, `DestroyEffect`, the `BlzSetSpecialEffect*`/`BlzGetSpecialEffect*` natives),
  text tags (`CreateTextTag`, `SetTextTag*`), lightning (`AddLightning*`, `GetLightning*`), weather, minimap pings and
  icons, cinematic mode, transmissions (`DisplayCineFilter`, `CinematicFilter*`), text messages and
  `ClearTextMessages` (in the run's log as D48 says) — Recorded with getters (facts: "Presentation").

## W5-4 The interface

- Frames: `BlzLoadTOCFile` with the map's TOC/FDF (and the install's), the frame tree from FDF (anchors, sizes, text)
  so `BlzFrameGet*` read back what the game gives, the game's own frames (`BlzGetOriginFrame`,
  `BlzGetFrameByName`), frame events registered (firing needs input, wave 6) — layout Close (facts: "UI: frames",
  "UI: the game's own frames", "UI: data instead of code"); a map's own FDF/TOC string lists for
  `GetLocalizedString` (later.md, wave 5).
- Dialogs, multiboards, leaderboards and quests, Recorded with getters (facts: "Initialisation" on when they may be
  made).
