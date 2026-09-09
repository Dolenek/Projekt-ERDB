"""Render full adaptive icon regions from a development manifest for visual auditing."""
import argparse
import json
import pathlib
import cv2
from PIL import Image, ImageDraw
from learned_probe import locate


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('manifest', type=pathlib.Path)
    parser.add_argument('output', type=pathlib.Path)
    arguments = parser.parse_args()
    records = json.loads(arguments.manifest.read_text())
    for start in range(0, len(records), 25):
        sheet = Image.new('RGB', (1250, 1100), '#eeeeee')
        draw = ImageDraw.Draw(sheet)
        for position, record in enumerate(records[start:start + 25]):
            left, top = position % 5 * 250, position // 5 * 220
            source = cv2.imread(str(arguments.manifest.parent / record['image']))
            colors = locate(source)
            if colors is None:
                colors = source
            icon = Image.fromarray(cv2.cvtColor(colors, cv2.COLOR_BGR2RGB))
            icon.thumbnail((240, 190))
            sheet.paste(icon, (left, top + 25))
            draw.text((left, top), str(record['index']) + ': ' + record['expected'], fill='black')
        sheet.save(arguments.output / ('cleaned-' + str(start // 25 + 1) + '.png'))


if __name__ == '__main__':
    main()
