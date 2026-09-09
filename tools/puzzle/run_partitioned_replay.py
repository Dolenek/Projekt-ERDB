"""Run independent production replays in bounded processes and merge audited predictions."""
import argparse
import concurrent.futures
import json
import os
import pathlib
import subprocess
from finalize_dataset import write_manifest


def evaluate_partition(executable, root, policy, partition, output):
    completed = subprocess.run([str(executable), str(root), str(partition), str(output), '--policy', str(policy)],
                               capture_output=True, text=True, timeout=3600, creationflags=subprocess.CREATE_NO_WINDOW)
    if completed.returncode:
        raise RuntimeError('Replay failed for ' + partition.name + ': ' + completed.stderr[-1000:])
    predictions = json.loads(output.read_text())
    print(json.dumps(dict(partition=partition.name, total=len(predictions),
                          correct=sum(p['Correct'] for p in predictions))), flush=True)
    return predictions


def summarize(predictions):
    return dict(Total=len(predictions), Correct=sum(p['Correct'] for p in predictions),
                Wrong=sum(p['Accepted'] and not p['Correct'] for p in predictions),
                Rejected=sum(not p['Accepted'] for p in predictions),
                TopCandidateCorrect=sum(bool(p['Candidates']) and p['Candidates'][0]['Label'] == p['Expected'] for p in predictions),
                Evaluation='development; concurrent replay processes; no policy seal')


def prepare(records, manifest, directory, workers):
    if len({record['index'] for record in records}) != len(records):
        raise ValueError('Partition merge requires unique indices')
    directory.mkdir(parents=True, exist_ok=True)
    partitions = []
    for offset in range(workers):
        part = [dict(record, image=os.path.relpath((manifest.parent / record['image']).resolve(), directory).replace('\\', '/'))
                for record in records[offset::workers]]
        path = directory / ('part-' + str(offset) + '.json')
        write_manifest(path, part)
        partitions.append((path, directory / ('result-' + str(offset) + '.json')))
    return partitions


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('manifest', type=pathlib.Path)
    parser.add_argument('policy', type=pathlib.Path)
    parser.add_argument('output', type=pathlib.Path)
    parser.add_argument('--workers', type=int, default=4, choices=range(1, 9))
    parser.add_argument('--configuration', choices=('Debug', 'Release'), default='Release')
    arguments = parser.parse_args()
    if arguments.manifest.name == 'holdout.json':
        raise ValueError('Use the single production CLI for final validation')
    root = pathlib.Path(__file__).resolve().parents[2]
    executable = root / 'tools/PuzzleReplay/bin' / arguments.configuration / 'net48/PuzzleReplay.exe'
    records = json.loads(arguments.manifest.read_text())
    partitions = prepare(records, arguments.manifest, arguments.output.parent / (arguments.output.stem + '-parts'), arguments.workers)
    with concurrent.futures.ThreadPoolExecutor(max_workers=arguments.workers) as workers:
        futures = [workers.submit(evaluate_partition, executable, root, arguments.policy.resolve(), part, result)
                   for part, result in partitions]
        predictions = [prediction for future in futures for prediction in future.result()]
    by_index = {prediction['Index']: prediction for prediction in predictions}
    if set(by_index) != {record['index'] for record in records} or len(predictions) != len(records):
        raise ValueError('Replay coverage mismatch')
    ordered = [by_index[record['index']] for record in records]
    if any(record['expected'] != prediction['Expected'] for record, prediction in zip(records, ordered)):
        raise ValueError('Replay label mismatch')
    write_manifest(arguments.output, ordered)
    summary = summarize(ordered)
    arguments.output.with_suffix('.summary.json').write_text(json.dumps(summary, indent=2) + '\n')
    print(json.dumps(summary), flush=True)


if __name__ == '__main__':
    main()
