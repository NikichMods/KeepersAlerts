# Production Gate — Core Candidate 0.1.0

**Target:** Graveyard Keeper 1.407 / BepInEx 5  
**Scope:** first integrated Keeper's Alerts runtime candidate  
**Gate state:** READY

## 1. Confession state transition

- **Observable property:** persistent confession indicator mirrors whether at least one native `confession_available` event exists; one event cue/toast occurs only on a real unavailable -> available transition.
- **Canonical owner:** confessional `WorldGameObject.custom_interaction_events`.
- **Final consumer:** `WorldGameObject.RedrawBubble(bool?)` runs after native add/remove/consume mutations and refreshes the stock interaction presentation.
- **Mechanism:** postfix `RedrawBubble`, filtered immediately to `church_budka_1` / `church_budka_2`, then aggregate canonical resync.
- **Load boundary:** a prefix on `DropsList.FromGameSave` disarms Keeper's Alerts transition presentation before reconstructed drops are added. `MainGame.OnGameStartedPlaying()` then performs the authoritative silent state resync and only afterward re-arms live transition cues. This also handles loading another save in the same game process without allowing restored state to masquerade as a new event.
- **Blast radius:** one filtered WGO postfix; no mutation of WGO events or confession mechanics.
- **Preserved invariants:** stock/Rebalanced RNG, interaction consumption, rewards, save/load, bubble rendering.
- **Evidence:** STATE_OWNERSHIP_RESEARCH + accepted PrayerClarity lifecycle evidence.
- **Gate:** READY.

## 2. Confession event cue

- **Observable property:** one short native sound on the real false -> true transition.
- **Canonical owner:** stock `Sounds.PlaySound` / MasterAudio.
- **Selected resource:** `bell_single`.
- **Final consumer:** native sound subsystem.
- **Blast radius:** one play request; no sound-resource or mixer mutation.
- **Preserved invariants:** sermon/prayer audio and unrelated sound groups.
- **Evidence:** direct asset identification plus installed-runtime A/B acceptance.
- **Gate:** ACCEPTED.

## 3. Confession transient visual

- **Observable property:** a short sibling notification using the stock corpse-arrival visual grammar.
- **Canonical owner/final consumer:** `NewBodyArrivedGUI.Display()` and its serialized `BodyArrivedPanel`.
- **Mechanism:** clone the stock `BodyArrivedPanel` under the same native parent; preserve its animation/timings/background; hide the cloned body image and replace the cloned `tiny_font` label content with `(pray_bubble)`. Never modify `GUIElements.me.body_arrived_gui`.
- **Blast radius:** one private cloned notification instance.
- **Preserved invariants:** stock corpse notification instance and timing.
- **Evidence:** Presentation Probe 0.1.0 + verified prayer font symbol.
- **Gate:** READY for visual acceptance.

## 4. Corpse persistent state

- **Observable property:** corpse reminder iff an uncollected loose Body occupies the applicable native delivery corridor.
- **Canonical owner:** `DropsList.me.drops` + live native delivery anchor.
- **Final writer/commit points:** `DropsList.Add` establishes a new loose Body; both normal collection and overhead Body pickup have set `is_collected=true` before `DropResGameObject.DestroyLinkedHint`; reconstructed drop truth is complete before `MainGame.OnGameStartedPlaying`.
- **Mechanism:** postfix `DropsList.Add`, filtered to successful Body adds; postfix `DropResGameObject.DestroyLinkedHint`, filtered to collected Body; use the verified direction-aware 96 / 48 / 544 corridor.
- **Load boundary:** prefix `DropsList.FromGameSave` disarms transition presentation before drop reconstruction; postfix `MainGame.OnGameStartedPlaying` performs authoritative silent resync and only then re-arms live transition cues.
- **Blast radius:** one generic add postfix and one generic hint-destroy postfix, both immediately Body-filtered, one load prefix that changes only Keeper's Alerts internal arming state, plus one game-started postfix shared by initial state resync.
- **Preserved invariants:** delivery, physics, stock bell/toast, pickup, morgue count, save data, unrelated drops.
- **Evidence:** CORPSE_RECEIVING_AREA_CLOSURE + accepted runtime trace.
- **Gate:** READY.

## 5. Persistent HUD presentation

- **Observable property:** one/two small indicators remain visible only while their corresponding state is actionable and follow stock HUD visibility.
- **Canonical owner:** `UI Root/HUD/hud left`, already anchored to `UI Root/Screen size panel/Screen size`.
- **Final consumer:** native NGUI under the stock HUD panel.
- **Mechanism:** create a private child container under `hud left`; reuse loaded stock `UI2DSprite` / `UILabel` assets. A postfix on `HUD.Open()` is the lifecycle seam for attach/re-attach and refreshing the already-known indicator state. State restoration/re-arming remains owned by `MainGame.OnGameStartedPlaying()`. `HUD.Hide()` needs no patch because parent deactivation hides children automatically.
- **Blast radius:** one zero-argument HUD postfix and private child objects only.
- **Preserved invariants:** stock HUD transforms/anchors/components are not rewritten.
- **Evidence:** Presentation Probe 0.1.0.
- **Gate:** READY for visual acceptance.

## Candidate acceptance

The first candidate should prove:
1. existing waiting confession after load -> persistent icon, no false event sound/toast;
2. native confession false -> true -> `bell_single` + transient prayer toast + persistent icon;
3. confession consumption -> persistent icon clears;
4. repaired corpse delivery -> stock behavior unchanged + persistent corpse icon appears;
5. corpse pickup -> icon clears;
6. save/load with waiting corpse -> icon restores;
7. unrelated loose Body elsewhere in graveyard does not trigger;
8. ordinary menus hide/show Keeper's Alerts together with stock HUD;
9. both indicators can coexist without overlapping stock HUD.

Visual size/spacing is explicitly a candidate-level perceptual acceptance item, not an unresolved host-owner assumption.
