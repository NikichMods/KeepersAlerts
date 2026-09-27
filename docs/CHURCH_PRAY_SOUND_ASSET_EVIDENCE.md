# Church Prayer Sound — Direct Asset Evidence

**Target:** Graveyard Keeper 1.407 installation  
**Source:** user-supplied `resources.assets` extracted from `resources.7z`  
**Status:** accepted direct asset evidence; proprietary game asset itself is not committed

## Source identity

- archive: `resources.7z`
- archive SHA-256: `05ff40aef0f3e2c5b95992a964cf92b9f26f5f6b356c5751fa098aa41fa76ffb`
- extracted file: `resources.assets`
- size: `93,209,412 bytes`
- SHA-256: `215c7981901a4b72d5db717666ba47ad3cc032527c95f58dc39d8af1293a69ca`
- Unity serialized-file version: `22`
- Unity engine string: `2020.3.17f1`

The asset file is research input only and must not be committed or redistributed.

## Verified prefab hierarchy

Direct serialized-object inspection identifies:

- GameObject pathID `60282`: `church_pulpit`
- child GameObject pathID `42691`: `PrayFX`
- child GameObject pathID `49100`: `pray sound`

Transform linkage proves:

`church_pulpit -> PrayFX -> pray sound`

The `pray sound` GameObject contains:
- Transform pathID `82717`;
- MonoBehaviour pathID `184947`.

Accepted runtime hierarchy evidence independently identifies this MonoBehaviour role as:

`DarkTonic.MasterAudio.EventSounds`

## Verified MasterAudio action payload

The serialized MonoBehaviour bytes for pathID `184947` contain a single non-default sound action payload with this exact string sequence:

- `Your action name`
- `chorus_short`
- `[None]`

At the AudioEvent payload boundary:
- `Your action name` is serialized as the action name;
- the following expanded flag is true;
- the next serialized string is `chorus_short`.

A matching public MasterAudio `AudioEvent` layout has the fields in exactly that order:

`actionName -> isExpanded -> soundType`

and `EventSoundFunctionType` value zero is `PlaySound`.

Therefore the game's church-pulpit `pray sound` EventSounds action identifies:

**MasterAudio sound group: `chorus_short`**

The same payload has `clipName="[None]"` and variation mode value `1`, which corresponds to MasterAudio's `PlayRandom` variation mode in the matching library layout. This indicates group-level playback rather than a hard-coded specific clip.

## Engineering implication

Keeper's Alerts does not need to clone, activate or retain the church-pulpit `EventSounds` component.

For the confession-available transition, the least-coupled native reuse is to ask MasterAudio to play the already-defined group:

`chorus_short`

using the same host audio system/API family already used by Graveyard Keeper.

This preserves the game's own audio asset/group configuration and does not ship extracted proprietary audio.

## Gate

### Confession audio

- **Observable property:** play one native church/prayer cue when confession availability transitions 0 -> 1+.
- **Canonical audio identity:** MasterAudio sound group `chorus_short`, sourced from the stock church-pulpit `PrayFX/pray sound` EventSounds action.
- **Blast radius:** one MasterAudio play request on the notification transition; no pulpit object mutation, no audio-resource replacement.
- **Preserved invariants:** sermon/prayer audio behavior, pulpit lifecycle, mixer settings, confession mechanics, unrelated sound groups.
- **Acceptance evidence:** direct serialized game asset + accepted runtime identification of the component/hierarchy.
- **Gate:** **READY for production prototype.**

Final perceptual acceptance still belongs in the integrated runtime candidate: verify that the cue is audible, appropriately brief, and does not sound misleading when heard remotely.

## resources.assets.resS

The supplied archive did not include `resources.assets.resS`.

It is **not required** to identify or reuse the MasterAudio group. It would only be useful if we wanted to extract/listen to the underlying streamed audio clip outside the game, which is not required for implementation and is not necessary for the current product decision.


## Better native candidate: `bell_single`

Direct search of the same installed `resources.assets` found a separate stock sound id:

`bell_single`

This is not inferred from an object name. It is explicitly invoked by three serialized FlowCanvas nodes:

`Flow_PlaySound(sound="bell_single")`

Each of those story flows immediately waits `1.5` seconds after playback, which is consistent with a short one-shot cue.

The three verified uses are refugee-story dialogue/notification flows (`refugee_s47_*`, `refugee_s55_*`), not the church preaching flow.

Separately, the stock church preaching flow explicitly invokes:

`Flow_PlaySound(sound="chorus")`

Therefore:
- `bell_single` is a real reusable stock sound id;
- it is distinct from the sermon/prayer chorus;
- current evidence does **not** tie `bell_single` to a church bell tower;
- its exact timbre has not yet been perceptually verified.

For Keeper's Alerts, `bell_single` is now the preferred first confession-notification audio candidate because its semantics and duration appear closer to a brief remote alert than `chorus_short`.

Final acceptance requires hearing it in the integrated candidate; do not claim from the identifier alone that it is specifically a church bell.
