"""Promote a frozen pipeline only after complete, correct independent validation."""
import collections
import hashlib
import json
import pathlib
from datetime import datetime, timezone

ROOT = pathlib.Path(__file__).resolve().parents[2]
DIRECTORY = ROOT / 'artifacts/puzzle-validation-20260909'


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def verify_frozen_inputs(freeze):
    for name, expected in freeze['frozenFiles'].items():
        if digest(ROOT / name) != expected:
            raise ValueError('Frozen implementation or template changed: ' + name)
    if digest(ROOT / freeze['holdoutSource']) != freeze['holdoutSourceSha256']:
        raise ValueError('Reserved holdout changed')
    development = ROOT / 'artifacts/puzzle-additions-20260909/v9-development-results.json'
    if digest(development) != freeze['developmentReportSha256']:
        raise ValueError('Development evidence changed')


def verify_predictions(freeze, policy, summary):
    records = read(DIRECTORY / 'holdout.json')
    predictions = read(DIRECTORY / 'holdout-results.json')
    by_index = {record['index']: record for record in records}
    if len(predictions) != 100 or len(by_index) != 100:
        raise ValueError('Validation requires the complete 100-example holdout')
    if {prediction['Index'] for prediction in predictions} != set(by_index):
        raise ValueError('Validation coverage mismatch')
    for prediction in predictions:
        expected = by_index[prediction['Index']]['expected']
        candidates = prediction['Candidates']
        if not (prediction['Accepted'] and prediction['Correct'] and
                prediction['Expected'] == prediction['Predicted'] == expected and
                len(candidates) >= 2 and candidates[0]['Label'] == expected and
                candidates[0]['Score'] >= policy['MinimumScore'] and
                candidates[0]['Score'] - candidates[1]['Score'] >= policy['MinimumMargin']):
            raise ValueError('Holdout is not 100% correctly accepted')
    if (summary['Total'], summary['Correct'], summary['Wrong'], summary['Rejected']) != (100, 100, 0, 0):
        raise ValueError('Validation summary mismatch')
    if summary['Fingerprint'] != freeze['fingerprint']:
        raise ValueError('Validation did not use the frozen fingerprint')
    return predictions, collections.Counter(record['expected'] for record in records)


def verify_policy(policy, freeze, counts):
    if (policy['Pipeline'], policy['MinimumScore'], policy['MinimumMargin']) != ('template-fine-v9', 0.65, 0.025):
        raise ValueError('Unexpected pipeline or acceptance thresholds')
    if policy['ValidatedFingerprint'] != freeze['fingerprint']:
        raise ValueError('Missing validation seal')
    if (policy['TestTotal'], policy['TestCorrect'], policy['TestWrong']) != (100, 100, 0):
        raise ValueError('Policy evidence mismatch')
    if policy['TestClassCounts'] != counts or len(counts) != 16 or min(counts.values()) < 5:
        raise ValueError('Incomplete class coverage')


def main():
    freeze = read(DIRECTORY / 'freeze.json')
    verify_frozen_inputs(freeze)
    policy = read(DIRECTORY / 'policy.json')
    summary = read(DIRECTORY / 'holdout-results.summary.json')
    predictions, counts = verify_predictions(freeze, policy, summary)
    verify_policy(policy, freeze, counts)
    (ROOT / 'puzzle-local.json').write_bytes((DIRECTORY / 'policy.json').read_bytes())
    source_summary_path = ROOT / 'artifacts/puzzle-training-20260908/summary.json'
    source_summary = read(source_summary_path)
    source_summary.update(holdoutEvaluated=True,
                          holdoutEvaluation='template-fine-v9; 100/100; artifacts/puzzle-validation-20260909')
    source_summary_path.write_text(json.dumps(source_summary, indent=2) + '\n')
    result = dict(completedUtc=datetime.now(timezone.utc).isoformat(), fingerprint=freeze['fingerprint'],
                  developmentCorrect=1601, developmentTotal=1601, holdoutCorrect=100, holdoutTotal=100,
                  minimumAcceptedScore=min(prediction['Candidates'][0]['Score'] for prediction in predictions),
                  minimumAcceptedMargin=min(prediction['Candidates'][0]['Score'] - prediction['Candidates'][1]['Score']
                                            for prediction in predictions), productionPolicy='puzzle-local.json',
                  policySha256=digest(ROOT / 'puzzle-local.json'))
    (DIRECTORY / 'completion.json').write_text(json.dumps(result, indent=2) + '\n')
    print(json.dumps(result))


if __name__ == '__main__':
    main()
