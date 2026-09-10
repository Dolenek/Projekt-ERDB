# Glyph-aware local puzzle recognition

`template-glyph-v10` is experimental and available through
`tools/puzzle/glyph-policy.json`, which has no validation seal.
The shipped policy is documented in [trained evaluation](puzzle-trained-evaluation.md).
Both use local files and OpenCV, without a remote inference API.

## Question cropping

Bright grayscale items can merge into white question letters during horizontal
morphological closing. This can move the detected text start onto the item or
cause rejection of the entire card.

V10 filters connected components of the white-letter mask before closing it.
Components wider than 32 pixels or taller than 42 pixels are removed from that
mask at the existing normalized card height. The original color image is untouched.
The regular question detector then chooses the crop boundary. These limits apply
to glyph candidates, not to item dimensions.

`IPuzzleLetterMaskFilter` and `PuzzleGlyphMaskFilter` isolate this step.
`IPuzzleQuestionRegionLocator` supplies the crop to `PuzzleCardPreprocessor`
through constructor injection. Existing pipelines retain their locator behavior;
v10 selects the filtered locator. Its distinct pipeline fingerprint prevents
reuse of a v9 validation seal.

Interference filtering, maximum-channel grayscale scoring, color comparison and
pose refinement follow [trained recognition](puzzle-trained-recognition.md).
The experimental score/margin thresholds are 0.65 and 0.01. The margin is a
development-selected setting; it requires evaluation on frozen independent data.

## Learned references

`GrayReferenceSelector` is the Python template-selection extension point.
`MedoidGrayReferenceSelector` selects up to two shape representatives.
`BrightnessStratifiedGrayReferenceSelector` splits at the largest observed brightness
gap before selecting representatives within each group, preserving rare bright
captures that global medoids can omit.

The training configuration fits grayscale references for epic coin, unicorn horn,
life potion, golden fish and normie fish. It uses visually annotated, line-free
examples from the supplied training partition. The selector mapping is injectable.
Every template records its source hash/index, selector, manifest and asset checksum.
Training uses glyph filtering before extracting the largest foreground component,
and rejects forbidden hashes before reading candidates.

Learned grayscale references remain ineligible for colored targets. The replay
provider loads each fold's assets through `--templates`, preserving isolation from
the repository's learned assets. See [the campaign contract](puzzle-campaign.md)
and [measurements](puzzle-campaign-results.md).

[V11](puzzle-multisource-recognition.md) uses the same preprocessing and training
with per-source geometric refinement instead of retaining one source per class.
