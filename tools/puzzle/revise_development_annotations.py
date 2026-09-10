"""Apply an explicit visual annotation revision, preserving previous development evidence."""
import argparse
import json
import shutil
from datetime import datetime, timezone
from pathlib import Path

from campaign_dataset import read, rebuild_development, verify_freeze
from finalize_dataset import write_manifest
from prepare_training_batch import reviewed_records


def revise(batch, reviewed_labels, reason):
    forbidden = verify_freeze(batch.parent)
    previous = read(batch / 'development.json')
    if forbidden.intersection(record['sha256'] for record in previous):
        raise ValueError('Annotation revisions cannot touch the frozen validation set')
    if {record['sha256'] for record in previous} != {record['sha256'] for record in read(batch / 'pending.json')}:
        raise ValueError('Only a fully development-only batch may be revised')
    archive = batch / ('annotation-revision-' + datetime.now(timezone.utc).strftime('%Y%m%dT%H%M%SZ'))
    archive.mkdir()
    for name in ('review-labels.json', 'visual-labels.json', 'development.json', 'summary.json'):
        shutil.copy2(batch / name, archive / name)
    shutil.copy2(reviewed_labels, batch / 'review-labels.json')
    updated = reviewed_records(batch)
    changes = [dict(index=old['index'], sha256=old['sha256'], before=old['expected'], after=new['expected'],
                    oldGrayscale=old['grayscale'], grayscale=new['grayscale'], oldLines=old['lines'], lines=new['lines'])
               for old, new in zip(previous, updated) if any(old[key] != new[key] for key in ('expected', 'grayscale', 'lines'))]
    write_manifest(archive / 'changes.json', changes)
    (archive / 'reason.json').write_text(json.dumps(dict(reason=reason, method='visual-template-comparison',
        previousReplayLabelsInvalid=True, frozenHoldoutUnchanged=True)) + '\n')
    write_manifest(batch / 'development.json', updated)
    rebuild_development(batch.parent)
    print(json.dumps(dict(revised=len(changes), audit=str(archive))))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('batch', type=Path)
    parser.add_argument('reviewed_labels', type=Path)
    parser.add_argument('--reason', required=True)
    arguments = parser.parse_args()
    revise(arguments.batch.resolve(), arguments.reviewed_labels.resolve(), arguments.reason)


if __name__ == '__main__':
    main()
