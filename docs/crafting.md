# Crafting

`EpicRPGBot.UI` includes a dedicated modal crafting workflow for the log chain.

Scope:
- Supported targets: `epic log`, `super log`, `mega log`, `hyper log`, `ultra log`, `golden fish`, `epic fish`, `banana`
- `wooden log`, `normie fish`, and `apple` are treated as implicit base materials and are not shown as input rows
- The dialog accepts final output amounts to keep after the run completes

UI behavior:
- The main header exposes a `Crafting` button that opens the modal dialog
- The dialog contains one amount input per supported log tier, a read-only status area, and bottom `Craft` / `Cancel` buttons
- Amounts default to `0` each time the dialog opens and are not persisted in settings
- `Cancel` closes the dialog while idle, or requests cancellation while a craft job is running

Planning behavior:
- The workflow builds one bundled craft plan from the full request before sending any craft command
- Higher-tier requests reserve lower-tier outputs instead of consuming them
- Example: requesting `15 ultra log` and `15 hyper log` produces enough extra lower tiers to leave the requested `15 hyper log` after the ultra chain is finished
- Planned commands run from lowest tier to highest tier: `epic` to `ultra`

Runtime behavior:
- Every craft command is sent as `rpg craft <item> <amount>` through the existing confirmed-command lane
- If EPIC RPG replies with the standard `wait at least 1s` cooldown warning, the workflow waits 1 second and resends the same craft command
- If normal automation is running, the app pauses the engine, waits for the current send/reply cycle to finish, runs the craft job, then starts the engine again and refreshes scheduling with `rpg cd`
- Only one craft job can run at a time
- Status lines are appended in the dialog and mirrored to the main Console log with a `[craft]` prefix

Failure rules:
- If EPIC RPG confirms the outgoing message but the reply text cannot be recognized, the craft job stops immediately
- If EPIC RPG reports missing `wooden log`, the whole craft job stops because the requested bundle cannot be completed from the base material
- If EPIC RPG reports any other missing dependency after the prebuilt low-to-high plan has already crafted lower tiers, the job also stops as an unexpected failure
- Cancelling a running job stops after the current in-flight command/reply cycle completes

Automation IDs:
- Launcher: `CraftingButton`
- Window: `CraftingWindow`
- Inputs: `CraftEpicAmountInput`, `CraftSuperAmountInput`, `CraftMegaAmountInput`, `CraftHyperAmountInput`, `CraftUltraAmountInput`
- Inputs: `CraftEpicFishAmountInput`, `CraftGoldenFishAmountInput`, `CraftBananaAmountInput`
- Status: `CraftStatusText`
- Actions: `CraftStartButton`, `CraftCancelButton`
