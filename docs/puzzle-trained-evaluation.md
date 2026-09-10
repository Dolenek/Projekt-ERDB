# Trained puzzle evaluation

## Shipped pipeline

`puzzle-local.json` selects `template-fine-v9` with score 0.65 and margin 0.025.
The policy has independent validation for all 16 catalog entries, including key.
Its fingerprint is
`df1ae03c97f77c2aaceaff327a3ed955bcd657cd1f53a9516f9cd3d133e8b12f`.
Algorithm details live in [trained recognition](puzzle-trained-recognition.md).

| Set | Correct accepted | Wrong accepted | Rejected |
| --- | ---: | ---: | ---: |
| Development | 1,601 / 1,601 | 0 | 0 |
| Independent holdout | 100 / 100 | 0 | 0 |

The 100% result is measured on these finite sets, not a guarantee for unseen cards.
Two learned-template source images are included in development fit, never holdout.
No recognition rules, templates or thresholds changed after the validation freeze.

## Independent holdout

The reserved split comes from [the sixteen-class batch](puzzle-training-dataset.md).
Apple, banana, chip and coin each have seven examples; every other class has six.
All 81 colored, 19 grayscale, 40 line-marked and 60 line-free examples are correct.
The grayscale-with-lines subset has only four examples, all correct.
The lowest accepted score is 0.792014; the lowest accepted margin is 0.055906.
The gate's thresholds remain 0.65 and 0.025.

Single-process holdout mean/p95 time is 1,546/2,058 ms per attachment, excluding
template preparation. These are host-dependent measurements.

## Frozen evidence

`artifacts/puzzle-validation-20260909/` contains:

- `freeze.json`: implementation/template hashes, pipeline fingerprint, reserved
  manifest hash, development report hash and the pre-evaluation duplicate audit.
- `calibration.json`: all 1,601 development/exposure records.
- `holdout.json`: the unchanged reserved 100 records with relocated image paths.
- `holdout-results.json` and `.summary.json`: first independent v9 predictions.
- `policy.json`: the validation seal and class counts, matching the shipped policy.
- `completion.json`: verified promotion details and minimum score/margin.

Byte hashes, decoded RGB pixels and fixed icon crops have no development/holdout
overlap or within-holdout duplicates. These checks cannot detect every transformed
duplicate. Learned assets' source hashes belong to development; asset hashes match
`Items/Trained/provenance.json`.

Development predictions are in
`artifacts/puzzle-additions-20260909/v9-development-results.json` and its eight
partition reports. Their complete index/label coverage, correctness and matching
fingerprints are audited. The resumed build also reproduces the two root examples.
Concurrent development timings are not a performance benchmark.

## Reproduction and runtime

Build the replay tool, then use the [replay CLI](puzzle-validation.md):

```powershell
tools/PuzzleReplay/bin/Release/net48/PuzzleReplay.exe . artifacts/puzzle-validation-20260909/holdout.json artifacts/puzzle-validation-20260909/replay-check.json
```

Omit `--validate` when reproducing this already-exposed set.
`prepare_frozen_validation.py` audits and creates the freeze once, refusing to
overwrite it or reuse an evaluated source holdout. `complete_frozen_validation.py`
checks the frozen inputs, complete 100% predictions and unchanged thresholds
before copying the sealed policy to the repository default.

The test suite checks shipped eligibility, canonical key recognition, interference,
grayscale, translations/scales, corrupt/blank inputs and challenge cancellation.
Actual sending still obeys observation mode and challenge handling checks in
[the runtime contract](puzzle-solver.md). Further tuning needs a fresh holdout.
