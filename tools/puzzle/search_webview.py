"""Enter a search in the observed Discord search combobox of an MCP-owned WebView."""
import argparse
import json
from collect_webview_batch import WebViewSession


def command(session, method, parameters):
    session.sequence += 1
    session.connection.send(json.dumps(dict(id=session.sequence, method=method, params=parameters)))
    while True:
        response = json.loads(session.connection.recv())
        if response.get('id') == session.sequence:
            if 'error' in response:
                raise RuntimeError(response['error']['message'])
            return response.get('result')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('port', type=int)
    parser.add_argument('query')
    arguments = parser.parse_args()
    session = WebViewSession(arguments.port)
    session.evaluate("""(()=>{const e=document.querySelector('[role="combobox"][contenteditable="true"]');
        if(!e)throw Error('Search combobox not found');e.focus();return true;})()""")
    command(session, 'Input.dispatchKeyEvent', dict(type='keyDown', key='a', code='KeyA', windowsVirtualKeyCode=65, modifiers=2))
    command(session, 'Input.dispatchKeyEvent', dict(type='keyUp', key='a', code='KeyA', windowsVirtualKeyCode=65, modifiers=2))
    command(session, 'Input.insertText', dict(text=arguments.query))
    command(session, 'Input.dispatchKeyEvent', dict(type='keyDown', key='Enter', code='Enter', windowsVirtualKeyCode=13))
    command(session, 'Input.dispatchKeyEvent', dict(type='keyUp', key='Enter', code='Enter', windowsVirtualKeyCode=13))
    session.connection.close()


if __name__ == '__main__':
    main()
