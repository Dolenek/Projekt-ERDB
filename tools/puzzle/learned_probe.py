"""Development-only leave-one-out nearest-exemplar probe; never opens a holdout."""
import os
os.environ['OPENBLAS_NUM_THREADS'] = '1'
import argparse
import json
import pathlib
import cv2
import numpy as np
from clean_probe import remove_lines
from finalize_dataset import write_manifest


def locate(source):
    height = 200 if source.shape[0] >= 150 else 85
    source = cv2.resize(source, (round(source.shape[1] * height / source.shape[0]), height))
    minimum, maximum = source.min(2), source.max(2)
    letters = ((minimum > 180) & (maximum - minimum < 30)).astype(np.uint8)
    letters[:5] = 0
    letters[-5:] = 0
    letters = cv2.morphologyEx(letters, cv2.MORPH_CLOSE, np.ones((3, 21), np.uint8))
    _, _, stats, _ = cv2.connectedComponentsWithStats(letters)
    starts = [x for x, y, width, height, area in stats[1:] if x >= 50 and width >= 120 and 12 <= height <= 55]
    if not starts:
        return None
    return remove_lines(source[5:-5, :min(starts) - 8])


def feature(colors, angle=0):
    if colors is None:
        return None
    mask = (np.max(abs(colors.astype(float) - 34), axis=2) > 12).astype(np.uint8)
    count, components, stats, _ = cv2.connectedComponentsWithStats(mask)
    if count <= 1:
        return None
    retained = np.zeros_like(mask)
    largest = max(stats[1:, 4])
    for index in range(1, count):
        if stats[index, 4] >= max(4, largest * .02):
            retained[components == index] = 1
    y, x = np.where(retained)
    icon = colors[y.min():y.max() + 1, x.min():x.max() + 1]
    icon = cv2.resize(icon, (48, 48), interpolation=cv2.INTER_AREA)
    if angle:
        icon = cv2.warpAffine(icon, cv2.getRotationMatrix2D((24, 24), angle, 1), (48, 48), borderValue=(34, 34, 34))
    gray = cv2.cvtColor(icon, cv2.COLOR_BGR2GRAY).astype(np.float32) - 34
    gray /= np.linalg.norm(gray) + 1e-8
    binary = (np.max(abs(icon.astype(float) - 34), axis=2) > 12).astype(np.float32)
    chroma = icon.astype(np.float32) - icon.mean(2, keepdims=True)
    chroma /= np.linalg.norm(chroma) + 1e-8
    colored = np.mean(icon.max(2).astype(float) - icon.min(2)) > 8
    return gray.ravel(), binary.ravel(), chroma.ravel(), colored


def load_records(manifest):
    records = json.loads(manifest.read_text())
    return [dict(record, path=str((manifest.parent / record['image']).resolve())) for record in records]


def prepare_bank(records, scenes):
    bank, identities, labels = [], [], []
    for record, colors in zip(records, scenes):
        if record['lines']:
            continue
        for angle in (-10, 0, 10):
            features = feature(colors, angle)
            if features is not None:
                bank.append(features)
                identities.append(record['sha256'])
                labels.append(record['expected'])
    grayscale = np.array([entry[0] for entry in bank])
    masks = np.array([entry[1] for entry in bank])
    chroma = np.array([entry[2] for entry in bank])
    areas = masks.sum(1)
    identities = np.array(identities)
    labels = np.array(labels)
    return grayscale, masks, chroma, areas, identities, labels


def predict(records, scenes, bank):
    grayscale, masks, chroma, areas, identities, labels = bank
    predictions = []
    for record, colors in zip(records, scenes):
        query = feature(colors)
        if query is None:
            predictions.append(dict(index=record['index'], expected=record['expected'], accepted=False, correct=False, ranking=[]))
            continue
        gray, binary, hue, colored = query
        scores = .7 * (grayscale @ gray) + .3 * (2 * (masks @ binary) / (areas + binary.sum() + 1e-8))
        if colored:
            scores *= .75 + .25 * np.maximum(0, chroma @ hue)
        scores[identities == record['sha256']] = -1
        ranking = sorted(((label, float(scores[labels == label].max())) for label in set(labels)), key=lambda item: -item[1])[:3]
        accepted = ranking[0][1] >= .65 and ranking[0][1] - ranking[1][1] >= .025
        predictions.append(dict(index=record['index'], expected=record['expected'], ranking=ranking,
                                accepted=accepted, correct=accepted and ranking[0][0] == record['expected']))
    return predictions


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('manifest', type=pathlib.Path)
    parser.add_argument('output', type=pathlib.Path)
    arguments = parser.parse_args()
    if arguments.manifest.name == 'holdout.json':
        raise ValueError('This probe is restricted to development manifests')
    records = load_records(arguments.manifest)
    scenes = [locate(cv2.imread(record['path'])) for record in records]
    print('Prepared ' + str(len(records)) + ' scenes', flush=True)
    predictions = predict(records, scenes, prepare_bank(records, scenes))
    write_manifest(arguments.output, predictions)
    print(json.dumps(dict(total=len(predictions), correct=sum(p['correct'] for p in predictions),
                          wrong=sum(p['accepted'] and not p['correct'] for p in predictions),
                          topCorrect=sum(bool(p['ranking']) and p['ranking'][0][0] == p['expected'] for p in predictions))))


if __name__ == '__main__':
    main()
