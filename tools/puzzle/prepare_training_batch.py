"""Materialize reviewed 16-class labels and reserve an unevaluated stratified holdout."""
import collections
import json
import pathlib
import random
import sys

from finalize_dataset import annotate, prior_exposure, verify_attachments, write_manifest

LABELS = dict(a='apple', b='banana', ch='chip', co='coin', dc='dragon scale', ec='epic coin',
              ef='epic fish', gf='golden fish', k='key', lp='life potion', mh='mermaid hair',
              nf='normie fish', r='ruby', uh='unicorn horn', ws='wolf skin', ze='zombie eye')
LAYOUT_EXPOSURE = {3710, 3712, 3713, 3749}


def reviewed_records(directory):
    pending = json.loads((directory / 'pending.json').read_text())
    tokens = ' '.join(json.loads((directory / 'review-labels.json').read_text())).split()
    if len(tokens) != len(pending):
        raise ValueError('Review count must match all retained images')
    labels = []
    for record, token in zip(pending, tokens):
        abbreviation, _, flags = token.partition(':')
        labels.append(dict(index=record['index'], expected=LABELS[abbreviation],
                           grayscale='g' in flags or abbreviation == 'ws', lines='l' in flags))
    reviewed = annotate(pending, labels)
    verify_attachments(directory, reviewed)
    write_manifest(directory / 'visual-labels.json', labels)
    return reviewed


def exposure_records():
    prior = prior_exposure(pathlib.Path('artifacts/puzzle-dataset'))
    directory = pathlib.Path('artifacts/puzzle-dataset-expansion-20260908')
    for name in ('development.json', 'holdout.json', 'unsupported.json'):
        for original in json.loads((directory / name).read_text()):
            prior.append(dict(original, image='../' + directory.name + '/' + original['image']))
    return prior


def split(reviewed):
    groups = collections.defaultdict(list)
    for record in reviewed:
        if record['index'] not in LAYOUT_EXPOSURE:
            groups[record['expected']].append(record)
    generator = random.Random(20260909)
    holdout = []
    for position, label in enumerate(sorted(LABELS.values())):
        candidates = sorted(groups[label], key=lambda record: record['messageId'])
        count = 7 if position < 4 else 6
        if len(candidates) < count:
            raise ValueError('Insufficient fresh examples: ' + label)
        generator.shuffle(candidates)
        holdout.extend(candidates[:count])
    held_ids = {record['messageId'] for record in holdout}
    return holdout, [record for record in reviewed if record['messageId'] not in held_ids]


def main():
    directory = pathlib.Path(sys.argv[1])
    if (directory / 'holdout.json').exists():
        raise ValueError('Refusing to overwrite a reserved holdout')
    reviewed = reviewed_records(directory)
    holdout, development = split(reviewed)
    prior = exposure_records()
    for name, records in (('holdout', holdout), ('development', development),
                          ('calibration', prior + development), ('prior-exposure', prior)):
        write_manifest(directory / (name + '.json'), records)
    summary = dict(total=len(reviewed), development=len(development), holdout=len(holdout),
                   priorExposure=len(prior), byItem=dict(collections.Counter(r['expected'] for r in reviewed)),
                   holdoutClassCounts=dict(collections.Counter(r['expected'] for r in holdout)),
                   layoutExposure=sorted(LAYOUT_EXPOSURE), splitSeed=20260909, holdoutEvaluated=False,
                   sourceChannel='557606805425881088', sourceSearch='in: bot-commands-1 has:image stop there, before:2025-04-28')
    (directory / 'summary.json').write_text(json.dumps(summary, indent=2) + '\n')
    print(json.dumps(summary))


if __name__ == '__main__':
    main()
