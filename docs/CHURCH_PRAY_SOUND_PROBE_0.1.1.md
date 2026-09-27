# Church Pray Sound Probe 0.1.1

**Target:** Graveyard Keeper 1.407  
**Purpose:** close the exact MasterAudio configuration of the church-pulpit prayer sound.  
**Status:** research probe; no production behavior.

## Why 0.1.1 exists

Presentation Probe 0.1.0 searched for:

`/church_pulpit/content/church_pulpit(Clone)/PrayFX/pray sound`

Accepted runtime evidence shows the actual hierarchy is:

`World/[wgo] church_pulpit/content/church_pulpit(Clone)/PrayFX/pray sound`

The omitted `[wgo] ` made the 0.1.0 suffix test incapable of matching the verified target.

A deliberate 0.1.0 rerun while the HUD zone label was Church still returned `<not found>`, which is consistent with this code defect.

## Exact research question

What serialized `DarkTonic.MasterAudio.EventSounds` configuration is attached to the verified `PrayFX/pray sound` object, including the event/sound-group fields needed to identify a native reusable cue?

## Contract

The probe:
- uses no Harmony;
- changes no UI;
- plays no sound;
- executes no FlowCanvas graph;
- writes no save data;
- only enumerates already-loaded Unity objects and reflects the target component into a text report.

The target finder includes inactive objects via `Resources.FindObjectsOfTypeAll<Transform>()` and verifies the `[wgo] church_pulpit` + `PrayFX` ancestry.

## Runtime action

1. Remove/disable Presentation Probe 0.1.0.
2. Install `KeepersAlerts.ChurchPraySoundProbe.0.1.1.dll`.
3. Start/load the game normally.
4. Enter the church. No prayer, sermon or interaction is required.
5. Once the pulpit hierarchy is loaded, the probe writes:
   `BepInEx/KeepersAlerts-church-pray-sound-probe-0.1.1.txt`
6. Return that report.

The probe keeps waiting until the verified target exists, so there is no short startup timing window.


## Frozen build identity

- exact source commit: `dc4997a31aba446c1e020529c67f7c64de75c1aa`
- GitHub Actions run: `36347907757`
- artifact ID: `10940952540`
- DLL: `KeepersAlerts.ChurchPraySoundProbe.0.1.1.dll`
- size: `16384 bytes`
- SHA-256: `51717dc1021b9db131d68e6cc50a193f01f33f87349fc8f74f129d528175d151`
- build: `0 warnings / 0 errors`

Do not rebuild different bytes under probe version 0.1.1. Any source change after this point requires a new probe version.
