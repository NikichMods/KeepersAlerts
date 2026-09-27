# Presentation Runtime Acceptance — 2026-09-27

**Target:** Graveyard Keeper 1.407  
**Probe:** Keeper's Alerts Presentation Probe 0.1.0  
**Status:** accepted for stock corpse toast hierarchy, HUD anchoring/lifecycle, and morgue endpoint geometry; church audio not captured

## Probe identity

- source commit: `31a314568375fc172255901378dcc4205442d4b0`
- frozen branch documentation commit: `8a0862204f2829031e0898de3527ab8fa0b70a8c`
- GitHub Actions run: `36317213484`
- artifact ID: `10930868874`
- DLL: `KeepersAlerts.PresentationProbe.0.1.0.dll`
- DLL size: `26,624 bytes`
- DLL SHA-256: `16184e1cbc948655c1a82519e8ed404aa8e2b13983c7a7a689934a694b0c5d8c`
- Assembly-CSharp MVID: `6f50b8e7-156b-49ac-bbe8-7505894b2364`
- contract: load-only/read-only; no Harmony, UI mutation, audio playback, FlowCanvas execution or save write

Do not rebuild or replace different bytes under probe version 0.1.0.

## Accepted stock corpse-arrival presentation

Installed runtime hierarchy:

`UI Root/NewBodyArrivedPanel/BodyArrivedPanel`

Owner:
- `NewBodyArrivedGUI`
- parent `UI Root/NewBodyArrivedPanel` is a full-screen NGUI `UIPanel`
- panel widget size: 108x62

Children:
- `Background`: `UI2DSprite("icon_frame_techno")`, 96x52
- `BodyImage`: `UI2DSprite("body_01")`, 96x96
- `PlusText`: `UILabel` using `tiny_font`, text `+`

Installed serialized timings:
- appear: 0.5 s
- display: 1.0 s
- hide: 0.5 s
- visible-point Y: 80

Static `NewBodyArrivedGUI.Display()` confirms that the component itself owns activation and slide-in/hold/slide-out animation.

Reusable implication: a confession transient cue can use this exact native presentation family as a sibling without recreating the animation from scratch. The stock corpse instance must remain untouched.

## Accepted persistent HUD owner

Runtime owner:

`UI Root/HUD`

The HUD has its own full-screen `UIPanel`.

Verified responsive top-left anchor family:

`UI Root/HUD/hud left`

That widget anchors to:

`UI Root/Screen size panel/Screen size`

with top-left relative anchoring rather than raw pixel/screen calculations.

Static `HUD.Open()` / `HUD.Hide()` toggles the HUD root itself, including normal GUI-window lifecycle.

Reusable implication: a Keeper's Alerts indicator container parented under `UI Root/HUD` and anchored to the verified screen-size target naturally follows HUD visibility and resolution behavior.

## Prayer icon evidence

The probe's inactive InteractionBubble prefab did not contain an instantiated dynamic text widget at snapshot time, so its local symbol scan was empty.

This does not leave the icon unresolved. Prior accepted installed-runtime PrayerClarity/font research already establishes:

`(pray_bubble) -> icon_pray_bubble`

and installed atlas evidence gives `icon_pray_bubble` a 17x20 texture footprint.

Therefore the native confession visual semantic is closed without another runtime probe.

## Morgue endpoint geometry

Presentation Probe 0.1.0 verified:

- `morgue_throw_out=(10656,-10992,-2297.572)`, active in the repaired state;
- `morgue_throw_in=(3672,-1896,-374.235)`, present but inactive in the repaired state.

Accepted Corpse State Probe samples place repaired-chute delivered bodies roughly 35-72 world units from `morgue_throw_out` after settling.

Reusable implication: the corpse reminder predicate should use a small endpoint-relative receiving area, not the entire `morgue` zone.

## Church audio — not closed

The report returned:

`sound_transform=<not found>`

The captured HUD zone was the morgue, and the church pulpit hierarchy was not loaded into this runtime snapshot.

Prior accepted church-loaded evidence proves that:

`World/[wgo] church_pulpit/content/church_pulpit(Clone)/PrayFX/pray sound`

can exist with `DarkTonic.MasterAudio.EventSounds`.

Therefore the 0.1.0 result is **absence from this loaded snapshot**, not evidence that the sound object/resource does not exist.

The exact MasterAudio event/sound-group configuration remains unknown.

## Evidence gates after acceptance

READY:
- confession canonical state ownership;
- confession normal-play transition observer;
- persistent HUD ownership/lifecycle;
- prayer icon identity;
- stock corpse transient presentation preservation;
- confession sibling transient visual family for prototype.

BLOCKED:
- confession audio only;
- final corpse receiving-area envelope / event seam, especially pre-repair coverage.

## Cheapest remaining audio test

Do not build another probe yet.

The same frozen 0.1.0 DLL can be rerun after process restart. Load gameplay and enter the church before the probe's optional-object wait expires; no sermon, prayer, confession roll, corpse delivery or state mutation is required. Return the overwritten report.

Only if the same artifact still cannot see the sound while the pulpit is definitely loaded should a dedicated sound-only 0.1.1 probe be justified.
