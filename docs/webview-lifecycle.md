# Discord WebView Lifecycle

Discord tabs use demand-managed WebView2 sessions so tabs that are not needed do
not retain Chromium renderer resources.

Activity rules apply independently to every account:

- `Bot` stays active while selected, while its engine runs, or while a workflow
  holds a lease.
- `Player` stays active only while selected.
- The currently selected optional browser tab stays active for manual use.
- `Dungeon` and `Duel` workflows hold their tab active until the workflow has
  completed, failed, or finished cancellation, including while running in the
  background.
- The Guild watcher holds its tab active only when its `Active` setting is on and
  its channel URL and trigger text form a valid configuration.

Each reason is tracked independently. Removing one reason does not close a tab
that still has another reason. When the last reason disappears, the WebView2 is
detached and disposed immediately. Reopening the tab creates a new WebView2 with
the account's persistent profile, so Discord reloads while authentication
cookies remain available. The session restores its last valid URL, or uses the
tab's configured initial URL on first activation.

Background engines and workflows use an off-screen loaded host. Selecting a tab
moves the same live WebView2 into its visible host. Switching away parks a tab
with active work and disposes it when no activity reason remains. Hidden Player
tabs are disposed immediately.

All accounts use one WebView2 user-data folder and separate `ProfileName`
values. Tabs for one account share cookies; different accounts remain isolated
while sharing the browser environment and its common process resources.

The Discord status reflects browser initialization for the selected account.
Tab loading failures are shown in the tab and written to that account's Console.
Leaving and selecting a failed tab retries its activation without affecting other
accounts.
