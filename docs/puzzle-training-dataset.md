# Sixteen-class puzzle dataset

## Acquisition and annotation

`artifacts/puzzle-training-20260908/` contains 750 original attachments from
channel `557606805425881088` in server `555971084415926272`. EpicRPG MCP opened
the channel and the search `in: bot-commands-1 has:image stop there, before:2025-04-28`.
Thirty newest-first result pages were collected. No chat messages or game commands
were sent. The MCP-managed app is closed after acquisition.

`tools/puzzle/collect_webview_batch.py` attaches to that MCP-created WebView's
local DevTools endpoint, observes attachment URLs in search-result DOM nodes,
downloads original bytes in the same browser context and advances the search UI.
It does not read account tokens or retain signed attachment URLs. `acquired.json`
retains only image provenance. Downloaded files are named by message ID.

File bytes, decoded RGB pixels and fixed icon crops are compared against both
previous datasets and within the new batch. Six duplicates are excluded, leaving
744 images. This detects exact matches, not all shifted or recompressed duplicates.
`excluded.json` records each match. All retained hashes are verified during
preparation. The new batch includes all 16 classes, including 16 keys.

`review-01.png` through `review-30.png` show numbered item regions. Labels were
assigned by visual comparison, not copied from classifier predictions.
`review-labels.json` retains compact review notation; `visual-labels.json` contains
canonical labels and flags. Full originals were inspected when a review crop
cut off an icon. Descriptions use the same outline/color/interference vocabulary
as [the earlier batch](puzzle-dataset-expansion.md).
`grayscale` includes near-grayscale targets such as naturally low-chroma wolf hides;
colored interference does not change the target's grayscale annotation.

## Split and exposure

`tools/puzzle/prepare_training_batch.py` creates a deterministic stratified split
with seed `20260909`: seven examples each for apple, banana, chip and coin, and
six for every other class, for a 100-image holdout. It refuses to overwrite a
reserved holdout. Classification results are not used to select that split.

Four images inspected specifically for layout development (indices 3710, 3712,
3713 and 3749) are excluded from holdout eligibility. They are development examples.
`development.json` contains the other 644 labeled images available for tuning.
`prior-exposure.json` contains 861 previously inspected records from both earlier
datasets, including earlier holdouts and key cards.
`calibration.json` combines those 861 records with the 644 development records
so validation can detect byte-hash overlap with all prior exposure.

`summary.json` records class counts, seed, layout exposure and holdout evaluation
status. The reserved holdout is not used for rule or threshold selection.
Any later tuning against its outcomes requires another independent holdout.
Its first evaluation is the frozen v9 validation described in
[trained evaluation](puzzle-trained-evaluation.md). The validation directory's
`calibration.json` includes the complete 1,601-example exposure union, including
the later additions, and its `holdout.json` preserves the same reserved records.

See [adaptive recognition](puzzle-adaptive.md) and [replay usage](puzzle-validation.md).
