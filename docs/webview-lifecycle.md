# Discord WebView Lifecycle

Discord tabs use demand-managed WebView2 sessions so tabs that are not needed do
not retain Chromium renderer resources.

Activity rules:
- `Bot` stays active for message polling and bot automation.
- `Player` is loaded at startup and stays active for the lifetime of the app.
- The currently selected optional browser tab stays active for manual use.
- `Dungeon` and `Duel` workflows hold their tab active until the workflow has
  completed, failed, or finished cancellation, including while running in the
  background.
- The Guild watcher holds its tab active only when its `Active` setting is on and
  its channel URL and trigger text form a valid configuration.

Each reason is tracked independently. Removing one reason does not close a tab
that still has another reason. When the last reason disappears, the WebView2 is
detached and disposed immediately. Reopening the tab creates a new WebView2 with
the shared persistent profile, so Discord reloads while authentication cookies
remain available. The session restores its last valid URL, or uses the tab's
configured initial URL on first activation.

Background workflows and the permanent Player session use an off-screen loaded
host. This lets Discord continue working without changing the selected tab.
Selecting the tab moves the same live WebView2 into its visible host; switching
away parks Player, parks an optional tab with an active workflow, or disposes an
optional tab when no other activity reason remains.

The main Discord status reflects the required Bot session. Optional-tab loading
failures are shown in that tab and written to the Console without marking the Bot
connection as failed. Leaving and selecting a failed tab retries its activation.
