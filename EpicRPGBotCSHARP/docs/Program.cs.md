# EpicRPGBotCSHARP/Program.cs

Legacy console automation for EpicRPG using Selenium/WebDriver. The new WPF UI (EpicRPGBot.UI) replaces browser automation with WebView2 DevTools, but this console app documents the original flow and can be used as reference.

High‑level responsibilities
- LoadEnv(): Reads .env into process for configuration (channel URL, cooldowns, area).
- Browser init: Launches a Chromium browser via Selenium (keeps session and navigates to the target Discord channel).
- ClickInterstitials(): Dismisses Discord interstitial prompts (Open/Continue in Browser etc.).
- SendMessage()/SendCommand(): Types commands into the channel input and submits them with Enter.
- CheckLastMessage(): Polls the DOM for the latest message to drive logic and cooldown handling.
- EventCheck(): Parses message text for specific triggers (e.g., help, farm/work variations, lootbox/boss events) and responds accordingly.
- StartBot()/Stop loop: Orchestrates initial opening salvo (cd → hunt → work → farm if area >= 4) and continuous timers for hunt/work/farm.

Core config keys (via .env)
- DISCORD_CHANNEL_URL: Discord channel to automate.
- AREA: Affects which “work” command to use:
  - 1–2: rpg chop
  - 3–5: rpg axe
  - 6–8: rpg bowsaw
  - 9–13: rpg chainsaw
- HUNT_COOLDOWN / WORK_COOLDOWN / FARM_COOLDOWN: timer intervals in milliseconds.

Key differences vs UI engine
- Console uses Selenium to automate an external browser; the UI uses WebView2 DevTools inside the app.
- The UI engine focuses the bottom composer reliably and uses DevTools Input.insertText and key dispatch for Enter; this avoids typing into the header search.
- The UI sends “rpg cd” immediately on Start and prevents double‑start; the console app’s StartBot was the original reference for the opening salvo.

Migration notes
- The UI (BotEngine.cs) ports SendMessage/SendCommand/CheckLastMessage logic using WebView2 ExecuteScript/DevTools with DispatcherTimers.
- Any future event responses added to Program.cs should be mirrored into BotEngine.EventCheck to keep behavior consistent.

Troubleshooting (legacy)
- If Discord layout changes, Selenium selectors may break. The UI engine’s composer heuristics and DevTools path are more robust for Discord’s React/Slate editor.
- Ensure the correct Chrome driver/browser version when running the console app.