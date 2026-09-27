# Presentation Seam Probe 0.1.0

**Branch:** `research/presentation-seams`  
**Purpose:** close serialized/native presentation owners without changing gameplay or UI.

## Frozen artifact identity

- source commit: `31a314568375fc172255901378dcc4205442d4b0`
- GitHub Actions run: `36317213484`
- artifact: `KeepersAlerts-PresentationProbe-0.1.0`
- artifact ID: `10930868874`
- DLL: `KeepersAlerts.PresentationProbe.0.1.0.dll`
- DLL size: `26,624 bytes`
- DLL SHA-256: `16184e1cbc948655c1a82519e8ed404aa8e2b13983c7a7a689934a694b0c5d8c`
- CI result: clean Release build, 0 warnings / 0 errors

Do not rebuild or replace different bytes under probe version 0.1.0.

## Exact questions

1. What runtime hierarchy, panel, anchors and visual children belong to the stock `NewBodyArrivedGUI`?
2. What HUD hierarchy/anchor should own one or two persistent Keeper's Alerts indicators?
3. Which native UILabel/UIFont context owns the `(pray_bubble)` symbol sequence?
4. What MasterAudio `EventSounds` configuration is attached to the church pulpit `PrayFX/pray sound` object?
5. What live endpoint/dock geometry exists for `morgue_throw_in` / `morgue_throw_out`?

## Contract

Read-only/load-only:
- no Harmony;
- no UI mutation;
- no audio playback;
- no FlowCanvas execution;
- no game-state mutation;
- no save writes.

The probe waits for loaded gameplay, inspects the existing runtime hierarchy/components once, writes a report, then becomes inert.

Output:

`BepInEx/KeepersAlerts-presentation-probe-0.1.0.txt`

## Runtime sequence

1. Install the exact probe DLL.
2. Start Graveyard Keeper and load any developed save where church/morgue content exists.
3. Remain in ordinary gameplay for roughly 10–30 seconds. No corpse delivery, confession roll, sermon or interaction is required.
4. Return the generated report.

Remove the probe after the report is produced.
