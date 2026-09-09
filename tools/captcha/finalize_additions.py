"""Materialize visually reviewed additions as development-only examples."""
import collections
import json
import pathlib
from finalize_dataset import annotate, verify_attachments, write_manifest
from prepare_training_batch import LABELS


def main():
    directory = pathlib.Path('artifacts/captcha-additions-20260909')
    pending = json.loads((directory / 'pending.json').read_text())
    tokens = ' '.join(json.loads((directory / 'review-labels.json').read_text())).split()
    if len(tokens) != len(pending):
        raise ValueError('Review count mismatch')
    labels = []
    for record, token in zip(pending, tokens):
        abbreviation, _, flags = token.partition(':')
        labels.append(dict(index=record['index'], expected=LABELS[abbreviation], lines='l' in flags,
                           grayscale='g' in flags or abbreviation == 'ws'))
    reviewed = annotate(pending, labels)
    reviewed += json.loads((directory / 'root-examples.json').read_text())
    verify_attachments(directory, reviewed)
    write_manifest(directory / 'visual-labels.json', labels)
    write_manifest(directory / 'development.json', reviewed)
    prior = pathlib.Path('artifacts/captcha-training-20260908')
    exposed = [dict(record, image='../' + prior.name + '/' + record['image'])
               for record in json.loads((prior / 'calibration.json').read_text())]
    write_manifest(directory / 'development-combined.json', exposed + reviewed)
    summary = dict(downloaded=100, duplicates=100 - len(pending), newAttachments=len(pending), rootExamples=2,
                   totalDevelopment=len(exposed) + len(reviewed), byItem=dict(collections.Counter(r['expected'] for r in reviewed)),
                   sourceSearch='in: bot-commands-1 has:image stop there, before:2023-12-26',
                   sourceChannel='557606805425881088', holdoutUsed=False)
    (directory / 'summary.json').write_text(json.dumps(summary, indent=2) + '\n')
    print(json.dumps(summary))


if __name__ == '__main__':
    main()
