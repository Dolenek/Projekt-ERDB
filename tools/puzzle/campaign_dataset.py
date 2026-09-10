"""Finalize reviewed campaign batches; reserve 200 cases once and keep them out of development."""
import argparse
import collections
import hashlib
import json
import os
import pathlib
import random
from datetime import datetime, timezone

from finalize_dataset import write_manifest
from prepare_training_batch import reviewed_records

ROOT = pathlib.Path(__file__).resolve().parents[2]
SEED = 20260909


def read(path):
    return json.loads(path.read_text(encoding='utf-8'))


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def relocate(records, source, destination):
    return [dict(record, image=os.path.relpath((source / record['image']).resolve(), destination).replace('\\', '/'))
            for record in records]


def prior_development(campaign):
    sources = [ROOT / 'artifacts/puzzle-additions-20260909/development-combined.json',
               ROOT / 'artifacts/puzzle-validation-20260909/holdout.json']
    records = [record for source in sources for record in relocate(read(source), source.parent, campaign)]
    if len({record['sha256'] for record in records}) != len(records):
        raise ValueError('Prior exposure union has duplicate hashes')
    return records


def split_initial(records):
    if len(records) != 500:
        raise ValueError('The initial split requires exactly 500 unique reviewed records')
    groups = collections.defaultdict(list)
    for record in records:
        groups[record['expected']].append(record)
    if len(groups) != 16 or min(map(len, groups.values())) < 6:
        raise ValueError('Need all 16 classes and at least six examples of each')
    generator = random.Random(SEED)
    holdout = []
    for label in sorted(groups):
        candidates = sorted(groups[label], key=lambda record: record['sha256'])
        generator.shuffle(candidates)
        holdout.extend(candidates[:5])
    selected = {record['sha256'] for record in holdout}
    remaining = [record for record in records if record['sha256'] not in selected]
    generator.shuffle(remaining)
    holdout.extend(remaining[:120])
    return remaining[120:], sorted(holdout, key=lambda record: record['index'])


def verify_freeze(campaign):
    frozen = read(campaign / 'holdout-freeze.json')
    if digest(campaign / 'holdout.json') != frozen['manifestSha256']:
        raise ValueError('The frozen holdout manifest changed')
    return set(frozen['attachmentHashes'])


def finalize(campaign, batch, initial):
    reviewed = reviewed_records(batch)
    if (batch / 'development.json').exists():
        raise ValueError('Refusing to overwrite a finalized batch')
    if initial:
        if (campaign / 'holdout-freeze.json').exists():
            raise ValueError('A holdout is already frozen')
        development, holdout = split_initial(reviewed)
        write_manifest(campaign / 'holdout.json', relocate(holdout, batch, campaign))
        freeze = dict(createdUtc=datetime.now(timezone.utc).isoformat(), seed=SEED, total=200,
                      manifestSha256=digest(campaign / 'holdout.json'),
                      attachmentHashes=[record['sha256'] for record in holdout], evaluated=False,
                      classCounts=dict(collections.Counter(record['expected'] for record in holdout)))
        (campaign / 'holdout-freeze.json').write_text(json.dumps(freeze, indent=2) + '\n')
        write_manifest(campaign / 'prior-development.json', prior_development(campaign))
    else:
        verify_freeze(campaign)
        development = reviewed
    write_manifest(batch / 'development.json', development)
    return development


def rebuild_development(campaign):
    forbidden = verify_freeze(campaign)
    records = read(campaign / 'prior-development.json')
    for manifest in sorted(campaign.glob('round-*/development.json')):
        records.extend(relocate(read(manifest), manifest.parent, campaign))
    hashes = [record['sha256'] for record in records]
    if len(set(hashes)) != len(hashes) or forbidden.intersection(hashes):
        raise ValueError('Duplicate development record or frozen holdout leakage')
    if len({record['index'] for record in records}) != len(records):
        raise ValueError('Development indices collide')
    write_manifest(campaign / 'development.json', records)
    write_manifest(campaign / 'calibration.json', records)
    return records


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('batch', type=pathlib.Path)
    parser.add_argument('--initial', action='store_true')
    arguments = parser.parse_args()
    campaign, batch = arguments.batch.parent, arguments.batch
    development = finalize(campaign, batch, arguments.initial)
    combined = rebuild_development(campaign)
    summary = dict(uniqueReviewed=len(read(batch / 'pending.json')), newDevelopment=len(development),
                   totalDevelopment=len(combined), frozenHoldout=200, seed=SEED,
                   sourceChannel=read(batch / 'acquisition.json')['channelId'], sourceSearch=read(batch / 'acquisition.json')['query'],
                   byItem=dict(collections.Counter(record['expected'] for record in read(batch / 'visual-labels.json'))))
    (batch / 'summary.json').write_text(json.dumps(summary, indent=2) + '\n')
    print(json.dumps(summary))


if __name__ == '__main__':
    main()
