# Release Acceptance — 1.0.0

**Target:** Graveyard Keeper 1.407 / BepInEx 5  
**Status:** **ACCEPTED AND PUBLISHED** — runtime behavior, final HUD responsiveness, stable artifact identity and GitHub Release publication are complete.

## Accepted player-facing behavior

Runtime and perceptual testing established:

- existing waiting state after load restores persistent HUD state without a false arrival cue;
- corpse-only, confession-only and both-active persistent states display correctly;
- whichever alert is the only active one occupies the first slot;
- both active indicators pack into adjacent slots;
- confession false -> true produces the selected short `bell_single` cue, a stock-style `+ [prayer]` transient and the persistent prayer indicator;
- clearing the native confession state removes the persistent prayer indicator;
- corpse waiting state appears/clears through the verified loose-Body lifecycle;
- the stock donkey arrival trigger, sound, popup content, geometry and easing are preserved;
- the confession popup preserves the stock panel/background/animation and visually reads as a sibling of the corpse popup;
- simultaneous corpse/confession transient playback can overlap but both visuals and both sounds remain functional; this rare case is accepted without additional queueing;
- stock corpse and Keeper's Alerts confession transients retain the serialized 0.5 s appear + 1.0 s hold + 0.5 s hide profile in wall-clock time, including while game time is accelerated;
- no Keeper's Alerts warning/error was observed in the final shared BepInEx runtime log.

Final confession transient calibration:
- prayer icon X = 8.89;
- Y = 14.06;
- scale = 1.67;
- stock visible Y = 80;
- stock background opacity retained.

## Final runtime acceptance

The accepted 0.1.5 behavior candidate was tested before 1.0.0 promotion.

The user explicitly accepted:
- accelerated-time notification duration: correct/readable rather than compressed;
- final HUD smoke test at **1920x1080**: corpse-only, confession-only and both-active layouts all appeared immediately to the right of the energy bar;
- no overlap, clipping or layout issue in that materially different resolution;
- no sound issue.

The final runtime log confirms Keeper's Alerts 0.1.5 loaded on the verified Graveyard Keeper 1.407 host and completed its initial silent resync without a Keeper's Alerts warning/error.

## 1.0.0 promotion rule

1.0.0 is a release-number promotion of the accepted 0.1.5 runtime behavior.

Allowed release-only changes:
- plugin/assembly/file version metadata from 0.1.5 to 1.0.0;
- public release documentation and bookkeeping;
- no runtime logic, constants, seams, assets or behavior changes.

Runtime re-acceptance is not required if this constraint is preserved and CI builds cleanly.

## Architecture acceptance

The final architecture:
- observes host-owned state rather than duplicating mechanics;
- adds no save data;
- uses event-driven hooks rather than recurring polling;
- preserves the stock corpse-arrival trigger/content/audio/geometry/easing while making its transient timing unscaled;
- uses private HUD/confession-transient objects only;
- fails closed on an unsupported host assembly;
- contains no research console or probe code in the production assembly.

## Accepted runtime baseline identity

Accepted behavior candidate 0.1.5:

- exact source head: `168098f32e05e829f3f3ad4ed7fdaaca0d9bb47b`;
- GitHub Actions run: `36625632889`;
- artifact ID: `11061005446`;
- candidate artifact ZIP SHA-256: `286fd94e68864649eda7b1840536a8be4844b26b760e2660c7037b100dfb91d8`;
- DLL size: `26,112 bytes`;
- DLL SHA-256: `f88373ab5040450cd9f85a32f6c6b538eab93690144ed225c668869182b34fbb`;
- build result: 0 warnings / 0 errors.

## Stable 1.0.0 artifact identity

Stable distribution artifact:

- exact release source head: `e9152da5e2df40cf61ce0c9f8276465bd92968ef`;
- branch: `release/1.0.0`;
- GitHub Actions run: `36628892284`;
- job: `109612800715`;
- artifact ID: `11061336058`;
- artifact ZIP SHA-256: `eaaa863ed5263f19c71dd2292ca48c0135644bbec9ff69dd956e484cc18002cc`;
- archive contents: one file, `KeepersAlerts.dll`;
- DLL size: `26,112 bytes`;
- DLL SHA-256: `c2fc852921c0587f3458fb3a56de05bb04a545e6605d9d1e9f0f30a8c5f1511e`;
- build result: 0 warnings / 0 errors.

Stable merge:

- release PR: #3;
- `main` merge commit: `a7dee87677bcf9550bb96f1d8fb3ed17a57c007e`;
- post-merge integration run: `36629029196`;
- post-merge build result: success.

Published release:

- tag: `v1.0.0`;
- tag target: `e9152da5e2df40cf61ce0c9f8276465bd92968ef`;
- GitHub Release ID: `399550100`;
- GitHub Release publish run: `36635074688`;
- release asset ID: `599291370`;
- release asset: `KeepersAlerts.dll`;
- release asset size: `26,112 bytes`;
- release asset SHA-256: `c2fc852921c0587f3458fb3a56de05bb04a545e6605d9d1e9f0f30a8c5f1511e`;
- release state: public, non-draft, non-prerelease.

The published asset digest exactly matches the accepted release-head DLL.

The post-merge DLL is not the stable distribution asset. The SDK appends the current Git commit SHA to `AssemblyInformationalVersion`, so rebuilding the identical production source tree at the merge commit changes binary metadata and therefore the DLL hash. The stable asset remains the exact release-head artifact above; `main` contains that release source state in its history.

## Distribution boundary

The stable release package contains the production plugin only.

Do not ship:
- research probes;
- `Keeper's Alerts Runtime Test Console`;
- host assemblies, decompiled host source, extracted game assets/audio, or dependency binaries.

The canonical installed filename is `KeepersAlerts.dll`.
