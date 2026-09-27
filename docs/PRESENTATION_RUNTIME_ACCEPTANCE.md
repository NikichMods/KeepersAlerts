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

Presentation Probe 0.1.0 reported:

`sound_transform=<not found>`

A deliberate rerun was then captured while the stock HUD zone label was **Церковь** (Church), yet the same result persisted. Source inspection explains why: probe 0.1.0 searched for path suffix

`/church_pulpit/content/church_pulpit(Clone)/PrayFX/pray sound`

while prior accepted installed-runtime evidence shows the actual hierarchy contains the WGO prefix:

`World/[wgo] church_pulpit/content/church_pulpit(Clone)/PrayFX/pray sound`

Because the suffix itself omits `[wgo] `, probe 0.1.0 cannot match the verified transform even when the pulpit is loaded.

Therefore the two `<not found>` reports are **probe-filter false negatives**, not evidence about runtime load state or sound-resource absence.

The exact MasterAudio event/sound-group configuration remains unknown. A new versioned sound-only probe is justified; do not rebuild or replace 0.1.0.

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

## Remaining audio test

The rerun condition for escalating beyond 0.1.0 is now met and the source bug is understood.

Build a new, versioned **sound-only 0.1.1 probe** that:
- locates the target using the verified `[wgo] church_pulpit` hierarchy or an equivalently robust ancestor/name predicate;
- includes inactive descendants;
- records the exact `DarkTonic.MasterAudio.EventSounds` serialized configuration and relevant nested event/sound-group fields;
- performs no audio playback or host mutation.

No prayer, sermon, confession roll or other gameplay action is required beyond loading the church/pulpit hierarchy.


## Direct asset follow-up — church audio closed

A user-supplied `resources.assets` from the installed game was inspected directly after this runtime probe.

Source identity:
- `resources.assets` SHA-256: `215c7981901a4b72d5db717666ba47ad3cc032527c95f58dc39d8af1293a69ca`
- size: `93,209,412 bytes`
- Unity engine string: `2020.3.17f1`

The serialized hierarchy contains:

`church_pulpit -> PrayFX -> pray sound`

The `pray sound` GameObject has one Transform and one MonoBehaviour; accepted runtime evidence identifies that MonoBehaviour as `DarkTonic.MasterAudio.EventSounds`.

Its serialized AudioEvent payload contains `Your action name`, followed by `chorus_short`, followed later by `[None]`. Matching MasterAudio field order establishes `chorus_short` as `AudioEvent.soundType` / sound-group identity.

**Confession audio resource identity is therefore READY:** `chorus_short`.

The previously prepared Church Pray Sound Probe 0.1.1 is superseded for this research question and should not be required from the user unless later runtime behavior contradicts the direct asset evidence.
