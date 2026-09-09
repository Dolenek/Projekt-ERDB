"""Audit confirmed development labels and materialize root regression examples."""
import hashlib
import collections
import json
import pathlib
from PIL import Image
from collect_dataset import pixel_digest, icon_digest
from finalize_dataset import write_manifest, annotate

ROOT = pathlib.Path(__file__).resolve().parents[2]
BATCH = ROOT / 'artifacts/captcha-training-20260908'
ADDITIONS = ROOT / 'artifacts/captcha-additions-20260909'


def root_examples():
    examples, labels = [], []
    for index, filename, label in ((4100, 'epic_guard_dragon_scale.webp', 'dragon scale'),
                                   (4101, 'epic_guard_mermaid.webp', 'mermaid hair')):
        path = ROOT / filename
        with Image.open(path) as image:
            examples.append(dict(index=index, messageId='user-' + str(index), image='../../' + filename,
                                 sha256=hashlib.sha256(path.read_bytes()).hexdigest(), width=image.width,
                                 height=image.height, pixelSha256=pixel_digest(image), iconCropSha256=icon_digest(image)))
        labels.append(dict(index=index, expected=label, lines=True, grayscale=False))
    return annotate(examples, labels)


def correct_confirmed_label():
    audit = dict(index=3374, messageId='1268927145611104319', previous='wolf skin', expected='epic fish',
                 reason='Full original shows a slender fish with tail, pointed head and fins, not a ragged hide.',
                 method='visual-template-comparison', date='2026-09-09')
    for filename in ('development.json', 'calibration.json', 'visual-labels.json'):
        path = BATCH / filename
        records = json.loads(path.read_text())
        for record in records:
            if record['index'] == audit['index']:
                record['expected'] = audit['expected']
                if 'description' in record:
                    record['description'] = 'Slender fish with pointed head and elongated fins; grayscale. Colored interference lines cross the icon.'
        write_manifest(path, records)
    write_manifest(ADDITIONS / 'annotation-audit.json', [audit])
    summary_path = BATCH / 'summary.json'
    summary = json.loads(summary_path.read_text())
    reviewed = json.loads((BATCH / 'development.json').read_text()) + json.loads((BATCH / 'holdout.json').read_text())
    summary['byItem'] = dict(collections.Counter(record['expected'] for record in reviewed))
    summary['annotationAudit'] = '../captcha-additions-20260909/annotation-audit.json'
    summary_path.write_text(json.dumps(summary, indent=2) + '\n')


def main():
    ADDITIONS.mkdir(exist_ok=True, parents=True)
    correct_confirmed_label()
    roots = root_examples()
    write_manifest(ADDITIONS / 'root-examples.json', roots)
    focused = []
    for manifest_name, report_name in (('development.json', 'v4-development-results.json'),
                                        ('prior-exposure.json', 'v4-prior-results.json')):
        records = json.loads((BATCH / manifest_name).read_text())
        failures = {record['Index'] for record in json.loads((BATCH / report_name).read_text()) if not record['Correct']}
        for record in records:
            if record['index'] in failures:
                focused.append(dict(record, image='../' + BATCH.name + '/' + record['image']))
    write_manifest(ADDITIONS / 'focused-development.json', focused + roots)


if __name__ == '__main__':
    main()
