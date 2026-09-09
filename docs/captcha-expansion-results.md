# Captcha evaluation on the additional dataset

## Scope

The legacy v1/v2 recognizers were evaluated on all 500 attachments in
`artifacts/captcha-dataset-expansion-20260908/`, using unchanged templates,
score threshold 0.65 and margin 0.025. The 395 development examples were evaluated
before the 100 reserved holdout examples in each pipeline. No rules were tuned
between these runs. Five out-of-catalog key cards were evaluated separately.
These are replay measurements, not training: adding labeled images does not
automatically alter the template bank or recognition rules.

## Results

| Split | Pipeline | Correct accepted | Wrong accepted | Rejected |
| --- | --- | ---: | ---: | ---: |
| Development (395) | v1 | 367 (92.91%) | 0 | 28 |
| Development (395) | v2 | 371 (93.92%) | 0 | 24 |
| Holdout (100) | v1 | 90 (90%) | 0 | 10 |
| Holdout (100) | v2 | 94 (94%) | 0 | 6 |
| Unsupported key (5) | v1 | 0 | 0 | 5 |
| Unsupported key (5) | v2 | 0 | 1 | 4 |

Across the 495 supported examples, v1 accepts 457 correct answers (92.32%) and
v2 accepts 465 (93.94%); neither accepts a wrong supported-class answer.
Correct rejection is the desired behavior on the separate unsupported set;
its rows must not be interpreted as a zero-percent successful rejection rate.

Both pipelines meet the numerical gate on the 15-class holdout alone.
That gate does not measure out-of-catalog rejection, where v2 has a failure.
The separate legacy v1 policy retains its seal. V2 remains unvalidated and is not
recommended for automatic submission on the strength of this positive-only gate.
Its current empty validation seal prevents actual automatic submission.

## Observed weaknesses

- Both variants accept only 17/27 wolf hides. Nine of the ten rejected wolf hides
  have interference lines; the remaining one is small and low contrast.
- Grayscale/near-grayscale images with lines are only 2/11 correct accepted for
  both variants. This subset is small and largely consists of wolf hides.
- Both variants reject nine life-potion examples; seven are compact 85-pixel cards.
- The v2 development improvements are two golden fish, one dragon scale and one
  zombie eye. It does not improve the compact life-potion failures in this batch.
- V2 accepts key card index 2019, message `1542152978981458002`, as `wolf skin`:
  score 0.6578 and margin about 0.0514. V1 rejects it. This is a false acceptance
  at the classifier boundary, even though v2's submission gate blocks sending.

The compact-region diagnostic uses development image
`images/1484098743375757404.png` (index 2094). The current 100-by-85 left crop
contains part of the question's white `w` and the top/bottom black borders.
At original resolution, the foreground rule marks 1,147 pixels: 253 in the left
interior region, 294 in the right text strip and 600 in the border strips.
These are preprocessing diagnostics before downsampling, not matcher scores.
`compact-region-diagnostic.png` shows the crop. Whole-region foreground coverage
therefore includes substantial non-item content in this case.

## Evidence and reproduction

The batch directory contains `v1-development-results.json`, `v2-development-results.json`,
corresponding `*-holdout-results.json` and `*-unsupported-results.json`, and their
companion `.summary.json` files. `evaluation-summary.json` aggregates the six runs;
`review-required.json` links rejected and incorrect predictions to original images.

Use [the replay CLI](captcha-validation.md) for development and holdout manifests.
Select v1 with `--policy tools/captcha/legacy-policy.json` or v2 with
`--policy tools/captcha/refined-policy.json`.
For unsupported cards also supply `--allow-unsupported`, never `--validate`.
Run `python tools/captcha/summarize_replay.py <batch-directory>` to audit report
coverage and labels against the frozen manifests and regenerate the aggregate.

All replay image hashes passed; the holdout has no hash overlap with the 756
exposure records in its neighboring `calibration.json`.
`summary.json` marks the holdout as evaluated. Finalization refuses to overwrite
an evaluated split. Subsequent tuning against these outcomes requires another
fresh holdout for independent validation.

Both pipelines ran concurrently. Holdout mean/p95 times were 870/939 ms for v1
and 1089/1292 ms for v2, excluding template initialization. These timings are
host/load dependent, not a controlled performance benchmark.

The test suite includes real key-card rejection by the legacy v1 pipeline
and a check that unsupported diagnostics cannot seal a validation policy.
See [dataset provenance and annotation semantics](captcha-dataset-expansion.md).
For the shipped 16-class pipeline, see [trained evaluation](captcha-trained-evaluation.md).
