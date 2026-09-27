# Integrated Production Candidate 0.1.1

**Branch:** `candidate/0.1.1`  
**Target:** Graveyard Keeper 1.407 / BepInEx 5  
**Status:** runtime acceptance required

## Delta from frozen 0.1.0

Internal review found that `WorldMap.GetWorldGameObjectByCustomTag` delegates to a comparator which logs a game error when no active match exists even if its wrapper's ignore flag is true. On a pre-repair save, using that method as the repaired-chute discriminator would therefore pollute the shared log during corpse resync.

0.1.1 changes only the branch lookup:

- query `WorldMap.GetWorldGameObjectsByCustomTag("morgue_throw_out", false)`, which returns a list without a not-found error;
- select the first candidate for which native `WorldGameObject.IsDisabled()` is false;
- if none is active, fall back to native `donkey_cemetery_point` / Up.

The receiving corridor, state semantics, hooks, load arming, HUD behavior, confession behavior and `bell_single` choice are unchanged.

## Included behavior

- confession availability from native `confession_available` state;
- one `bell_single` cue on live false -> true confession transition;
- cloned native corpse-arrival family for the prayer transient;
- persistent confession HUD indicator;
- persistent corpse-waiting HUD indicator from the verified native corridor;
- silent post-load resync;
- no mod-owned save state;
- no recurring polling.

Stock corpse arrival behavior is untouched.

## Acceptance focus

1. plugin loads without Keeper's Alerts errors;
2. no false confession sound/transient on load with already-active state;
3. live confession false -> true gives exactly one `bell_single` + one prayer transient;
4. final confession consumption clears persistent prayer indicator;
5. repaired corpse delivery preserves stock cue/toast and adds persistent corpse indicator;
6. corpse pickup clears it;
7. save/load waiting corpse restores the indicator silently;
8. indicators hide/show with stock HUD and can coexist;
9. log remains sparse and contains no repeated Keeper's Alerts / branch-discriminator errors.

Visual size/placement is intentionally subject to runtime perceptual acceptance.
