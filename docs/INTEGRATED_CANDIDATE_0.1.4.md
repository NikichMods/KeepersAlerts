# Integrated Production Candidate 0.1.4

**Branch:** `candidate/0.1.4`  
**Target:** Graveyard Keeper 1.407 / BepInEx 5  
**Baseline:** candidate 0.1.3 source/build head `fc9e85b2ef1bafbe49efec99ce72da982e2738b8` plus its documentation-only freeze commit  
**Status:** ready for stable-promotion approval

## Purpose

0.1.4 freezes the final confession transient horizontal alignment selected in the 0.1.3 runtime calibration pass.

The only production presentation change from 0.1.3 is:

- `ConfessionToastIconX`: 5.53 -> **8.89**

All other accepted geometry remains unchanged:

- transient icon Y: 14.06;
- transient icon scale: 1.67;
- stock `+` preserved;
- stock background/panel position/panel scale/animation/timing preserved;
- stock visible-point Y: 80;
- persistent first-slot offset: 24.92;
- persistent slot spacing: 30.68;
- corpse Y / scale: -6.25 / 0.62;
- confession Y / scale: -1.88 / 0.95.

## Runtime acceptance inherited from 0.1.3

The 0.1.3 runtime pass established:

- corpse-only, confession-only and both-active persistent states pack correctly;
- whichever alert is the only active one occupies slot 1;
- both active indicators occupy adjacent compact slots;
- confession transient and stock corpse transient read as the same native visual family;
- stock corpse `+` and confession `+` align exactly;
- combined transient preview may visually overlap, but both visuals and both sounds remain functional; this rare overlap is explicitly accepted and no arbitration/queue is required;
- the Runtime Test Console no longer causes the previous early presentation failure;
- initial resync remains silent and live state transitions still fire through the verified seams.

The user's final live calibration moved only the confession prayer icon from X 5.53 to **X 8.89** and accepted that position as better balanced relative to the stock corpse icon.

Because 0.1.4 hardcodes exactly the already-observed live value into the same private `PrayerIcon.transform.localPosition.x` path, no new host mechanism or lifecycle assumption is introduced.

## Final presentation gate

**READY**

- observable property: confession transient balances the compact prayer icon against the unchanged stock plus sign;
- canonical owner: Keeper's Alerts private clone of the stock `BodyArrivedPanel`;
- final writer/consumer: the private `PrayerIcon` local transform consumed by the cloned `NewBodyArrivedGUI.Display()` animation path;
- blast radius: one private X coordinate only;
- preserved invariants: stock corpse notification, stock plus sign, background, animation, audio, persistent HUD, state observation, save/load and gameplay mechanics;
- acceptance evidence: live 0.1.3 calibration at X 8.89 plus screenshot/user perceptual acceptance;
- gate state: **READY**.

## Log acceptance

The supplied 0.1.3 runtime log showed:
- Keeper's Alerts 0.1.3 and Runtime Test Console 0.1.2 loaded;
- initial silent resync completed with no Keeper's Alerts presentation/state failure;
- corpse clear through `DestroyLinkedHint`;
- confession false -> true through `RedrawBubble`;
- corpse false -> true through `DropsList.Add`.

No Keeper's Alerts error/warning was present in the supplied log.

## Frozen artifact identity

- exact production source/build head: `2b18eefcf999ca0f9cd118a19a82d74a249f122c`
- GitHub Actions run: `36409865438`
- job: `108887262025`
- artifact ID: `10964285252`
- artifact ZIP digest: `sha256:248f625da0deb84694150a88ec545289c45aa026142ea7b5ac0c9a547a34fca6`
- DLL: `KeepersAlerts.0.1.4.dll`
- DLL size: `21,504 bytes`
- DLL SHA-256: `f4736b10a9373fd6ef6c9319a4fef255579f7fd415faf53bbf381f447bf8d86d`
- build: `0 warnings / 0 errors`

The downloaded artifact was independently extracted and its DLL size/SHA-256 matched CI.

Do not rebuild or replace different bytes under candidate version 0.1.4.
