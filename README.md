# EpicRPGBot.UI

This repository now centers on `EpicRPGBot.UI`, a WPF `.NET Framework 4.8` desktop app that embeds Discord with WebView2 and drives the Epic RPG workflow from the UI.

What the app provides:
- Embedded Discord browser with persistent sign-in state
- Start/stop automation for hunt, adventure, work, and farm
- Last-message feed, hunt counter, and structured console log
- Cooldown discovery and visual cooldown tracking
- Integrated captcha solver and optional offline self-test

Docs:
- [Documentation index](documentation.md)
- [UI shell](docs/ui-shell.md)
- [Automation engine](docs/automation-engine.md)
- [Cooldown management](docs/cooldown-management.md)
- [Captcha solver](docs/captcha-solver.md)

Requirements:
- Windows 10/11 x64
- .NET Framework 4.8 Developer Pack
- Microsoft Edge WebView2 Runtime

Build and run:
```bash
dotnet build EpicRPGBotCSharp.sln -c Debug
dotnet run --project EpicRPGBot.UI -c Debug
```

Local settings:
- User-editable settings are stored in `%LocalAppData%/EpicRPGBot.UI/settings/app-settings.ini`.
- The right-side settings fields auto-save when changed and are restored on the next launch.

Captcha `.env`:
- Only captcha-related configuration stays in `.env` at the repository root.
- Supported keys:
  - `CAPTCHA_REFS_DIR`
  - `CAPTCHA_HASH_THRESHOLD`
  - `CAPTCHA_SELFTEST`

Notes:
- `Start Bot` sends `rpg cd` immediately, then schedules hunt/adventure/work/farm from the parsed cooldown snapshot instead of sending an opening burst.
- `Inicialize` discovers cooldown baselines and saves them into the same local settings file.
- The embedded browser auto-clicks common Discord “continue in browser” prompts after navigation.
