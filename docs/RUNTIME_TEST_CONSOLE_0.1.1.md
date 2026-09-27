# Runtime Test Console 0.1.1

Research-only companion DLL for fast Keeper's Alerts runtime and UX iteration.

## Working UI baseline

The console starts from the current preferred working layout taken from the second comparison screenshot:

- corpse persistent icon: X 133.92, Y 26.75, scale 0.62;
- confession persistent icon: X 164.60, Y 31.12, scale 0.95;
- confession transient icon: X 5.53, Y 14.06, scale 1.95;
- confession transient visible Y: 45.79.

Panel X/scale and background alpha use the live cloned stock panel values because those properties had not yet been adjusted.

Reset controls return to this working baseline:
- **Reset Persistent**
- **Reset Transient**
- **Reset All UI**

This baseline is research UX state, not final production acceptance.

## Preview controls

- Corpse HUD ON / OFF
- Confession HUD ON / OFF
- Preview confession cue: selected `bell_single` + confession transient
- Preview stock corpse toast: invokes stock `NewBodyArrivedGUI.Display()`
- Hold confession transient on screen
- Restore native HUD state

## UI calibration

Live controls:
- persistent corpse X / Y / scale;
- persistent confession X / Y / scale;
- confession transient panel X / scale;
- transient icon X / Y / scale;
- transient visible Y;
- transient Background alpha.

`Log exact UI values` emits one `UI_CALIBRATION ...` line containing every value.

## Native test-state controls

- Add test confession
- Clear console confession
- Spawn test corpse
- Remove console corpse
- **CLEAR ALL confessions**
- **CLEAR corpses in alert zone**

The two ALL/CLEAR-zone actions are intentionally destructive test helpers. Do not save unless that state change is actually desired.

The previous 0.1.0 clear-confession failure is avoided by not forcing `WorldGameObject.RedrawBubble` after manual cleanup; production is resynced directly after the canonical event list is changed.

## Scope

Research DLL only. It must never ship with the Nexus release.
