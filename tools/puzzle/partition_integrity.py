"""Audit frozen image content and hash every input that defines an evaluation."""
from pathlib import Path

from PIL import Image
from campaign_dataset import digest, read, verify_freeze
from collect_dataset import icon_digest, pixel_digest

ROOT = Path(__file__).resolve().parents[2]


def signatures(manifest, records):
    result = []
    for record in records:
        path = manifest.parent / record['image']
        if digest(path) != record['sha256']:
            raise ValueError('Image checksum mismatch: ' + str(path))
        with Image.open(path) as image:
            result.append((record['sha256'], pixel_digest(image), icon_digest(image)))
    return result


def audit_frozen_separation(campaign, manifest):
    forbidden = verify_freeze(campaign)
    reserved = signatures(campaign / 'holdout.json', read(campaign / 'holdout.json'))
    exposed = signatures(manifest, read(manifest))
    if len(reserved) != 200 or {signature[0] for signature in reserved} != forbidden:
        raise ValueError('Frozen holdout count or registered hashes mismatch')
    for position in range(3):
        fresh = [signature[position] for signature in reserved]
        known = {signature[position] for signature in exposed}
        if len(set(fresh)) != len(fresh) or known.intersection(fresh):
            raise ValueError('Frozen holdout duplicate or development overlap')
    return dict(holdoutCount=200, developmentCount=len(exposed), overlapCount=0,
                checks=['SHA-256', 'decoded RGB', 'fixed icon crop'])


def input_snapshot(manifest, policy, templates=None):
    files = list((ROOT / 'EpicRPGBot.UI/Puzzle').rglob('*.cs'))
    files += list((ROOT / 'tools/PuzzleReplay').glob('*.cs'))
    files += list((ROOT / 'tools/puzzle').glob('*.py'))
    files += list((templates or ROOT / 'Items').glob('*.webp'))
    if templates:
        files += list((templates / 'Trained').rglob('*.webp'))
        files += list((templates / 'Trained').rglob('*.json'))
    files += [ROOT / 'items.json', manifest, policy]
    files += list((ROOT / 'tools/PuzzleReplay/bin/Release/net48').glob('*.exe'))
    files += list((ROOT / 'tools/PuzzleReplay/bin/Release/net48').glob('*.dll'))
    return {str(path.resolve()): digest(path) for path in sorted(set(files))}


def partition_snapshot(partitions):
    snapshot = {}
    for directory in partitions:
        files = [directory / name for name in ('training.json', 'test.json', 'training-audit.json')]
        files += list((directory / 'Items').rglob('*.webp'))
        files += list((directory / 'Items').rglob('*.json'))
        snapshot.update({str(path.resolve()): digest(path) for path in files})
        for name in ('training.json', 'test.json'):
            for record in read(directory / name):
                path = (directory / record['image']).resolve()
                snapshot[str(path)] = record['sha256']
    return snapshot


def verify_snapshot(snapshot):
    for path, expected in snapshot.items():
        if not Path(path).is_file() or digest(Path(path)) != expected:
            raise ValueError('Evaluation input changed after freeze: ' + path)
