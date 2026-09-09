# Adaptive local puzzle recognition

## Pipeline contracts

`template-clean-v3` and `template-adaptive-v4` support the 16 catalog entries,
including `key`. They use the original local templates and do not call an inference
API. Their evaluation policies are `tools/puzzle/clean-policy.json` and
`tools/puzzle/adaptive-policy.json`. Legacy v1/v2 still recognize their original
15 labels; adding the key catalog entry does not change their fingerprints.

`TemplatePuzzleRecognizer` selects preprocessing and label set by pipeline
identity. `ILocalPuzzleRecognizer` remains the provider extension point.
The acceptance thresholds are score 0.65 and margin 0.025 in the evaluation policies.
Automatic submission still requires a matching validation seal.

## Preprocessing

Both pipelines normalize supported 200-pixel and 85-pixel cards before matching.
V3 uses a fixed icon crop excluding the compact card's question and border strips.
V4 locates the question's bright, low-chroma letters with a 21-by-3 morphological
closing operation and connected components. It searches to the left of that text,
leaving an eight-pixel gap. This accommodates older, wider cards whose icons sit
farther right. Cards without a qualifying question region are rejected.

`PuzzleQuestionRegionLocator` owns text-region detection.
`PuzzleCardPreprocessor` owns cropping and downsampling.
`IPuzzleInterferenceFilter` permits alternative local interference processing.
`PuzzleColoredLineFilter` groups saturated pixels by quantized BGR color and
examines connected components. Long, thin components are masked and inpainted:
at least 12 pixels, length at least 18, and aspect ratio at least 8 after adding
one pixel to the short side. The mask is dilated one pixel before local inpainting.
Broad colored items are retained. Grayscale or broad interference is not removed
by this filter. The recognition score remains grayscale correlation, silhouette
coverage and aligned chroma agreement. These pipelines use the base transform grid.

## Key template provenance

The additional dataset contains five visually confirmed full key questions;
the newer batch contains another 16. The key is an actual target in these cards.
`Items/key.webp` is a lossless crop (x=65..120, y=65..120) of development attachment
`artifacts/puzzle-dataset-expansion-20260908/images/1542152978981458002.png`.
It retains the card background and the observed red-orange head/yellow toothed
shaft. The template loader crops its foreground and creates the usual geometric
variants. The source attachment belongs to prior exposure, never to the fresh holdout.

## Evidence boundaries

V3 C# replay accepts 392/395 earlier development images correctly (99.24%),
with zero wrong accepted answers. All 21 wolf hides and all 57 life potions in
that development split are accepted correctly. This is development evidence,
not independent validation of either v3 or v4.

See [the new dataset](puzzle-training-dataset.md) for provenance, split boundaries
and evaluation status, and [the validation contract](puzzle-validation.md).
