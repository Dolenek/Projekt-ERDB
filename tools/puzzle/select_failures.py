"""Select development failures without changing their labels or exposing a holdout."""
import argparse
import json
import os
import pathlib
from finalize_dataset import write_manifest


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('manifest', type=pathlib.Path)
    parser.add_argument('predictions', type=pathlib.Path)
    parser.add_argument('output', type=pathlib.Path)
    arguments = parser.parse_args()
    if arguments.manifest.name == 'holdout.json':
        raise ValueError('Development-only selection')
    records = json.loads(arguments.manifest.read_text())
    failures = {prediction['Index'] for prediction in json.loads(arguments.predictions.read_text()) if not prediction['Correct']}
    selected = [dict(record, image=os.path.relpath((arguments.manifest.parent / record['image']).resolve(),
                                                  arguments.output.parent).replace('\\', '/'))
                for record in records if record['index'] in failures]
    write_manifest(arguments.output, selected)


if __name__ == '__main__':
    main()
