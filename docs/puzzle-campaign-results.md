# Puzzle validation evidence

The active model bundle is `models/puzzle/production/` with pipeline
`template-multisource-v11`. Runtime selection is defined in
[the solver contract](puzzle-solver.md).

## Independent evaluation

The frozen 200-image holdout has **200 correct accepted answers, zero wrong
answers and zero rejections**. All 16 classes have at least five examples.
The two-sided 95% Wilson interval is **98.1155%-100%**.

| Condition | Correct / total |
| --- | ---: |
| Color with lines | 66 / 66 |
| Color without lines | 98 / 98 |
| Grayscale with lines | 8 / 8 |
| Grayscale without lines | 28 / 28 |

The local evaluation snapshot in ignored `artifacts/puzzle-validation-20260910/`
contains the model, `holdout.json`, the replay reports
and `completion.json`. Templates were fitted on development examples only.
The recorded freeze audit covers implementation, policy, templates, source images
and manifests. The holdout is already exposed; rerunning it is a regression
check, not new independent validation.

Measured mean/p95 recognition time is 1,787/2,998 ms per attachment, excluding
initial template preparation. Timing depends on host and workload.

## Development coverage

Grouped cross-validation accepts **3,944/3,944** development records correctly,
with zero wrong answers and zero rejections. Counting exact-image groups once
produces **3,941/3,941**. Evidence is under
`artifacts/puzzle-additions-20260909/campaign/models/multisource-full-cv-01/`.
Each record has one out-of-fold prediction with partition-specific template fitting.
All 725 grayscale and 3,219 colored records are correctly accepted.

Development data influenced model selection, so its Wilson interval is nominal,
not an independent population guarantee. Neither that result nor 200/200 holdout
accuracy establishes a 99.9% population accuracy floor. The reports and source
manifests remain in `artifacts/` for reproducibility.

See [replay and validation procedures](puzzle-validation.md).
