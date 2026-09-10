# Puzzle replay and validation

The current model's measured evidence is documented in
[puzzle validation evidence](puzzle-campaign-results.md). Runtime configuration
and the automatic-answer gate live in [the solver contract](puzzle-solver.md).

## Replay

Build from the repository root:

```powershell
dotnet build tools/PuzzleReplay/PuzzleReplay.csproj -c Release
```

```text
PuzzleReplay.exe <repository-root> <manifest-json> <output-json> [--policy <path>] [--templates <directory>] [--validate] [--allow-unsupported]
```

Default policy and templates select the same model bundle as the UI. The CLI
resolves them relative to its repository-root argument; explicit CLI overrides
select another bundle. It does not use the UI's environment overrides.
Replay invokes the production provider without starting the UI or sending chat.

The manifest is a JSON array with `index`, `image`, `sha256`, `expected`,
`lines` and `grayscale`; records can also retain message IDs and provenance.
Image paths are relative to the manifest directory or absolute. Labels use
canonical item names. Checks reject invalid records, duplicate hashes, checksum
mismatches and unsupported labels. `--allow-unsupported` permits rejection
diagnostics for out-of-catalog inputs and cannot be combined with validation.

Output is a JSON array of predictions with a companion `.summary.json` containing
accepted-correct, accepted-wrong, rejected and top-candidate counts, class/condition
breakdowns, timing and 95% Wilson intervals. Rejections remain in the accuracy
denominator. These reports are evidence artifacts, not canonical documentation.

For a regression check of the active bundle, run from the repository root:

This example needs the local evaluation dataset in ignored `artifacts/`.
The dataset is not required to build or run the production application.

```powershell
tools/PuzzleReplay/bin/Release/net48/PuzzleReplay.exe . artifacts/puzzle-validation-20260910/holdout.json artifacts/puzzle-replay-check.json
```

## Validation integrity

`--validate` requires `holdout.json` and checks separation from its neighboring
`calibration.json`. It clears the selected policy's old seal before evaluating
and writes measured evidence to that policy. Passing the runtime gate enables
eligibility; an insufficient result returns 3, input/runtime errors return 1.
Run validation with the bot stopped, then rebuild and restart to load the policy.
Do not use this option for ordinary regression checks of the sealed model.

Independent evaluation requires frozen implementation, thresholds and templates;
training and model selection exclude the reserved set. Exact-file, decoded-pixel
and icon-crop duplicates must be excluded across development and reserved groups.
Checksum comparison alone does not prove independence. The campaign tooling
records provenance, partitions, frozen hashes and holdout exposure in `artifacts/`.
An inspected holdout cannot be reused as fresh evidence after model tuning.

## Integration checks

```powershell
dotnet test EpicRPGBot.Tests/EpicRPGBot.Tests.csproj --filter FullyQualifiedName~Puzzle
```

Tests cover production model selection and fingerprint eligibility, canonical
answers including key, environment overrides, preprocessing and pose refinement,
invalid images, observation mode, missing images, duplicate attempts and
cancellation. Windows image tests require the native OpenCV runtime.
See [MCP operation](testing-automation.md) for inspecting the application.

The focused `--filter FullyQualifiedName~ProductionPuzzleTests` suite uses only
the versioned production bundle and a synthetic key card. It runs without local
datasets; the broader puzzle suite includes dataset-dependent regression tests.
