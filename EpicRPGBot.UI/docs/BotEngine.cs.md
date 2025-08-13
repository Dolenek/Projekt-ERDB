# BotEngine.cs

Purpose
- Core automation engine that drives Discord through WebView2 DevTools.
- Sends commands, manages cooldown timers, reads last messages, and publishes events for the UI (logging and stats).

Files
- [EpicRPGBot.UI/BotEngine.cs](EpicRPGBot.UI/BotEngine.cs:1)
- UI consumers:
  - [EpicRPGBot.UI/MainWindow.xaml.cs](EpicRPGBot.UI/MainWindow.xaml.cs:1)
  - [EpicRPGBot.UI/MainWindow.xaml](EpicRPGBot.UI/MainWindow.xaml:1)

Key Responsibilities
- Start/Stop the automation loop
- Send commands using WebView2 DevTools (Input.insertText and Enter key events) into the real message composer
- Keep and use cooldown timers via DispatcherTimer (hunt/work/farm)
- Poll the DOM for the latest message for lightweight reactions
- Raise UI events for logging and the “Last messages” panel

Public API
- bool IsRunning
  - Indicates whether the engine is currently running.
- void Start()
  - Starts automation timers, chooses work command by area, and schedules an opening salvo (hunt/work[/farm]).
  - Note: “rpg cd” is not sent here; the UI calls SendImmediateAsync before Start to ensure an instant send on button click.
- void Stop()
  - Stops all timers and marks the engine as stopped.
- Task<bool> SendImmediateAsync(string text)
  - Sends a command immediately, publishes OnCommandSent, and returns success/failure.

Events
- OnEngineStarted
- OnEngineStopped
- OnCommandSent(string cmd)
  - Raised after a successful send. The UI logs “Message (cmd) sent” and updates stats (e.g., hunt counter).
- OnMessageSeen(string text)

How sending works
1) FocusComposerAsync()
   - Finds the Discord message composer reliably (bottom-most visible role="textbox"/data-slate-editor).
   - Scrolls to, clicks, focuses, and places caret at the end to avoid the header search box.
2) DevTools send
   - Input.insertText to type the text
   - Input.dispatchKeyEvent for Enter (keyDown + keyUp)
   - Fallback: if DevTools fails, uses document.execCommand('insertText') + KeyboardEvent(Enter)

Timers
- _huntT: Hunt cooldown
- _workT: Work cooldown (chop/axe/bowsaw/chainsaw chosen by Area)
- _farmT: Farm cooldown (enabled for Area >= 4)
- _checkMessageT: Polls for the last message to drive simple reactions

DOM polling (CheckLastMessageAsync)
- Reads the last li[id^='chat-messages-'] item’s innerText and id
- Emits OnMessageSeen when a new message id appears (simple rolling detection)

Opening salvo
- The UI first sends “rpg cd” (via Start button) using SendImmediateAsync
- Start() then waits ~2s and sends hunt, wait, work, wait, and optionally farm
- All these scheduled sends raise OnCommandSent so the UI can log and count

Internal reactions and event emission
- EventCheck(...) reacts to certain keywords in the chat text (e.g., “BOT HELP”, “START”, etc.)
- To ensure the UI receives OnCommandSent for these reactions, the engine uses an internal helper:
  - SendAndEmitAsync(string text): wraps the actual send and raises OnCommandSent on success
- This guarantees UI logging (“Message (message) sent”) and stats updates (e.g., “rpg hunt” counter) for both timer-based and reaction-based sends.

Notes
- The composer focus and DevTools strategy avoid typing into Discord’s header search.
- If Discord markup changes, FocusComposerAsync uses broad selectors and a fallback path.
- Keep WebView2 runtime up to date for compatibility.

Configuration/Cooldowns
- Area, HuntCooldown, WorkCooldown, FarmCooldown are passed in from the UI.
- Work command is derived from Area:
  - 1–2: rpg chop
  - 3–5: rpg axe
  - 6–8: rpg bowsaw
  - 9–13: rpg chainsaw

Troubleshooting
- If messages go into the header search, update to the latest build; the engine uses a bottom-most textbox heuristic and DevTools insertText.
- If sending fails temporarily, the fallback path (execCommand) kicks in automatically.