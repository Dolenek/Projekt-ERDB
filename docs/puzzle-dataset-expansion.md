# Additional puzzle attachments

## Source and scope

`artifacts/puzzle-dataset-expansion-20260908/` contains 500 original PNG attachments
retrieved through EpicRPG MCP from Discord channel `557606805425881088` in server
`555971084415926272`. Search: `in: bot-commands-1 has:image stop there, `.
The batch covers the first 20 search pages in newest-first order, with message
timestamps from 2025-04-28 through 2026-09-08. It is not an exhaustive archive.
Collection does not send chat messages or run bot commands.

## Contents

- `images/`: 500 unchanged attachment files, named by Discord message ID.
- `development.json`: 395 newly labeled supported examples available for tuning.
- `holdout.json`: 100 supported examples reserved before tuning and evaluated
  by both fixed pipelines; see [the results](puzzle-expansion-results.md).
- `calibration.json`: 756 exposure records: the 395 development examples plus
  361 examples from the earlier calibration, holdout and user-example manifests.
  This file enables the replay tool's existing cross-split hash check.
- `unsupported.json`: five visually confirmed full question cards showing `key`.
- `excluded.json`: duplicate findings; currently empty.
- `pending.json`: acquisition metadata before labeling, retained for reproducibility.
- `visual-labels.json`: the manually assigned labels and condition flags.
- `review-01.png` through `review-20.png`: numbered icon crops for visual review.
- `templates.png`: reference item sheet; `summary.json`: counts and split metadata.

The holdout uses deterministic stratified sampling with seed `20260908`: seven
examples for the first ten alphabetically sorted supported labels and six for
the remaining five. Its exposure status is recorded in `summary.json`.
No thresholds, recognition rules or validation seals are changed by collection.

## Labels and descriptions

Each labeled record retains `index`, `messageId`, `image`, `sha256`, `expected`,
`lines` and `grayscale`, compatible with the existing replay reader.
Additional fields are timestamp, dimensions, decoded-pixel and icon-crop hashes,
`description`, and `annotationMethod: visual-template-comparison`.

Labels were assigned by visually comparing the numbered crops with `Items/`.
Descriptions identify the observed item shape, color condition and interference.
They are not classifier predictions or labels inferred from subsequent chat replies.
`grayscale` means grayscale or near-grayscale target appearance, including the
naturally low-chroma wolf hide; colored interference lines do not make a target
colored for this annotation. There are 76 such images and 161 images with lines.
The batch contains all 15 supported classes, with 10–64 examples per class.

`key` is outside the legacy v1/v2 recognition sets. The [clean/adaptive pipelines](puzzle-adaptive.md)
support it as a sixteenth target. `unsupported.json` retains the five original
key records as a separate
diagnostic set, accepted by the replay CLI only with `--allow-unsupported`.
V1 rejects all five; v2 falsely accepts one. See [the results](puzzle-expansion-results.md).

## Integrity and duplication

All 500 file hashes and decoded-image hashes are verified. No exact duplicates
were found within the batch or against any image in the earlier dataset, using
file bytes, decoded RGB pixels, and fixed left icon crops. The crop is x=0..120,
y=12..height-12 for tall cards, or x=0..60 for compact cards.
This does not detect every shifted, recompressed or otherwise near-duplicate icon;
it is not proof of statistical independence across all possible transformations.
All images share game templates by design.

Persisted provenance excludes signed attachment URLs, account credentials and
full chat histories. Original image bytes and message IDs remain available for audit.

## Collection utilities

`tools/puzzle/collect_dataset.py` accepts ephemeral MCP-observed URL records,
downloads missing files, checks exact duplicates and creates review sheets.
Already-saved original files are reused. If the CDN requires browser context,
retrieve the observed URLs with MCP WebView `fetch` and save the original bytes
before running the script. Signed URLs and transfer payloads belong outside the repo.

`tools/puzzle/finalize_dataset.py <batch-directory>` combines acquisition metadata
with the reviewed labels, verifies hashes, writes descriptions and reserves the
holdout. Both utilities use Python with Pillow; no remote recognition API is used.
Finalization refuses to overwrite a split marked as evaluated. A used holdout
cannot be relabeled as fresh independent evidence.

See [validation requirements](puzzle-validation.md) and
[the local runtime contract](puzzle-solver.md).
