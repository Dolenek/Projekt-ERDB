# Production puzzle model

This versioned bundle contains the sealed policy and the 33 WebP assets needed
by the production recognizer. The item catalog is the repository's `items.json`.

Runtime configuration and packaging are documented in
[the puzzle solver contract](../../../docs/puzzle-solver.md).
Evaluation evidence is summarized in
[validation evidence](../../../docs/puzzle-campaign-results.md).

Training datasets, captures and replay output belong in ignored `artifacts/`,
not in this bundle. Template bytes and the policy must be updated together to
retain the validated fingerprint.
