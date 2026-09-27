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

For confession, inspect whether the dedicated corpse-arrival UI family can be reused or mirrored more faithfully than a generic custom toast.

Persistent indicators should use verified native NGUI/HUD lifecycle and anchoring. Do not position them by raw screen arithmetic.

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

**BLOCKED — presentation/lifecycle research only.**

State ownership and persistence are established. Remaining:
- verify least-sufficient transition hooks and blast radius;
- verify transient presentation owner;
- verify persistent HUD anchor/lifecycle;
- identify a suitable native church/confession sound or make an explicit product choice if none exists.

### Corpse persistent reminder

**BLOCKED — bounded receiving-area + presentation research only.**

The delivery path, loose-drop lifecycle and save/load reconstruction are established. Remaining:
- verify a bounded receiving-area predicate for both pre-repair and repaired-chute states;
- choose the least-sufficient live transition/resync seam;
- verify persistent HUD anchor/lifecycle.

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
