import hashlib
import json
import pathlib
import sys
import tempfile
import unittest

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parents[1]))
from campaign_validation import grouped_interval, prepare, verify_outcomes
from partition_integrity import partition_snapshot, verify_snapshot


class CampaignFreezeTests(unittest.TestCase):
    def test_exact_duplicates_do_not_inflate_the_grouped_trial_count(self):
        groups = [[dict(index=1), dict(index=2)], [dict(index=3)]]
        predictions = [dict(Index=1, Correct=True), dict(Index=2, Correct=True), dict(Index=3, Correct=False)]
        measured = grouped_interval(groups, predictions)
        self.assertEqual((measured['Successes'], measured['Trials']), (1, 2))

    def test_partition_snapshot_detects_changed_learned_template(self):
        with tempfile.TemporaryDirectory() as directory:
            fold = pathlib.Path(directory)
            for name in ('training.json', 'test.json'):
                (fold / name).write_text('[]')
            (fold / 'training-audit.json').write_text('{}')
            trained = fold / 'Items/Trained/apple'
            trained.mkdir(parents=True)
            reference = trained / 'medoid.webp'
            reference.write_bytes(b'original reference')
            snapshot = partition_snapshot([fold])
            verify_snapshot(snapshot)
            reference.write_bytes(b'changed reference')
            with self.assertRaisesRegex(ValueError, 'changed after freeze'):
                verify_snapshot(snapshot)

    def test_outcomes_recompute_correctness_from_actual_answer(self):
        records = [dict(index=1, expected='apple')]
        predictions = [dict(Index=1, Expected='apple', Predicted='coin', Accepted=True, Correct=True)]
        with self.assertRaisesRegex(ValueError, 'correctness flag'):
            verify_outcomes(records, predictions, {})

    def test_rejected_answer_is_not_counted_as_correct(self):
        records = [dict(index=1, expected='apple')]
        predictions = [dict(Index=1, Expected='apple', Predicted='', Accepted=False, Correct=False)]
        verify_outcomes(records, predictions, {})
        predictions[0]['Correct'] = True
        with self.assertRaisesRegex(ValueError, 'correctness flag'):
            verify_outcomes(records, predictions, {})

    def test_preparation_cannot_reopen_an_exposed_holdout(self):
        with tempfile.TemporaryDirectory() as directory:
            campaign = pathlib.Path(directory)
            (campaign / 'holdout-freeze.json').write_text(json.dumps(dict(evaluated=True)))
            with self.assertRaisesRegex(ValueError, 'already been exposed'):
                prepare(campaign, campaign / 'cv', campaign / 'new-run', campaign / 'policy.json')


if __name__ == '__main__':
    unittest.main()
