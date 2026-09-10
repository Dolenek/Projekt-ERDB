"""Fit local grayscale references using only an explicitly supplied training partition."""
import hashlib
import json
import pathlib
import shutil
from dataclasses import dataclass
from typing import Protocol

import cv2
import numpy as np

from finalize_dataset import write_manifest
from learned_probe import locate


@dataclass
class GrayReferenceCandidate:
    record: dict
    icon: np.ndarray
    feature: np.ndarray


class GrayReferenceSelector(Protocol):
    def select(self, candidates: list[GrayReferenceCandidate]) -> list[GrayReferenceCandidate]: ...


class MedoidGrayReferenceSelector:
    """Select up to two deterministic representatives of a supplied appearance group."""
    def select(self, candidates):
        if not candidates:
            raise ValueError('At least one clean grayscale epic-coin training example is required')
        if len(candidates) <= 2:
            return list(candidates)
        features = np.array([candidate.feature for candidate in candidates])
        distances = np.sum((features[:, None] - features[None, :]) ** 2, axis=2)
        center = int(np.argmin(distances.sum(axis=1)))
        selected = [center, int(np.argmax(distances[center]))]
        if selected[0] == selected[1]:
            selected[1] = (selected[0] + 1) % len(candidates)
        for _ in range(10):
            assignments = np.argmin(distances[:, selected], axis=1)
            updated = list(selected)
            for cluster in range(2):
                members = np.flatnonzero(assignments == cluster)
                if len(members):
                    updated[cluster] = int(members[np.argmin(distances[np.ix_(members, members)].sum(axis=1))])
            if updated == selected:
                break
            selected = updated
        return [candidates[index] for index in selected]


class BrightnessStratifiedGrayReferenceSelector:
    """Retain rare bright captures before choosing representative shapes within each group."""
    def __init__(self, shape_selector: GrayReferenceSelector | None = None):
        self.shape_selector = shape_selector or MedoidGrayReferenceSelector()

    def select(self, candidates):
        if len(candidates) < 2:
            raise ValueError('At least two clean grayscale epic-coin training examples are required')
        ordered = sorted(candidates, key=lambda candidate: (candidate.feature[-1], candidate.record['sha256']))
        brightness = np.array([candidate.feature[-1] for candidate in ordered])
        split = int(np.argmax(np.diff(brightness))) + 1
        return self.shape_selector.select(ordered[:split]) + self.shape_selector.select(ordered[split:])


def extract_icon(path):
    colors = locate(cv2.imread(str(path)), glyph_aware=True)
    if colors is None:
        raise ValueError('Training card has no supported question layout: ' + str(path))
    foreground = (np.max(abs(colors.astype(float) - 34), axis=2) > 8).astype(np.uint8)
    count, _, stats, _ = cv2.connectedComponentsWithStats(foreground)
    if count < 2:
        raise ValueError('Training card has no foreground')
    left, top, width, height = stats[1 + int(np.argmax(stats[1:, 4])), :4]
    return colors[top:top + height, left:left + width]


def features(icon):
    gray = cv2.cvtColor(cv2.resize(icon, (32, 32)), cv2.COLOR_BGR2GRAY).astype(float) - 34
    appearance = gray.ravel() / (np.linalg.norm(gray) + 1e-8)
    foreground = gray[abs(gray) > 8]
    brightness = np.median(foreground) / 255 if len(foreground) else 0
    return np.concatenate([appearance, [brightness]])


def training_candidates(manifest, forbidden, label='epic coin'):
    records = json.loads(manifest.read_text(encoding='utf-8'))
    if forbidden.intersection(record['sha256'] for record in records):
        raise ValueError('Frozen holdout or test-fold image leaked into template training')
    candidates = []
    for record in sorted(records, key=lambda record: record['sha256']):
        if record['expected'] != label or record['lines'] or not record['grayscale']:
            continue
        path = (manifest.parent / record['image']).resolve()
        if hashlib.sha256(path.read_bytes()).hexdigest() != record['sha256']:
            raise ValueError('Training image checksum mismatch')
        icon = extract_icon(path)
        candidates.append(GrayReferenceCandidate(record, icon, features(icon)))
    return candidates


def fit_templates(manifest, destination, originals, forbidden, selectors=None):
    if destination.exists():
        raise ValueError('Refusing to overwrite a template partition')
    selectors = selectors or {'epic coin': BrightnessStratifiedGrayReferenceSelector(),
                              'unicorn horn': MedoidGrayReferenceSelector(),
                              'life potion': BrightnessStratifiedGrayReferenceSelector(),
                              'golden fish': BrightnessStratifiedGrayReferenceSelector(),
                              'normie fish': BrightnessStratifiedGrayReferenceSelector()}
    selected = {label: strategy.select(training_candidates(manifest, forbidden, label))
                for label, strategy in selectors.items()}
    destination.mkdir(parents=True)
    for original in sorted(originals.glob('*.webp')):
        shutil.copy2(original, destination / original.name)
    provenance = [reference for label, candidates in selected.items()
                  for reference in write_references(candidates, label, selectors[label], manifest, destination)]
    write_manifest(destination / 'Trained/provenance.json', provenance)
    return provenance


def write_references(candidates, label, selector, manifest, destination):
    output = destination / 'Trained' / label
    output.mkdir(parents=True)
    provenance = []
    for position, candidate in enumerate(candidates):
        path = output / ('medoid-' + str(position) + '.webp')
        if not cv2.imwrite(str(path), candidate.icon, [cv2.IMWRITE_WEBP_QUALITY, 101]):
            raise OSError('Failed to write trained template')
        provenance.append(dict(sourceIndex=candidate.record['index'], sourceSha256=candidate.record['sha256'],
                               sourceManifest=str(manifest.resolve()), template=str(path.relative_to(destination)),
                               templateSha256=hashlib.sha256(path.read_bytes()).hexdigest(),
                               method=type(selector).__name__, label=label, split='training-partition'))
    return provenance
