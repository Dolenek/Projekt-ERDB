"""Download MCP-observed attachment URLs and prepare unlabeled visual review sheets."""
import argparse
import concurrent.futures
import hashlib
import json
import pathlib
import urllib.request

from PIL import Image, ImageDraw


def write_records(path, records):
    serialized = [json.dumps(record, ensure_ascii=False) for record in records]
    rows = [','.join(serialized[start:start + 2]) for start in range(0, len(serialized), 2)]
    path.write_text('[\n' + ',\n'.join(rows) + '\n]\n', encoding='utf-8')


def pixel_digest(image):
    image = image.convert('RGB')
    return hashlib.sha256(str(image.size).encode() + image.tobytes()).hexdigest()


def icon_digest(image):
    width, height = image.size
    bounds = (0, 12, min(width, 120), height - 12) if height >= 150 else (0, 0, min(width, 60), height)
    return pixel_digest(image.crop(bounds))


def download_attachment(record, directory):
    destination = directory / 'images' / (record['messageId'] + '.png')
    if not destination.exists():
        with urllib.request.urlopen(record['urls'][0], timeout=30) as response:
            attachment = response.read(8 * 1024 * 1024 + 1)
        if len(attachment) > 8 * 1024 * 1024:
            raise ValueError('Oversized attachment: ' + record['messageId'])
        destination.write_bytes(attachment)
    attachment = destination.read_bytes()
    with Image.open(destination) as image:
        return dict(messageId=record['messageId'], image='images/' + destination.name,
                    sha256=hashlib.sha256(attachment).hexdigest(), timestamp=record['timestamp'],
                    width=image.width, height=image.height, pixelSha256=pixel_digest(image),
                    iconCropSha256=icon_digest(image))


def known_images(existing):
    known = {}
    for path in sorted((existing / 'images').glob('*')):
        if not path.is_file():
            continue
        with Image.open(path) as image:
            for digest in (hashlib.sha256(path.read_bytes()).hexdigest(),
                           pixel_digest(image), icon_digest(image)):
                known[digest] = path.stem
    return known


def exclude_duplicates(records, known):
    retained, excluded = [], []
    for record in records:
        matching = next((known[record[field]] for field in
                         ('sha256', 'pixelSha256', 'iconCropSha256') if record[field] in known), None)
        if matching:
            excluded.append(dict(record, reason='duplicate-image-or-exact-icon-crop', duplicateOf=matching))
        else:
            retained.append(record)
            for field in ('sha256', 'pixelSha256', 'iconCropSha256'):
                known[record[field]] = record['messageId']
    return retained, excluded


def review_sheets(records, directory):
    for start in range(0, len(records), 25):
        sheet = Image.new('RGB', (1050, 1275), '#eeeeee')
        draw = ImageDraw.Draw(sheet)
        for position, record in enumerate(records[start:start + 25]):
            left, top = (position % 5) * 210, (position // 5) * 255
            with Image.open(directory / record['image']) as original:
                width = 140 if original.height >= 150 else 70
                icon = original.convert('RGB').crop((0, 0, min(original.width, width), original.height))
                icon.thumbnail((200, 200), Image.Resampling.NEAREST)
                if original.height < 150:
                    icon = icon.resize((min(200, icon.width * 2), min(200, icon.height * 2)), Image.Resampling.NEAREST)
                sheet.paste(icon, (left + 5, top + 38))
            draw.text((left + 5, top + 5), str(record['index']) + ' / ' + record['messageId'][-6:], fill='black')
            if 'expected' in record:
                caption = record['expected'] + (' | gray' if record['grayscale'] else '')
                caption += ' | lines' if record['lines'] else ''
                draw.text((left + 5, top + 19), caption, fill='black')
        sheet.save(directory / ('review-' + str(start // 25 + 1).zfill(2) + '.png'))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('source', type=pathlib.Path, help='Ephemeral MCP URL records; do not commit signed URLs')
    parser.add_argument('output', type=pathlib.Path)
    parser.add_argument('--existing', type=pathlib.Path, default=pathlib.Path('artifacts/captcha-dataset'))
    parser.add_argument('--start-index', type=int, default=2000)
    arguments = parser.parse_args()
    records = json.loads(arguments.source.read_text(encoding='utf-8'))
    unique = list({record['messageId']: record for record in records}.values())
    (arguments.output / 'images').mkdir(parents=True, exist_ok=True)
    with concurrent.futures.ThreadPoolExecutor(max_workers=4) as executor:
        downloaded = list(executor.map(lambda record: download_attachment(record, arguments.output), unique))
    for index, record in enumerate(downloaded, arguments.start_index):
        record['index'] = index
    retained, excluded = exclude_duplicates(downloaded, known_images(arguments.existing))
    write_records(arguments.output / 'pending.json', retained)
    write_records(arguments.output / 'excluded.json', excluded)
    review_sheets(retained, arguments.output)
    print(json.dumps(dict(downloaded=len(downloaded), pending=len(retained), excluded=len(excluded))))


if __name__ == '__main__':
    main()
