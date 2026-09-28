# Integrated Production Candidate 0.1.3

**Branch:** `candidate/0.1.3`  
**Target:** Graveyard Keeper 1.407 / BepInEx 5  
**Baseline:** frozen candidate 0.1.1 source `e6f86ee29058fb946eb55567abec4f2b71b3ec8a`  
**Status:** runtime/perceptual acceptance required

Candidate 0.1.2 remains a separate frozen, unselected experiment. 0.1.3 intentionally starts from the exact 0.1.1 handoff source so the already-preferred corpse BodyImage visual is preserved.

## Accepted calibration promoted into the candidate

Persistent HUD calibration from the latest runtime pass:
- first indicator center: 24.92 NGUI units to the right of the stock energy-bar edge;
- slot step: 30.68;
- corpse Y relative to energy bar: -6.25;
- corpse scale: 0.62;
- confession Y relative to energy bar: -1.88;
- confession scale: 0.95.

Confession transient:
- keep the cloned stock BodyArrivedPanel geometry, background, panel scale, X position, animation and timings unchanged;
- keep the stock `+` glyph unchanged;
- hide only the stock BodyImage;
- add a separate `(pray_bubble)` label at X 5.53 / Y 14.06 / scale 1.67;
- retain the native visible-point Y 80 and opaque background.

## Persistent HUD change

Accepted Presentation Probe 0.1.0 already established the required host geometry:
- `HUD.bar_energy` is the live `UIProgressBar` on `UI Root/HUD/hud left/hud spr/bar_energy`;
- the same object has a `UI2DSprite` widget;
- widget size in the accepted runtime snapshot: 91 x 11;
- pivot: TopLeft.

The two private Keeper's Alerts indicators are now parented directly to the live energy-bar transform rather than placed as unrelated coordinates under `hud left`.

Layout uses the live energy-bar widget width:
- if corpse only: corpse occupies slot 1;
- if confession only: confession occupies slot 1;
- if both: corpse occupies slot 1 and confession slot 2.

This preserves the accepted current placement while making it relative to the actual energy-bar edge and inheriting the host HUD transform/scale/resolution behavior.

## Confession transient change

The previous prototype repurposed the cloned stock `PlusText` as the prayer symbol. Candidate 0.1.3 instead preserves the native `+` unchanged and creates a separate private `PrayerIcon` label for `(pray_bubble)`.

The stock corpse-arrival instance remains untouched.

## Preserved behavior

No changes to:
- confession state ownership, RNG, PrayerClarity compatibility, interaction or rewards;
- corpse receiving-area semantics or transition hooks;
- donkey schedule/delivery, stock corpse sound or stock corpse toast;
- save/load arming and silent resync;
- unrelated HUD, input, localization or audio behavior.

## Production gates

### Confession transient polish — READY
- observable property: confession toast uses the stock corpse-arrival visual grammar with `+ [prayer]`;
- canonical owner/final consumer: private clone of stock `BodyArrivedPanel` / `NewBodyArrivedGUI.Display()`;
- blast radius: one private clone;
- acceptance: visual comparison with stock corpse toast.

### Energy-bar-relative persistent layout — READY
- observable property: active indicators form a compact row immediately right of the stock energy bar;
- canonical owner: live `HUD.bar_energy`;
- final consumer: private indicators parented directly to that transform;
- blast radius: private indicator transforms only;
- preserved invariant: stock energy bar and HUD hierarchy are not modified;
- evidence: accepted Presentation Probe 0.1.0 geometry plus current accepted calibration.

## Runtime acceptance focus

1. no Keeper's Alerts presentation error during startup;
2. corpse-only state -> corpse is in the first slot;
3. confession-only state -> confession moves into the first slot rather than leaving a corpse-sized gap;
4. both states -> corpse then confession, matching the accepted visual placement;
5. HUD hide/show still hides/shows both indicators;
6. change to at least one other normal resolution or HUD scale and verify the row remains attached to the energy bar;
7. confession cue renders `+ [prayer]` with the stock panel/background/animation;
8. stock corpse cue remains visually and audibly unchanged;
9. use the research console's combined-cue preview once to observe what happens when both transient notifications start together.

## Frozen artifact identity

- exact production source/build head: `fc9e85b2ef1bafbe49efec99ce72da982e2738b8`
- GitHub Actions run: `36362070783`
- job: `108741070377`
- artifact ID: `10946375643`
- artifact ZIP digest: `sha256:6f1298843b7c34047a26e3ecbf314d608bfff2e4efc21394ab2b999662a25548`
- DLL: `KeepersAlerts.0.1.3.dll`
- DLL size: `21,504 bytes`
- DLL SHA-256: `1c3bf0a17d8d823f7b7e8046e9571276ae13a2cd1bfb762b229f07041322181e`
- build: `0 warnings / 0 errors`

The downloaded artifact was independently extracted and its DLL size/SHA-256 matched CI.

Do not rebuild or replace different bytes under candidate version 0.1.3.
