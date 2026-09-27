# Corpse Waiting-State Runtime Acceptance — 2026-09-27

**Target:** Graveyard Keeper 1.407  
**Probe:** Keeper's Alerts Corpse State Probe 0.1.0  
**Status:** accepted evidence for native loose-body persistence/removal

## Probe identity

- source commit: `efaa1c636a79815e66c451244c5dcf7735f9a173`
- GitHub Actions run: `36284873364`
- artifact ID: `10920222408`
- DLL: `KeepersAlerts.CorpseStateProbe.0.1.0.dll`
- DLL SHA-256: `a249bb20ecefb924e08b2c64cec816c5de84e487299939b65b4b51724cc66375`
- build: 0 warnings / 0 errors

The probe contract was read-only: no Harmony, mutation, save writes or synthetic body generation.

## Accepted observations

On the repaired-chute delivery path:

1. A new loose Body appeared in `DropsList` with `zone_id="morgue"` and `item_drop_zone_id="morgue"`.
2. The nearby native `morgue_throw_out` WGO was present at the authored drop endpoint.
3. The Body settled and remained at a stable world position while uncollected.
4. During save/load/world reconstruction, the old Unity instance disappeared.
5. A new Unity instance representing a Body then appeared at the **same saved world position** with the same `morgue` zone values.
6. Pickup subsequently removed that reconstructed loose Body and the Body-drop snapshot returned to zero.

## Conclusions

- The physical delivered corpse is canonical host state in `DropsList`.
- Loose Body position and zone are sufficient for the host to reconstruct that physical state across save/load.
- Unity instance ID is ephemeral and must never be the persisted identity.
- A Keeper's Alerts save flag is not required merely to remember that the physical Body still exists.

## Evidence boundary

The host does not preserve a dedicated “this loose Body was delivered by the donkey” provenance marker in `SavedDropItem`.

Therefore runtime evidence proves **physical-state reconstruction**, not historical provenance after arbitrary movement.

The preferred next model is native receiving-area occupancy:
- exact delivery can be observed at the native owner during the live event;
- after load, current loose Body position/zone can reconstruct whether the receiving area is still occupied.

This deliberately treats a manually placed Body in the same bounded receiving area as the same actionable world state. If strict historical provenance is ever required, it would need a separately justified persisted mod-owned mechanism.

## Presentation/geometry follow-up

Presentation Probe 0.1.0 independently confirmed the live native endpoints:

- `morgue_throw_out=(10656,-10992,-2297.572)`;
- `morgue_throw_in=(3672,-1896,-374.235)`.

Across accepted repaired-chute Corpse State Probe samples, the delivered body settled at approximately 35-72 world units from `morgue_throw_out`. This supports a small endpoint-relative receiving area rather than a whole-morgue-zone predicate.

The persistent HUD owner is also closed independently: `UI Root/HUD` follows native show/hide lifecycle and contains screen-size-anchored widgets suitable for a small reminder.

## Follow-up

Do not repeat Corpse State Probe 0.1.0 unless a later implementation contradicts these accepted facts.

The remaining geometry and transition questions were subsequently closed statically; see `docs/CORPSE_RECEIVING_AREA_CLOSURE.md`.

No additional pre-repair runtime probe is required before the first production candidate unless implementation evidence contradicts the derived corridor.
