"""Compare grayscale/silhouette weights using development attachments only."""
import argparse
import concurrent.futures
import json
import pathlib
import cv2
import numpy as np
from learned_probe import locate
from prototype import variants
from finalize_dataset import write_manifest

WEIGHTS = (.45, .65, .85)


def rank(colors, choices):
    if colors is None:
        return {str((weight, maximum)): [] for weight in WEIGHTS for maximum in (False, True)}
    colors = cv2.resize(colors, None, fx=.5, fy=.5, interpolation=cv2.INTER_AREA)
    colors = cv2.copyMakeBorder(colors, 5, 5, 5, 5, cv2.BORDER_CONSTANT, value=(34, 34, 34))
    gray = cv2.cvtColor(colors, cv2.COLOR_BGR2GRAY)
    binary = (np.max(abs(colors.astype(float) - 34), axis=2) > 10).astype(np.float32)
    rankings = {(weight, maximum): {} for weight in WEIGHTS for maximum in (False, True)}
    for label, normal, reference, mask in choices:
        if normal.shape[0] > gray.shape[0] or normal.shape[1] > gray.shape[1]:
            continue
        overlap = 2 * cv2.matchTemplate(binary, mask, cv2.TM_CCORR) / (binary.sum() + mask.sum() + 1e-8)
        for maximum in (False, True):
            template = reference.max(2) if maximum else normal
            correlation = cv2.matchTemplate(gray, template, cv2.TM_CCOEFF_NORMED)
            for weight in WEIGHTS:
                best = rankings[weight, maximum]
                _, shape, _, (x, y) = cv2.minMaxLoc(weight * correlation + (1 - weight) * overlap)
                if shape <= best.get(label, -1):
                    continue
                hue = color_similarity(colors[y:y+reference.shape[0], x:x+reference.shape[1]], reference)
                best[label] = max(best.get(label, -1), float(shape * (.75 + .25 * hue)))
    return {str(key): sorted(value.items(), key=lambda item: -item[1])[:3] for key, value in rankings.items()}


def color_similarity(actual, reference):
    selected = (reference.max(2) - reference.min(2) > 25) & (reference.max(2) > 65)
    if selected.sum() <= 5:
        return 1.
    measured, expected = actual[selected].astype(float), reference[selected].astype(float)
    if np.mean(measured.max(1) - measured.min(1)) <= 18:
        return 1.
    measured -= measured.mean(1, keepdims=True)
    expected -= expected.mean(1, keepdims=True)
    return max(0., np.sum(measured * expected) / (np.linalg.norm(measured) * np.linalg.norm(expected) + 1e-8))


def summarize(predictions):
    for key in predictions[0]['rankings']:
        correct = wrong = top = 0
        for prediction in predictions:
            ranking = prediction['rankings'][key]
            if not ranking:
                continue
            accepted = ranking[0][1] >= .65 and ranking[0][1] - ranking[1][1] >= .025
            matches = ranking[0][0] == prediction['expected']
            correct += accepted and matches
            wrong += accepted and not matches
            top += matches
        print(json.dumps(dict(mode=key, total=len(predictions), correct=correct, wrong=wrong, top=top)), flush=True)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('manifest', type=pathlib.Path)
    parser.add_argument('output', type=pathlib.Path)
    arguments = parser.parse_args()
    if arguments.manifest.name == 'holdout.json':
        raise ValueError('Development-only probe')
    choices = variants(include_key=True)
    records = json.loads(arguments.manifest.read_text())
    def evaluate(record):
        source = cv2.imread(str(arguments.manifest.parent / record['image']))
        return dict(index=record['index'], expected=record['expected'], rankings=rank(locate(source), choices))
    with concurrent.futures.ThreadPoolExecutor(max_workers=4) as workers:
        predictions = list(workers.map(evaluate, records))
    write_manifest(arguments.output, predictions)
    summarize(predictions)


if __name__ == '__main__':
    main()
