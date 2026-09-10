"""Save original attachments from the search opened by EpicRPG MCP, without credentials."""
import argparse
import base64
import json
import pathlib
import time
import urllib.request

import websocket
from collect_dataset import download_attachment, exclude_duplicates, known_images, review_sheets
from finalize_dataset import write_manifest


class WebViewSession:
    def __init__(self, port):
        with urllib.request.urlopen(f'http://127.0.0.1:{port}/json/list') as response:
            targets = json.load(response)
        target = next(target for target in targets if
                      '/555971084415926272/557606805425881088' in target.get('url', ''))
        self.connection = websocket.create_connection(target['webSocketDebuggerUrl'], suppress_origin=True, timeout=60)
        self.sequence = 0

    def evaluate(self, expression):
        self.sequence += 1
        self.connection.send(json.dumps(dict(id=self.sequence, method='Runtime.evaluate',
                                             params=dict(expression=expression, awaitPromise=True, returnByValue=True))))
        while True:
            response = json.loads(self.connection.recv())
            if response.get('id') != self.sequence:
                continue
            if 'error' in response or 'exceptionDetails' in response.get('result', {}):
                raise RuntimeError('WebView evaluation failed; inspect the MCP page')
            return response['result']['result'].get('value')


OBSERVE = """Array.from(document.querySelectorAll('section[aria-label="Search Results"] [id^="search-result-"]'))
.filter(e=>e.textContent.includes('EPIC GUARD: stop there,'))
.map(e=>({messageId:e.id.replace('search-result-',''),timestamp:e.querySelector('time')?.dateTime,
urls:Array.from(e.querySelectorAll('a[href*="/attachments/"]')).map(a=>a.href)})).filter(r=>r.urls.length)"""


def save_page(session, directory, records):
    for record in records:
        path = directory / 'images' / (record['messageId'] + '.png')
        if path.exists():
            continue
        # The URL comes only from the observed attachment DOM. No account tokens are read.
        expression = """(async()=>{const r=await fetch(ATTACHMENT_URL);if(!r.ok)throw Error('Attachment HTTP '+r.status);
const b=await r.arrayBuffer();if(b.byteLength>8388608)throw Error('Oversized attachment');
return await new Promise(resolve=>{const f=new FileReader();f.onload=()=>resolve(f.result.split(',')[1]);
f.readAsDataURL(new Blob([b]));});})()""".replace('ATTACHMENT_URL', json.dumps(record['urls'][0]))
        path.write_bytes(base64.b64decode(session.evaluate(expression)))


def next_page(session, previous_id):
    clicked = session.evaluate("""(()=>{const b=Array.from(document.querySelectorAll(
        'section[aria-label="Search Results"] button')).find(b=>b.textContent==='Next');
        if(!b||b.disabled)return false;b.click();return true})()""")
    if not clicked:
        return False
    for _ in range(60):
        time.sleep(0.5)
        records = session.evaluate(OBSERVE)
        if records and records[0]['messageId'] != previous_id:
            return True
    raise TimeoutError('Search page did not advance; no repeat click attempted')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('port', type=int)
    parser.add_argument('directory', type=pathlib.Path)
    parser.add_argument('--pages', type=int, default=30)
    parser.add_argument('--start-index', type=int, default=3000)
    arguments = parser.parse_args()
    directory = arguments.directory
    (directory / 'images').mkdir(parents=True, exist_ok=True)
    if (directory / 'pending.json').exists():
        raise ValueError('Use a new batch directory; acquisition manifests are not overwritten')
    session = WebViewSession(arguments.port)
    observed = []
    for page in range(arguments.pages):
        records = session.evaluate(OBSERVE)
        if not records:
            raise ValueError('No EPIC GUARD search results on the MCP page')
        save_page(session, directory, records)
        observed.extend(download_attachment(record, directory) for record in records)
        write_manifest(directory / 'acquired.json', observed)
        print(json.dumps(dict(page=page + 1, total=len(observed))), flush=True)
        if page + 1 < arguments.pages and not next_page(session, records[0]['messageId']):
            break
    finalize_collection(directory, observed, arguments.start_index)
    session.connection.close()


def finalize_collection(directory, observed, start_index):
    known = {}
    for existing in pathlib.Path('artifacts').glob('puzzle-*'):
        if existing.resolve() != directory.resolve() and (existing / 'images').is_dir():
            known.update(known_images(existing))
    for index, record in enumerate(observed, start_index):
        record['index'] = index
    retained, excluded = exclude_duplicates(observed, known)
    write_manifest(directory / 'pending.json', retained)
    write_manifest(directory / 'excluded.json', excluded)
    review_sheets(retained, directory)
    print(json.dumps(dict(retained=len(retained), excluded=len(excluded))), flush=True)


if __name__ == '__main__':
    main()

