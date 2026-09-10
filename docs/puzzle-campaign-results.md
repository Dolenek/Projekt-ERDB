# Puzzle campaign measurements

Development CV and the independent 200-image evaluation are reported separately.
Both numerical campaign targets pass for v11. Statistical interpretation is
defined in [the campaign contract](puzzle-campaign.md); the CV interval is nominal.

## Independent validation

The one-time v11 evaluation in `artifacts/puzzle-validation-20260910/` accepts
**200/200 correctly**, with zero wrong answers and zero rejections. Its two-sided
95% Wilson interval is **98.1155%–100%**. All 16 classes have at least five cases.
The reserved registry records exposure; this set cannot be reused as fresh evidence.
Every frozen implementation, policy, template, source-image and manifest checksum
passes post-evaluation verification. No model or policy tuning uses this set.

`completion.json` includes full CV, exact-group CV and independent Wilson results,
plus the successful target and submission-eligibility flags. `policy.json` is the
sealed v11 policy, and `Items/` contains its isolated originals and 17 learned
grayscale references fitted only on development. Mean/p95 time is 1,787/2,998 ms
per attachment, excluding template initialization; these are host-dependent values.
The [runtime selection instructions](puzzle-multisource-recognition.md) keep these
validated assets separate from the shipped v9 default.

Perfect 200/200 does **not** establish a 99.9% population accuracy floor. Nor does
development-adaptive CV supply an independent 95% guarantee at that floor.

## Baseline development partitions

| Baseline development partition | Correct / total | Wrong accepted | Rejected | 95% Wilson interval |
| --- | ---: | ---: | ---: | --- |
| Round 01, shipped v9 templates | 300 / 300 | 0 | 0 | 98.736%–100% |
| Round 02, corrected visual labels | 499 / 500 | 0 | 1 | 98.876%–99.965% |
| Round 03, shipped v9 templates | 490 / 500 | 0 | 10 | 96.358%–98.910% |

Round 02 has 33 audited visual label corrections involving mermaid hair, unicorn
horn and five chip examples. Original labels are preserved in `annotation-revision-*/`.
`baseline-reviewed-labels-results.json` reconciles unchanged predictions against
the corrected labels; this does not represent a classifier change.

Round 03's failures are grayscale: normie fish 12008, 12031, 12294 and 12403;
golden fish 12026, 12247, 12411 and 12418; life potion 12191; epic coin 12424.
Several originate in a white item merging into the question mask. Other failures
have insufficient margin, including small grayscale unicorn horn 11524 in round 02.
The corresponding manifests map indices to original attachments and hashes.

## Complete development CV

| Pipeline | Correct / total | Wrong accepted | Rejected | 95% Wilson interval |
| --- | ---: | ---: | ---: | --- |
| v10, one seed per class | 3,943 / 3,944 | 0 | 1 | 99.8565%–99.9955% |
| v11, one seed per source | 3,944 / 3,944 | 0 | 0 | 99.9027%–100% |

V11 evidence is in `campaign/models/multisource-full-cv-01/`. Every record has
exactly one out-of-fold prediction, with partition-specific template fitting.
Counting exact-image groups once gives 3,941/3,941 and interval
99.9026%–100%, also above the required lower bound. Frozen input hashes and
the absence of development/holdout overlap pass the final entry audit.
All 725 grayscale and 3,219 colored records are correctly accepted; the complete
per-class and condition Wilson intervals are in `summary.json`.
The only changed acceptance decision relative to v10 is record 12294.

| V11 CV condition | Correct / total | 95% Wilson interval |
| --- | ---: | --- |
| Color, lines | 879 / 879 | 99.5649%–100% |
| Color, no lines | 2,340 / 2,340 | 99.8361%–100% |
| Grayscale, lines | 83 / 83 | 95.5765%–100% |
| Grayscale, no lines | 642 / 642 | 99.4052%–100% |

The complete v10 grouped five-fold run in `campaign/models/glyph-full-cv-01/`
has **3,943/3,944 correct accepted answers**, zero wrong accepted answers and one
rejection. Its two-sided 95% Wilson interval is **99.8565%–99.9955%**; the point
estimate is 99.9746%, but the required 99.9% lower bound is not met.
The rejection is grayscale normie fish 12294. The
[multiple-reference search](puzzle-multisource-recognition.md) addresses the
discarded-source refinement limitation without changing acceptance thresholds.

## Training diagnostics

Two global epic-coin medoids rejected development case 3388: 317/318 grayscale
cases were correct, Wilson interval 98.241%–99.944%. The references omitted the
rare bright appearance.

Brightness-stratified coin references plus grayscale horn references achieved
398/398 out-of-fold grayscale predictions across five folds, interval 99.044%–100%.
This subset diagnostic refits templates within each fold. Evidence is in
`campaign/models/coin-horn-grayscale-diagnostic/`; it does not satisfy the complete
dataset criterion or establish population accuracy.

The [v10 pipeline](puzzle-glyph-recognition.md) adds glyph-aware cropping and more
grayscale reference classes. Neither this subset diagnostic nor the inspected
fish example may be used as independent validation evidence.

## Verification

Wilson tests cover reference values, invalid counts and the 3,838-case requirement.
Python tests cover date bounds, reencoded-pixel grouping, fold coverage, training
leakage rejection, rare-bright selection and post-freeze changes. Image tests cover
a bright item beside text and rejection of an item-only card. Fixed example
fixtures live under `EpicRPGBot.Tests/Puzzle/Fixtures/`.
