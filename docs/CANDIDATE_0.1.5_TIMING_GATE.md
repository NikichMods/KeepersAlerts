# Candidate 0.1.5 — Real-Time Transient Timing Gate

**Target:** Graveyard Keeper 1.407 / BepInEx 5  
**Baseline:** release-prep head `e3a64d60f6930585d38c043a5f284308a31dd9f1`  
**Gate:** **READY**

## Behavior change

Keep the stock corpse-arrival transient and Keeper's Alerts confession transient at the existing serialized timing:

- appear: 0.5 s;
- display: 1.0 s;
- hide: 0.5 s;

but interpret those durations as real/unscaled time instead of Unity scaled game time.

## Observable property

At normal play and while sleep/meditation accelerates `Time.timeScale`, each transient should still take approximately two wall-clock seconds from appearance start through hide completion.

## Canonical owner / final consumer

`NewBodyArrivedGUI.RunAppearCoroutine()` owns the transient lifecycle.

The inspected implementation uses:

1. `transform.DOLocalMoveY(..., _appear_time, false)` for entry;
2. `WaitForSeconds(_display_time)` for the visible hold;
3. `transform.DOLocalMoveY(..., _hide_time, false)` for exit.

Both DOTween's default update mode and `WaitForSeconds` are scaled-time consumers.

Runtime presentation evidence from the verified 1.407 installation records the serialized values 0.5 / 1.0 / 0.5. Sleep and WaitingGUI/meditation run with accelerated `Time.timeScale`; the user observed the confession transient collapsing correspondingly.

## Solution-space checkpoint

Rejected:

- multiplying serialized durations by the current `Time.timeScale`: fails if speed changes while the transient is active and mutates shared component timing state;
- replacing `NewBodyArrivedGUI.Display()` with a custom animation: needlessly takes ownership of native geometry/easing/lifecycle;
- recurring compensation/polling: unnecessary for a discrete presentation lifecycle.

Selected:

- patch the compiler-generated `RunAppearCoroutine` state-machine `MoveNext()`;
- make exactly the two `DOLocalMoveY` tweens independent of `Time.timeScale`;
- replace exactly one `WaitForSeconds(float)` construction with `WaitForSecondsRealtime(float)`;
- structurally validate the expected call counts and fail closed if the verified shape is absent.

## Blast radius

The patch affects only `NewBodyArrivedGUI` lifecycle instances.

For Keeper's Alerts this means:

- the stock corpse-arrival panel;
- the private confession sibling clone, which intentionally retains the same controller.

No state-observation, corpse-delivery, confession-RNG, sound, save/load, HUD, or unrelated UI path is changed.

## Preserved invariants

Preserve:

- 0.5 / 1.0 / 0.5 nominal durations;
- stock positions and DOTween easing;
- stock corpse trigger and presentation contents;
- Keeper's Alerts confession icon/sound/state behavior;
- persistent HUD behavior;
- save/load semantics;
- no recurring polling or new persistence.

## Acceptance evidence

Candidate runtime acceptance should verify:

1. normal-speed corpse/confession transient still looks unchanged and lasts about two seconds;
2. accelerated sleep or meditation no longer compresses the transient;
3. at least one accelerated case with `Time.timeScale >= 10`;
4. no new errors in the shared BepInEx log.

The existing final HUD resolution/scale smoke test remains separately required before stable promotion.
