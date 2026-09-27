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


## Frozen artifact identity

- exact production source commit: `d5c4688a852564157cd5a965cf2897f2cd851b3c`
- GitHub Actions run: `36355506541`
- job: `108722323025`
- artifact ID: `10943478181`
- artifact ZIP digest: `sha256:2fd6f5a99c61bb2c044733b2ea32c46cc0167f534cf49664464a394f62def5fd`
- DLL: `KeepersAlerts.0.1.0.dll`
- DLL size: `20,480 bytes`
- DLL SHA-256: `189e81b21faccd23410b296117827d553d4700eb35680f7c3715a47e6388cf68`
- build: `0 warnings / 0 errors`

The downloaded artifact was independently extracted and the DLL size/SHA-256 matched CI.

**Immutable handoff rule:** do not rebuild or replace different bytes under candidate version 0.1.0. Any source change after this handoff requires a new candidate version.
