# Keeper's Alerts — State Ownership Research

**Target:** Graveyard Keeper 1.407  
**Evidence status:** static inspection + accepted prior runtime/graph evidence  
**Production status:** research only; no runtime source yet

## Research questions

Before implementing persistent reminders, establish the host-owned truth for:

1. **confession waiting** — whether at least one native `confession_available` interaction exists;
2. **delivered corpse waiting** — whether the corpse delivered by the donkey is still lying uncollected at the delivery endpoint.

The notification layer should derive from canonical game state and avoid a second persisted source of truth unless the host does not preserve enough information.

## Confession availability — established

### Canonical state

`Flow_AddInteractionEvent` resolves its target WGO and calls `WorldGameObject.AddInteractionEvent(event)`.

`WorldGameObject.AddInteractionEvent`:
- stores the exact event string in `WorldGameObject.custom_interaction_events`;
- does not add duplicates;
- redraws the interaction bubble.

`Flow_RemoveInteractionEvent` removes the exact event from the same list and redraws the bubble.

Therefore a confessional is natively waiting when its live WGO contains:

`custom_interaction_events.Contains("confession_available")`

This observes the result of the stock roll and remains agnostic about whether stock or PrayerClarity: Rebalanced supplied the effective probability.

### Consumption / clear

The custom-interaction path in `WorldGameObject.Interact` consumes the first custom interaction event before firing it, then redraws the bubble. Accepted runtime logs confirm that interacting with an available booth fires:

`confession_available`

and the booth graph then drops the normal Faith/Story reward.

The notification does not need to infer reward collection or duplicate booth logic. It can query the native event list after the interaction.

### Save/load

`SerializableWGO.FromWGO` serializes `custom_interaction_events` into `interaction_events`.

`SerializableWGO.ToWGO` restores it.

Therefore confession availability is already host-persistent. **Do not save a separate Keeper's Alerts confession flag.**

### Native icon semantic

Accepted PrayerClarity lifecycle evidence for both `church_budka_1` and `church_budka_2` shows:

`custom_interaction_icon="(pray_bubble)"`

The generic WGO interaction-bubble path uses an object's `custom_interaction_icon` while custom interaction events exist.

Therefore `(pray_bubble)` is the verified stock visual semantic for an available confession and is the preferred symbol for both transient and persistent Keeper's Alerts presentation unless a later UI constraint requires otherwise.

## Confession detection solution space

### A. Filtered lifecycle hooks + canonical resync

Observe only the exact add/remove/consume seams relevant to `confession_available`, and query the native state on initialization/load to establish current truth.

**Advantages:** event-driven during normal play; no duplicate RNG or stored state; immediate transitions.  
**Risks:** several lifecycle exits must be covered coherently; Harmony targets/blast radius still need explicit verification.

**Current preferred research direction.**

### B. Bounded recurring canonical query

Periodically inspect the relevant live WGO event lists.

**Advantages:** one truth function naturally self-heals after load and missed transitions.  
**Risks:** recurring work for a state that changes only at discrete events; less host-native than an event seam.

Keep as fallback if the event-driven lifecycle becomes materially more complex than the state itself.

### C. Hard-code booth IDs / mirror state

Unnecessary coupling and/or a second source of truth. Reject unless later evidence makes the canonical event list insufficient.

## Delivered corpse — established

### Stock arrival

The stock transient notification is a dedicated host path:

`Flow_BodyArrivedNotify -> GUIElements.me.body_arrived_gui.Display() -> NewBodyArrivedGUI.Display()`

Keeper's Alerts should leave this behavior unchanged.

### Native delivery path

Accepted PrayerClarity donkey-graph research establishes ordinary delivery through `Flow_DropBody` in `npc_donkey`.

The graph has two ordinary delivery branches depending on the morgue chute state:
- an outside delivery branch;
- a repaired-chute branch whose `WGO to drop` is supplied by `Flow_FindWGO(custom_tag="morgue_throw_out")`.

`Flow_DropBody` creates the body and delegates to the resolved WGO's `DropItem`.

`WorldGameObject.DropItem` delegates to `DropResGameObject.Drop`.

For a Body drop, the host:
- creates a `DropResGameObject`;
- adds it to `DropsList.me.drops`;
- records the world zone from the drop position in `drop.zone_id` and `res.drop_zone_id`;
- removes/destroys the drop after collection through the normal `DropsList` lifecycle.

### Save/load

`DropsList.ToGameSave` serializes every loose drop's:
- position;
- item/body data;
- `zone_id`.

`DropsList.FromGameSave` recreates the loose drop and restores `zone_id`.

Therefore the physical loose corpse is already host-persistent. A separate mod save flag is not automatically justified.

### Rejected proxy: cur_bodies_count

`cur_bodies_count` is **not** a delivered-corpse waiting flag.

Evidence shows:
- the morgue/morgue-outside zone HUD displays this value as morgue occupancy;
- both ordinary donkey delivery branches increment it;
- body disposal such as pyre/crematorium paths decrement it.

It can remain nonzero because other bodies are stored/processed in the morgue. Using it would produce stale false reminders. **Do not use `cur_bodies_count` as Keeper's Alerts state.**

## Corpse-state solution space

### A. Track the actual delivered loose drop, reconstruct from native drop state after load

At delivery, identify the exact `DropResGameObject` created by the ordinary donkey delivery path. While the same loose drop exists, the reminder remains active; collection clears it.

After load, reconstruct from `DropsList` using verified delivery-endpoint/position/zone semantics.

**Advantages:** aligns with the player-facing meaning "the delivered corpse is still lying there"; uses native physical state; no duplicate body-count logic.  
**Open issue:** save data does not explicitly preserve "this drop came from the donkey". Reconstruction must be proved narrow enough not to mistake a manually dropped corpse for a delivery corpse.

**Current preferred research direction.**

### B. Any loose Body in morgue/morgue-outside

Simpler canonical query but changes the product meaning to "some loose corpse is near the morgue". That can include manually dropped bodies. Do not adopt silently.

### C. Persist mod-owned provenance

Could distinguish the exact donkey-delivered body across load, but adds a second persisted lifecycle/source of truth. Consider only if native reconstruction cannot satisfy the accepted semantics safely.

## Research-method checkpoint for the remaining corpse question

**Question:** can an uncollected donkey-delivered body be reconstructed after save/load from native `DropsList` state with a predicate narrow enough to avoid meaningful false positives?

**Existing evidence already available:**
- exact ordinary donkey delivery graph topology;
- exact repaired-chute target `morgue_throw_out`;
- native drop creation/collection lifecycle;
- native loose-drop save/load serialization;
- accepted real-runtime evidence that ordinary donkey delivery executes the identified graph path.

**Next method:** continue direct/static inspection of delivery endpoint geometry and zone semantics first. Do **not** build a probe merely to repeat delivery ownership.

Only if static/prior evidence cannot distinguish the post-load drop cleanly should a new read-only probe be created. That probe should answer only the missing provenance/reconstruction question by logging the delivered Body drop's native position/zone before save, after load, and at collection.

## Current evidence gates

### Confession state ownership

For the narrow property **"is at least one native confession currently available?"**, the canonical owner and save/load truth are now established.

The full player-facing confession notification remains **BLOCKED**, because the production presentation mechanism still needs:
- verified transition-hook blast radius;
- transient presentation owner;
- persistent HUD anchor/lifecycle;
- suitable native audio resource or an explicit decision not to reuse one.

### Corpse waiting indicator

**BLOCKED.**

Established:
- ordinary delivery owner/path;
- stock transient UI owner;
- canonical loose-drop lifecycle;
- host persistence of loose drops;
- `cur_bodies_count` is invalid as the waiting-state proxy.

Still open:
- sufficiently narrow post-load identification of the delivered loose corpse;
- exact persistent-indicator event/query seam;
- persistent HUD anchor/lifecycle.

No production runtime source should be added under either blocked behavior gate.
