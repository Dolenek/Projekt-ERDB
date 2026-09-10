"""Collect a bounded unique batch from the search displayed by EpicRPG MCP."""
import argparse
import json
import pathlib
from datetime import datetime, timezone

from collect_dataset import download_attachment, exclude_duplicates, known_images, review_sheets
from collect_webview_batch import OBSERVE, WebViewSession, next_page, save_page
from finalize_dataset import write_manifest
from collection_scope import allowed_timestamp, observed_scope


def prior_images(directory):
    signatures = {}
    for image_directory in pathlib.Path('artifacts').rglob('images'):
        if image_directory.parent.resolve() != directory.resolve():
            signatures.update(known_images(image_directory.parent))
    return signatures


def persist(directory, acquired, retained, excluded):
    for name, records in (('acquired', acquired), ('pending', retained), ('excluded', excluded)):
        write_manifest(directory / (name + '.json'), records)


def collect(session, arguments):
    directory = arguments.directory
    signatures = prior_images(directory)
    existing_messages = {path.stem for path in pathlib.Path('artifacts').rglob('images/*.png')}
    acquired, retained, excluded = [], [], []
    skipped_existing = []
    seen_messages = set()
    for page in range(arguments.pages):
        records = session.evaluate(OBSERVE)
        if not records:
            raise ValueError('No challenge attachment results in the observed search')
        unseen = [record for record in records if record['messageId'] not in seen_messages]
        if not unseen:
            break
        for observed in unseen:
            seen_messages.add(observed['messageId'])
            if not allowed_timestamp(observed.get('timestamp'), arguments.minimum_date):
                continue
            if observed['messageId'] in existing_messages:
                skipped_existing.append(observed['messageId'])
                write_manifest(directory / 'skipped-existing-messages.json', skipped_existing)
                continue
            save_page(session, directory, [observed])
            record = download_attachment(observed, directory)
            record['index'] = arguments.start_index + len(acquired)
            acquired.append(record)
            unique, duplicates = exclude_duplicates([record], signatures)
            retained.extend(unique)
            excluded.extend(duplicates)
            persist(directory, acquired, retained, excluded)
            if len(retained) == arguments.count or len(acquired) == arguments.max_downloads:
                return acquired, retained, excluded
        print(json.dumps(dict(page=page + 1, downloaded=len(acquired), unique=len(retained))), flush=True)
        if not next_page(session, records[0]['messageId']):
            break
    return acquired, retained, excluded


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('port', type=int)
    parser.add_argument('directory', type=pathlib.Path)
    parser.add_argument('--count', type=int, default=500)
    parser.add_argument('--max-downloads', type=int, default=550)
    parser.add_argument('--start-index', type=int, required=True)
    parser.add_argument('--pages', type=int, default=30)
    parser.add_argument('--channel', choices=['bot-commands-' + str(number) for number in range(1, 5)], default='bot-commands-2')
    parser.add_argument('--minimum-date', default=None)
    arguments = parser.parse_args()
    execute(arguments)


def execute(arguments):
    if arguments.channel == 'bot-commands-1':
        arguments.minimum_date = max(arguments.minimum_date or '2020-01-01', '2020-01-01')
    if not 1 <= arguments.count <= 500 or not arguments.count <= arguments.max_downloads <= 550:
        raise ValueError('Collect at most 500 unique attachments, with a small duplicate allowance')
    used = len(list(arguments.directory.parent.glob('*/images/*')))
    arguments.max_downloads = min(arguments.max_downloads, 3000 - used)
    if arguments.max_downloads < arguments.count:
        raise ValueError('Requested batch would exceed the authorized 3000-download campaign limit')
    if arguments.directory.exists():
        raise ValueError('Use a new batch directory; existing acquisition is never overwritten')
    (arguments.directory / 'images').mkdir(parents=True)
    session = WebViewSession(arguments.port)
    try:
        scope = observed_scope(session, arguments.channel, arguments.minimum_date)
        provenance = dict(**scope, startedUtc=datetime.now(timezone.utc).isoformat(),
                          campaignDownloadLimit=3000, batchUniqueTarget=arguments.count)
        (arguments.directory / 'acquisition.json').write_text(json.dumps(provenance, indent=2) + '\n')
        acquired, retained, excluded = collect(session, arguments)
        review_sheets(retained, arguments.directory)
        print(json.dumps(dict(downloaded=len(acquired), retained=len(retained), excluded=len(excluded))), flush=True)
    finally:
        session.connection.close()


if __name__ == '__main__':
    main()
