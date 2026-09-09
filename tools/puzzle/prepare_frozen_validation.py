"""Audit v9 development coverage and reserve the existing untouched holdout for validation."""
import collections
import hashlib
import json
import os
import pathlib
from datetime import datetime, timezone

from PIL import Image
from collect_dataset import icon_digest, pixel_digest
from finalize_dataset import write_manifest

ROOT = pathlib.Path(__file__).resolve().parents[2]
DEVELOPMENT = ROOT / 'artifacts/puzzle-additions-20260909'
RESERVED = ROOT / 'artifacts/puzzle-training-20260908'
OUTPUT = ROOT / 'artifacts/puzzle-validation-20260909'


def read(path):
    return json.loads(path.read_text(encoding='utf-8'))


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def image_signatures(directory, records):
    signatures = []
    for record in records:
        path = directory / record['image']
        actual = digest(path)
        if actual != record['sha256']:
            raise ValueError('Attachment checksum mismatch: ' + str(path))
        with Image.open(path) as image:
            signatures.append((actual, pixel_digest(image), icon_digest(image)))
    return signatures


def audit_development(records):
    predictions = read(DEVELOPMENT / 'v9-development-results.json')
    expected = {record['index']: record['expected'] for record in records}
    if len(expected) != len(records) or len(predictions) != len(records):
        raise ValueError('Development coverage count mismatch')
    if {prediction['Index'] for prediction in predictions} != set(expected):
        raise ValueError('Development index coverage mismatch')
    for prediction in predictions:
        if not (prediction['Accepted'] and prediction['Correct'] and
                prediction['Expected'] == prediction['Predicted'] == expected[prediction['Index']]):
            raise ValueError('Development is not completely correct')
    parts = DEVELOPMENT / 'v9-development-results-parts'
    fingerprints = {read(path)['Fingerprint'] for path in parts.glob('result-*.summary.json')}
    current = read(DEVELOPMENT / 'v9-resume-check.summary.json')['Fingerprint']
    if fingerprints != {current}:
        raise ValueError('Development and current recognizer fingerprints differ')
    return current


def audit_separation(development, holdout):
    exposed = image_signatures(DEVELOPMENT, development)
    reserved = image_signatures(RESERVED, holdout)
    for position, name in enumerate(('bytes', 'decoded pixels', 'icon crop')):
        known = {signature[position] for signature in exposed}
        fresh = [signature[position] for signature in reserved]
        if len(set(fresh)) != len(fresh) or known.intersection(fresh):
            raise ValueError('Holdout duplicate or development overlap: ' + name)
    provenance = read(ROOT / 'Items/Trained/provenance.json')
    development_hashes = {record['sha256'] for record in development}
    for reference in provenance:
        if reference['sourceSha256'] not in development_hashes:
            raise ValueError('Learned reference has no development provenance')
        if digest(ROOT / reference['template']) != reference['templateSha256']:
            raise ValueError('Learned template checksum mismatch')


def relocate(records, source):
    return [dict(record, image=os.path.relpath((source / record['image']).resolve(), OUTPUT).replace('\\', '/'))
            for record in records]


def frozen_files():
    files = list((ROOT / 'EpicRPGBot.UI/Puzzle').rglob('*.cs'))
    files += list((ROOT / 'Items').rglob('*.webp'))
    files += [ROOT / 'items.json', ROOT / 'tools/puzzle/fine-policy.json']
    return {str(path.relative_to(ROOT)).replace('\\', '/'): digest(path) for path in sorted(files)}


def main():
    if OUTPUT.exists():
        raise ValueError('Refusing to overwrite a frozen validation directory')
    if read(RESERVED / 'summary.json')['holdoutEvaluated']:
        raise ValueError('The reserved holdout has already been evaluated')
    development = read(DEVELOPMENT / 'development-combined.json')
    holdout = read(RESERVED / 'holdout.json')
    fingerprint = audit_development(development)
    audit_separation(development, holdout)
    counts = collections.Counter(record['expected'] for record in holdout)
    if len(holdout) != 100 or len(counts) != 16 or min(counts.values()) < 5:
        raise ValueError('Insufficient holdout class coverage')
    OUTPUT.mkdir()
    write_manifest(OUTPUT / 'calibration.json', relocate(development, DEVELOPMENT))
    write_manifest(OUTPUT / 'holdout.json', relocate(holdout, RESERVED))
    audit = dict(frozenUtc=datetime.now(timezone.utc).isoformat(), pipeline='template-fine-v9',
                 fingerprint=fingerprint, developmentTotal=len(development), holdoutTotal=len(holdout),
                 holdoutClassCounts=dict(counts), overlapChecks=['SHA-256', 'decoded RGB', 'fixed icon crop'],
                 overlapCount=0, holdoutSource=str((RESERVED / 'holdout.json').relative_to(ROOT)),
                 holdoutSourceSha256=digest(RESERVED / 'holdout.json'),
                 developmentReportSha256=digest(DEVELOPMENT / 'v9-development-results.json'),
                 frozenFiles=frozen_files())
    (OUTPUT / 'freeze.json').write_text(json.dumps(audit, indent=2) + '\n')
    print(json.dumps({key: value for key, value in audit.items() if key != 'frozenFiles'}))


if __name__ == '__main__':
    main()
