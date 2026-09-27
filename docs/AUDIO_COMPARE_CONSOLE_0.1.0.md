# Audio Compare Console 0.1.0

**Branch:** `research/audio-compare-console`  
**Purpose:** direct perceptual A/B comparison of two verified stock Graveyard Keeper sound groups.

## Exact question

Which existing game sound is a better confession-available event cue:

- `bell_single`
- `chorus_short`

## Evidence before the harness

- `bell_single` is a verified stock sound id used by native `Flow_PlaySound` nodes.
- `chorus_short` is the verified MasterAudio sound group used by the church-pulpit `PrayFX/pray sound` EventSounds component.
- GK 1.407 exposes `Sounds.PlaySound(string snd, Vector2? pos = null, bool force_play = false, float custom_distance = 0f)`.

No further host-internals research is needed. The remaining question is perceptual UX judgment.

## Contract

Research-only:
- no Harmony;
- no save writes;
- no state mutation except asking the stock audio subsystem to play one requested sound;
- no confession/corpse mechanics;
- no extracted or redistributed game audio.

The plugin resolves the exact GK 1.407 `Sounds.PlaySound` signature once. If the supported Assembly-CSharp MVID/signature does not match, it disables itself rather than guessing another API.

## UI

A small draggable IMGUI window contains two sound buttons:

- **Bell Single**
- **Chorus Short**

Press **F10** to hide/show the window.

Each click emits concise before/after markers to the normal BepInEx log.

## Runtime acceptance

Install the DLL, load normal gameplay, click both buttons, and report which sound better matches the desired short remote church/confession cue.

No log is required unless a button fails to produce sound.


## Frozen artifact identity

- exact source commit: `6da95e5ae11a2542cb3cb8816c61b699832e9f4a`
- GitHub Actions run: `36349736266`
- artifact ID: `10940764849`
- DLL: `KeepersAlerts.AudioCompareConsole.0.1.0.dll`
- size: `9,728 bytes`
- SHA-256: `9fa54fb10688c4f5ae252ccf54681f64b86a83b05c0cc03b6bc36b4dbed4d1a1`
- build: `0 warnings / 0 errors`

Do not rebuild different bytes under console version 0.1.0.
