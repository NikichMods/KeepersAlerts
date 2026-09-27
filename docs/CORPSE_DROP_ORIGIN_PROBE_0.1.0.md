# Corpse Drop Origin Probe 0.1.0

Research-only read-only probe for Graveyard Keeper 1.407.

## Exact question

The pre-repair donkey branch calls `Flow_DropBody` with no explicit WGO, so the donkey itself is the source and `WorldGameObject.GetDropPos()` chooses the exact source point. The donkey ObjectDefinition has `drop_point=Auto`, so a child DockPoint could theoretically shift that source away from the donkey root.

This probe closes only that uncertainty.

## Existing evidence

Already accepted:
- donkey reaches `donkey_cemetery_point=(3676,-2016)`;
- pre-repair drop direction is Up;
- repaired drop direction is Down and uses `morgue_throw_out`;
- both paths call the same native drop implementation with force=3;
- `DropResCurve.duration_factor` is constrained to 0..1;
- drop kick friction is 0.96 and its native displacement is therefore bounded;
- `morgue_throw_in=(3672,-1896)`.

No pre-repair delivery is required for this probe.

## Contract

- reflection/read only;
- no Harmony;
- no graph execution;
- no item/body generation;
- no mutation;
- no save writes;
- automatically writes once when donkey + both morgue endpoints are loaded, then disables itself.

Report:
`BepInEx/KeepersAlerts-corpse-drop-origin-probe-0.1.0.txt`


## Frozen artifact identity

- exact source commit: `6f6e2b7f8766e65be2394d011ba6b11feb952532`
- GitHub Actions run: `36354493935`
- artifact ID: `10943342689`
- DLL: `KeepersAlerts.CorpseDropOriginProbe.0.1.0.dll`
- size: `12,288 bytes`
- SHA-256: `0b1d6554f2e0b42cd81f01015e3f64cf99bce99ed0b5cf3b99721895f8c70804`
- build: `0 warnings / 0 errors`

Do not rebuild different bytes under probe version 0.1.0.
