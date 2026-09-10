# Puzzle campaign and statistical evaluation

## Acquisition and partitions

The campaign is `artifacts/puzzle-additions-20260909/campaign/`.
It supplements the [existing development examples](puzzle-additions.md).
Original attachments are read through EpicRPG MCP from server `555971084415926272`.
Authorized channels are bot-commands-1 through bot-commands-4. Channel -5 is not
present in the observed server. Only channel -1 has a lower date bound:
2020-01-01, inclusive. Already acquired older examples remain available.
The hard budget is 3,000 downloaded files, including excluded duplicates.

Each `round-*/` retains images, acquisition metadata, pending records, visual
review sheets, reviewed labels, exclusions and a development manifest.
`acquisition.json` records the actual channel ID, query and applicable date bound.
The first 500 unique images split into 300 development and 200 reserved cases.
Later retained cases belong to development. Historical exposed cases, including
the previously evaluated 100-image v9 holdout, also belong to development.

Eight reviewed rounds contain 2,443 retained attachments from 2,838 downloads.
The current development union contains 3,944 records, including 725 grayscale
targets and 962 cards with interference. The separately frozen set contains 200
and has one completed v11 evaluation; see [the measurements](puzzle-campaign-results.md).
Round 08's 200 images have direct visual annotations on eight review sheets;
they belong only to development. Round 01's `summary.json` and later rounds'
`acquisition.json` preserve source channel/query metadata. The first 500-image
split is from bot-commands-2 (channel `581966008047239177`); the channel ID in
the supplied navigation link, `557606805425881088`, is bot-commands-1. Search
scope, rather than the open channel, determines the source of the first batch.

`resumption-audit-20260910.json` verifies that round 01's 500 unique attachments
are exactly the union of its 300 development and 200 frozen records. The campaign
already contains the requested initial acquisition; continuing evaluation does
not collect another overlapping 500-image batch.

The new `holdout.json` is frozen by manifest and attachment hashes in
`holdout-freeze.json`. It is excluded from template selection, tuning, diagnostics
and every CV fold. Integrity checks may hash its bytes and pixels without running
the recognizer. The split reserves five examples per class, then 120 additional
seeded random examples; the seed is `20260909`.

Annotations use the [existing visual vocabulary](puzzle-dataset-expansion.md).
Blue curved strands are mermaid hair; pink diagonal horns are unicorn horn;
green rectangular circuit icons are chip. Predictions are not annotation labels.
Development annotation revisions preserve the previous labels and explicit visual
reasons under `annotation-revision-*/`. Affected old reports require reconciliation.
The revision utility cannot revise the frozen validation set.

## Measurement contract

The primary metric is correct accepted answers divided by **all** evaluated
attachments; abstentions count as unsuccessful recognition. Wrong accepted answers
and rejections are reported separately. Totals and class/condition breakdowns
include two-sided 95% Wilson score intervals without continuity correction.
The requested numerical CV criterion is a lower bound of at least 0.999.
The frozen 200-case evaluation permits at most one unsuccessful case.

Even zero failures require at least 3,838 trials for this Wilson lower bound.
Perfect independent 200/200 evidence has interval 98.115%–100%; it cannot itself
establish a 99.9% population accuracy floor. The formula follows
[NIST's interval reference](https://www.itl.nist.gov/div898/handbook/prc/section2/prc241.htm).

Five grouped, stratified folds use a fixed seed. Identical bytes, decoded RGB
pixels or fixed icon crops stay together. Each attachment receives one out-of-fold
prediction. Each fold refits learned references on its training records and copies
only original `Items/*.webp`, excluding existing learned references. Source hashes
are audited against both fold-test hashes and the frozen holdout registry.
Synthetic transformations never count as additional trials.

The pooled CV interval is nominal: folds share training data, historical examples
influenced pipeline design, and channel history is not a random sample of future
play. Exact deduplication does not prove independence under transformations.
See [Bengio and Grandvalet on CV variance](https://www.jmlr.org/papers/v5/grandvalet04a.html).
The independent holdout result must remain a separate statement.
Pipeline design uses inspected development/CV errors; these runs are not nested
cross-validation with an untouched outer model-selection assessment. Repeated
tuning does not turn the nominal CV interval into a population guarantee.

## Reproduction

- `collect_unique_batch.py`: bounded acquisition, pre-download date checks,
  existing-message skips, exact deduplication and review sheets.
- `campaign_dataset.py`: reviewed manifests, initial split and overlap checks.
- `train_gray_templates.py --manifest ... --output ... --forbidden ...`:
  partition-specific fitting with source provenance and injectable selectors.
- `cross_validation.py <campaign> <new-output-directory>`: input snapshots,
  grouped folds, isolated templates, replay and Wilson summaries. `--grayscale-only`
  is a subset diagnostic, not a complete CV result.
- `partition_integrity.py`: image separation and input checksum audits.
- `revise_development_annotations.py`: audited development-only label revisions.
- `campaign_validation.py <campaign> <full-cv-output> <new-validation-output>`:
  verify the strict CV criterion, fit final references on development, freeze
  inputs and run the reserved holdout once. See [final evaluation](puzzle-campaign-validation.md).

The [replay CLI](puzzle-validation.md) supports explicit template and policy paths.
CV never seals a runtime policy. Output directories cannot be reused. Final
validation requires freezing the implementation, templates and policy before
opening the reserved set. Changes after viewing it require new independent evidence.
CV snapshots cover each fold's manifests, template bytes, template provenance and
every source attachment. They are verified after training and after all replays.

See [campaign measurements](puzzle-campaign-results.md) and
[glyph-aware recognition](puzzle-glyph-recognition.md).
