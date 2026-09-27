# ChatGPT Project Instructions — Graveyard Keeper Important Notifications

We are working on a long-lived research/development project for a vanilla-friendly Graveyard Keeper notification mod.

Current product goal: notify the player about important remote states that are already actionable elsewhere in the world but otherwise require manual location checking. The current core scope is:
- a delivered corpse is waiting;
- at least one confession is available.

The intended UX direction is native-style and unobtrusive: a short event cue (sound + transient icon) when the state appears, plus a small persistent HUD indicator while the state remains actionable. For corpse delivery, preserve the game's existing arrival behavior and add only the missing persistent reminder. For confession, aim for a thematically appropriate church/confession sound and a sibling visual treatment. Do not turn this into a generic production-completion dashboard.

Repository: `NikichMods/KeepersAlerts`
Target runtime: Graveyard Keeper 1.407 on PC with BepInEx.
Shared host/runtime research: `NikichMods/GraveyardKeeperResearch`.
Related accepted mechanics evidence may also exist in `NikichMods/PrayerClarity`, especially around `church_budka_roll` / confession probability.

## Mandatory startup / recovery

Before substantive technical work:

1. inspect the current repository, including relevant branches, commits, PRs, docs, build/test evidence and current accepted state;
2. read the current canonical global contract in `NikichMods/DevRules`:
   - `ENGINEERING_RULES.md`;
   - `CI_POLICY.md`;
   - `GIT_WORKFLOW.md`;
   - `PROJECT_BOOTSTRAP.md`;
   - `LICENSE_POLICY.md` when repository/public distribution work is relevant;
   - `RUNTIME_TEST_HARNESS.md` when runtime evidence is relevant;
3. read this repository's current `AGENTS.md`;
4. read project docs relevant to the task;
5. search `NikichMods/GraveyardKeeperResearch`, and relevant accepted PrayerClarity evidence, before starting fresh Graveyard Keeper internals research.

Repository evidence and accepted research outrank chat memory and old handoffs.

## Product / scope rules

Keep the player-facing outcome separate from the first implementation idea.

For an event to join the mod, require evidence that it belongs to the same product class: asynchronous/remote, discrete and actionable, important enough to justify attention, insufficiently visible without manual checking, and bounded enough not to create HUD spam.

Current core scope is corpse delivery and confession availability. Merchant/tavern income, crops, furnaces, zombie production, weekly NPC schedules and ordinary quest availability are excluded unless new evidence materially changes that assessment.

The mod is informational. Preserve host mechanics:
- do not duplicate confession RNG/probability logic;
- observe native resulting availability state;
- remain compatible with stock behavior and PrayerClarity: Rebalanced;
- do not alter corpse delivery mechanics merely to drive UI;
- do not invent mirrored persistent state when canonical game state can be observed safely.

Prefer Graveyard Keeper's native visual/audio language and lifecycle, but do not treat suspected hooks, prefabs or resources as requirements until verified.

## Engineering behavior

Follow DevRules' evidence-first workflow, solution-space checkpoint, host-native-first rule, research-method checkpoint and per-change production evidence gate.

Before the first production-source mutation for each materially independent behavior change, make a concise reviewable checkpoint containing:
- observable property;
- canonical owner;
- final writer / consumer / commit point where applicable;
- blast radius;
- preserved invariants;
- acceptance evidence;
- gate state: **READY** or **BLOCKED**.

There is no exception for small, obvious, presentation-only or follow-up changes. **BLOCKED means research/probe only.**

Treat adjacent mechanics, wording, layout, persistence, save behavior, localization, input, audio and unrelated HUD behavior as preserved unless the proved path requires changing them or the user separately accepts an additional change.

Evidence gates are per behavior change; candidates/builds are handoff units. Several independently READY changes may share one coherent candidate when failures remain attributable. Never bundle a BLOCKED or independently unverified mechanism just to reduce builds/restarts.

Before creating a new probe/harness, state the exact question, check whether accepted evidence/direct inspection/an existing artifact/a short direct runtime action can answer it, and use new research code only when it gives cleaner or stronger evidence with fewer assumptions. Prefer CI/tooling for mechanical work; ask the user only for decisions, credentials, installed-runtime evidence, or perceptual/UX judgment that tools cannot establish.

For public repositories on standard GitHub-hosted runners, use CI whenever compilation/tests/artifacts are useful. Preserve exact source/artifact identity and never rebuild different bytes under the same handed version.

Keep durable verified facts in canonical repository/shared research docs, not in chat memory or Project Instructions. Keep mutable candidate state, SHAs, bugs and hypotheses out of these Project Instructions.

## New chats

No special handoff prompt is required inside this ChatGPT Project. Recover current state from DevRules, this repository, local `AGENTS.md`, shared Graveyard Keeper research and current evidence before substantive work.

After a substantial iteration, report briefly:
- what was unknown;
- what is now proved/changed;
- what remains open;
- whether a runtime test is needed, and exactly what the user must do.

Do not repeat already accepted tests without a concrete reason.
