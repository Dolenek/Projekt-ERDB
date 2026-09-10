# Multiple-reference puzzle recognition

The production pipeline is `template-multisource-v11`. Bundle selection,
thresholds and sending behavior are defined in [the solver contract](puzzle-solver.md).

## Image processing

The input is an original tall or compact puzzle attachment at a supported scale.
Malformed, oversized, unsupported and standalone-icon inputs are rejected.
The scene decoder normalizes display scale. The adaptive question-region locator
uses a glyph-filtered text mask so a bright item is not mistaken for question text.
A narrow supported tall card can use its icon region without visible question text.

Colored-line filters remove thin interference, including intersecting lines,
with local inpainting before the region is reduced for matching. Border padding
supports partially clipped items. Low-chroma scenes use maximum-channel template
luminance and a 0.65/0.35 correlation/silhouette mixture. Colored scenes use
weighted luminance and a 0.45/0.55 mixture, with aligned chroma agreement.
Grayscale references are eligible only for grayscale scenes.

## Template search

The bundle contains original item templates and learned grayscale references.
The initial search evaluates translated, rotated, resized and horizontally
stretched variants. It keeps the strongest pose per source image, allowing
multiple references of the same item to proceed to refinement.

Each retained source receives coarse refinement (angle offsets +/-7 degrees,
length +/-2 pixels, aspect +/-0.15), followed by fine refinement (+/-3 degrees,
+/-1 pixel, +/-0.05). The final score is the maximum per canonical class.
Two references of the same item cannot occupy both winner and runner-up slots.
The policy then checks absolute score and the lead over the next class.

Catalog descriptions are not model inputs. Recognition does not look up answers
by message ID, image checksum or dataset label. Scores are similarities, not
probabilities. Unsupported layouts and ambiguous matches can still be rejected;
measured accuracy is described in [the evaluation page](puzzle-campaign-results.md).

## Ownership and extension points

- `ILocalPuzzleRecognizer` exposes immutable identity, labels and ranked candidates.
- `TemplatePuzzleRecognizer` owns scene decoding and the OpenCV template library.
- `LocalPuzzleAnswerProvider` owns its recognizer, serializes calls, snapshots
  thresholds and requires matching pipeline identity for submission eligibility.
- `IPuzzleQuestionRegionLocator` and `IPuzzleLetterMaskFilter` separate layout
  detection from glyph-mask filtering.
- `IPuzzleInterferenceFilter` supplies scene cleanup.
- `IPuzzlePoseRefiner` supplies geometric refinement; immutable pose snapshots
  avoid retaining disposed OpenCV image variants.
- `IPuzzleImageSource` separates attachment retrieval from solver orchestration.

Injected recognizers return finite, descending scores for distinct canonical
labels and honor cancellation. Image preprocessing and search remain local.
