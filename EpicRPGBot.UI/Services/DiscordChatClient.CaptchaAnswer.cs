using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace EpicRPGBot.UI.Services
{
    public sealed partial class DiscordChatClient : ICaptchaAnswerChatClient
    {
        public async Task<bool> SendCaptchaAnswerOnceAsync(string answer,
            Func<bool> incidentIsCurrent, CancellationToken cancellationToken)
        {
            if (_web.CoreWebView2 == null || !incidentIsCurrent()) return false;
            if (!await FocusComposerAsync(cancellationToken)) return false;
            cancellationToken.ThrowIfCancellationRequested();
            if (!incidentIsCurrent()) return false;
            await _web.CoreWebView2.CallDevToolsProtocolMethodAsync("Input.insertText",
                JsonSerializer.Serialize(new { text = answer }));
            await DispatchCaptchaKeyAsync("Escape", 27, "keyDown");
            await DispatchCaptchaKeyAsync("Escape", 27, "keyUp");
            cancellationToken.ThrowIfCancellationRequested();
            if (!incidentIsCurrent()) return false;
            // Once Enter is dispatched, never retry: a delayed receipt is not proof of failure.
            await DispatchCaptchaKeyAsync("Enter", 13, "keyDown");
            await DispatchCaptchaKeyAsync("Enter", 13, "keyUp");
            return true;
        }

        private Task<string> DispatchCaptchaKeyAsync(string key, int keyCode, string eventType)
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
