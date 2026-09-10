"""Freeze and evaluate a campaign holdout once, after the strict full-CV criterion passes."""
import argparse
import collections
import json
import pathlib
import shutil
import subprocess
from datetime import datetime, timezone

from campaign_dataset import digest, read, relocate, verify_freeze
from cross_validation import image_groups, verify_predictions
from evaluation_statistics import summary, wilson
from finalize_dataset import write_manifest
from gray_template_training import fit_templates
from partition_integrity import audit_frozen_separation, input_snapshot, verify_snapshot

ROOT = pathlib.Path(__file__).resolve().parents[2]


def reviewed_identity(records):
    return sorted((record['index'], record['sha256'], record['expected'],
                   record['grayscale'], record['lines']) for record in records)


def grouped_interval(groups, predictions):
    correct = {prediction['Index']: prediction['Correct'] for prediction in predictions}
    successes = sum(all(correct[member['index']] for member in group) for group in groups)
    return wilson(successes, len(groups))


def audit_cv(campaign, cross_validation, policy):
    report = read(cross_validation / 'summary.json')
    records = read(cross_validation / 'development-input.json')
    predictions = read(cross_validation / 'predictions.json')
    verify_snapshot(read(cross_validation / 'input-hashes.json'))
    verify_predictions(records, predictions)
    verify_outcomes(records, predictions, read(policy))
    measured = summary(predictions)
    if reviewed_identity(records) != reviewed_identity(read(campaign / 'development.json')):
        raise ValueError('CV does not cover the current reviewed development dataset')
    if report['evaluationSubset'] != 'all development' or not report['templatesRefittedPerFold']:
        raise ValueError('Final validation requires complete partition-specific cross-validation')
    if report['Accuracy95'] != measured['Accuracy95'] or measured['Accuracy95']['Lower'] < .999:
        raise ValueError('The full-CV 95% Wilson lower bound has not reached 99.9%')
    groups = image_groups(records, cross_validation / 'development-input.json')
    measured['ExactGroupAccuracy95'] = grouped_interval(groups, predictions)
    if measured['ExactGroupAccuracy95']['Lower'] < .999:
        raise ValueError('The CV lower bound fails after counting exact-image groups once')
    return measured


def prepare(campaign, cross_validation, destination, policy):
    if destination.exists():
        raise ValueError('Use a new final-validation output directory')
    frozen = read(campaign / 'holdout-freeze.json')
    if frozen.get('evaluated') or frozen.get('evaluationStartedUtc'):
        raise ValueError('The reserved holdout has already been exposed')
    forbidden = verify_freeze(campaign)
    measured_cv = audit_cv(campaign, cross_validation, policy)
    if digest(policy) != read(cross_validation / 'design.json')['policySha256']:
        raise ValueError('Final policy differs from the CV policy')
    audit_frozen_separation(campaign, campaign / 'development.json')
    destination.mkdir(parents=True)
    write_manifest(destination / 'calibration.json', relocate(read(campaign / 'development.json'), campaign, destination))
    write_manifest(destination / 'holdout.json', relocate(read(campaign / 'holdout.json'), campaign, destination))
    shutil.copy2(policy, destination / 'policy-input.json')
    fit_templates(destination / 'calibration.json', destination / 'Items', ROOT / 'Items', forbidden)
    snapshot = input_snapshot(destination / 'calibration.json', destination / 'policy-input.json', destination / 'Items')
    snapshot.update({str((destination / 'holdout.json').resolve()): digest(destination / 'holdout.json')})
    for name in ('calibration.json', 'holdout.json'):
        for record in read(destination / name):
            snapshot[str((destination / record['image']).resolve())] = record['sha256']
    snapshot.update({str((cross_validation / name).resolve()): digest(cross_validation / name)
                     for name in ('summary.json', 'predictions.json', 'input-hashes.json')})
    freeze = dict(createdUtc=datetime.now(timezone.utc).isoformat(), frozenInputs=snapshot,
                  campaign=str(campaign), crossValidation=measured_cv, holdoutTotal=200)
    (destination / 'freeze.json').write_text(json.dumps(freeze) + '\n')
    return freeze


def verify_outcomes(records, predictions, policy):
    verify_predictions(records, predictions)
    expected = {record['index']: record['expected'] for record in records}
    for prediction in predictions:
        correct = bool(prediction['Accepted'] and prediction['Predicted'] == expected[prediction['Index']])
        if prediction['Correct'] != correct:
            raise ValueError('Prediction correctness flag does not match its canonical answer')
        if prediction['Accepted']:
            candidates = prediction['Candidates']
            if len(candidates) < 2 or candidates[0]['Label'] != prediction['Predicted']:
                raise ValueError('Accepted answer does not match its ranking')
            if (candidates[0]['Score'] < policy['MinimumScore'] or
                    candidates[0]['Score'] - candidates[1]['Score'] < policy['MinimumMargin']):
                raise ValueError('Accepted answer does not meet frozen thresholds')


def evaluate(destination, freeze):
    verify_snapshot(freeze['frozenInputs'])
    campaign = pathlib.Path(freeze['campaign'])
    registry_path = campaign / 'holdout-freeze.json'
    registry = read(registry_path)
    if registry.get('evaluated') or registry.get('evaluationStartedUtc'):
        raise ValueError('Refusing a second evaluation of the reserved set')
    registry.update(evaluated=True, evaluationStartedUtc=datetime.now(timezone.utc).isoformat(),
                    evaluationDirectory=str(destination))
    registry_path.write_text(json.dumps(registry, indent=2) + '\n')
    executable = ROOT / 'tools/PuzzleReplay/bin/Release/net48/PuzzleReplay.exe'
    command = [str(executable), str(ROOT), str(destination / 'holdout.json'),
               str(destination / 'predictions.json'), '--policy', str(destination / 'policy-input.json'),
               '--templates', str(destination / 'Items')]
    with (destination / 'replay.log').open('w', encoding='utf-8') as log:
        subprocess.run(command, stdout=log, stderr=subprocess.STDOUT, check=True,
                       timeout=1800, creationflags=subprocess.CREATE_NO_WINDOW)
    verify_snapshot(freeze['frozenInputs'])
    return complete(destination, freeze)


def complete(destination, freeze):
    records = read(destination / 'holdout.json')
    predictions = read(destination / 'predictions.json')
    policy = read(destination / 'policy-input.json')
    verify_outcomes(records, predictions, policy)
    measured = summary(predictions)
    counts = dict(collections.Counter(record['expected'] for record in records))
    frozen_passed = len(records) == 200 and measured['Correct'] >= 199
    policy.update(ValidatedFingerprint='', TestTotal=len(records), TestCorrect=measured['Correct'],
                  TestWrong=measured['Wrong'], TestClassCounts=counts)
    if frozen_passed and measured['Wrong'] == 0 and len(counts) == 16 and min(counts.values()) >= 5:
        policy['ValidatedFingerprint'] = read(destination / 'predictions.summary.json')['Fingerprint']
    (destination / 'policy.json').write_text(json.dumps(policy, indent=2) + '\n')
    result = dict(crossValidation=freeze['crossValidation'], holdout=measured,
                  FrozenTargetMet=frozen_passed, CombinedTargetMet=frozen_passed,
                  AutomaticSubmissionEligible=bool(policy['ValidatedFingerprint']))
    (destination / 'completion.json').write_text(json.dumps(result) + '\n')
    print(json.dumps(result), flush=True)
    return result


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('campaign', type=pathlib.Path)
    parser.add_argument('cross_validation', type=pathlib.Path)
    parser.add_argument('destination', type=pathlib.Path)
    parser.add_argument('--policy', type=pathlib.Path, default=ROOT / 'tools/puzzle/glyph-policy.json')
    arguments = parser.parse_args()
    destination = arguments.destination.resolve()
    freeze = prepare(arguments.campaign.resolve(), arguments.cross_validation.resolve(), destination,
                     arguments.policy.resolve())
    result = evaluate(destination, freeze)
    raise SystemExit(0 if result['CombinedTargetMet'] else 3)


if __name__ == '__main__':
    main()
