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

The verified `(pray_bubble)` value is not currently evidenced as an `EasySpritesCollection` sprite ID.

The stock path places the string into `BubbleWidgetTextData`. `BubbleWidgetText.Draw` renders that data through an NGUI `UILabel`, applies the native font/style, calls `GJL.EnsureLabelHasCorrectFont`, and then assigns the token as label text.

Therefore the strongest current hypothesis is that `(pray_bubble)` is a native NGUI font-symbol sequence. A persistent HUD indicator should reuse the verified native label/font/symbol context rather than assume a standalone Sprite exists.

This must be confirmed from the installed runtime before production UI is written.

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

The exact production radius remains a design/geometry parameter until the live endpoint hierarchy/dock geometry is inspected. Do not hard-code a world-space rectangle or use all of `zone_id="morgue"`.

## Stock transient presentation

Corpse arrival is already host-owned:

`Flow_BodyArrivedNotify -> GUIElements.me.body_arrived_gui.Display() -> NewBodyArrivedGUI.Display()`

The donkey graph separately plays:

`donkey_bell`

`NewBodyArrivedGUI` animates a serialized GUI hierarchy, so decompiled C# does not reveal the actual child sprites, panel ownership, anchors or final visual geometry. Production confession presentation should not imitate this by guess.

## Confession audio candidate

Existing installed-runtime PrayerClarity research found:

`World/[wgo] church_pulpit/content/church_pulpit(Clone)/PrayFX/pray sound`

with component:

`DarkTonic.MasterAudio.EventSounds`

This is a stronger thematic candidate than reusing `donkey_bell`, but the actual MasterAudio event/sound-group configuration was not recorded. The object name is not evidence of a playable sound ID.

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
