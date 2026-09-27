# Keeper's Alerts

A vanilla-friendly Graveyard Keeper mod in development for important remote states that otherwise require manual location checking.

Current core scope:
- remind the player when a delivered corpse is still waiting;
- notify and remind the player when at least one confession is available.

Target: Graveyard Keeper 1.407 on PC with BepInEx 5.

The intended UX is deliberately small: preserve native game behavior, use a short native-style event cue when appropriate, and keep a small persistent HUD indicator only while the state remains actionable.

Development is currently in evidence-first research/design. Runtime implementation will begin only after the relevant native state owners and lifecycle seams are verified.

See:
- `AGENTS.md` for the project development contract;
- `docs/PROJECT_DIRECTION.md` for accepted scope and current evidence gates;
- `docs/CHATGPT_PROJECT_INSTRUCTIONS.md` for the canonical ChatGPT Project bootstrap.
