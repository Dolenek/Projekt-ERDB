# Training Automation

`EpicRPGBot.UI` can now run `rpg tr` as a tracked command while the bot engine is active.

Runtime behavior:
- Training joins the same scheduler lane as hunt/adventure/work/farm/lootbox.
- The engine sends `rpg tr` when the tracked training cooldown reaches ready.
- Training replies are treated as fast prompt events after the confirmed `rpg tr` reply is received.
- The tracked `training` cooldown row is resynced from parsed `rpg cd` snapshots and time-cookie reductions.

Supported prompt families:
- Fish-name multiple choice: match the fish emoji to the numbered options and answer with the matching number.
- Inventory question: always answer `no`.
- Yes/no identity question: compare the asked item name to the shown emoji and answer `yes` or `no`.
- Letter question: parse the requested ordinal letter from the shown item name and answer with the lowercase letter.
- Count question: count the requested emoji on the item row and answer with the numeric count.

Answer delivery:
- The bot prefers clicking the matching Discord button when the prompt message exposes visible buttons.
- If no matching button can be found, the bot sends the raw answer text through the normal fast send path.
- Training answers do not use the slower `rpg ...` confirmation loop.
- After sending the answer, the runtime keeps the send lane blocked until it sees the follow-up training completion message containing `Well done`.
- If that completion message is not observed before timeout, the runtime raises a training alert instead of assuming the training step is finished.

Failure handling:
- If a training prompt is recognized but cannot be parsed safely, the bot skips the answer instead of guessing.
- Skipped prompts are logged in the Console with a `[training]` prefix.
- Skipped prompts also show a desktop balloon notification titled `Training prompt skipped`.

Initialization:
- `Inicialize` includes training.
- If training is ready in the opening `rpg cd`, the workflow sends `rpg tr`, solves the prompt, refreshes `rpg cd`, and saves the resulting `training_ms` baseline.

Parsing inputs:
- Training parsing uses a rendered message body that keeps custom emoji `alt` tokens such as `:Banana:` and excludes message buttons from the main body text.
- Visible button labels are captured with row/column metadata so the runtime can match answers to buttons reliably.
- Training reply detection also falls back to the prompt body signature itself (`is training in` plus `15 seconds`) when Discord app-message author metadata is missing or unstable.
- Yes/no item checks tolerate Discord splitting the shown emoji onto the next rendered line instead of keeping it inline with the question text.
- Yes/no item checks also normalize known training synonyms before comparison, including `diamond` matching `:gem:`.
