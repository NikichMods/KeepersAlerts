# Keeper's Alerts 0.1.0 — Runtime Candidate

**Status:** frozen runtime candidate; awaiting installed-game acceptance  
**Target:** Graveyard Keeper 1.407 / BepInEx 5

## Source / build identity

- exact source commit: `faa04737fe895bfe60ea81b124a7c54f4f94e3c3`
- GitHub Actions run: `36352230502`
- artifact ID: `10942598262`
- artifact name: `KeepersAlerts-0.1.0-candidate-faa04737fe895bfe60ea81b124a7c54f4f94e3c3`
- canonical installed DLL: `KeepersAlerts.dll`
- DLL size: `22,528 bytes`
- DLL SHA-256: `f3b6aac65164d8a2cdcbf653828ccb95c7a966ecd8e0b18eddb8e9563c6f0fb1`
- build: `0 warnings / 0 errors`
- supported Assembly-CSharp MVID: `6f50b8e7-156b-49ac-bbe8-7505894b2364`

Do not rebuild or replace different bytes under candidate version 0.1.0. Any runtime-source change after this handoff requires a new candidate version.

## Included behavior

### Corpse
- stock donkey bell and stock corpse-arrival toast remain untouched;
- a persistent HUD indicator reflects current loose-Body occupancy of the accepted native delivery corridor;
- recompute is event-driven on Body add, Body pickup and save reconstruction;
- no recurring state polling and no mod-owned save flag.

### Confession
- canonical state is the native `confession_available` event on the two confessionals;
- persistent HUD indicator follows aggregate availability;
- real unavailable -> available transition plays stock `bell_single`;
- the transition also shows a private clone of the stock corpse-arrival toast family with the prayer icon substituted;
- load restoration is silent: an already-waiting confession restores the persistent indicator without replaying the event cue.

### HUD
- private indicator container is parented under the verified native `hud left` owner;
- stock HUD transforms/anchors/components are not rewritten;
- menu-driven HUD hide/show naturally hides/shows the indicators.

## Runtime acceptance

Before testing, remove/disable Keeper's Alerts research DLLs so only `KeepersAlerts.dll` owns the project behavior.

Required acceptance observations:
1. normal save loads without Keeper's Alerts errors;
2. waiting corpse after load -> corpse indicator visible;
3. corpse pickup -> corpse indicator clears;
4. new repaired-chute delivery -> stock bell/toast still occurs and persistent corpse indicator appears;
5. existing confession after load -> confession indicator visible, but no false `bell_single` / transient toast merely because the save loaded;
6. real confession unavailable -> available transition -> one `bell_single`, one prayer transient, persistent confession indicator visible;
7. consuming the last available confession -> persistent confession indicator clears;
8. ordinary GUI windows hide/show both indicators together with the stock HUD;
9. visual placement/scale is acceptable and does not cover stock HUD;
10. an unrelated loose Body elsewhere in the graveyard does not create the corpse indicator.

If a behavior fails, return `BepInEx/LogOutput.log` from that exact session and describe the visible symptom. State-change logging is intentionally concise rather than per-frame.
