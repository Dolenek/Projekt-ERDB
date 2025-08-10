# Env.cs

Simple .env loader used by the UI to mirror the console app’s configuration approach.

Responsibilities
- Load(): Reads a .env file from repository root (same folder where the solution resides) into process environment variables (only for keys that are not already set).
- Get(key, fallback): Retrieves a value from environment variables with a string fallback.

Used keys (current UI)
- DISCORD_CHANNEL_URL: Target Discord channel URL to open in WebView2.
- AREA: Integer used by BotEngine to choose work command (chop/axe/bowsaw/chainsaw).
- HUNT_COOLDOWN / WORK_COOLDOWN / FARM_COOLDOWN: Millisecond cooldowns used by timers.

Notes
- The loader is intentionally minimal and tolerant of blank lines and comments.
- If a key is present in the OS env already, that value “wins” and .env does not override it.