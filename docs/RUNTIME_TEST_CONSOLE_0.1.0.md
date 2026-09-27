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
