import hashlib
import json
import pathlib
import sys
import tempfile
import unittest

import numpy as np
from PIL import Image, PngImagePlugin

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parents[1]))
from collection_scope import allowed_timestamp
from cross_validation import assign_folds, image_groups, verify_predictions
from evaluation_statistics import totals, wilson
from gray_template_training import BrightnessStratifiedGrayReferenceSelector, GrayReferenceCandidate, training_candidates
from partition_integrity import verify_snapshot


class EvaluationIntegrityTests(unittest.TestCase):
    def test_target_requires_at_least_3838_perfect_trials(self):
        self.assertLess(wilson(3837, 3837)['Lower'], .999)
        self.assertGreaterEqual(wilson(3838, 3838)['Lower'], .999)
        self.assertAlmostEqual(wilson(200, 200)['Lower'], .9811546736)

    def test_rejections_remain_in_the_accuracy_denominator(self):
        predictions = [dict(Correct=True, Accepted=True), dict(Correct=False, Accepted=False)]
        measured = totals(predictions)
        self.assertEqual(measured['Rejected'], 1)
        self.assertEqual(measured['Accuracy95']['Estimate'], .5)

    def test_date_boundary_is_checked_before_acquisition(self):
        self.assertFalse(allowed_timestamp('2019-12-31T23:59:59Z', '2020-01-01'))
        self.assertTrue(allowed_timestamp('2020-01-01T00:00:00Z', '2020-01-01'))
        self.assertTrue(allowed_timestamp('2019-01-01T00:00:00Z', None))
        with self.assertRaises(ValueError):
            allowed_timestamp(None, '2020-01-01')

    def test_template_training_rejects_test_hashes_before_image_io(self):
        with tempfile.TemporaryDirectory() as directory:
            manifest = pathlib.Path(directory) / 'training.json'
            manifest.write_text(json.dumps([dict(sha256='reserved-image')]))
            with self.assertRaisesRegex(ValueError, 'leaked'):
                training_candidates(manifest, {'reserved-image'})

    def test_brightness_selection_preserves_rare_bright_sources(self):
        candidates = [GrayReferenceCandidate(dict(sha256=str(index)), np.zeros((2, 2, 3)),
                      np.array([index / 100, brightness]))
                      for index, brightness in enumerate([.2] * 20 + [.65, .7])]
        selected = BrightnessStratifiedGrayReferenceSelector().select(candidates)
        self.assertTrue(any(candidate.feature[-1] < .3 for candidate in selected))
        self.assertTrue(any(candidate.feature[-1] > .6 for candidate in selected))
        self.assertLessEqual(len(selected), 4)

    def test_grouped_folds_never_split_related_images(self):
        groups = [[dict(sha256=f'{label}-{group}-{member}', expected=label)
                   for member in range(2)] for label in ('apple', 'coin') for group in range(8)]
        folds = assign_folds(groups, 5, 123)
        self.assertEqual(folds, assign_folds(groups, 5, 123))
        for group in groups:
            owners = {number for number, fold in enumerate(folds) for member in group if member in fold}
            self.assertEqual(len(owners), 1)

    def test_reencoded_pixels_are_grouped_and_conflicting_labels_fail(self):
        with tempfile.TemporaryDirectory() as directory:
            manifest = pathlib.Path(directory) / 'manifest.json'
            records = self.write_equivalent_images(manifest.parent)
            self.assertNotEqual(records[0]['sha256'], records[1]['sha256'])
            self.assertEqual(len(image_groups(records, manifest)), 1)
            records[1]['expected'] = 'coin'
            with self.assertRaisesRegex(ValueError, 'Conflicting'):
                image_groups(records, manifest)

    @staticmethod
    def write_equivalent_images(directory):
        records = []
        for index in range(2):
            path = directory / f'{index}.png'
            metadata = PngImagePlugin.PngInfo()
            metadata.add_text('encoding', str(index))
            Image.new('RGB', (150, 200), (180, 30, 30)).save(path, pnginfo=metadata)
            records.append(dict(index=index, image=path.name, sha256=hashlib.sha256(path.read_bytes()).hexdigest(), expected='apple'))
        return records

    def test_snapshot_detects_post_freeze_changes(self):
        with tempfile.TemporaryDirectory() as directory:
            path = pathlib.Path(directory) / 'policy.json'
            path.write_text('first')
            snapshot = {str(path): hashlib.sha256(path.read_bytes()).hexdigest()}
            verify_snapshot(snapshot)
            path.write_text('changed')
            with self.assertRaisesRegex(ValueError, 'changed after freeze'):
                verify_snapshot(snapshot)

    def test_each_record_requires_exactly_one_out_of_fold_prediction(self):
        records = [dict(index=1, expected='apple'), dict(index=2, expected='coin')]
        repeated = [dict(Index=1, Expected='apple')] * 2
        with self.assertRaises(ValueError):
            verify_predictions(records, repeated)


if __name__ == '__main__':
    unittest.main()
