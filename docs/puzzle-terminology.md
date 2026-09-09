# Puzzle terminology

This page defines the repository-wide vocabulary for the in-game image challenge feature. The bot receives an image challenge with a question, recognizes the referenced item, and submits an answer. Use these names everywhere: code identifiers, file names, environment variables, log messages, and documentation.

## Core names

| Concept | Name |
| --- | --- |
| Feature domain | Puzzle |
| Solver orchestration service | `PuzzleSolverService` (`EpicRPGBot.UI/Services/`) |
| Answer provider contract | `IPuzzleAnswerProvider` (`EpicRPGBot.UI/Puzzle/`) |
| Chat client answer contract | `IPuzzleAnswerChatClient` (`EpicRPGBot.UI/Services/`) |
| Local recognition pipeline | `EpicRPGBot.UI/Puzzle/Local/` (`ILocalPuzzleRecognizer`, `LocalPuzzlePolicy`, `PuzzleTemplateMatcher`, ...) |
| Engine integration | `BotEngine.Puzzle.cs` (partial class alongside `BotEngine.cs`) |
| Replay harness | `tools/PuzzleReplay/` (C# console tool) |
| Dataset tooling | `tools/puzzle/` (Python scripts, policy JSONs) |
| Local policy config | `puzzle-local.json` (repository root) |

## Environment variables

- `PUZZLE_LOCAL_POLICY_FILE` - path to the local policy JSON (default `puzzle-local.json`).
- `PUZZLE_ITEM_NAMES_FILE` - file with recognized item names for the catalog.
- `PUZZLE_TEMPLATES_DIR` - directory with learned templates.
- `PUZZLE_DEBUG_DIR`, `PUZZLE_DEBUG_CAPTURE` - debug artifact output.
- `PUZZLE_AUTO_SEND` - auto-submit the recognized answer.
- `PUZZLE_SELFTEST`, `PUZZLE_SELFTEST_REPLAY_DIR` - self-test mode inputs.

## Dataset locations

Dataset batches live under `artifacts/` using `puzzle`-prefixed names (for example `artifacts/puzzle-dataset`, `artifacts/puzzle-training-20260908`, `artifacts/puzzle-validation-20260909`). Tests, calibration JSONs, `Items/*/provenance.json`, and the Python tooling reference those folder names directly. Keep new outputs in the same `puzzle`-prefixed scheme (for example `artifacts/puzzle-debug` for debug artifacts) and do not introduce other prefixes.
