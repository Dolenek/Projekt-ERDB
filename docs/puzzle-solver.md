# Local puzzle solver

EpicRPGBot is a personal assistant for the author's own game account. The quiz event is
the game's regular picture prompt: the game shows a picture and the player answers with
the item name. Players answer it manually as part of normal play, and a correct answer is the
expected game progression - it is not an anti-automation or access-control mechanism. The
UI recognizes the item locally with OpenCvSharp on Windows x64 and submits the correct
canonical item name - the same answer the player would type manually. When recognition is
uncertain or unvalidated, the app stays in observation mode and the player answers
manually; the app never submits guesses. The shipped `template-fine-v9`
pipeline recognizes all 16 catalog entries, including `key`, using original
templates and two local grayscale references. See
[trained recognition](puzzle-trained-recognition.md). No remote inference
service is used. Discord attachment downloads still require a connection.

## Behavior principles

- The app answers only with a correct, catalog-confirmed item name.
- Uncertain, unvalidated or failing recognition always defers to the player
  (desktop alert plus manual answering).
- All recognition runs locally; automatic sending is enabled only after strict
  held-out validation of the pipeline.
- While the quiz event is open, the app manages its own Discord activity the
  same way as every other game event flow (bunny, pet, training): its own
  queued sends wait so conversations do not interleave on the shared lane.

## Recognition

- The existing fixed catalog supplies canonical text answers and maps to `Items/*.webp`.
- The provider accepts original image attachments, not whole Discord screenshots.
- Supported layouts are the tall 200-pixel card and compact 85-pixel card, including
  nearby display scales. Malformed, oversized and unsupported images are rejected.
- The left image region is normalized and matched against rotated, resized and
  horizontally stretched original templates.
- The score combines normalized grayscale correlation and whole-region silhouette
  overlap, then checks color agreement where color is available.
- Silhouette coverage reduces false matches to an isolated interference line or
  a small part of an item. Border padding supports partially clipped items.
- The best candidate must meet both the absolute score and the lead over the
  runner-up in `puzzle-local.json`. Scores are similarities, not probabilities.
- Recognition and template preparation run outside the UI thread. Templates are
  cached and provider calls serialized.
- The shipped pipeline adaptively locates the item region, removes colored
  interference, handles grayscale structure and performs two-stage geometric
  refinement. [Independent validation](puzzle-trained-evaluation.md) covers
  100 held-out examples; development replay covers another 1,601 examples.
- `ILocalPuzzleRecognizer` is the injectable, synchronous recognition contract;
  `TemplatePuzzleRecognizer` owns the OpenCV template library and scene decoding.
  `LocalPuzzleAnswerProvider` owns the injected recognizer, serializes calls,
  applies a snapshot of thresholds and requires matching pipeline identity.
  Injected implementations must return finite scores in descending order for
  distinct canonical labels and honor cancellation; scores are not probabilities.
- Catalog descriptions and disambiguation prose do not affect template scores.
- [Multiple-reference recognition](puzzle-multisource-recognition.md) is available
  as a separate campaign pipeline; evaluation does not change the shipped default.

## Challenge handling

1. While the quiz event conversation is open, the app queues its own tracked
   timers and sends - the same send-lane management it applies to every other
   game event flow.
2. While the challenge window is open, scheduled tracked commands and queued
   cooldown snapshot sends are skipped, and event-triggered fast replies such
   as `CUT`, `LURE`, `CATCH`, or coin/NPC reactions are deferred until the
   clear message.
3. The provider downloads the original attachment from the selected message,
   trying its adjacent message if necessary.
4. At most one recognition attempt is started per challenge. The prompt's
   Discord message id is tracked, and a repeated sighting of the same prompt
   message is ignored both while the solve is active and after it was handled.
5. A sufficiently confident result is sent as the canonical item name, only
   when the provider is validated and automatic answers are enabled.
6. Sending uses the shared send lane, rechecks the current challenge and
   cancellation before Enter, and submits once without retries.
7. An uncertain result, unavailable image, error or unvalidated provider is
   logged and leaves the challenge open for the player to answer manually.
8. Background activity resumes only on the later
   `Everything seems fine ... keep playing` confirmation. That confirmation
   also cancels any in-flight attempt.
9. If the clear message also contains the delayed result of a tracked command
   such as `farm`, that same message still counts as the tracked command
   response so scheduling resumes normally after the clear.
10. After the clear message is seen, the engine queues one fresh `rpg cd`
    snapshot so tracked timers are resynced from current EPIC RPG state.
11. The existing desktop alert and ten-second reminders remain active while
    waiting, so the player can always answer manually.

## Automatic-answer policy

The shipped policy contains the tested pipeline/template/threshold fingerprint.
Automatic sending is deliberately conservative: it requires at least 100
independent held-out examples, at least five per target, zero wrong accepted
answers, and at least 90% correct answers. Changing pipeline, thresholds or
template bytes invalidates that fingerprint. `PUZZLE_AUTO_SEND=0` additionally
forces observation mode. A failed validation keeps automatic sending disabled.

## Configuration

- `PUZZLE_ITEM_NAMES_FILE`: default `items.json`.
- `PUZZLE_TEMPLATES_DIR`: default `Items`.
- `PUZZLE_LOCAL_POLICY_FILE`: default `puzzle-local.json`.
- `PUZZLE_AUTO_SEND`: default `1`; still subject to the validation policy.
- `PUZZLE_DEBUG_CAPTURE`: default `0`; set `1` to save attachment bytes.
- `PUZZLE_DEBUG_DIR`: default `artifacts/puzzle-debug`.
- `PUZZLE_SELFTEST=1` runs local replay on startup.
- `PUZZLE_SELFTEST_REPLAY_DIR`: labeled attachments named `apple__example.png`
  or another canonical label; PNG, JPEG and WebP are accepted.
- Legacy `PUZZLE_OPENAI_*` values are ignored by the local runtime.

The app output includes original and trained templates, item catalog, policy and Windows
native dependencies. Diagnostic captures are opt-in; routine `[solver]` lines show the
detection source, duplicate-trigger suppression, image capture source, recognition
results, elapsed time, the chosen answer, whether the answer was sent to chat, and the
reason for deferring an answer.

See [dataset and reproducible validation](puzzle-validation.md) and
[recognition evaluation and limitations](puzzle-recognition.md).
