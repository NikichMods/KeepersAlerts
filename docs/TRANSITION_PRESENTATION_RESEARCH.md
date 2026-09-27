# Keeper's Alerts — Transition and Presentation Seam Research

**Target:** Graveyard Keeper 1.407  
**Status:** state-transition observation partly closed; presentation remains research-only.

## Confession transition observer

### Observable property

Keeper's Alerts needs to detect only a semantic transition:

- **inactive -> active:** zero native `confession_available` interactions becomes one or more;
- **active -> inactive:** one or more becomes zero.

The mod must not participate in the roll or rewards.

### Canonical owner

The truth remains the two confessional WGOs' native:

`custom_interaction_events.Contains("confession_available")`

This list is already serialized/restored by the host.

### Final consumer

All three relevant stock mutation paths converge on:

`WorldGameObject.RedrawBubble(bool?)`
-> `ComponentsManager.RefreshBubblesData(bool?)`

Specifically:

- `WorldGameObject.AddInteractionEvent` adds the event then calls `RedrawBubble`;
- `Flow_RemoveInteractionEvent` removes the exact event then calls `RedrawBubble`;
- `WorldGameObject.Interact` consumes the first custom event, fires it, then calls `RedrawBubble`.

`ComponentsManager.RefreshBubblesData` then reads the already-mutated `custom_interaction_events` list and, when nonempty, renders the object's `custom_interaction_icon`.

This makes a filtered **postfix on `WorldGameObject.RedrawBubble`** a stronger transition/resync candidate than patching confession RNG, add, remove and interaction separately.

### Filter / blast radius

Installed balance evidence contains exactly two ObjectDefinitions with:

`custom_interaction_icon="(pray_bubble)"`

They are:

- `church_budka_1`;
- `church_budka_2`.

Therefore a global `RedrawBubble` patch can immediately exit unless the WGO has the verified prayer-bubble definition and can then recompute aggregate canonical confession availability.

The host method itself is broad, but Keeper's Alerts semantic work is bounded to the two verified confessionals.

### Save/load resync

A one-time canonical resync is still required after the world has been restored because no mutation event needs to occur immediately after load.

No Keeper's Alerts confession save state is needed.

### Solution-space comparison

1. **Patch add/remove/interact separately** — more hooks, duplicates host lifecycle knowledge, easier to miss a path.
2. **Periodic polling** — robust but performs recurring work for a discrete event.
3. **Filtered `RedrawBubble` postfix + one-time load resync** — one final-consumer seam during play, canonical truth after mutation, no mechanics coupling.

**Preferred:** option 3.

### Evidence gate — confession state transitions

- **Observable property:** 0 <-> 1+ native available confessions.
- **Canonical owner:** confessional `custom_interaction_events`.
- **Final consumer / commit point:** post-`WorldGameObject.RedrawBubble`, after all relevant mutation paths.
- **Blast radius:** method is global; notification resync is filtered to the two verified `(pray_bubble)` ObjectDefinitions and performs no host mutation.
- **Preserved invariants:** confession RNG/probability, PrayerClarity behavior, rewards, interaction consumption, save/load, unrelated bubbles.
- **Acceptance evidence:** direct 1.407 static lifecycle inspection plus existing accepted confession runtime/graph evidence; final candidate runtime test should verify the UI transitions while using the real roll/interaction path.
- **Gate:** **READY for state-transition observation**, but not yet READY for player-facing presentation.

## Prayer icon rendering

This is now established from prior accepted installed-runtime font/atlas research:

- NGUI symbol sequence `(pray_bubble)` maps to sprite name `icon_pray_bubble`;
- the same mapping is present in the native small-font symbol table;
- the installed icon atlas contains `icon_pray_bubble` at 17x20 pixels.

The stock interaction path still expresses the semantic as the token `(pray_bubble)`, so the preferred native rendering path for Keeper's Alerts is an NGUI `UILabel` using a native bitmap font with symbol rendering enabled. A direct `UI2DSprite` using the verified `icon_pray_bubble` asset is a fallback when a sprite component is materially simpler for the chosen presentation.

Do not pass the token string itself to `EasySpritesCollection.GetSprite`; the token and sprite ID are distinct verified identifiers.

## Corpse receiving-area model

Accepted runtime evidence proves repaired-chute delivery and save/load reconstruction at the physical loose-Body layer.

Relevant native anchors/geometry already established:

- repaired chute: `morgue_throw_out`;
- outside/pre-repair side: `morgue_throw_in` plus authored `donkey_cemetery_point`;
- old runtime evidence places `donkey_cemetery_point` at approximately `(3676,-2016)`;
- `morgue_throw_in` is approximately `(3672,-1896)`;
- the repaired runtime delivery settled roughly 72 world units from `morgue_throw_out`.

This supports a **small endpoint-relative receiving-area predicate**, not a whole-zone query.

A deliberately placed Body inside the same receiving area is intentionally equivalent from the informational UX perspective: the receiving area contains an actionable corpse.

Presentation Probe 0.1.0 verified both native endpoint transforms in the installed game:

- `morgue_throw_out`: world position approximately `(10656,-10992)`;
- `morgue_throw_in`: world position approximately `(3672,-1896)`.

The repaired-chute runtime delivery samples settled approximately 35-72 world units from `morgue_throw_out`. This confirms that a small endpoint-relative envelope is viable and that the whole `zone_id="morgue"` would be unnecessarily broad.

The exact production radius remains a bounded compatibility/design parameter, especially for the pre-repair outside branch, which has not yet been runtime-sampled under the current save. Do not hard-code a large world-space rectangle or use all of `zone_id="morgue"`.

## Stock transient presentation

Corpse arrival is already host-owned:

`Flow_BodyArrivedNotify -> GUIElements.me.body_arrived_gui.Display() -> NewBodyArrivedGUI.Display()`

The donkey graph separately plays:

`donkey_bell`

Presentation Probe 0.1.0 closes the serialized stock hierarchy:

- owner: `UI Root/NewBodyArrivedPanel/BodyArrivedPanel`;
- parent: full-screen `UIPanel` under `UI Root/NewBodyArrivedPanel`;
- panel widget: 108x62;
- background: `UI2DSprite("icon_frame_techno")`, 96x52;
- body image: `UI2DSprite("body_01")`, 96x96;
- plus glyph: native `tiny_font` UILabel;
- installed timings: 0.5 s appear, 1.0 s display, 0.5 s hide.

The stock `NewBodyArrivedGUI.Display()` owns activation and the slide-in/hold/slide-out animation. For a confession sibling cue, the strongest native-first direction is therefore to clone/reuse this presentation family and substitute only the semantic visual content, rather than recreating an unrelated toast animation.

## Confession audio candidate

Existing installed-runtime PrayerClarity research found:

`World/[wgo] church_pulpit/content/church_pulpit(Clone)/PrayFX/pray sound`

with component:

`DarkTonic.MasterAudio.EventSounds`

This is a stronger thematic candidate than reusing `donkey_bell`, but the actual MasterAudio event/sound-group configuration is still not recorded. Presentation Probe 0.1.0 ran while the player/world snapshot was in the morgue and reported the church sound transform as not loaded; this is not negative evidence against the earlier church-loaded observation.

The object name is not evidence of a playable sound ID. Audio remains a separate BLOCKED behavior gate until the same read-only probe captures the church-loaded component or another direct source proves the exact sound-group/event configuration.

## Research-method checkpoint — presentation probe

### Exact questions

1. What is the installed runtime hierarchy, panel, anchoring and visual content of `GUIElements.me.body_arrived_gui`?
2. Which native HUD parent/anchor can host one or two tiny persistent indicators and naturally follow HUD hide/show/resolution?
3. Which live NGUI `UILabel` / `UIFont` context renders `(pray_bubble)`, and does that font expose the symbol sequence directly?
4. What exact `EventSounds` configuration is attached to the church pulpit `PrayFX/pray sound` object?
5. What are the live endpoint/dock transforms for `morgue_throw_in` and `morgue_throw_out`?

### Existing evidence

Static decompilation answers lifecycle but cannot recover serialized prefab hierarchy or MasterAudio component values. Existing historical probes identify relevant objects but did not record enough fields to select a production presentation owner.

### Method decision

A new **load-only, read-only presentation probe is justified**.

It must:
- use no Harmony;
- mutate no UI/audio/game state;
- play no sound;
- execute no FlowCanvas graph;
- write no save data;
- inspect only the live hierarchy/components after the world and GUI exist;
- produce one diagnostic report.

No corpse delivery or confession roll is required for this probe.


## Presentation Probe 0.1.0 accepted result

Accepted installed-runtime report: `KeepersAlerts-presentation-probe-0.1.0.txt`.

### Persistent HUD owner

The runtime HUD is `UI Root/HUD`, with its own `UIPanel`. Static `HUD.Open/Hide` toggles the HUD root itself when windows open/close.

The native `hud left` widget is anchored directly to `UI Root/Screen size panel/Screen size` at the top-left. Therefore a small Keeper's Alerts container parented under the HUD and anchored to that verified screen-size target will inherit native HUD visibility and responsive placement without raw screen arithmetic.

**Persistent HUD ownership/lifecycle gate: READY.**

### Confession transient visual family

The stock corpse-arrival UI hierarchy and its animation owner are now known exactly. A separate Keeper's Alerts instance can reuse/clone that native visual grammar without modifying the existing corpse instance.

Preserved invariant: `GUIElements.me.body_arrived_gui` and stock `Flow_BodyArrivedNotify` remain untouched.

**Confession transient visual-family gate: READY for a visual prototype.**

The final size/placement of the substituted prayer symbol is a perceptual UX decision to validate visually; it is not an unknown host owner.

### Remaining presentation unknown

Only the intended confession **audio resource/configuration** remains materially unresolved. It is independently BLOCKED and does not invalidate the READY visual/persistent-HUD mechanisms.
