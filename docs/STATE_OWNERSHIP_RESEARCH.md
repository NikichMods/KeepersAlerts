# Keeper's Alerts — State Ownership Research

**Target:** Graveyard Keeper 1.407  
**Evidence status:** static inspection + accepted runtime/graph evidence  
**Production status:** research/design; no production runtime source yet

## Research questions

Establish the host-owned truth for:

1. **confession waiting** — whether at least one native `confession_available` interaction exists;
2. **corpse waiting** — whether an actionable loose Body remains at the native corpse-delivery/receiving area.

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

The custom-interaction path in `WorldGameObject.Interact` consumes the first custom interaction event before firing it, then redraws the bubble. Accepted runtime logs confirm that interacting with an available booth fires `confession_available` and the booth graph then drops the normal Faith/Story reward.

The notification does not need to infer reward collection or duplicate booth logic. It can query the native event list after interaction.

### Save/load

`SerializableWGO.FromWGO` serializes `custom_interaction_events` into `interaction_events`; `SerializableWGO.ToWGO` restores it.

Therefore confession availability is already host-persistent. **Do not save a separate Keeper's Alerts confession flag.**

### Native icon semantic

Accepted PrayerClarity lifecycle evidence for both `church_budka_1` and `church_budka_2` shows:

`custom_interaction_icon="(pray_bubble)"`

The generic WGO interaction-bubble path uses the object's `custom_interaction_icon` while custom interaction events exist.

Therefore `(pray_bubble)` is the verified stock visual semantic for an available confession and is the preferred symbol for Keeper's Alerts presentation unless a later UI constraint requires otherwise.

## Confession detection solution space

### A. Filtered lifecycle hooks + canonical resync

Observe only exact add/remove/consume seams relevant to `confession_available`, and query native state on initialization/load.

**Advantages:** event-driven during normal play; no duplicate RNG or stored state; immediate transitions.  
**Risks:** several lifecycle exits must be covered coherently; Harmony targets/blast radius still need explicit verification.

**Current preferred direction.**

### B. Bounded recurring canonical query

Periodically inspect the relevant live WGO event lists.

**Advantages:** one truth function naturally self-heals after load and missed transitions.  
**Risks:** recurring work for a state that changes only at discrete events.

Keep as fallback if the event-driven lifecycle becomes materially more complex than the state itself.

### C. Hard-code booth IDs / mirror state

Unnecessary coupling and/or a second source of truth. Reject unless later evidence makes the canonical event list insufficient.

## Corpse waiting — established host facts

### Stock arrival

The stock transient notification is:

`Flow_BodyArrivedNotify -> GUIElements.me.body_arrived_gui.Display() -> NewBodyArrivedGUI.Display()`

The ordinary donkey arrival graph also uses the stock sound id:

`donkey_bell`

Keeper's Alerts must leave the stock corpse-arrival behavior unchanged.

### Native delivery path

Accepted PrayerClarity donkey-graph research establishes ordinary delivery through `Flow_DropBody` in `npc_donkey`.

There are two ordinary delivery branches:

- **pre-repair / outside:** `Flow_DropBody` uses the current donkey WGO as the default source;
- **repaired chute:** `Flow_FindWGO(custom_tag="morgue_throw_out")` feeds `Flow_DropBody.WGO to drop`.

The donkey travels to the authored `donkey_cemetery_point` before the delivery event.

`Flow_DropBody` generates a normal Body `Item` and calls the resolved WGO's `DropItem`; it does not stamp delivery provenance onto the Item.

`WorldGameObject.DropItem` delegates to `DropResGameObject.Drop`.

The resulting loose Body:
- exists in `DropsList.me.drops`;
- receives native `zone_id` / `Item.drop_zone_id`;
- remains there until normal collection/removal;
- is removed by `DropResGameObject.CollectDrop` -> `is_collected=true` -> `DropsList.Update`.

### Save/load

`DropsList.ToGameSave` serializes every loose drop's position, Item/body data and `zone_id`.

`DropsList.FromGameSave` recreates the drop and restores `zone_id`.

Accepted Corpse State Probe 0.1.0 runtime evidence on the repaired-chute path establishes:

- native delivery created a Body drop in `zone_id="morgue"`;
- the body settled to a stable native world position;
- a save/load reconstruction destroyed/replaced the old Unity instance but recreated a Body at the exact same saved position and zone;
- subsequent pickup removed that reconstructed Body from the loose-drop list.

Therefore **Unity instance identity is not persistent, but the native physical loose-body state is.**

### No persisted native delivery provenance

The host does **not** preserve a dedicated “delivered by donkey” marker in `GameSave.SavedDropItem`.

Static inspection also shows:
- `BodyDefinition.GenerateBodyItem` creates a normal Body Item;
- `Flow_DropBody` does not set a provenance field;
- ordinary Item construction leaves `linked_id=-1` unless another mechanic explicitly changes it;
- SavedDropItem contains only `res`, `pos` and `zone_id`.

Consequently, after a later load, an arbitrary loose Body cannot be proven to have originated from the donkey solely from saved Item identity.

This is an evidence boundary, not a reason to invent an unreliable hidden-ID heuristic.

### Rejected proxy: cur_bodies_count

`cur_bodies_count` is general morgue occupancy, not “the delivered corpse is still waiting”.

Evidence shows:
- the morgue/morgue-outside HUD uses it as occupancy;
- both ordinary donkey delivery branches increment it;
- disposal paths decrement it.

It can remain nonzero because of unrelated bodies. **Do not use it as Keeper's Alerts state.**

## Corpse-state solution space after runtime closure

### A. Native receiving-area occupancy

Define the player-facing state as:

**at least one loose Body is still occupying the native corpse receiving/delivery area.**

During the live delivery transition, exact provenance can be observed at the delivery owner. After load, reconstruct from native Body drop position/zone relative to the verified delivery endpoint/receiving area.

**Advantages:**
- no mod save data;
- naturally survives save/load because the physical state survives;
- reminder follows the actionable world state rather than a historical flag;
- collection clears through the host lifecycle.

**Semantic edge:** a player who deliberately drops some other Body into the same receiving area can satisfy the same predicate. This is not strict historical provenance. Conversely, if the delivered corpse is deliberately moved far away before save/load, a narrow receiving-area reconstruction may no longer classify it.

**Current preferred product/engineering direction**, because Keeper's Alerts is an informational state observer rather than a provenance database.

### B. Persist mod-owned delivery provenance

Record a mod-owned identity/fingerprint when the exact donkey delivery happens and restore it later.

**Advantage:** can preserve historical provenance beyond native state.

**Disadvantages:** creates a second persisted lifecycle, must handle movement, pickup, save/load and conflicts, and duplicates information the host does not itself model as a durable semantic.

Do not choose this merely for theoretical exactness without a demonstrated player-facing requirement.

### C. Any loose Body in all morgue zones

Too broad. It can include ordinary player-dropped bodies elsewhere in the morgue. Reject as the default predicate.

## Runtime evidence — Corpse State Probe 0.1.0

Frozen probe source/artifact identity is recorded on branch `research/corpse-waiting-state`.

Accepted installed-runtime result:

- exact source: `efaa1c636a79815e66c451244c5dcf7735f9a173`;
- probe: `KeepersAlerts.CorpseStateProbe.0.1.0.dll`;
- supported Assembly-CSharp MVID matched GK 1.407;
- delivery, load reconstruction and final removal were all observed through native `DropsList`;
- no Harmony, mutation, synthetic body generation or save write was used by the probe.

The probe answered its intended question. Do not repeat it without a new contradiction.

## Current evidence gates

### Confession state / detection

For **“is at least one native confession currently available?”**, the canonical owner, save/load truth and normal-play final-consumer seam are established.

Preferred observer:
- postfix `WorldGameObject.RedrawBubble`;
- immediately filter to the two verified confessionals;
- recompute aggregate canonical state from `custom_interaction_events`;
- perform one canonical resync after world/load restoration.

**State-transition observation gate: READY.**

### Corpse state / detection

The physical state lifecycle and save/load behavior are established.

Presentation Probe 0.1.0 confirms the two native receiving endpoints at approximately:
- `morgue_throw_out=(10656,-10992)`;
- `morgue_throw_in=(3672,-1896)`.

Accepted repaired-chute samples settled approximately 35-72 world units from `morgue_throw_out`, so the state can be bounded much more narrowly than an entire world zone.

The remaining geometry/lifecycle work is now closed in `docs/CORPSE_RECEIVING_AREA_CLOSURE.md`.

Accepted implementation model:
- select repaired `morgue_throw_out` when that WGO exists; otherwise use `donkey_cemetery_point`;
- use a narrow direction-aware corridor, not an entire zone;
- recompute on successful `DropsList.Add`, Body `DropResGameObject.CollectDrop`, and `DropsList.FromGameSave`;
- no recurring polling and no mod-owned persisted corpse flag.

**Corpse state-transition / receiving-area gate: READY.**

Persistent HUD ownership/lifecycle is independently READY from Presentation Probe 0.1.0.
