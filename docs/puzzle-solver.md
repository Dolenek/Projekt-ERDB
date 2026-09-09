# Local puzzle solver

The UI recognizes item icons locally using OpenCvSharp on Windows x64.
The shipped `template-fine-v9` pipeline recognizes all 16 catalog entries,
including `key`, using original templates and two local grayscale references.
See [trained recognition](puzzle-trained-recognition.md). No OpenAI key or remote
inference service is used. Discord attachment downloads still require a connection.

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

## Incident lifecycle

1. Guard detection pauses tracked timers and blocks normal engine sends.
2. The solver downloads the original attachment from the selected message, trying
   its adjacent message if necessary.
3. At most one recognition attempt is started per incident.
4. A sufficiently confident, validated result is sent as the canonical item name.
5. Puzzle sending uses the shared send lane, checks the current incident and
   cancellation again before Enter, and never retries submission.
6. An uncertain result, unavailable image, error or unvalidated provider leaves
   the incident waiting for manual resolution.
7. Timers resume only on the later `EPIC GUARD: Everything seems fine ... keep playing`
   confirmation. That confirmation also cancels any in-flight attempt.
8. The existing desktop alert and ten-second reminders remain active while waiting.

## Automatic-answer gate

The shipped policy contains the tested pipeline/template/threshold fingerprint.
Automatic answers require at least 100 independent held-out examples, at least
five per target, zero wrong accepted answers, and at least 90% correct answers.
Changing pipeline, thresholds or template bytes invalidates that fingerprint.
`PUZZLE_AUTO_SEND=0` additionally forces observation mode.
A failed validation keeps automatic answers disabled.

## Configuration

- `PUZZLE_ITEM_NAMES_FILE`: default `items.json`.
- `PUZZLE_TEMPLATES_DIR`: default `Items`.
- `PUZZLE_LOCAL_POLICY_FILE`: default `puzzle-local.json`.
- `PUZZLE_AUTO_SEND`: default `1`; still subject to the validation gate.
- `PUZZLE_DEBUG_CAPTURE`: default `0`; set `1` to save attachment bytes.
- `PUZZLE_DEBUG_DIR`: default `artifacts/puzzle-debug`.
- `PUZZLE_SELFTEST=1` runs local replay on startup.
- `PUZZLE_SELFTEST_REPLAY_DIR`: labeled attachments named `apple__example.png`
  or another canonical label; PNG, JPEG and WebP are accepted.
- Legacy `PUZZLE_OPENAI_*` values are ignored by the local runtime.

The app output includes original and trained templates, item catalog, policy and Windows
native dependencies. Diagnostic captures are opt-in; routine logs show the best
candidates, scores, elapsed time and the reason for withholding an answer.

See [dataset and reproducible validation](puzzle-validation.md) and
[recognition evaluation and limitations](puzzle-recognition.md).
