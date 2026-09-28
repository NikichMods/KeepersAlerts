# Runtime Test Console 0.1.2

Research-only companion for Keeper's Alerts candidate 0.1.3.

## Purpose

This version aligns the calibration harness with the energy-bar-relative persistent HUD architecture and adds direct transient comparison controls.

It does not ship with the production mod.

## Changes from 0.1.1

### Startup safety
The console no longer calls production `RenderHud()` while waiting for calibration objects. It waits until `GUIElements.me`, the stock body-arrival GUI and the production-owned UI objects already exist.

This removes the harness-caused early `GUIElements.me is unavailable` presentation error seen in the previous runtime log.

### Full stock corpse cue preview
`Preview stock corpse cue` now invokes:
- native `Sounds.PlaySound("donkey_bell", ...)`;
- native stock `GUIElements.me.body_arrived_gui.Display()`.

It does not spawn a corpse or run donkey mechanics. It exists only to reproduce the stock audio/visual arrival presentation for comparison.

### Combined transient preview
`Preview BOTH cues` starts in one action:
- the stock corpse `donkey_bell` + stock corpse-arrival panel;
- Keeper's Alerts `bell_single` + confession panel.

It mutates no corpse/confession state. Its purpose is to expose overlap/coexistence behavior without waiting for two real events.

### Persistent calibration
Persistent controls are now expressed relative to the actual energy-bar edge:
- first slot offset;
- slot spacing;
- corpse Y / scale;
- confession Y / scale.

Reset baseline:
- first slot offset 24.92;
- slot spacing 30.68;
- corpse Y -6.25 / scale 0.62;
- confession Y -1.88 / scale 0.95.

The calibration helper respects compact packing: confession uses slot 1 when corpse is inactive.

### Confession transient baseline
- panel X / scale: stock live values;
- prayer icon X 5.53;
- prayer icon Y 14.06;
- prayer icon scale 1.67;
- visible Y 80;
- background alpha: stock live value (normally 1.00).

For candidate 0.1.3 the console targets the dedicated cloned `PrayerIcon`; the legacy `PlusText` lookup remains only as a fallback.

## Save safety

Preview buttons are nonpersistent.

`Add test confession`, `Spawn test corpse`, `CLEAR ALL confessions` and `CLEAR corpses in alert zone` still mutate native runtime state. Do not save while synthetic state exists unless that mutation is intentionally desired.

## Frozen artifact identity

- exact build head: `39e397088e454933805a878f2fddcb4d6040bff1`
- GitHub Actions run: `36362244478`
- job: `108741576688`
- artifact ID: `10946007830`
- artifact ZIP digest: `sha256:961ec32a0c12a64444260a50fbfa74f770f34141fb36773fc0e1d9715f373a02`
- DLL: `KeepersAlerts.RuntimeTestConsole.0.1.2.dll`
- DLL size: `31,744 bytes`
- DLL SHA-256: `0146f6e31e9c90440ba7031375d8f982edfb5aa0d68719f974b1e1dde54e7b7b`
- build: `0 warnings / 0 errors`

The downloaded artifact was independently extracted and its DLL size/SHA-256 matched CI.

Do not rebuild or replace different bytes under console version 0.1.2.
