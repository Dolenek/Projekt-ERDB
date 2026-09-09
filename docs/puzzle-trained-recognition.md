# Trained local puzzle recognition

## Pipeline and learned assets

`template-fine-v9` combines local template matching, adaptive card cropping,
colored-line removal, grayscale handling and two learned epic-coin references.
The validated production policy is `puzzle-local.json`; the unsealed evaluation
policy is `tools/puzzle/fine-policy.json`. Acceptance thresholds remain score
0.65 and margin 0.025; a matching validation seal is still required.

`Items/Trained/epic coin/` contains two lossless grayscale templates extracted
from labeled development examples 3076 and 3130. The extraction retains the
largest foreground component, excluding disconnected card-border pixels.
`Items/Trained/provenance.json` records attachment IDs through their source paths,
source indices, SHA-256 values and the extraction method.
`tools/puzzle/train_gray_templates.py` reproduces the assets without opening a
holdout. Scores on those two source examples are training fit, not validation.

The template fingerprint includes learned filenames and image bytes in stable
order. Altering or adding a learned template invalidates the validation seal.
Legacy v1/v2 fingerprints exclude learned templates and retain their label sets.
The UI distribution includes both original and learned templates.

## Recognition details

`PuzzleCrossingLineFilter` implements `IPuzzleInterferenceFilter`. It finds
Hough segments near the observed drawing colors with per-channel tolerance 8.
It requires at least 18 supporting pixels and a segment length of at least 18.
Thinness compares support within 3 pixels to support between 3 and 8 pixels;
v9 requires a ratio of at least 1.5. This permits intersecting lines while
rejecting broad filled regions. The mask is dilated before local inpainting.
The component-based filter also processes the cleaned region.

`PuzzleLuminance` detects low-chroma scenes using mean foreground chroma below
18. For these scenes, matching uses the template's maximum RGB channel, giving
weight 0.65 to correlation and 0.35 to silhouette overlap. Colored scenes retain
the original weighted luminance and 0.45/0.55 score mixture. Aligned chroma
agreement remains the color check. Learned grayscale references are eligible
only for grayscale scenes, preventing them from bypassing color discrimination.

`PuzzleTemplatePose` retains transform metadata without keeping disposed OpenCV
images alive. Every class receives coarse refinement (angle ±7°, length ±2,
aspect ±0.15), followed by fine refinement (±3°, ±1, ±0.05) around its best pose.
Scores and margins compare the final per-class winners.

Adaptive text location remains the primary layout detector. A narrow 200-pixel
card with width 140–180 can use its icon region without question text. Arbitrary
icon files and wide blank cards remain unsupported. This accommodates an existing
retained cropped attachment without changing the 32-pixel icon-only contract.

## Evaluation utilities and boundaries

`tools/puzzle/run_partitioned_replay.py` partitions development manifests across
1–8 production replay processes, verifies complete index/label coverage, and
merges predictions. It cannot validate a policy or run `holdout.json`.
Concurrent timing is not a controlled performance benchmark.

`learned_probe.py` is a development-only leave-one-out normalized nearest-example
experiment. It excludes each query's own hash and is not used in production.
`score_probe.py` compares luminance/shape weights; `select_failures.py` selects
development diagnostics without modifying labels. Neither is a validation tool.
`review_preprocessing.py` renders the adaptive preprocessing for visual review.

See [new attachment provenance](puzzle-additions.md),
[evaluation results](puzzle-trained-evaluation.md), and
[validation requirements](puzzle-validation.md).
