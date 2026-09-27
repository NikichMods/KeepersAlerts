# Integrated Production Candidate 0.1.2

**Branch:** `candidate/0.1.2`  
**Target:** Graveyard Keeper 1.407 / BepInEx 5  
**Status:** runtime acceptance required

## Runtime finding from 0.1.1

The first integrated runtime pass proved the state layer is working at load:
- the plugin loaded normally;
- silent resync reported `confession=False, corpse=True`;
- the screenshot showed no visible persistent corpse reminder.

Therefore the failure is presentation-only, not corpse-state detection.

## Change from 0.1.1

The corpse HUD reminder now follows the already-approved persistent-HUD design instead of cloning the corpse-arrival BodyImage:

- clone the stock `tiny_font` `PlusText` UILabel;
- set its text to the native font token `(body)`;
- keep it under the verified `hud left` anchor;
- use the same native-label family as the confession `(pray_bubble)` indicator.

This removes the mismatch between the production gate and the 0.1.1 implementation. No state observation, corpse geometry, save/load behavior, confession logic, transient UI, or audio behavior changes.

## Production checkpoint

- observable property: active corpse waiting state produces a visible small native `(body)` HUD symbol;
- canonical presentation owner: stock tiny_font UILabel / NGUI font-symbol rendering;
- final consumer: private cloned UILabel under `UI Root/HUD/hud left`;
- blast radius: one private corpse-indicator object;
- preserved invariants: stock HUD transforms/anchors, corpse arrival cue/toast, corpse mechanics, confession behavior;
- acceptance evidence: screenshot with corpse state active plus HUD hide/show check;
- gate: **READY**.

## Runtime acceptance focus

1. load the same save where 0.1.1 reported `corpse=True`;
2. confirm a small body symbol is now visible;
3. open/close a normal GUI that hides the HUD and confirm the symbol follows it;
4. visual judgment: placement and scale should be unobtrusive and not collide with the stock day wheel/energy bar;
5. provide screenshot + log.

A separate research-only Keeper's Alerts Test Console is being established for fast presentation/event simulation so later iterations do not require waiting for natural donkey/confession timing.


## Frozen artifact identity

- exact production source commit: `3f85508064704bf2ad95647859b26073bca8c939`
- GitHub Actions run: `36356718926`
- job: `108725801576`
- artifact ID: `10944126976`
- artifact ZIP digest: `sha256:0586592ad2e38f7052557f8baff0e152c9cca4a3ef9f830aff1c67ae08eea496`
- DLL: `KeepersAlerts.0.1.2.dll`
- DLL size: `20,992 bytes`
- DLL SHA-256: `aabd996461c00f5b389da3ae2552da100cc7edc9a16a56beddeae59a8ad46ac9`
- build: `0 warnings / 0 errors`

The artifact is frozen but **not selected for runtime handoff**. The 0.1.1 screenshot proved its corpse visual already renders; UI calibration should happen against 0.1.1 first rather than changing the corpse visual family prematurely.

Do not rebuild or replace different bytes under candidate version 0.1.2.
