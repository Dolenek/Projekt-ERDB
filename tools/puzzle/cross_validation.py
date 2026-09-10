"""Grouped stratified k-fold replay with partition-specific learned references and frozen-holdout exclusion."""
import argparse
import collections
import concurrent.futures
import hashlib
import json
import pathlib
import random
import subprocess

from PIL import Image
from campaign_dataset import digest, read, relocate, verify_freeze
from collect_dataset import icon_digest, pixel_digest
from evaluation_statistics import summary
from finalize_dataset import write_manifest
from gray_template_training import fit_templates
from partition_integrity import audit_frozen_separation, input_snapshot, partition_snapshot, verify_snapshot

ROOT = pathlib.Path(__file__).resolve().parents[2]


def image_groups(records, manifest):
    owners, groups = {}, {}
    for record in records:
        path = manifest.parent / record['image']
        if digest(path) != record['sha256']:
            raise ValueError('Cross-validation image checksum mismatch')
        with Image.open(path) as image:
            signatures = [record['sha256'], pixel_digest(image), icon_digest(image)]
        matched = {owners[signature] for signature in signatures if signature in owners}
        group_id = min(matched) if matched else record['sha256']
        combined = [record]
        for previous in matched:
            combined.extend(groups.pop(previous))
            for signature in owners:
                if owners[signature] == previous:
                    owners[signature] = group_id
        if len({member['expected'] for member in combined}) != 1:
            raise ValueError('Conflicting visual labels in an exact-image/icon group')
        groups[group_id] = combined
        for signature in signatures:
            owners[signature] = group_id
    return list(groups.values())


def assign_folds(groups, fold_count, seed):
    by_label = collections.defaultdict(list)
    for group in groups:
        by_label[group[0]['expected']].append(group)
    assignments = [[] for _ in range(fold_count)]
    generator = random.Random(seed)
    for label in sorted(by_label):
        candidates = sorted(by_label[label], key=lambda group: group[0]['sha256'])
        generator.shuffle(candidates)
        counts = [0] * fold_count
        for group in sorted(candidates, key=len, reverse=True):
            fold = min(range(fold_count), key=lambda position: (counts[position], len(assignments[position])))
            assignments[fold].extend(group)
            counts[fold] += len(group)
    return assignments


def prepare_fold(number, test, records, manifest, destination, forbidden, grayscale_only=False):
    directory = destination / ('fold-' + str(number))
    directory.mkdir()
    test_hashes = {record['sha256'] for record in test}
    train = [record for record in records if record['sha256'] not in test_hashes]
    evaluated = [record for record in test if record['grayscale']] if grayscale_only else test
    write_manifest(directory / 'training.json', relocate(train, manifest.parent, directory))
    write_manifest(directory / 'test.json', relocate(evaluated, manifest.parent, directory))
    provenance = fit_templates(directory / 'training.json', directory / 'Items', ROOT / 'Items', forbidden | test_hashes)
    if any(reference['sourceSha256'] in forbidden | test_hashes for reference in provenance):
        raise ValueError('Trained reference source overlaps a test partition')
    (directory / 'training-audit.json').write_text(json.dumps(dict(training=len(train), testing=len(evaluated),
        testHashes=sorted(test_hashes), trainingManifestSha256=digest(directory / 'training.json'),
        testManifestSha256=digest(directory / 'test.json'), templateProvenance=provenance)) + '\n')
    return directory


def replay_fold(directory, policy):
    executable = ROOT / 'tools/PuzzleReplay/bin/Release/net48/PuzzleReplay.exe'
    output = directory / 'predictions.json'
    command = [str(executable), str(ROOT), str(directory / 'test.json'), str(output),
               '--policy', str(policy), '--templates', str(directory / 'Items')]
    with (directory / 'replay.log').open('w', encoding='utf-8') as log:
        completed = subprocess.run(command, stdout=log, stderr=subprocess.STDOUT,
                                   timeout=7200, creationflags=subprocess.CREATE_NO_WINDOW)
    if completed.returncode:
        raise RuntimeError('Fold replay failed: ' + str(directory))
    predictions = read(output)
    measured = summary(predictions)
    print(json.dumps(dict(Fold=directory.name, Total=measured['Total'], Correct=measured['Correct'],
                          Accuracy95=measured['Accuracy95'])), flush=True)
    return predictions


def verify_predictions(records, predictions):
    expected = {record['index']: record['expected'] for record in records}
    if len(expected) != len(records) or len(predictions) != len(records):
        raise ValueError('Cross-validation count or index collision')
    if {prediction['Index'] for prediction in predictions} != set(expected):
        raise ValueError('Cross-validation did not predict each record exactly once')
    for prediction in predictions:
        if prediction['Expected'] != expected[prediction['Index']]:
            raise ValueError('Cross-validation label mismatch')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('campaign', type=pathlib.Path)
    parser.add_argument('destination', type=pathlib.Path)
    parser.add_argument('--folds', type=int, default=5, choices=range(2, 11))
    parser.add_argument('--workers', type=int, default=5, choices=range(1, 9))
    parser.add_argument('--seed', type=int, default=20260909)
    parser.add_argument('--policy', type=pathlib.Path, default=ROOT / 'tools/puzzle/glyph-policy.json')
    parser.add_argument('--grayscale-only', action='store_true', help='Development diagnostic; not a complete CV result')
    arguments = parser.parse_args()
    campaign, destination = arguments.campaign.resolve(), arguments.destination.resolve()
    if destination.exists():
        raise ValueError('Refusing to overwrite cross-validation evidence')
    forbidden = verify_freeze(campaign)
    manifest = campaign / 'development.json'
    records = read(manifest)
    if forbidden.intersection(record['sha256'] for record in records):
        raise ValueError('Frozen holdout leaked into the cross-validation manifest')
    destination.mkdir(parents=True)
    write_manifest(destination / 'development-input.json', relocate(records, manifest.parent, destination))
    manifest = destination / 'development-input.json'
    records = read(manifest)
    assignments = assign_folds(image_groups(records, manifest), arguments.folds, arguments.seed)
    policy = arguments.policy.resolve()
    audit_frozen_separation(campaign, manifest)
    preparation_inputs = input_snapshot(manifest, policy)
    partitions = [prepare_fold(number, test, records, manifest, destination, forbidden, arguments.grayscale_only)
                  for number, test in enumerate(assignments)]
    verify_snapshot(preparation_inputs)
    if arguments.grayscale_only:
        records = [record for record in records if record['grayscale']]
    run(partitions, records, policy, arguments, destination, manifest)


def run(partitions, records, policy, arguments, destination, manifest):
    frozen_inputs = input_snapshot(manifest, policy)
    frozen_inputs.update(partition_snapshot(partitions))
    verify_snapshot(frozen_inputs)
    (destination / 'input-hashes.json').write_text(json.dumps(frozen_inputs, indent=2) + '\n')
    separation = audit_frozen_separation(arguments.campaign.resolve(), manifest)
    snapshot = dict(seed=arguments.seed, folds=arguments.folds, manifestSha256=digest(manifest),
                    policySha256=digest(policy), grouping='bytes + decoded RGB + fixed icon crop',
                    frozenHoldoutExcluded=True, templatesRefittedPerFold=True,
                    evaluationSubset='grayscale diagnostic' if arguments.grayscale_only else 'all development',
                    partitionAudit=separation)
    (destination / 'design.json').write_text(json.dumps(snapshot, indent=2) + '\n')
    with concurrent.futures.ThreadPoolExecutor(max_workers=arguments.workers) as executor:
        predictions = [prediction for fold in executor.map(lambda path: replay_fold(path, policy), partitions)
                       for prediction in fold]
    verify_predictions(records, predictions)
    verify_snapshot(frozen_inputs)
    write_manifest(destination / 'predictions.json', sorted(predictions, key=lambda prediction: prediction['Index']))
    measured = dict(summary(predictions), **snapshot)
    measured['TargetLowerBoundMet'] = not arguments.grayscale_only and measured['Accuracy95']['Lower'] >= .999
    measured['IntervalInterpretation'] = 'Nominal binomial interval on pooled out-of-fold outcomes; folds share training data and pipeline design was developed on historical examples.'
    (destination / 'summary.json').write_text(json.dumps(measured) + '\n')
    print(json.dumps({key: value for key, value in measured.items() if not key.startswith('By')}), flush=True)


if __name__ == '__main__':
    main()
