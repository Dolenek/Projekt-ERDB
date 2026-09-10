# Multiple-reference local recognition

`template-multisource-v11` has a completed, sealed campaign evaluation.
`tools/puzzle/multisource-policy.json` remains an unsealed development policy.
Both retain score 0.65 and margin 0.01. Production defaults remain described
in [the runtime contract](puzzle-solver.md).

## Validated model selection

The validated model is `artifacts/puzzle-validation-20260910/`. To select it in
the existing runtime configuration, set both:

```text
PUZZLE_LOCAL_POLICY_FILE=artifacts/puzzle-validation-20260910/policy.json
PUZZLE_TEMPLATES_DIR=artifacts/puzzle-validation-20260910/Items
```

The policy requires those exact template bytes. `PUZZLE_AUTO_SEND=0` still forces
observation mode. The repository's shipped `puzzle-local.json` and `Items/Trained`
remain the v9 configuration; evaluation does not silently switch a running bot.
See [the measurements and statistical limits](puzzle-campaign-results.md).

## Search behavior

The pipeline uses [glyph-aware preprocessing](puzzle-glyph-recognition.md),
partition-trained grayscale references and the existing two geometric refinement
stages. It changes which poses survive the initial search.

V10 retains the strongest pose per canonical class. A class may have several
learned source images; a source with a weaker coarse match can become the best
match after refinement, but cannot do so if its pose was already discarded.
V11 retains the strongest pose **per source image**. Each source receives the
same rotation, length and aspect search budget. Final scores still take the
maximum per canonical class, so two references of the same item cannot become
the winner and runner-up. Colored inputs cannot use learned grayscale references.

`IPuzzlePoseRefiner` is the constructor-injected search extension point.
`PuzzlePoseRefiner` snapshots immutable poses before creating disposable transformed
variants. `PuzzleTemplateSearch` maintains separate source scores and class scores.
Previous pipelines retain one seed per class and their existing ranking behavior.
The v11 identity prevents reuse of a v10 or shipped validation fingerprint.

This is a general reference-search change. There are no image-ID exceptions,
class-specific acceptance thresholds, label lookups, or reduced score/margin limits.

## Development diagnosis

Development record 12294 is a small grayscale normie fish in a tall card.
With its original out-of-fold training assets, v10 gives normie fish 0.891034
and golden fish 0.881203, below the required 0.01 margin. V11 gives 0.911863
and 0.886185, a 0.025678 margin. The target image is absent from those assets'
training partition. This inspected example is a regression diagnostic, not an
independent accuracy estimate.

The complete evaluation refits references within all five grouped folds and
keeps the reserved 200-image set excluded. See [campaign measurements](puzzle-campaign-results.md)
and [the frozen-validation procedure](puzzle-campaign-validation.md).

Tests cover retaining a nonwinning source for refinement, preserving one final
candidate per class, cancellation before transformation and pipeline fingerprinting.
Additional source searches increase processing work for grayscale inputs; timings
from concurrent CV processes are not a latency benchmark.
