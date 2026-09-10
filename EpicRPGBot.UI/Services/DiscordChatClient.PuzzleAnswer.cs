using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace EpicRPGBot.UI.Services
{
    public sealed partial class DiscordChatClient : IPuzzleAnswerChatClient
    {
        public async Task<bool> SendPuzzleAnswerOnceAsync(string answer,
            Func<bool> incidentIsCurrent, CancellationToken cancellationToken)
        {
            if (_web.CoreWebView2 == null || !incidentIsCurrent()) return false;
            if (!await FocusComposerAsync(cancellationToken)) return false;
            cancellationToken.ThrowIfCancellationRequested();
            if (!incidentIsCurrent()) return false;
            await _web.CoreWebView2.CallDevToolsProtocolMethodAsync("Input.insertText",
                JsonSerializer.Serialize(new { text = answer }));
            await DispatchPuzzleKeyAsync("Escape", 27, "keyDown");
            await DispatchPuzzleKeyAsync("Escape", 27, "keyUp");
            cancellationToken.ThrowIfCancellationRequested();
            if (!incidentIsCurrent()) return false;
            // Once Enter is dispatched, never retry: a delayed receipt is not proof of failure.
            await DispatchPuzzleKeyAsync("Enter", 13, "keyDown");
            await DispatchPuzzleKeyAsync("Enter", 13, "keyUp");
            return true;
        }

        private Task<string> DispatchPuzzleKeyAsync(string key, int keyCode, string eventType)
        {
            return _web.CoreWebView2.CallDevToolsProtocolMethodAsync("Input.dispatchKeyEvent",
                JsonSerializer.Serialize(new
                {
                    type = eventType, key, code = key,
                    windowsVirtualKeyCode = keyCode, nativeVirtualKeyCode = keyCode
                }));
        }
    }
}
