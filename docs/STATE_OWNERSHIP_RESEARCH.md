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


## Corpse receiving-area closure

Further direct inspection closes the remaining pre-repair geometry and normal-play resync seams.

### Pre-repair source point

The stock donkey ObjectDefinition has `drop_point=Auto`.

`WorldGameObject.GetDropPos()` returns the WGO transform itself when there are no DockPoint components. Direct inspection of the installed `resources.assets` donkey prefab found no DockPoint component anywhere in its hierarchy.

Therefore the pre-repair / outside `Flow_DropBody` node, whose `WGO to drop` input is null/self and whose direction is `Up`, drops from the donkey WGO's actual transform position.

The stock donkey route targets `donkey_cemetery_point=(3676,-2016)`. Accepted historical runtime logs show delivery-time donkey positions at or very near that authored point, including:
- `(3676.0,-2016.0)` during the initial donkey/body sequence;
- `(3677.0,-2016.0)`;
- approximately `(3686.4..3686.5,-2016.0)` in later ordinary deliveries.

The movement graph uses speed 1.2 and does not snap NPCs to the exact destination, explaining the small observed X offset.

### Native directional-drop bound

`Flow_DropBody` uses force factor 3 for directional drops.

`DropResGameObject.Drop` first applies an immediate 28.800001-unit directional offset, then `KickComponent` decays the normalized kick by factor 0.96 each fixed step. GK startup sets `Time.fixedDeltaTime=1/60`. The drop-curve `duration_factor` field is authored in the range 0..1.

At the maximal authored factor 1:
- horizontal dynamic kick displacement is approximately 114.80 world units;
- vertical dynamic kick displacement is approximately 91.84 world units because vertical movement uses the native 0.8 factor;
- including the initial 28.8-unit offset, maximum source-relative displacement is approximately 143.61 world units.

Accepted repaired-chute runtime samples are smaller: roughly 35-72 units from `morgue_throw_out`.

### Selected receiving-area predicate

The current corpse-waiting truth is:

**at least one uncollected loose Body in `DropsList.me.drops` lies within 192 world units of either native delivery anchor:**
- pre-repair/outside: `WorldMap.GetGDPointByName("donkey_cemetery_point", false)`;
- repaired chute: live WGO `custom_tag="morgue_throw_out"`.

192 equals two native 96-unit world steps. It exceeds the maximal authored directional-drop displacement with margin for the observed NPC arrival offset while remaining a small local receiving area rather than a morgue/graveyard-wide proxy.

No `zone_id` filter is required. This is intentional: the pre-repair and repaired branches occupy different world/zone contexts, while the physical Body + bounded native-anchor geometry is the shared semantic.

Known product edge remains explicit: a player deliberately placing another loose Body in one of these small receiving areas will satisfy the same actionable-state predicate.

### Live resync seams

#### Add / drop

Use a postfix on the exact public static `DropResGameObject.Drop(Vector3, Item, Transform, Direction, float, int, bool, bool)` overload.

After it returns:
- the Body drop has been created and inserted into `DropsList`;
- native zone metadata has been assigned;
- the initial directional offset / kick has been initiated;
- a canonical receiving-area resync can query `DropsList`.

Filter immediately to Body items before resync. This also safely self-heals if a player manually drops a Body into or out of the receiving area.

During save restoration the same host method is used with `Direction.IgnoreDirection`; transition presentation must remain suppressed until the initial world-ready resync described below.

#### Clear / pickup

Body pickup has a special large-item path: `BaseCharacterComponent.TryOtherInteractions` can set the highlighted Body drop's `is_collected=true` directly instead of calling `DropResGameObject.CollectDrop`.

Both the normal `CollectDrop` path and the large-item overhead pickup path then call `DropResGameObject.DestroyLinkedHint()` after `is_collected` becomes true. Stack-merging also uses this method, but `DoTryMerging` explicitly returns for `definition.is_big`, so Bodies do not enter the merge-removal path.

Therefore a postfix on `DropResGameObject.DestroyLinkedHint()`, filtered to `is_collected && Body`, is the least-sufficient common normal-play clear seam. The canonical query ignores collected drops even before `DropsList.Update` destroys/removes them on the following update.

#### Load / initial resync

The load sequence restores drops through `WorldMap.FromGameSave -> DropsList.FromGameSave` before the final gameplay callback.

`MainGame.OnGameStartedPlaying()` occurs after the reconstructed drop list exists and after the HUD has reopened. A postfix there is the preferred one-time canonical initial resync for both corpse and confession state.

Initial resync establishes persistent indicators only. It must **not** synthesize transient/audio notifications for a state that was already active in the save.

### Corpse state/detection gate

- **Observable property:** persistent reminder exactly while an uncollected loose Body occupies either bounded native delivery receiving area.
- **Canonical owner:** `DropsList.me.drops` Body entries and their current world positions; native delivery anchors `donkey_cemetery_point` and `morgue_throw_out`.
- **Final writer / consumer:** `DropResGameObject.Drop` after drop creation/metadata/kick initiation; pickup sets `is_collected` before `DestroyLinkedHint`; save restoration completes before `MainGame.OnGameStartedPlaying`.
- **Blast radius:** generic drop/pickup methods are shared globally, but every callback returns immediately unless the affected item is a Body; the canonical query examines only Body drops near two small anchors.
- **Preserved invariants:** donkey delivery, stock arrival cue, body physics/pickup, morgue occupancy, save/load, unrelated drops and UI remain unchanged.
- **Acceptance evidence:** accepted repaired-chute delivery/save-load/pickup runtime test; historical real donkey arrival logs for the outside source; direct installed-asset prefab inspection; GK 1.407 decompiled drop/movement/load lifecycle.
- **Gate: READY for production implementation.**
