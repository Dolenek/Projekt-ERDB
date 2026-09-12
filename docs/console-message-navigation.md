# Console message navigation

Console rows can carry an exact reference to the Discord message that produced
the log. A navigable row uses the hand cursor, cyan message text and the tooltip
`Double-click to show Discord message`. A single click keeps its normal selection
behavior; double-clicking switches to the referenced internal Discord tab,
centers the message and outlines it briefly.

References contain the Discord DOM message ID, source tab role and channel URL.
They come only from WebView snapshots and are never inferred from log text or a
nearby message. Rows without all three fields remain ordinary non-navigable rows.
Copying with `Ctrl+C`, text and kind filtering, rendered `LogEntry.ToString()`
output and the 500-entry retention limit are unchanged.

The current linked sources are:

- registered outgoing bot commands;
- guard detection, notification and puzzle-solver telemetry for the active challenge;
- cooldown synchronization, time-cookie reduction, previous-command-busy and
  profile-area logs produced directly from an observed bot message;
- training, bunny and card-hand prompt handling when an exact prompt or reply is known.

Puzzle answers sent through the one-shot guarded submission path do not receive
a link because that path does not confirm an outgoing Discord message ID. Guild,
dungeon, duel, crafting and dismantling report streams also remain unlinked.

Navigation first searches the referenced tab's current DOM. If the message is
not present, the app builds an internal Discord guild-channel or DM permalink,
navigates the existing WebView2 tab and searches again. It does not open an
external browser, activate the application window or change guard foreground
settings. Invalid, deleted or inaccessible targets produce one non-navigable
warning. A failed permalink lookup restores the tab's previous URL. A second
navigation request is ignored while the first is running.
