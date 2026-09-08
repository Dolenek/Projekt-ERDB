# Captcha dataset and validation

## Dataset

`artifacts/captcha-dataset` contains original historical attachments obtained
through EpicRPGBot.Mcp and manually verified labels. Only attachment images and
message identifiers are retained, not account credentials or full chat histories.

- `calibration.json`: 257 examples used to develop and calibrate the matcher.
- `holdout.json`: 100 independent examples, at least five for each of 15 targets.
- `excluded.json`: duplicate images, matching icon crops and standalone icons.
- Each record contains the image path, SHA-256, message ID, expected answer, index,
  and line/grayscale annotations.
- Labels are based on visual comparison to the original item templates.
  Text answers followed by a guard-clear message are not authoritative labels:
  a user may also have clicked a button.
- Exact attachment duplicates and cross-split identical icon crops are excluded.
  The held-out set is not used for changing recognition rules or thresholds.

## Windows replay tool

Build from the repository root:

```powershell
dotnet build tools/CaptchaReplay/CaptchaReplay.csproj -c Release
```

Run the generated `tools/CaptchaReplay/bin/Release/net48/CaptchaReplay.exe` with:

```text
CaptchaReplay.exe <repository-root> <manifest-json> <output-json> [--validate] [--policy <path>]
```

All paths may be absolute. Calibration uses `calibration.json`; final validation
uses `holdout.json`. The executable invokes the production provider in the UI
assembly without starting the UI or sending Discord messages.

The output is a JSON array with one prediction per line. The companion
`.summary.json` reports correct, wrong and rejected counts, top-candidate accuracy
before rejection, item/condition breakdowns, mean time and p95. Image checksums,
labels and duplicate hashes are checked. Validation additionally rejects hash
overlap with the calibration manifest. Visual/crop deduplication remains a dataset
curation responsibility; different bytes do not prove independence.

`--policy` selects an alternate policy without changing the repository default.
For v2 evaluation, pass `--policy tools/captcha/refined-policy.json` and omit
`--validate`; the existing holdout is regression evidence, not a fresh validation set.

`--validate` writes measured evidence to the selected policy (default `captcha-local.json`).
It enables eligibility only with at least 100 examples, all 15 classes represented
at least five times, zero wrong accepted answers and at least 90% correct answers.
It clears the old seal before reading the dataset, so a hash error or interrupted
run cannot leave stale eligibility. An insufficient measured result returns exit
code 3; input/runtime errors return 1. Run validation with the bot stopped: an
already-created provider holds a snapshot and does not reload the policy.
Rebuild the UI afterward to copy that policy into the distribution.

Policy fingerprinting binds the pipeline version, thresholds and template bytes.
When modifying the matcher, increment its pipeline version and obtain an
independent holdout before enabling the new version. Do not tune against a failed
holdout and call the same set independent again.

For dataset expansion, collect original attachments with verified labels and
condition annotations. Keep development examples separate from a fresh validation
set satisfying the gate above. Targeted development samples should include
low-contrast `wolf skin`, grayscale icons crossed by colored lines, small compact
icons and visually similar coins. The fresh set's neighboring `calibration.json`
must include all previously inspected development/regression examples so the hash
overlap check covers them; exclude duplicate icon crops during manual curation.

## Integration checks

Run `dotnet test EpicRPGBot.Tests/EpicRPGBot.Tests.csproj --filter FullyQualifiedName~Captcha`.
The test project covers unavailable images, corrupt and icon-only inputs,
unvalidated results, observation mode, recognition failures, duplicate concurrent
attempts and cancellation after guard clear, policy mutation and pipeline identity,
dataset corruption/overlap, synthetic translation and display scaling, and
color/grayscale handling. Synthetic cases do not count toward holdout validation.
Windows-only image tests report a
skip when run without the Windows native runtime.

Read [the runtime contract](captcha-solver.md) and
[MCP setup](testing-automation.md) before live inspection. Regular and MCP app
instances share their WebView2 profile; close the regular instance before launching
the MCP-managed instance. The bot need not be started to inspect Discord history.
