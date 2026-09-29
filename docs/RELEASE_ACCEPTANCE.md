# Release Acceptance — 0.1.4

**Target:** Graveyard Keeper 1.407 / BepInEx 5  
**Status:** runtime behavior accepted; stable promotion pending one display-scale smoke test

## Accepted player-facing behavior

Runtime/perceptual testing established:

- existing waiting state after load restores persistent HUD state without a false arrival cue;
- corpse-only, confession-only and both-active persistent states display correctly;
- whichever alert is the only active one occupies the first slot;
- both active indicators pack into adjacent slots;
- confession false -> true produces the selected short `bell_single` cue, a stock-style `+ [prayer]` transient and the persistent prayer indicator;
- clearing the native confession state removes the persistent prayer indicator;
- corpse waiting state appears/clears through the verified loose-Body lifecycle;
- the stock donkey sound and stock corpse-arrival popup remain untouched;
- the confession popup preserves the stock panel/background/animation and visually reads as a sibling of the corpse popup;
- simultaneous corpse/confession transient playback can overlap but both visuals and both sounds remain functional; this rare case is accepted without additional queueing;
- the previous research-console startup presentation warning is absent from the accepted runtime log.

Final confession transient calibration:
- prayer icon X = 8.89;
- Y = 14.06;
- scale = 1.67;
- stock visible Y = 80;
- stock background opacity retained.

## Remaining pre-release verification

One targeted visual smoke test remains because the persistent indicators were calibrated after moving to the live `HUD.bar_energy` transform at **2560x1440 / HUD scale 1.1**.

Test one materially different supported layout, for example **1920x1080 / HUD scale 1.0**, and confirm:

- corpse-only remains immediately to the right of the energy bar;
- confession-only occupies the same first slot;
- both-active remains compact, adjacent and unclipped.

This is a downstream layout/responsiveness check only. The already accepted corpse/confession mechanics, transition behavior, audio, transient popup and save/load cases do not need to be replayed.

## Architecture acceptance

The final architecture:
- observes host-owned state rather than duplicating mechanics;
- adds no save data;
- uses event-driven hooks rather than recurring polling;
- leaves stock corpse presentation untouched;
- uses private HUD/transient objects only;
- fails closed on an unsupported host assembly;
- contains no research console or probe code in the production assembly.

## Accepted artifact identity

Production candidate 0.1.4:

- build source head: `2b18eefcf999ca0f9cd118a19a82d74a249f122c`;
- GitHub Actions run: `36409865438`;
- job: `108887262025`;
- artifact ID: `10964285252`;
- candidate artifact ZIP SHA-256: `248f625da0deb84694150a88ec545289c45aa026142ea7b5ac0c9a547a34fca6`;
- DLL size: `21,504 bytes`;
- DLL SHA-256: `f4736b10a9373fd6ef6c9319a4fef255579f7fd415faf53bbf381f447bf8d86d`;
- build result: 0 warnings / 0 errors.

For stable distribution, the exact accepted DLL bytes may be renamed from the candidate filename `KeepersAlerts.0.1.4.dll` to the canonical installed filename `KeepersAlerts.dll`. The SHA-256 must remain unchanged.

## Distribution boundary

The release package must contain the production plugin only. Research probes and `Keeper's Alerts Runtime Test Console` are not release payloads.
