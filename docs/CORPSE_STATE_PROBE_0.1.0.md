# Corpse Waiting-State Probe 0.1.0

**Branch:** `research/corpse-waiting-state`  
**Purpose:** close only the remaining post-delivery provenance/reconstruction question.

## Exact question

Can an uncollected donkey-delivered corpse be recognized after save/load from the game's own loose-drop state with a predicate narrow enough to drive Keeper's Alerts without a separate persisted mod flag?

## Why a new probe is justified

Already accepted/static evidence proves:
- ordinary donkey delivery ownership and `Flow_DropBody` topology;
- the repaired-chute target `morgue_throw_out`;
- native `DropResGameObject` / `DropsList` creation and collection lifecycle;
- native save/load serialization of loose-drop position, item and zone.

What the existing evidence does **not** record is the actual settled `pos` / `zone_id` of a delivered body in the user's installed runtime, nor the reconstructed values after loading that save.

This probe does not repeat the delivery-owner experiment. It observes the missing native state directly.

## Contract

Read-only:
- no Harmony;
- no graph execution;
- no game-state mutation;
- no save writes;
- no synthetic corpse spawning;
- no UI mutation.

The probe reads the live `DropsList` every 0.5 seconds and logs only Body drops. It records:
- item id;
- Unity object instance id;
- world position;
- `DropResGameObject.zone_id`;
- `Item.drop_zone_id`;
- addition/removal;
- periodic snapshots;
- current `morgue_throw_out`, `morgue_throw_in`, and donkey positions when available.

Output:
`BepInEx/KeepersAlerts-corpse-state-probe-0.1.0.txt`

## Runtime sequence

1. Start with the exact probe installed.
2. Load a save and let the donkey deliver a corpse normally.
3. Leave that corpse uncollected long enough for at least one periodic snapshot.
4. Save while it is still lying there, then reload that saved state.
5. Leave it uncollected for one periodic snapshot after load.
6. Pick up that delivered corpse.
7. Return the dedicated probe report.

Existing unrelated loose bodies are acceptable: the probe logs all Body drops so the delivery transition can be distinguished from the baseline.

No special cleanup is required; the probe never changes save state. Remove the probe DLL after the test.
