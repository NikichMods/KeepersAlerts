# Keeper's Alerts — Product Direction

**Target:** Graveyard Keeper 1.407 on PC with BepInEx 5  
**Status:** research/design; no production runtime implementation yet.

## Goal

Keeper's Alerts should expose a very small set of important remote states that are already actionable but otherwise require the player to travel somewhere merely to discover whether anything is waiting.

The mod is informational. It should feel like missing vanilla feedback was restored, not like a new management dashboard was added.

## Core scope

### Delivered corpse waiting

Preserve the stock corpse-arrival behavior unchanged and add a persistent reminder only while the delivered loose corpse still warrants player attention.

The stock transient notification path is verified as:

`Flow_BodyArrivedNotify -> NewBodyArrivedGUI.Display()`

### Confession available

Notify when at least one native `confession_available` interaction exists, then keep a persistent reminder until none remain.

Do not duplicate or predict the roll. The canonical availability state is the host-owned confessional `custom_interaction_events` list, which is already serialized/restored by the game.

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

For confession, first investigate whether the dedicated corpse-arrival UI family can be reused or mirrored more faithfully than a generic custom toast.

Persistent indicators should use verified native NGUI/HUD lifecycle and anchoring. Do not position against raw screen dimensions.

No numeric confession count is currently required.

## State ownership summary

See `docs/STATE_OWNERSHIP_RESEARCH.md` for evidence.

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
- collection removes the loose drop;
- `cur_bodies_count` is general morgue occupancy and is **not** a valid "delivered corpse still waiting" flag.

Preferred research direction: track the actual delivered loose drop during the session and determine whether its native position/zone can reconstruct provenance safely after load.

## Current evidence gates

### Confession notification

**BLOCKED — presentation/lifecycle research only.**

The core state owner and persistence are established. Remaining:
- verify the least-sufficient transition-hook family and blast radius;
- verify transient presentation owner;
- verify persistent HUD anchor/lifecycle;
- identify a suitable native church/confession sound, or make an explicit product choice if none exists.

### Corpse persistent reminder

**BLOCKED — state reconstruction + presentation research only.**

The delivery path, loose-drop lifecycle and native persistence are established. Remaining:
- prove a sufficiently narrow post-load predicate for the donkey-delivered loose corpse;
- choose the least-sufficient event/query seam;
- verify persistent HUD anchor/lifecycle.

Do not use `cur_bodies_count` as the reminder state.

## Acceptance invariants

A future implementation must not change:
- corpse delivery schedule or mechanics;
- existing corpse arrival notification behavior;
- confession RNG/probability;
- PrayerClarity: Rebalanced probability effects;
- confession rewards or native interaction;
- unrelated UI/audio;
- save/load semantics.

Runtime acceptance should exercise the real native path and include appearance, clear and save/load only where those behaviors are touched. Do not repeat already accepted upstream mechanics tests without a concrete reason.
