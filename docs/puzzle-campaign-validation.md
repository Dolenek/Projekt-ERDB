# Frozen campaign evaluation

The completed v11 run is `artifacts/puzzle-validation-20260910/`, evaluated with
`--policy tools/puzzle/multisource-policy.json` after
`campaign/models/multisource-full-cv-01/`. It passes both numerical criteria;
see [results and runtime selection](puzzle-campaign-results.md). Its reserved
registry is marked exposed, so preparation refuses a second fresh evaluation.

The [campaign](puzzle-campaign.md) separates development CV from a reserved
200-image validation set. The strict CV criterion is a two-sided 95% Wilson
lower bound of at least 0.999 on all out-of-fold recognition outcomes.
Abstentions remain in the denominator. The CV interval is nominal, with the
sampling and shared-training limitations stated in the campaign contract.

## Entry checks

`tools/puzzle/campaign_validation.py` accepts a campaign directory, completed
full-CV output and a new validation output directory. An optional `--policy`
selects the evaluated policy. Preparation requires:

- Exactly the same reviewed development identities and labels as the CV input.
- Complete, once-per-record CV predictions with consistent canonical answers.
- The strict Wilson criterion, rather than the point estimate alone.
- The same lower-bound criterion after counting each exact-image group once;
  a group succeeds only when all its members are correct.
- The same policy bytes as CV and unchanged frozen CV input files.
- An unexposed reserved set and no development overlap by bytes, pixels or crops.

It refits the final learned references on development only. Its validation
directory contains isolated `Items/`, relocated `calibration.json` and
`holdout.json`, an immutable `policy-input.json`, and `freeze.json`.
The freeze binds code, binaries, templates, provenance, manifests, source images
and the completed CV evidence. It does not change the shipped assets or policy.

## One-time evaluation

Before invoking PuzzleReplay, the campaign registry records that exposure has
started. An interrupted or failed attempt therefore cannot be described as a fresh
holdout on a subsequent run. The output directory cannot be reused.

The replay produces `predictions.json` and its summary. Afterward the script
verifies frozen inputs again and checks every prediction against its label and
acceptance thresholds. `completion.json` contains the CV and independent results,
their Wilson intervals, and explicit target-status flags.

The frozen criterion requires at least 199 correct accepted answers out of 200.
Wrong accepted answers and rejections are reported separately. Even 200/200 has
a 98.115% Wilson lower bound; this set cannot establish a 99.9% population floor.

`policy.json` receives validation eligibility only if there are zero wrong accepted
answers and all 16 classes have the required coverage. A run with one wrong answer
may meet the requested frozen numerical criterion but cannot pass the runtime's
zero-wrong gate. Promotion to the shipped assets is separate from measurement.
