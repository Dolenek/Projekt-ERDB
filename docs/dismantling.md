# Dismantling

`EpicRPGBot.UI` includes a dedicated modal dismantling workflow that mirrors the crafting flow.

Supported targets:
- `ultra log`, `hyper log`, `mega log`, `super log`, `epic log`
- `epic fish`, `golden fish`
- `banana`

Base materials:
- `wooden log`
- `normie fish`
- `apple`

UI behavior:
- The main header exposes a `Dismantle` button that opens the modal dialog.
- The dialog shows one row per dismantlable item, a shared status area, and bottom `Dismantle` / `Cancel` buttons.
- The user must enter a positive number or `all` in exactly one row.
- The dialog defaults every row to `0` on open and does not persist dismantle inputs.

Workflow behavior:
- Numeric dismantles compute the full cascade before sending commands.
- `all` dismantles use `rpg dismantle <item> all` on every tier in the chain.
- In `all` mode, if a tier has no items to dismantle, the workflow skips that tier and keeps going downward.
- Dismantling always runs down to the base material family for the selected item.
- If the engine is running, the app pauses it, waits for the send lane to go idle, runs dismantling exclusively, then resumes the engine and refreshes with `rpg cd`.

Configured yields:
- `ultra -> 8 hyper`
- `hyper -> 8 mega`
- `mega -> 8 super`
- `super -> 8 epic log`
- `epic log -> 20 wooden log`
- `epic fish -> 80 golden fish`
- `golden fish -> 12 normie fish`
- `banana -> 12 apple`

Failure rules:
- If EPIC RPG does not confirm the command with a reply, the workflow stops.
- If the reply is not recognized as a success, the workflow stops.
- `all` mode intentionally dismantles all matching lower-tier inventory at each downstream step instead of preserving pre-existing stock.

Automation IDs:
- Launcher: `DismantleButton`
- Window: `DismantleWindow`
- Inputs: `DismantleUltraAmountInput`, `DismantleHyperAmountInput`, `DismantleMegaAmountInput`, `DismantleSuperAmountInput`, `DismantleEpicAmountInput`
- Inputs: `DismantleEpicFishAmountInput`, `DismantleGoldenFishAmountInput`, `DismantleBananaAmountInput`
- Status: `DismantleStatusText`
- Actions: `DismantleStartButton`, `DismantleCancelButton`
