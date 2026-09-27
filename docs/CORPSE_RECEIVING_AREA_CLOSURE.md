# Corpse Receiving-Area and Transition Closure

**Target:** Graveyard Keeper 1.407  
**Status:** accepted static + installed-runtime evidence  
**Production gate:** READY

## Observable state

Keeper's Alerts defines corpse waiting as:

**at least one uncollected loose Body occupies the currently applicable native donkey-delivery corridor.**

This is a current physical-world predicate, not historical provenance.

## Delivery branches

Accepted `npc_donkey` graph evidence establishes:

- pre-repair: `Flow_DropBody` uses the donkey itself as the source and `Direction.Up`;
- repaired chute: `Flow_DropBody` uses `morgue_throw_out` and `Direction.Down`;
- both paths call the same native Body drop machinery;
- `Flow_DropBody` supplies force factor `3f` for directional drops and disables wall-direction correction when direction is not `None`.

Historical installed-runtime logs place `donkey_cemetery_point` at approximately `(3676,-2016)`.

Accepted repaired runtime evidence places `morgue_throw_out` at approximately `(10656,-10992)`.

## Canonical source position

`WorldGameObject.GetDropPos()` uses the WGO center when it has no dock points.

Direct inspection of the user-supplied GK `resources.assets` identifies the native `DockPoint` component from the authored morgue broken-dock prefabs, then scans the relevant prefab hierarchies:

- donkey prefab: no `DockPoint` component;
- repaired `morgue_throw_out`: no `DockPoint` component.

Therefore the directional delivery origin is the source WGO transform itself for both ordinary branches.

Use live native anchors rather than hard-coded coordinates where possible:

- pre-repair origin: `WorldMap.GetGDPointByGDTag("donkey_cemetery_point", ..., ...)`;
- repaired origin: live WGO `morgue_throw_out`.

Pre-repair installed-runtime evidence shows `morgue_throw_out_broken`; its repair craft changes the WGO to `morgue_throw_out`. Thus the presence of the repaired WGO selects the repaired branch; otherwise use the cemetery-point branch.

## Directed movement bound

Native `DropResGameObject` first offsets directional drops by:

`direction * 28.800001`

Then `KickComponent` receives:

`3f * bounce_curve_factor`

with `DropResCurve.duration_factor` authored in the range `0..1`.

The kick:
- normalizes the supplied direction;
- multiplies velocity by `0.96` each fixed step;
- stops the component once its absolute delta falls below `0.01`;
- applies a `0.8` multiplier to vertical movement;
- uses `delta * fixedDeltaTime * 96` world displacement.

GK normally sets `Time.fixedDeltaTime=1/60`. Sleep/waiting raises it to `1/12`, which is the largest fixed timestep assigned by inspected game code.

Using the worst authored duration factor `1`, force `3`, fixed timestep `1/12`, and the native 0.96 decay gives a maximum kick travel of approximately `459.22` world units. Including the immediate 28.8 offset gives approximately `488.02` units along the commanded direction.

Because `Flow_DropBody` passes `check_walls=false` for these Up/Down drops, the direction cannot be rotated by the wall-avoidance branch.

## Production receiving corridor

Use one generic directed corridor relative to the applicable live source:

- lateral half-width: `96` world units;
- backward allowance: `48` world units;
- forward length: `544` world units.

The forward length deliberately exceeds the derived ~488-unit worst case by ~56 units.

For `Up`:
- lateral = absolute X delta;
- forward = +Y delta.

For `Down`:
- lateral = absolute X delta;
- forward = -Y delta.

This is substantially narrower than treating the entire graveyard or morgue as waiting state.

The already accepted semantic edge remains: a player who deliberately places another Body inside the active delivery corridor will satisfy the same physical-state predicate.

## State transition seams

### Add / appearance

`DropResGameObject.Drop` creates the drop and adds it through:

`DropsList.Add(DropResGameObject)`

This is used by normal delivery and by ordinary loose-drop creation.

A postfix on `DropsList.Add`, filtered to successful Body additions, can recompute the canonical corridor predicate.

### Pickup / clear

Normal big-item pickup converges on:

`DropResGameObject.CollectDrop(WorldGameObject)`

which sets `is_collected=true` before inventory transfer and before `DropsList.Update` physically removes the object.

Bodies are big/non-stack-merging items, so ordinary stack-merging removal is not applicable.

A postfix on `CollectDrop`, filtered to Body, can recompute while excluding `is_collected` drops. This observes the current actionable state without waiting for a later frame.

### Save/load resync

`DropsList.FromGameSave(GameSave)` recreates every saved loose drop via the normal `DropResGameObject.Drop` path and then restores its saved `zone_id`.

A postfix on `FromGameSave` gives one authoritative whole-list resync and, crucially, also clears stale in-memory Keeper's Alerts state when a loaded save contains no qualifying Body.

Known direct list-removal exceptions in inspected host code are unrelated:
- a save-migration cleanup removes only stone/marble in a refugee-area rectangle;
- dungeon teardown affects dungeon-local drops;
- invalid-drop cleanup targets null/broken drop definitions.

They do not create a normal cemetery/morgue Body-removal path outside `CollectDrop`.

## Production evidence gate

### Corpse persistent reminder state

- **Observable property:** persistent reminder is present iff an uncollected loose Body occupies the currently applicable native delivery corridor.
- **Canonical owner:** `DropsList.me.drops` + live Body position; branch anchor is repaired `morgue_throw_out` or pre-repair `donkey_cemetery_point`.
- **Final writer / consumer:** `DropResGameObject.Drop` establishes physical drop; `CollectDrop` commits pickup; `FromGameSave` reconstructs saved loose-drop truth.
- **Blast radius:** three narrow Harmony postfixes; all immediately filter/recompute read-only state and do not alter host return values or data.
- **Preserved invariants:** donkey schedule/delivery, stock bell/toast, Body physics, pickup, morgue occupancy, save/load, manually moved bodies, unrelated drops.
- **Acceptance evidence:** accepted repaired delivery/save/load/pickup runtime trace + accepted donkey graph + direct installed asset geometry + inspected host drop/kick lifecycle.
- **Gate:** **READY**.

The integrated production candidate must still exercise:
1. existing repaired-chute delivery -> indicator appears;
2. pickup -> indicator clears;
3. save/load with waiting corpse -> indicator restores;
4. unrelated loose Body elsewhere in graveyard does not trigger.

A pre-repair runtime replay is not required before implementation because the source position, direction and movement envelope are now bounded from accepted host evidence; it can be added later only if a real runtime contradiction appears.
