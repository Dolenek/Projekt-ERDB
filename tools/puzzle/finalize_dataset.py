"""Combine visually assigned labels with attachment provenance and reserve a holdout."""
import argparse
import collections
import hashlib
import json
import pathlib
import random

from PIL import Image

from collect_dataset import icon_digest, pixel_digest, review_sheets


OUTLINES = {
    'apple': 'Rounded fruit with a short stem and leaf',
    'banana': 'Thick curved crescent with tapered tips',
    'chip': 'Angular circuit token with internal traces and bright lower contacts',
    'coin': 'Round or stretched plain coin with a central inset',
    'dragon scale': 'Irregular pointed scale with segmented internal ridges',
    'epic coin': 'Ornate medallion with layered rim and star-like highlights',
    'epic fish': 'Slender fish with pointed head and elongated fins',
    'golden fish': 'Rounded fish with a broad tail and facial marks',
    'life potion': 'Bottle with a narrow neck and broad liquid-filled base',
    'mermaid hair': 'Thin flowing S-shaped strand',
    'normie fish': 'Horizontal fish with a broad tail and facial marks',
    'ruby': 'Faceted gemstone with a broad crown and pointed lower tip',
    'unicorn horn': 'Rigid tapered horn with diagonal bands',
    'wolf skin': 'Dark ragged hide with spread limb-like projections',
    'zombie eye': 'Square eye with a pale center and dark pupil',
    'key': 'Small key with a red-orange head and yellow toothed shaft',
}
COLORS = dict(apple='red', banana='yellow', chip='green', coin='yellow',
              **{'dragon scale': 'red', 'epic coin': 'purple and gold', 'epic fish': 'green',
                 'golden fish': 'yellow', 'life potion': 'red and pale blue',
                 'mermaid hair': 'cyan', 'normie fish': 'cyan', 'ruby': 'red',
                 'unicorn horn': 'pink', 'wolf skin': 'dark gray', 'zombie eye': 'green and white',
                 'key': 'red-orange and yellow'})


def write_manifest(path, records):
    serialized = [json.dumps(record, ensure_ascii=False) for record in records]
    # Keep even the aggregate exclusion manifest below the repository file-length limit.
    per_line = max(4, (len(records) + 199) // 200)
    rows = [','.join(serialized[start:start + per_line]) for start in range(0, len(records), per_line)]
    path.write_text('[\n' + ',\n'.join(rows) + '\n]\n', encoding='utf-8')


def annotate(records, labels):
    annotations = {record['index']: record for record in labels}
    if len(annotations) != len(records) or set(annotations) != {record['index'] for record in records}:
        raise ValueError('Visual labels must cover every attachment exactly once')
    reviewed = []
    for record in records:
        label = annotations[record['index']]
        color = 'grayscale or nearly grayscale' if label['grayscale'] else COLORS[label['expected']]
        interference = 'Colored interference lines cross or surround the icon.' if label['lines'] else 'No interference lines.'
        reviewed.append(dict(record, **{field: label[field] for field in ('expected', 'lines', 'grayscale')},
                             description=OUTLINES[label['expected']] + '; ' + color + '. ' + interference,
                             annotationMethod='visual-template-comparison'))
    return reviewed


def reserve_holdout(records):
    groups = collections.defaultdict(list)
    for record in records:
        groups[record['expected']].append(record)
    if len(groups) != 15 or min(map(len, groups.values())) < 7:
        raise ValueError('Need at least seven examples per supported class for this split')
    generator = random.Random(20260908)
    holdout = []
    for position, label in enumerate(sorted(groups)):
        candidates = sorted(groups[label], key=lambda record: record['messageId'])
        generator.shuffle(candidates)
        holdout.extend(candidates[:7 if position < 10 else 6])
    held_ids = {record['messageId'] for record in holdout}
    return sorted(holdout, key=lambda record: record['index']), [r for r in records if r['messageId'] not in held_ids]


def prior_exposure(existing):
    records = []
    for name in ('calibration.json', 'holdout.json', 'user-examples.json'):
        for original in json.loads((existing / name).read_text(encoding='utf-8')):
            record = dict(original)
            record['image'] = '../' + existing.name + '/' + record['image']
            records.append(record)
    return list({record['sha256']: record for record in records}.values())


def verify_attachments(directory, reviewed):
    for record in reviewed:
        path = directory / record['image']
        if hashlib.sha256(path.read_bytes()).hexdigest() != record['sha256']:
            raise ValueError('Checksum mismatch: ' + record['image'])
        with Image.open(path) as image:
            if pixel_digest(image) != record['pixelSha256'] or icon_digest(image) != record['iconCropSha256']:
                raise ValueError('Pixel mismatch: ' + record['image'])


def write_outputs(directory, reviewed, prior):
    unsupported = [dict(record, reason='unsupported-target') for record in reviewed if record['expected'] == 'key']
    supported = [record for record in reviewed if record['expected'] != 'key']
    holdout, development = reserve_holdout(supported)
    write_manifest(directory / 'holdout.json', holdout)
    write_manifest(directory / 'calibration.json', prior + development)
    write_manifest(directory / 'development.json', development)
    write_manifest(directory / 'unsupported.json', unsupported)
    summary = dict(total=len(reviewed), supported=len(supported), unsupported=len(unsupported),
                   development=len(development), holdout=len(holdout), priorExposure=len(prior),
                   byItem=dict(sorted(collections.Counter(r['expected'] for r in reviewed).items())),
                   grayscale=sum(r['grayscale'] for r in reviewed), lines=sum(r['lines'] for r in reviewed),
                   earliest=min(r['timestamp'] for r in reviewed), latest=max(r['timestamp'] for r in reviewed),
                   holdoutClassCounts=dict(sorted(collections.Counter(r['expected'] for r in holdout).items())),
                   holdoutEvaluated=False, splitSeed=20260908)
    (directory / 'summary.json').write_text(json.dumps(summary, indent=2) + '\n', encoding='utf-8')
    print(json.dumps(summary))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('directory', type=pathlib.Path)
    parser.add_argument('--existing', type=pathlib.Path, default=pathlib.Path('artifacts/puzzle-dataset'))
    arguments = parser.parse_args()
    summary_path = arguments.directory / 'summary.json'
    if summary_path.exists() and json.loads(summary_path.read_text(encoding='utf-8')).get('holdoutEvaluated'):
        raise ValueError('This holdout has been evaluated; do not overwrite its split or exposure metadata')
    records = json.loads((arguments.directory / 'pending.json').read_text(encoding='utf-8'))
    labels = json.loads((arguments.directory / 'visual-labels.json').read_text(encoding='utf-8'))
    reviewed = annotate(records, labels)
    verify_attachments(arguments.directory, reviewed)
    write_outputs(arguments.directory, reviewed, prior_exposure(arguments.existing))
    review_sheets(reviewed, arguments.directory)


if __name__ == '__main__':
    main()
