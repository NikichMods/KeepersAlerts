# Keeper's Alerts — Product Direction

**Target:** Graveyard Keeper 1.407 on PC with BepInEx 5  
**Status:** research/design; no production runtime implementation yet.

## Goal

Keeper's Alerts should expose a very small set of important remote states that are already actionable but otherwise require the player to travel somewhere merely to discover whether anything is waiting.

The mod is informational. It should feel like missing vanilla feedback was restored, not like a new management dashboard was added.

## Core scope

### Delivered corpse waiting

The stock game already treats corpse delivery as an important remote event and provides a dedicated transient arrival notification through `Flow_BodyArrivedNotify -> NewBodyArrivedGUI.Display()`.

Keeper's Alerts should preserve that stock arrival behavior unchanged and add the missing persistent reminder while the delivered corpse still warrants player attention.

### Confession available

At least one confessional may become actionable through the game's native daily confession logic. Existing accepted PrayerClarity research establishes that the stock daily path uses `church_budka_roll` and the player parameter `confession_probability`.

Keeper's Alerts must observe the resulting native availability state rather than duplicate or predict the roll.

Desired presentation:
- one thematically appropriate church/confession sound when availability first appears;
- a transient icon treatment that feels like a sibling of the stock corpse-arrival cue;
- a small persistent HUD indicator while at least one native confession remains available.

## Product-class rule for future events

Do not add an event merely because it can be observed.

A candidate should normally be:
- asynchronous or remote;
- discrete and actionable;
- important enough to justify attention;
- insufficiently visible without manual location checking;
- bounded enough not to create recurring HUD spam.

Current research finds corpse delivery and confession availability to be strong fits.

Merchant/tavern income, crops, furnaces, zombie production, weekly NPC schedules and ordinary quest availability are not core scope unless new evidence changes that assessment.

## Current solution direction

Transient and persistent feedback serve different jobs:

- transient cue: **something just happened**;
- persistent indicator: **it is still waiting**.

For corpse delivery, reuse/preserve the game's native transient path.
For confession, first investigate whether the dedicated corpse-arrival UI family can be reused or mirrored more faithfully than a generic custom toast.

Persistent indicators should use verified native NGUI/HUD lifecycle and anchoring. Do not position against raw screen dimensions or invent a parallel persistent state when canonical game state can be queried safely.

No numeric count is currently required for confession. The primary product need is knowing that there is a reason to return to church.

## Current evidence gates

### Confession waiting indicator

**BLOCKED — research only.**

Unknowns still requiring closure:
- exact add/remove owner for `confession_available`;
- persistence/reconstruction behavior across save/load;
- final consumer that drives the local prayer icon;
- clean transition seam for no confession -> one or more, and back;
- best native HUD anchor/presentation owner;
- suitable existing audio resource, if one exists.

### Corpse waiting indicator

**BLOCKED — research only.**

Known:
- stock transient arrival notification owner is `Flow_BodyArrivedNotify -> NewBodyArrivedGUI.Display()`.

Unknowns still requiring closure:
- canonical state representing a newly delivered corpse that still waits for the player;
- exact lifecycle/clear point;
- save/load behavior of that waiting state;
- best event/query seam for a persistent reminder.

## Acceptance invariants

A future implementation must not change:
- corpse delivery schedule or mechanics;
- existing corpse arrival notification behavior;
- confession RNG/probability;
- PrayerClarity: Rebalanced probability effects;
- confession rewards or native interaction;
- unrelated UI/audio;
- save/load semantics.

Runtime acceptance should exercise the real native path and should include state appearance, clear, and save/load where relevant. Avoid repeating already accepted upstream mechanics tests when the notification layer is strictly downstream.
