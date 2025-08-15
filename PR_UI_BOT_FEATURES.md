# UI Bot Features

Summary
- Ported event-driven replies from reference [EventCheck()](EpicRPGBotCSHARP/Program.cs:658) into UI [EventCheck()](EpicRPGBot.UI/BotEngine.cs:393).
- Added helper [RespondFirstPresent()](EpicRPGBot.UI/BotEngine.cs:499) to echo the first matching phrase found in a message.
- No author filter (bot reacts to last message regardless of author). Deduplication by last message id via [CheckLastMessageAsync()](EpicRPGBot.UI/BotEngine.cs:318).

What's included
- Zombie horde: detects “You were about to hunt a defenseless monster, but then you notice a zombie horde coming your way” → replies RUN
- Megarace: detects “megarace boost” → replies yes
- Epic tree: detects “AN EPIC TREE HAS JUST GROWN” → replies CUT
- Megalodon: detects “A MEGALODON HAS SPAWNED IN THE RIVER” → replies LURE
- Raining coins: detects “IT'S RAINING COINS” → replies CATCH
- Epic coin drop: detects “God accidentally dropped an EPIC coin” → echoes the first present phrase:
  - I SHALL BRING THE EPIC TO THE COIN
  - MY PRECIOUS
  - WHAT IS EPIC? THIS COIN
  - YES! AN EPIC COIN
  - OPERATION: EPIC COIN
- Solo coin drop: detects “OOPS! God accidentally dropped” → echoes the first present phrase:
  - BACK OFF THIS IS MINE!!
  - HACOINA MATATA
  - THIS IS MINE
  - ALL THE COINS BELONG TO ME
  - GIMME DA MONEY
  - OPERATION: COINS
- Epic NPC trade: detects “EPIC NPC: I have a special trade today!” → echoes the first present phrase:
  - YUP I WILL DO THAT
  - I WANT THAT
  - HEY EPIC NPC! I WANT TO TRADE WITH YOU
  - THAT SOUNDS LIKE AN OP BUSINESS
  - OWO ME!!!
- Preserved existing handlers:
  - Lootbox summoning → SUMMON
  - Legendary boss → TIME TO FIGHT
  - BOT HELP, START/STOP, CHANGE WORK/FARM, BOT FARMING

Implementation notes
- Matching performed with case-insensitive IndexOf checks in [EventCheck()](EpicRPGBot.UI/BotEngine.cs:393).
- Messages are sent via [SendMessageAsyncDevTools()](EpicRPGBot.UI/BotEngine.cs:241).
- Debounce/duplicate protection via message id tracking in [CheckLastMessageAsync()](EpicRPGBot.UI/BotEngine.cs:318).

Testing
- Build succeeded.
- Manual test steps:
  - Start UI app and bot.
  - Paste sample event text lines into channel; verify one response per message for each event as listed above.

Considerations
- Without author filtering, any user can intentionally trigger replies by posting these phrases.
- If a filter is desired later, we can extend [CheckLastMessageAsync()](EpicRPGBot.UI/BotEngine.cs:318) to extract author and only respond to EPIC RPG messages.