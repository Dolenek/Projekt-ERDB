# Multiple accounts

The application stores an ordered account registry in
`%LocalAppData%/EpicRPGBot.UI/settings/accounts.json`. Each account has an
immutable `AccountId`, an editable display name, a settings file, and a stable
WebView2 profile name.

The title bar lists every account as `Running • Name` or `Stopped • Name`.
Selecting an entry switches the whole visible workspace, including browser
tabs, settings, messages, Console, statistics, cooldowns, and workflow buttons.
The `+` button creates an account and immediately opens its isolated Discord
profile for sign-in. Right-click an account entry and choose `Rename` to change
its display name. Renaming does not change storage or browser identity.

Accounts run independently. Start and Stop affect the selected account only.
Switching accounts does not stop its engine, guild watcher, dungeon, duel, or
other active workflow. Async work captures its originating account so later UI
selection changes cannot redirect commands or results.

When `accounts.json` does not exist, the registry creates one `Default`
account. It copies an existing `app-settings.ini` into the account store and
keeps the original file as a backup. The WebView2 `Default` profile remains in
use for that account, preserving Discord authentication. This migration is
idempotent. New accounts use `settings/accounts/<AccountId>.ini` and a new
WebView2 profile.

The app restores the account list and last selection after restart. Engines are
not restarted automatically. Guild watchers retain their existing per-account
setting and can run without the engine.

Account ids are visible in account-entry tooltips. Browser pages expose both
`data-epicrpg-account-id` and `data-epicrpg-tab-role`; MCP WebView tools require
an `accountId` and select the matching active Bot page.

## Registry integrity

The registry supports version 1 and requires at least one complete account.
Account IDs, normalized settings paths and browser profile names must be unique;
settings paths must remain relative and inside the settings directory. An invalid
selection falls back to the first account without changing account identities.

An existing empty, malformed, unsupported or unreadable registry is preserved.
Startup shows an error and exits instead of silently creating another account.
Check the reported file and permissions before restarting; restore a known valid
registry if necessary. There is no automatic backup recovery.

Registry updates use a flushed staging file and atomic replacement. The in-memory
account list changes only after persistence succeeds. A failed add, rename or
selection reports an error and leaves the previous workspace intact. A failed
first migration removes only its newly copied settings file; legacy settings
remain available for the next attempt.
