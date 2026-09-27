# Integrated Production Candidate 0.1.0

**Branch:** `candidate/0.1.0-integrated`  
**Target:** Graveyard Keeper 1.407 / BepInEx 5  
**Status:** runtime acceptance required

## Included behavior

This candidate implements only the already-READY core scope:

- confession availability from native `confession_available` state;
- one `bell_single` cue on a live false -> true confession transition;
- cloned stock corpse-arrival panel as the confession transient visual, with `(pray_bubble)`;
- persistent confession HUD indicator;
- persistent delivered-corpse HUD indicator from the verified native delivery corridor;
- silent post-load resync;
- no mod-owned save state and no recurring polling.

Stock corpse-arrival sound/toast is untouched.

## Runtime hooks

- `WorldGameObject.RedrawBubble(bool?)` postfix, immediately filtered to the two confessionals;
- `DropsList.Add(DropResGameObject)` postfix, successful Body only;
- `DropResGameObject.DestroyLinkedHint()` postfix, collected Body only;
- `DropsList.FromGameSave(GameSave)` prefix, internal presentation disarm only;
- `MainGame.OnGameStartedPlaying()` postfix, authoritative silent resync then re-arm;
- `HUD.Open()` postfix, attach/refresh private HUD children.

## Corpse corridor

The production predicate uses the accepted direction-aware corridor:

- lateral half-width 96;
- backward allowance 48;
- forward length 544;
- pre-repair source: `donkey_cemetery_point`, Up;
- repaired source: live `morgue_throw_out`, Down.

The dedicated 0.1.0 drop-origin probe additionally confirmed at runtime that donkey `GetDropPos()` equals the donkey transform and that the donkey has zero DockPoints.

## Logging policy

Normal logging is intentionally sparse:

- plugin initialization;
- initial silent state snapshot;
- semantic state transitions;
- first state/presentation failure only.

No frame/update spam is emitted.

## Acceptance pass

Exercise only touched behavior:

1. load with no active states -> no false cue;
2. load with an already waiting confession -> persistent confession icon, no `bell_single` and no transient;
3. native confession false -> true -> exactly one `bell_single` + one prayer transient + persistent icon;
4. consume final confession -> persistent icon clears;
5. repaired corpse delivery -> stock corpse behavior remains unchanged and persistent corpse icon appears;
6. pick corpse up -> persistent corpse icon clears;
7. save/load with waiting corpse -> persistent corpse icon restores without a new stock-mod cue;
8. unrelated loose Body outside the delivery corridor does not trigger;
9. menus hide/show the private indicators with the stock HUD;
10. both persistent indicators can coexist without unacceptable overlap.

Visual size/placement remains a candidate-level perceptual item.
