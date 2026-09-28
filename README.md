# Keeper's Alerts

A small, vanilla-friendly notification mod for **Graveyard Keeper**.

Keeper's Alerts covers two remote states that are easy to miss without travelling back to check them:

- **A corpse is waiting** — the game's normal donkey arrival sound and popup stay untouched; Keeper's Alerts adds a small persistent body icon while a loose corpse remains in the delivery area.
- **A confession is available** — a short native bell cue and stock-style popup appear when the first confession becomes available, then a small prayer icon remains on the HUD until no confession is waiting.

The persistent indicators sit immediately to the right of the energy bar and pack together automatically. If only one alert is active, it occupies the first slot.

## Design

Keeper's Alerts is informational only.

It does **not**:
- change donkey delivery timing or corpse mechanics;
- change confession probability, rewards, reset timing, or interaction logic;
- add its own save data;
- poll every frame;
- turn production tasks into a general notification dashboard.

Confession availability is read from the game's native state, so mods such as **PrayerClarity: Rebalanced** can change confession probability without Keeper's Alerts duplicating or overriding that logic.

## Requirements

- Graveyard Keeper **1.407** on PC
- BepInEx 5

The current release was tested on the Windows/Steam build of Graveyard Keeper 1.407. The plugin verifies the host `Assembly-CSharp` identity at startup and disables itself rather than patching an unknown game build.

## Installation

1. Install BepInEx 5 for Graveyard Keeper.
2. Put `KeepersAlerts.dll` anywhere under `Graveyard Keeper/BepInEx/plugins/`.
3. Start the game.

There are no configuration options.

## Uninstalling

Delete `KeepersAlerts.dll`.

Keeper's Alerts stores no mod-owned save data, so removing it does not require save cleanup.

## Source and license

Source code is available in this repository under **MPL-2.0**. Graveyard Keeper, its assets, audio, and other game content remain the property of their respective owners and are not redistributed by this project.
