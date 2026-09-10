"""Development-only probe: bounded card crop and thin colored-line removal."""
import argparse
import concurrent.futures
import json
import pathlib
import time

import cv2
import numpy as np
from prototype import variants
from finalize_dataset import write_manifest

cv2.setNumThreads(1)


def remove_lines(colors):
    quantized = colors.astype(np.int32) // 32
    codes = quantized[:, :, 0] + 8 * quantized[:, :, 1] + 64 * quantized[:, :, 2]
    chroma = colors.max(2).astype(int) - colors.min(2)
    removal = np.zeros(colors.shape[:2], np.uint8)
    for code in np.unique(codes[chroma > 60]):
        mask = ((codes == code) & (chroma > 60)).astype(np.uint8)
        count, components, stats, _ = cv2.connectedComponentsWithStats(mask, 8)
        for component in range(1, count):
            if stats[component, cv2.CC_STAT_AREA] < 12:
                continue
            points = cv2.findNonZero((components == component).astype(np.uint8))
            width, height = cv2.minAreaRect(points)[1]
            if max(width, height) >= 18 and max(width, height) / (min(width, height) + 1) >= 8:
                removal[components == component] = 255
    if removal.any():
        removal = cv2.dilate(removal, np.ones((3, 3), np.uint8))
        return cv2.inpaint(colors, removal, 3, cv2.INPAINT_TELEA)
    return colors


def scene(path, clean):
    source = cv2.imread(str(path))
    height = 200 if source.shape[0] >= 150 else 85
    source = cv2.resize(source, (round(source.shape[1] * height / source.shape[0]), height))
    crop = source[12:188, :120] if height == 200 else source[5:80, :65]
    if clean:
        crop = remove_lines(crop)
    crop = cv2.resize(crop, None, fx=.5, fy=.5, interpolation=cv2.INTER_AREA)
    return cv2.copyMakeBorder(crop, 5, 5, 5, 5, cv2.BORDER_CONSTANT, value=(34, 34, 34))


def rank(colors, choices):
    gray = cv2.cvtColor(colors, cv2.COLOR_BGR2GRAY)
    binary = (np.max(abs(colors.astype(float) - 34), axis=2) > 10).astype(np.float32)
    best = {}
    for label, template, reference, mask in choices:
        if template.shape[0] > gray.shape[0] or template.shape[1] > gray.shape[1]:
            continue
        correlation = cv2.matchTemplate(gray, template, cv2.TM_CCOEFF_NORMED)
        intersection = cv2.matchTemplate(binary, mask, cv2.TM_CCORR)
        scores = .45 * correlation + 1.1 * intersection / (binary.sum() + mask.sum() + 1e-8)
        _, shape, _, (x, y) = cv2.minMaxLoc(scores)
        if shape <= best.get(label, -1):
            continue
        actual = colors[y:y + reference.shape[0], x:x + reference.shape[1]]
        selected = (reference.max(2) - reference.min(2) > 25) & (reference.max(2) > 65)
        hue = 1.
        if selected.sum() > 5:
            measured, expected = actual[selected].astype(float), reference[selected].astype(float)
            if np.mean(measured.max(1) - measured.min(1)) > 18:
                measured -= measured.mean(1, keepdims=True)
                expected -= expected.mean(1, keepdims=True)
                hue = max(0., np.sum(measured * expected) / (np.linalg.norm(measured) * np.linalg.norm(expected) + 1e-8))
        best[label] = max(best.get(label, -1), float(shape * (.75 + .25 * hue)))
    return sorted(best.items(), key=lambda row: -row[1])[:3]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('manifest', type=pathlib.Path)
    parser.add_argument('output', type=pathlib.Path)
    parser.add_argument('--clean', action='store_true')
    arguments = parser.parse_args()
    choices = variants(include_key=True)
    records = json.loads(arguments.manifest.read_text())
    started = time.time()
    def evaluate(record):
        ranking = rank(scene(arguments.manifest.parent / record['image'], arguments.clean), choices)
        accepted = ranking[0][1] >= .65 and ranking[0][1] - ranking[1][1] >= .025
        return dict(index=record['index'], expected=record['expected'], ranking=ranking,
                    accepted=accepted, correct=accepted and ranking[0][0] == record['expected'])
    with concurrent.futures.ThreadPoolExecutor(max_workers=4) as workers:
        predictions = list(workers.map(evaluate, records))
    write_manifest(arguments.output, predictions)
    print(json.dumps(dict(total=len(predictions), correct=sum(p['correct'] for p in predictions),
                          wrong=sum(p['accepted'] and not p['correct'] for p in predictions),
                          seconds=time.time() - started)), flush=True)


if __name__ == '__main__':
    main()
