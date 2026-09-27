# Keeper's Alerts — Product Direction

**Target:** Graveyard Keeper 1.407 on PC with BepInEx 5  
**Status:** research/design; no production runtime implementation yet.

## Goal

Keeper's Alerts should expose a very small set of important remote states that are already actionable but otherwise require the player to travel somewhere merely to discover whether anything is waiting.

The mod is informational. It should feel like missing vanilla feedback was restored, not like a new management dashboard was added.

## Core scope

### Corpse waiting

Preserve the stock corpse-arrival behavior unchanged and add a persistent reminder while a loose Body still occupies the native corpse receiving/delivery area.

The stock transient path is verified as:

`Flow_BodyArrivedNotify -> NewBodyArrivedGUI.Display()`

The stock arrival graph also uses `donkey_bell`.

The persistent reminder should reflect current actionable world state, not maintain a parallel historical “delivery happened” flag unless native state later proves insufficient.

### Confession available

Notify when at least one native `confession_available` interaction exists, then keep a persistent reminder until none remain.

Do not duplicate or predict the roll. Canonical availability is the host-owned confessional `custom_interaction_events` list, which is serialized/restored by the game.

The verified stock local icon semantic is `(pray_bubble)`.

Desired presentation:
- a thematically appropriate church/confession sound when availability first appears;
- a transient `(pray_bubble)` treatment that feels like a sibling of the stock corpse-arrival cue;
- a small persistent HUD indicator while at least one confession remains available.

## Product-class rule for future events

Do not add an event merely because it can be observed.

A candidate should normally be:
- asynchronous or remote;
- discrete and actionable;
- important enough to justify attention;
- insufficiently visible without manual location checking;
- bounded enough not to create recurring HUD spam.

Current core scope is corpse delivery and confession availability.

Merchant/tavern income, crops, furnaces, zombie production, weekly NPC schedules and ordinary quest availability remain outside core scope unless new evidence changes that assessment.

## Current solution direction

Transient and persistent feedback serve different jobs:

- transient cue: **something just happened**;
- persistent indicator: **it is still waiting**.

For corpse delivery, preserve the game's native transient path.

For confession, use the verified stock corpse-arrival presentation family as the sibling transient visual direction: its owner, hierarchy, assets and timings are now known from installed runtime evidence. Preserve the stock corpse instance unchanged.

Persistent indicators should live under the verified `UI Root/HUD` lifecycle and use a native screen-size anchor (the top-left `hud left` anchor family is verified). Do not position them by raw screen arithmetic.

The confession symbol is also closed: native `(pray_bubble)` maps to `icon_pray_bubble` (17x20 in the installed icon atlas).

No numeric confession count is currently required.

## State ownership summary

See `docs/STATE_OWNERSHIP_RESEARCH.md`.

### Confession

Established:
- native truth is `custom_interaction_events.Contains("confession_available")`;
- add/remove and interaction-consume lifecycle are host-owned;
- native save/load preserves the interaction event list;
- `(pray_bubble)` is the stock icon semantic.

Implication: do not persist a Keeper's Alerts confession flag.

### Corpse

Established:
- ordinary donkey delivery creates a real Body `DropResGameObject`;
- loose drops live in `DropsList.me.drops` and are serialized/restored by the host;
- runtime testing proves the delivered loose body is reconstructed after load with the same saved position/zone and is removed after pickup;
- Unity instance identity changes across reconstruction and must not be treated as persistent;
- `cur_bodies_count` is general morgue occupancy and is not the reminder state;
- the host does not save an explicit “delivered by donkey” provenance marker.

Current semantic direction: observe **loose Body occupancy of the bounded native receiving area** rather than persist a parallel provenance flag. This remains truthful to the actionable player state, with a documented edge case if another body is deliberately placed in that same area.

## Current evidence gates

### Confession notification

The behavior is split into independent evidence gates:

- **state-transition observation: READY** — filtered post-`WorldGameObject.RedrawBubble` resync plus world/load resync;
- **persistent HUD ownership/lifecycle: READY** — `UI Root/HUD` with native screen-size anchoring;
- **transient visual family: READY for prototype** — clone/reuse the verified `NewBodyArrivedGUI` presentation family without mutating the stock corpse instance;
- **prayer visual asset: READY** — `(pray_bubble)` -> `icon_pray_bubble`;
- **confession audio: BLOCKED** — exact MasterAudio event/sound-group configuration remains unknown.

A BLOCKED audio gate must not be bundled into production by guessing. It is independent of the READY visual/state mechanisms.

### Corpse persistent reminder

**BLOCKED only on final receiving-area/state-transition envelope; presentation ownership is READY.**

Established:
- delivery path, loose-drop lifecycle and save/load reconstruction;
- repaired endpoint and delivered-body geometry;
- both `morgue_throw_in` and `morgue_throw_out` native transforms;
- persistent HUD parent/anchor lifecycle.

Remaining:
- choose/verify the bounded receiving-area predicate that safely covers both pre-repair and repaired-chute states;
- close the least-sufficient live add/remove/load resync seam for that predicate.

Do not use `cur_bodies_count`.

## Acceptance invariants

A future implementation must not change:
- corpse delivery schedule or mechanics;
- existing corpse arrival notification behavior;
- confession RNG/probability;
- PrayerClarity: Rebalanced probability effects;
- confession rewards or native interaction;
- unrelated UI/audio;
- save/load semantics.

Runtime acceptance should exercise the real native path and include only behavior newly touched by a candidate. Do not repeat already accepted upstream mechanics tests without a concrete reason.
