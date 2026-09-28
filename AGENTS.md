# AGENTS.md — KeepersAlerts

This repository follows the canonical global development rules in `NikichMods/DevRules`.

Read before substantive technical work:
- `ENGINEERING_RULES.md`
- `CI_POLICY.md`
- `GIT_WORKFLOW.md`
- `PROJECT_BOOTSTRAP.md`
- `LICENSE_POLICY.md`
- `RUNTIME_TEST_HARNESS.md` when installed-runtime evidence or a user-operated research harness is relevant

This file contains project-specific additions only.

## Project identity

- Project: Keeper's Alerts
- Repository: `NikichMods/KeepersAlerts`
- Target/runtime: Graveyard Keeper 1.407 on PC, BepInEx 5
- Canonical installed DLL name: `KeepersAlerts.dll`
- Purpose: add vanilla-friendly notification/reminder UX for important remote actionable states that otherwise require manual location checking.

## Current product scope

Core states:
1. a delivered corpse is waiting;
2. at least one confession is available.

The intended UX direction is a short native-style event cue when a state appears plus a small persistent HUD indicator while it remains actionable.

For corpse delivery, preserve the stock arrival cue and add only the missing persistent reminder.

For confession availability, aim for a thematically appropriate church/confession sound and a sibling transient visual treatment, then keep a persistent reminder until no confession remains available.

Do not broaden this into a generic production-completion dashboard. Add another event only when evidence shows it belongs to the same class: asynchronous/remote, discrete and actionable, important enough to deserve attention, insufficiently visible without manual checking, and bounded enough not to create HUD spam.

## Mandatory project-specific start-of-work checks

Before substantive implementation:
1. inspect current repository state/history/branches/PRs/docs/build/test evidence;
2. read this file and relevant project docs;
3. search `NikichMods/GraveyardKeeperResearch` before fresh host-internals research;
4. inspect relevant accepted `NikichMods/PrayerClarity` evidence for confession mechanics;
5. verify unfamiliar Graveyard Keeper internals before production code relies on them.

Repository/accepted evidence outrank chat memory and old handoffs.

## Shared host/runtime research

Canonical cross-project source: `NikichMods/GraveyardKeeperResearch`.

- Start with `docs/RESEARCH_INDEX.md`.
- Reusable Graveyard Keeper 1.407 host/runtime facts belong there.
- Keep KeepersAlerts product decisions, implementation choices, gates and acceptance state in this repository.
- Promote newly accepted reusable host/runtime findings back into the shared research repository.

Relevant accepted mechanics may also exist in `NikichMods/PrayerClarity`, especially around `church_budka_roll`, `confession_probability` and Prayer of Repentance.

## Project-specific evidence contract

The mod is informational. Preserve native mechanics unless a separately accepted feature explicitly changes them.

- Do not duplicate confession RNG/probability logic.
- Observe the resulting native confession availability state.
- Remain compatible with stock probability and PrayerClarity: Rebalanced modifying the effective probability.
- Do not alter corpse delivery mechanics merely to drive notification UI.
- Prefer canonical host state over mirrored persistent mod state when it can be observed safely.
- Prefer event-driven/native lifecycle seams over polling when evidence supports them.
- Native-looking UI/audio is a product goal, not evidence for any specific prefab, hook, sound ID or anchor.

## Preserved invariants

Unless a separately gated change requires otherwise, preserve:
- corpse delivery timing/mechanics and existing stock arrival behavior;
- confession RNG, rewards, daily reset and interaction behavior;
- PrayerClarity compatibility;
- save/load semantics;
- localization/input behavior;
- unrelated HUD/UI/audio behavior.

## Git / acceptance workflow

- `main` is stable/documentation baseline.
- Keep unaccepted runtime behavior off `main`; use `research/*`, `feature/*`, `fix/*` or `dev/*` branches as appropriate.
- Documentation/research conclusions may update `main` directly when no runtime acceptance boundary is crossed.
- Numbered handed artifacts are immutable.
- Installed binary name remains `KeepersAlerts.dll`; version identity belongs in metadata/artifact/release records.
- Follow DevRules for candidate identity, explicit runtime acceptance and stable promotion.

## Long-lived sources of truth

Future chats must consult:
- `AGENTS.md`
- `docs/CHATGPT_PROJECT_INSTRUCTIONS.md`
- `docs/ARCHITECTURE.md`
- `docs/RELEASE_ACCEPTANCE.md` when release/runtime acceptance state matters
- `NikichMods/GraveyardKeeperResearch`
- relevant accepted PrayerClarity evidence

Do not store mutable candidate state in ChatGPT Project Instructions.
