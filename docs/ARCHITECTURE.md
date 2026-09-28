# Architecture

**Target:** Graveyard Keeper 1.407 / BepInEx 5  
**Production version:** 0.1.4

Keeper's Alerts is intentionally a thin observer/presentation layer. It derives current state from Graveyard Keeper and uses native UI/audio systems without changing the mechanics that create those states.

## Product invariants

The mod must preserve:
- donkey schedule, delivery, physics, pickup, morgue capacity and the stock corpse-arrival cue;
- confession RNG/probability, including PrayerClarity: Rebalanced changes to the effective probability;
- confession rewards, daily reset and native interaction behavior;
- save/load semantics;
- unrelated HUD, localization, input and audio behavior.

No Keeper's Alerts state is serialized.

## Confession availability

Canonical truth:

`WorldGameObject.custom_interaction_events.Contains("confession_available")`

for the two native confessionals `church_budka_1` and `church_budka_2`.

Normal add/remove/consume mutations converge on `WorldGameObject.RedrawBubble(bool?)`. Keeper's Alerts patches that method with a postfix, immediately filters to the two confessionals, then recomputes the aggregate native state.

Behavior:
- false -> true: update persistent HUD, play one `bell_single` cue, show one confession transient;
- true -> true: no duplicate cue;
- true -> false: clear the persistent indicator;
- load with an already-active confession: persistent indicator only, no synthetic arrival cue.

This deliberately represents **at least one confession is available**. It does not show a count.

## Corpse waiting state

The host does not persist historical “delivered by donkey” provenance. Keeper's Alerts therefore uses a current-world predicate:

**at least one uncollected loose Body occupies the applicable native delivery corridor.**

The corridor is relative to live host anchors:
- pre-repair: `donkey_cemetery_point`, direction Up;
- repaired chute: live `morgue_throw_out`, direction Down.

Bounds:
- lateral half-width: 96 world units;
- backward allowance: 48;
- forward length: 544.

This covers the verified native directional drop envelope while avoiding a whole-graveyard or whole-morgue proxy.

The intentional semantic edge is that a player who deliberately places another loose Body inside that same receiving corridor will also satisfy the reminder predicate. That is preferable to inventing a second persisted provenance system the game itself does not provide.

Event-driven resync:
- `DropsList.Add` postfix after a successful Body add;
- `DropResGameObject.DestroyLinkedHint` postfix after Body pickup has committed `is_collected=true`;
- one silent authoritative resync at `MainGame.OnGameStartedPlaying`.

No recurring scan runs during ordinary play.

## Load boundary

A prefix on `DropsList.FromGameSave` disarms live transition presentation while the host reconstructs saved drops.

`MainGame.OnGameStartedPlaying` then:
1. marks the world ready;
2. ensures presentation objects exist;
3. recomputes confession state silently;
4. recomputes corpse state silently;
5. arms future live-transition presentation.

This prevents restored state from masquerading as a newly arrived event.

## Persistent HUD

The persistent indicators are private children of the live `HUD.bar_energy` transform.

Their first slot is positioned from the energy bar's actual widget width rather than from raw screen coordinates. Active alerts are compacted:
- corpse only -> slot 1;
- confession only -> slot 1;
- both -> corpse slot 1, confession slot 2.

Because the indicators inherit the stock HUD/energy-bar transform chain, normal HUD visibility, resolution anchoring and HUD scaling remain host-owned.

Accepted visual calibration:
- first-slot offset: 24.92 NGUI units beyond the energy-bar edge;
- slot step: 30.68;
- corpse Y / scale: -6.25 / 0.62;
- confession Y / scale: -1.88 / 0.95.

## Confession transient

The confession popup is a private clone of the stock `BodyArrivedPanel` and keeps the stock `NewBodyArrivedGUI.Display()` lifecycle.

Preserved stock properties include:
- background;
- panel geometry;
- appear / hold / hide animation;
- visible Y = 80;
- the stock `+` label.

Only the cloned body image is hidden. A separate prayer label using the native `(pray_bubble)` symbol is added at:
- X = 8.89;
- Y = 14.06;
- scale = 1.67.

The stock corpse-arrival instance is never modified.

If the stock corpse popup and confession popup happen to start at the same instant, they may overlap. Both cues remain functional. This rare overlap is intentionally accepted instead of adding a notification queue for a negligible case.

## Runtime seams

Keeper's Alerts installs six Harmony patches:

| Host seam | Patch | Purpose |
| --- | --- | --- |
| `WorldGameObject.RedrawBubble(bool?)` | postfix | confession state resync, filtered to the two confessionals |
| `DropsList.Add(DropResGameObject)` | postfix | corpse state resync, filtered to successful Body adds |
| `DropResGameObject.DestroyLinkedHint()` | postfix | corpse clear resync, filtered to collected Bodies |
| `DropsList.FromGameSave(...)` | prefix | disarm transition presentation during reconstruction |
| `MainGame.OnGameStartedPlaying()` | postfix | silent authoritative post-load resync and re-arm |
| `HUD.Open()` | postfix | attach/refresh private HUD indicators |

The mod has no per-frame `Update()`, background timer, periodic polling, or custom persistence.

## Compatibility and failure behavior

The plugin validates the exact verified `Assembly-CSharp` MVID before patching host internals. If the host identity or a required member/signature is missing, initialization fails closed and any already-installed Keeper's Alerts patches are removed.

Presentation and state-observation failures are logged once per category rather than repeatedly spamming the shared BepInEx log.

The production package contains only Keeper's Alerts code. It does not ship Graveyard Keeper assets, extracted audio, decompiled host code, or the research test console.
