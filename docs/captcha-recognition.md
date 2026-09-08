# Captcha recognition evaluation

## Reproducible evidence

The repository provides two local pipelines using the same 15 templates and
the same acceptance thresholds (score 0.65, margin 0.025).
`template-correlation-v1` is the shipped, validated pipeline.
`template-refinement-v2` is an optional evaluation pipeline without a validation seal.
The runtime contract and configuration live in [captcha-solver.md](captcha-solver.md).

Calibration replay covers 257 labeled attachments:

| Pipeline | Correct accepted | Wrong accepted | Rejected | Correct top candidate |
| --- | ---: | ---: | ---: | ---: |
| v1 | 244 (94.94%) | 0 | 13 | 254 |
| v2 | 245 (95.33%) | 0 | 12 | 254 |

Evidence is in `artifacts/captcha-dataset/baseline-calibration.json` and
`refined-calibration.json`, with companion summary files. A correct top candidate
does not imply an accepted answer: both score and margin must pass.
The existing Python normalization-probe output has 236/257 correct top candidates;
the correlation prototype has 254/257. These are stored prototype outputs, not
fresh Python runs, and their scores are not directly interchangeable with C# scores.

The 100-image holdout regression comparison gives:

| Pipeline | Correct accepted | Wrong accepted | Rejected | Correct top candidate |
| --- | ---: | ---: | ---: | ---: |
| v1 | 90 | 0 | 10 | 99 |
| v2 | 90 | 0 | 10 | 100 |

Results are in `baseline-holdout.json` and `refined-holdout.json` in the same
artifact directory. All 100 v1 rankings, scores and acceptance decisions match
the stored `holdout-results.json` exactly. V2 improves top-candidate accuracy
without improving accepted accuracy. Its grayscale result is 10/15 versus v1's
11/15; both accept only 1/3 grayscale examples with lines.
Measured holdout mean/p95 recognition times are 861/917 ms for v1 and
1140/1352 ms for v2. Timings depend on the host and concurrent load and exclude
template initialization. This small gain does not justify making v2 the default.

## Geometry

Template matching searches all positions inside the normalized, padded left region.
The base grid uses lengths 12–56 pixels in steps of 4, rotations -30° through 30°
in steps of 15°, and horizontal aspects 0.65, 1.0 and 1.5.
The v2 search refines the best transform of each class using length offsets
-2/0/+2, angle offsets -7°/0/+7°, and aspect offsets -0.15/0/+0.15.
Every class receives that search budget, including competing candidates.

On calibration, v2 accepts the small compact `epic fish` (index 20), `ruby` (96)
and grayscale `apple` (236) that v1 rejects. It rejects two additional `epic coin`
examples (48 and 260) because stronger competing matches reduce the margin.
The net gain is one accepted answer, with unchanged top-candidate accuracy.

Synthetic tests exercise ±10-pixel translations and 80%/120% display scales on
a life-potion card in color and grayscale. This is targeted geometry coverage,
not evidence of accuracy across all classes, clipping patterns or scales.

## Color and interference

On calibration, v1 accepts 209/215 colored and 35/42 grayscale attachments;
v2 accepts 211/215 colored and 34/42 grayscale attachments.
The line-marked subset is 65/74 for v1 and 66/74 for v2.
Both pipelines accept 13/18 `wolf skin` examples.

Color agreement compares aligned chroma vectors, not global histograms.
It becomes neutral when the sampled region has little chroma. Colored lines can
still influence the sampled chroma of a grayscale icon. Whole-region foreground
coverage also counts interference and any text inside the fixed crop.
Low-contrast grayscale silhouettes and similar round objects remain difficult.
The current pipelines do not segment lines, detect arbitrary layouts or infer
missing parts of a clipped icon. A global histogram alone cannot distinguish
same-color items and does not solve grayscale ambiguity.

## Validation boundary

The optional v2 policy has no automatic-submission eligibility. Existing holdout
replays are regression comparisons; promoting a changed pipeline requires fresh
independent evidence under [the validation procedure](captcha-validation.md).
The shipped v1 policy and its limits remain unchanged.
