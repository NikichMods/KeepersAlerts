# Runtime Test Console 0.1.0

Research-only companion DLL for rapid Keeper's Alerts runtime acceptance.

## Questions

This harness exists to remove natural-event waiting from development:

- can each persistent HUD state be shown/hidden immediately?
- can the confession transient + selected `bell_single` cue be replayed immediately?
- does a real native `confession_available` mutation traverse the production observer path?
- does a real native Body drop in the production receiving corridor traverse the production add/remove path?
- what exact HUD/transient position and scale should be frozen into production?

Existing evidence is sufficient for these operations; the harness does not duplicate production state logic.

## Controls

F9 toggles the IMGUI console.

### Save-safe presentation controls

- Corpse HUD ON / OFF
- Confession HUD ON / OFF
- Play confession cue
- Restore native state

These mutate only Keeper's Alerts' in-memory presentation state and are not serialized.

### Native observer-path controls

- Add / clear test confession
- Spawn / remove test corpse

The confession uses the game's own `WorldGameObject.AddInteractionEvent("confession_available")` and `RedrawBubble`.

The corpse uses the production candidate's verified receiving-corridor origin/direction, `GameSave.GenerateBody`, and native `DropResGameObject.Drop`. Removal marks only the console-owned drop collected, invokes the native linked-hint commit point, removes that owned drop from the live DropsList, and destroys its GameObject.

**Do not save while either native test state exists.** They are real live host state. Clear them first.

## UI calibration

Live sliders expose:

- persistent corpse X / Y / scale;
- persistent confession X / Y / scale;
- transient prayer icon X / Y / scale;
- transient panel visible Y.

`Log exact UI values` writes one `UI_CALIBRATION ...` line with exact values to BepInEx LogOutput. These values are intended to be copied into production constants after perceptual acceptance; they are not player configuration.

## Scope

Research DLL only. It must never ship with the Nexus release.


## Frozen artifact identity

- exact source commit: `5f83ad5d3181c64bebfb476bd34434513c7f751e`
- GitHub Actions run: `36357054461`
- job: `108726778732`
- artifact ID: `10944422649`
- artifact ZIP digest: `sha256:af20644db93b61480131cc93299cd74cae62c0b53d63e98fbfe334f6cc0ecef7`
- DLL: `KeepersAlerts.RuntimeTestConsole.0.1.0.dll`
- DLL size: `23,552 bytes`
- DLL SHA-256: `8122b1eb25510a985264de1f3d2a7cc91613d5befbe755ec25b4c26ab7539f48`
- build: `0 warnings / 0 errors`

The downloaded artifact was independently extracted and its DLL size/SHA-256 matched CI.

Do not rebuild or replace different bytes under console version 0.1.0.
