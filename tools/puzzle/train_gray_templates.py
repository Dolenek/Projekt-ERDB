"""Build traceable grayscale epic-coin templates from visually confirmed development cards."""
import hashlib
import argparse
import json
import pathlib
import cv2
import numpy as np
from learned_probe import locate
from finalize_dataset import write_manifest


def reproduce_original_templates():
    root = pathlib.Path(__file__).resolve().parents[2]
    directory = root / 'artifacts/puzzle-training-20260908'
    records = json.loads((directory / 'development.json').read_text())
    output = root / 'Items/Trained/epic coin'
    output.mkdir(parents=True, exist_ok=True)
    provenance = []
    for index, name in ((3076, 'gray'), (3130, 'bright-gray')):
        record = next(record for record in records if record['index'] == index)
        if record['expected'] != 'epic coin' or record['lines'] or not record['grayscale']:
            raise ValueError('Expected a verified grayscale development epic coin without lines')
        path = directory / record['image']
        if hashlib.sha256(path.read_bytes()).hexdigest() != record['sha256']:
            raise ValueError('Training attachment hash mismatch')
        colors = locate(cv2.imread(str(path)))
        foreground = (np.max(abs(colors.astype(float) - 34), axis=2) > 8).astype(np.uint8)
        _, components, stats, _ = cv2.connectedComponentsWithStats(foreground)
        component = 1 + int(np.argmax(stats[1:, 4]))
        left, top, width, height = stats[component, :4]
        icon = colors[top:top + height, left:left + width]
        destination = output / (name + '.webp')
        cv2.imwrite(str(destination), icon, [cv2.IMWRITE_WEBP_QUALITY, 101])
        provenance.append(dict(source=record['image'], sourceBatch=directory.name, sourceIndex=index,
                               sourceSha256=record['sha256'], label='epic coin',
                               template=str(destination.relative_to(root)).replace('\\', '/'),
                               templateSha256=hashlib.sha256(destination.read_bytes()).hexdigest(),
                               method='adaptive-question-crop-then-largest-foreground-component', split='development'))
    write_manifest(output.parent / 'provenance.json', provenance)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--manifest', type=pathlib.Path)
    parser.add_argument('--output', type=pathlib.Path)
    parser.add_argument('--forbidden', type=pathlib.Path)
    arguments = parser.parse_args()
    if arguments.manifest is None:
        reproduce_original_templates()
        return
    if arguments.output is None or arguments.forbidden is None:
        parser.error('Partition training requires --output and --forbidden hash registry')
    from gray_template_training import fit_templates
    forbidden = set(json.loads(arguments.forbidden.read_text()))
    provenance = fit_templates(arguments.manifest, arguments.output,
                               pathlib.Path(__file__).resolve().parents[2] / 'Items', forbidden)
    print(json.dumps(provenance))


if __name__ == '__main__':
    main()
