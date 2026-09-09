"""Audit v1/v2 batch replay outputs against manifests and summarize observed accuracy."""
import argparse
import collections
import json
import pathlib


def read_json(path):
    return json.loads(path.read_text(encoding='utf-8'))


def summarize(predictions):
    accepted = sum(record['Accepted'] for record in predictions)
    correct = sum(record['Correct'] for record in predictions)
    return dict(total=len(predictions), correct=correct, wrong=accepted - correct,
                rejected=len(predictions) - accepted,
                topCandidateCorrect=sum(bool(record['Candidates']) and
                                        record['Candidates'][0]['Label'] == record['Expected']
                                        for record in predictions))


def audit_report(directory, pipeline, split):
    manifest = read_json(directory / (split + '.json'))
    predictions = read_json(directory / (pipeline + '-' + split + '-results.json'))
    expected = {record['index']: record for record in manifest}
    if len(predictions) != len(expected) or {r['Index'] for r in predictions} != set(expected):
        raise ValueError('Replay records do not cover the manifest exactly once')
    review = []
    for prediction in predictions:
        source = expected[prediction['Index']]
        if prediction['Expected'] != source['expected']:
            raise ValueError('Replay label differs from the frozen visual annotation')
        correct = prediction['Accepted'] and prediction['Predicted'] == source['expected']
        if prediction['Correct'] != correct:
            raise ValueError('Inconsistent correctness flag')
        if not correct:
            review.append(dict(pipeline=pipeline, split=split, image=source['image'],
                               messageId=source['messageId'], **prediction))
    report = summarize(predictions)
    report['byItem'] = {label: summarize([r for r in predictions if r['Expected'] == label])
                        for label in sorted({r['Expected'] for r in predictions})}
    report['byCondition'] = {name: summarize([r for r in predictions if r['Grayscale'] == gray and r['Lines'] == lines])
                             for name, gray, lines in [('color/plain', False, False), ('color/lines', False, True),
                                                       ('grayscale/plain', True, False), ('grayscale/lines', True, True)]}
    counts = collections.Counter(record['expected'] for record in manifest)
    report['meetsHoldoutGate'] = (split == 'holdout' and len(manifest) >= 100 and len(counts) == 15
                                  and min(counts.values()) >= 5 and report['wrong'] == 0
                                  and report['correct'] >= len(manifest) * 0.9)
    return report, review


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('directory', type=pathlib.Path)
    directory = parser.parse_args().directory
    reports, review = {}, []
    for pipeline in ('v1', 'v2'):
        for split in ('development', 'holdout', 'unsupported'):
            report, cases = audit_report(directory, pipeline, split)
            reports[pipeline + '/' + split] = report
            review.extend(cases)
            print(pipeline + '/' + split, json.dumps({k: v for k, v in report.items() if not isinstance(v, dict)}))
    (directory / 'evaluation-summary.json').write_text(json.dumps(reports) + '\n', encoding='utf-8')
    per_line = len(review) // 250 + 1
    rows = [','.join(json.dumps(r) for r in review[start:start + per_line])
            for start in range(0, len(review), per_line)]
    (directory / 'review-required.json').write_text('[\n' + ',\n'.join(rows) + '\n]\n', encoding='utf-8')


if __name__ == '__main__':
    main()
