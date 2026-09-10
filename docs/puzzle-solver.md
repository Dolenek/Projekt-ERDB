# Local puzzle solver

The default recognizer is `template-multisource-v11`. It recognizes all 16
canonical item names in `items.json`, including `key`, using local OpenCvSharp
processing on Windows x64. No remote inference service or API key is required.
Downloading the original Discord attachment still requires a connection.

## Model and deployment

The model bundle is `artifacts/puzzle-validation-20260910/`:

- `policy.json` supplies thresholds and the validation fingerprint.
- `Items/` contains 16 original templates and 17 grayscale references under `Trained/`.

The UI build copies the policy and all 33 WebP assets into the same relative
location beside the executable, together with `items.json` and native dependencies.
A copied build directory is self-contained for recognition. Defaults are shared
by `PuzzleSettings` and the replay CLI. Relative settings resolve from the app
folder, then its parent directories; absolute paths are used directly.

## Runtime flow

1. Puzzle detection tracks the Discord message ID, pauses tracked timers and
   blocks normal outgoing bot sends while the challenge is active.
2. `PuzzleSolverService` loads the original attachment for the selected message,
   trying the adjacent message when necessary. Whole-page screenshots are unsupported.
3. It lazily creates the provider through `PuzzleProviderFactory` outside the UI
   thread. The provider caches templates and serializes recognition calls.
4. At most one attempt starts for a challenge message. Repeated detection does
   not trigger another solve or submission.
5. The best canonical candidate must pass the score and margin thresholds,
   provider validation and the automatic-answer setting before it can be sent.
6. Sending uses the shared lane, rechecks the active challenge and cancellation
   before Enter, and makes one submission without retries.
7. An uncertain result, unavailable image or error leaves manual resolution
   active. Desktop alerts and ten-second reminders continue while waiting.
8. The `Everything seems fine ... keep playing` confirmation cancels any active
   attempt and resumes scheduling. A delayed command result in that message is
   still processed, and a fresh `rpg cd` snapshot resynchronizes timers.

The model score is a similarity, not a probability or a guarantee of correctness.
See [recognition internals](puzzle-multisource-recognition.md) and
[measured validation](puzzle-campaign-results.md).

## Automatic answers

The selected policy uses minimum score **0.65** and minimum margin **0.01**.
Eligibility requires matching pipeline/template/threshold fingerprints, at least
100 held-out examples, at least five per class, zero wrong accepted answers and
at least 90% correct accepted answers. The default bundle has eligible evidence.
`PUZZLE_AUTO_SEND=0` forces observation mode even for an eligible provider.
Providers snapshot thresholds and eligibility; restart the app after configuration
or model changes. Do not replace model files while a provider is using them.

## Configuration

| Variable | Default |
| --- | --- |
| `PUZZLE_ITEM_NAMES_FILE` | `items.json` |
| `PUZZLE_TEMPLATES_DIR` | `artifacts/puzzle-validation-20260910/Items` |
| `PUZZLE_LOCAL_POLICY_FILE` | `artifacts/puzzle-validation-20260910/policy.json` |
| `PUZZLE_AUTO_SEND` | `1` |
| `PUZZLE_DEBUG_CAPTURE` | `0` |
| `PUZZLE_DEBUG_DIR` | `artifacts/puzzle-debug` |
| `PUZZLE_SELFTEST` | Disabled; set `1` to run startup replay |
| `PUZZLE_SELFTEST_REPLAY_DIR` | Empty |

Startup reads puzzle configuration from `.env`. Policy and template overrides
must identify a matching bundle. Self-test inputs are labeled attachments such as
`apple__example.png`; PNG, JPEG and WebP are accepted. Debug capture is opt-in.
Routine `[solver]` logs report recognition, elapsed time, submission and reasons
for waiting for manual resolution. See [replay and validation](puzzle-validation.md).
