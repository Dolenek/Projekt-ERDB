# Additional development examples

`artifacts/puzzle-additions-20260909/` contains 100 original attachments collected
through the EpicRPG MCP-managed Discord WebView from channel `557606805425881088`.
Search: `in: bot-commands-1 has:image stop there, before:2023-12-26`.
Four newest-first pages extend beyond the earlier batches' date range.
The app is closed after collection; no chat messages or game commands are sent.

Byte, decoded-pixel and fixed icon-crop checks exclude six duplicates against all
existing puzzle image directories and within the batch. The 94 retained images
are development-only. Exact-crop checks do not detect all transformed duplicates.
Review sheets and `review-labels.json` preserve the visual review; canonical
labels and descriptions are in `development.json` and `visual-labels.json`.
Descriptions follow [the established annotation vocabulary](puzzle-dataset-expansion.md).

The two user-supplied root WebP files are also development examples:

- `epic_guard_dragon_scale.webp`: red segmented pointed scale, with interference.
- `epic_guard_mermaid.webp`: cyan flowing strand, with interference.

`root-examples.json` stores their hashes, labels, condition flags and descriptions.
`development-combined.json` contains all 1,601 development examples: 1,505 earlier
exposure records plus 94 new attachments and the two root examples.
It excludes the separately reserved 100-image holdout.

## Annotation audit

`annotation-audit.json` records a visual correction for development index 3374,
attachment `1268927145611104319`: its slender body, tail and fins identify
grayscale `epic fish`, rather than the previous `wolf skin` label.
The earlier batch's development/calibration manifests and final labels include
the correction; raw shorthand review notes and earlier reports preserve their
original annotations. Its holdout labels are unchanged.

`tools/puzzle/prepare_development_audit.py` reproduces that correction and the
root manifest. `finalize_additions.py` verifies attachment hashes, applies the
reviewed labels, writes descriptions and builds the development union.

## Acquisition tools

`search_webview.py` enters the observed search combobox using the local DevTools
endpoint created by EpicRPG MCP. `collect_webview_batch.py` reads attachment URLs
from search-result DOM nodes and retrieves original bytes in browser context.
`--start-index` prevents index collisions between batches. Only provenance and
image bytes are retained; signed URLs, account tokens and full chats are not stored.

See [trained recognition](puzzle-trained-recognition.md) and
[validation](puzzle-validation.md).
