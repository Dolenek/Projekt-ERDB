using System.Net.Http;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using EpicRPGBot.UI.Services;

namespace EpicRPGBot.UI
{
    public sealed class BotEngine
    {
        private readonly WebView2 _web;
        private readonly DispatcherTimer _huntT;
        private readonly DispatcherTimer _workT;
        private readonly DispatcherTimer _farmT;
        private readonly DispatcherTimer _checkMessageT;
        private readonly Stopwatch _commandDelay = new Stopwatch();

        private int _area;
        private int _huntCooldown;
        private int _workCooldown;
        private int _farmCooldown;

        private string _hunt = "rpg hunt h";
        private string _work = "rpg chop";
        private string _farm = "rpg farm";

        private int _huntMarryTracker = 0;
        private string _lastMessageId = string.Empty;
        private string _prevMessageId = string.Empty;
        private string _prevMessageText = string.Empty;

        private bool _running;

        // Expose running state for UI checks
        public bool IsRunning => _running;

        // Events for UI consumption
        public event Action OnEngineStarted;
        public event Action OnEngineStopped;
        public event Action<string> OnCommandSent;
        public event Action<string> OnMessageSeen;

        // Solver telemetry (optional UI console log)
        public event Action<string> OnSolverInfo;

        // Tracks manual "rpg cd" to avoid duplicate opening cd when Start() runs
        private DateTime _lastManualCdUtc = DateTime.MinValue;

        // Captcha solver state
        private bool _captchaInProgress = false;
        private HttpClient _http;
        private CaptchaClassifier _classifier;

        public BotEngine(WebView2 web, int area, int huntCooldown, int workCooldown, int farmCooldown)
        {
            _web = web ?? throw new ArgumentNullException(nameof(web));

            _area = area;
            _huntCooldown = huntCooldown;
            _workCooldown = workCooldown;
            _farmCooldown = farmCooldown;

            _huntT = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(_huntCooldown) };
            _huntT.Tick += async (s, e) => await SendCommandAsync(_hunt);

            _workT = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(_workCooldown) };
            _workT.Tick += async (s, e) => await SendCommandAsync(_work);

            _farmT = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(_farmCooldown) };
            _farmT.Tick += async (s, e) => await SendCommandAsync(_farm);

            _checkMessageT = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(2000) };
            _checkMessageT.Tick += async (s, e) =>
            {
                try
                {
                    var msg = await CheckLastMessageAsync();
                    if (!string.IsNullOrEmpty(msg))
                    {
                        EventCheck(msg);
                    }
                }
                catch { }
            };
        }


        public void Start()
        {
            if (_running) return;
            _running = true;
            OnEngineStarted?.Invoke();

            // Choose work command based on area (mirrors Program.StartBot() switch)
            if (_area == 3 || _area == 4 || _area == 5)
                _work = "rpg axe";
            else if (_area == 6 || _area == 7 || _area == 8)
                _work = "rpg bowsaw";
            else if (_area == 9 || _area == 10 || _area == 11 || _area == 12 || _area == 13)
                _work = "rpg chainsaw";
            else
                _work = "rpg chop";

            _commandDelay.Restart();

            // Opening salvo (no immediate cd here; UI sends it before calling Start)
            _ = Task.Run(async () =>
            {
                await SafeDelay(2001);
                await SendMessageAsyncDevTools(_hunt);
                OnCommandSent?.Invoke(_hunt);
                await SafeDelay(2001);
                await SendMessageAsyncDevTools(_work);
                OnCommandSent?.Invoke(_work);
                await SafeDelay(2001);
                if (_area >= 4)
                {
                    await SendMessageAsyncDevTools(_farm);
                    OnCommandSent?.Invoke(_farm);
                }
            });

            StartTimers();
        }

        public void Stop()
        {
            if (!_running) return;
            _running = false;
            StopTimers();
            OnEngineStopped?.Invoke();
        }

        private void StartTimers()
        {
            _huntT.Start();
            _workT.Start();
            if (_area >= 4) _farmT.Start();
            _checkMessageT.Start();
        }

        private void PauseWorkTimers()
        {
            _huntT.Stop();
            _workT.Stop();
            _farmT.Stop();
            // keep _checkMessageT running to observe chat while solving
        }

        private void ResumeWorkTimers()
        {
            _huntT.Start();
            _workT.Start();
            if (_area >= 4) _farmT.Start();
        }

        private void StopTimers()
        {
            _huntT.Stop();
            _workT.Stop();
            _farmT.Stop();
            _checkMessageT.Stop();
        }

        private async Task SendCommandAsync(string command)
        {
            if (!_running) return;
            try
            {
                // Alternate marry tracker logic (same as Program.SendCommand)
                if (command == "rpg hunt h" && _huntMarryTracker == 0)
                {
                    _huntMarryTracker = 1;
                }
                else if (command == "rpg hunt h" && _huntMarryTracker == 1)
                {
                    command = "rpg hunt h";
                    _huntMarryTracker = 0;
                }

                if (_commandDelay.ElapsedMilliseconds > 2000)
                {
                    await SendMessageAsyncDevTools(command);
                    OnCommandSent?.Invoke(command);
                    _commandDelay.Restart();
                    await SafeDelay(2001);
                    var msg = await CheckLastMessageAsync();
                    if (!string.IsNullOrEmpty(msg)) EventCheck(msg);
                }
                else
                {
                    await SafeDelay(2000);
                    await SendMessageAsyncDevTools(command);
                    OnCommandSent?.Invoke(command);
                    _commandDelay.Restart();
                    await SafeDelay(2001);
                    var msg = await CheckLastMessageAsync();
                    if (!string.IsNullOrEmpty(msg)) EventCheck(msg);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("SendCommand error: " + ex.Message);
            }
        }

        // Focus the real message composer (bottom text editor), avoiding the header "Search" box.
        private async Task<bool> FocusComposerAsync()
        {
            if (_web.CoreWebView2 == null) return false;

            // Try for up to ~5 seconds
            for (int i = 0; i < 10; i++)
            {
                var focused = await _web.CoreWebView2.ExecuteScriptAsync(@"
(function(){
  // Prefer Discord's Slate editor for message composing
  let candidates = Array.from(document.querySelectorAll(
    'div[role=""textbox""][data-slate-editor=""true""], ' +
    'div[aria-label^=""Message""][role=""textbox""], ' +
    'div[aria-label*=""Message""][role=""textbox""]'
  ));
  if (candidates.length === 0) {
    // fallback to any role=textbox contenteditable true
    candidates = Array.from(document.querySelectorAll('div[role=""textbox""], div[contenteditable=""true""]'));
  }
  // Filter visible
  const vis = candidates.filter(el => {
    const r = el.getBoundingClientRect();
    const st = window.getComputedStyle(el);
    return r.width > 0 && r.height > 0 && st.visibility !== 'hidden' && st.display !== 'none';
  });
  const list = vis.length ? vis : candidates;
  if (list.length === 0) return false;

  // Pick bottom-most element (composer sits at the bottom of the chat)
  list.sort((a,b) => a.getBoundingClientRect().top - b.getBoundingClientRect().top);
  const editor = list[list.length - 1];

  try { editor.scrollIntoView({block:'center'}); } catch(e) {}
  try { editor.click(); } catch(e) {}
  try { editor.focus(); } catch(e) {}

  // Place caret at the end
  try {
    const r = document.createRange();
    r.selectNodeContents(editor);
    r.collapse(false);
    const sel = window.getSelection();
    sel.removeAllRanges();
    sel.addRange(r);
  } catch(e) {}

  return document.activeElement === editor || true;
})();");
                if (string.Equals(focused?.Trim(), "true", StringComparison.OrdinalIgnoreCase))
                    return true;

                await Task.Delay(500);
            }
            return false;
        }

        // Primary send using DevTools Protocol (reliable on Discord)
        private async Task<bool> SendMessageAsyncDevTools(string message)
        {
            if (_web.CoreWebView2 == null) return false;

            if (!await FocusComposerAsync())
                return false;

            string text = (message ?? string.Empty)
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r", "")
                .Replace("\n", "\\n");

            try
            {
                await _web.CoreWebView2.CallDevToolsProtocolMethodAsync(
                    "Input.insertText",
                    $"{{\"text\":\"{text}\"}}"
                );

                const string keyDown = "{\"type\":\"keyDown\",\"key\":\"Enter\",\"code\":\"Enter\",\"windowsVirtualKeyCode\":13,\"nativeVirtualKeyCode\":13}";
                const string keyUp = "{\"type\":\"keyUp\",\"key\":\"Enter\",\"code\":\"Enter\",\"windowsVirtualKeyCode\":13,\"nativeVirtualKeyCode\":13}";

                await _web.CoreWebView2.CallDevToolsProtocolMethodAsync("Input.dispatchKeyEvent", keyDown);
                await _web.CoreWebView2.CallDevToolsProtocolMethodAsync("Input.dispatchKeyEvent", keyUp);

                return true;
            }
            catch
            {
                // Fallback to execCommand if DevTools fails (rare)
                try
                {
                    string escaped = text;
                    string js = $@"
(async () => {{
  const sleep = (ms) => new Promise(r => setTimeout(r, ms));
  const editor = (function(){{
    let c = Array.from(document.querySelectorAll('div[role=""textbox""][data-slate-editor=""true""], div[aria-label^=""Message""][role=""textbox""], div[aria-label*=""Message""][role=""textbox""]'));
    if (c.length === 0) c = Array.from(document.querySelectorAll('div[role=""textbox""], div[contenteditable=""true""]'));
    if (c.length === 0) return null;
    c.sort((a,b)=>a.getBoundingClientRect().top - b.getBoundingClientRect().top);
    return c[c.length - 1];
  }})();
  if (!editor) return false;
  editor.focus();
  try {{
    document.execCommand('insertText', false, ""{escaped}"");
    await sleep(50);
    const eDown = new KeyboardEvent('keydown', {{ key:'Enter', code:'Enter', keyCode:13, which:13, bubbles:true, cancelable:true }});
    const eUp = new KeyboardEvent('keyup', {{ key:'Enter', code:'Enter', keyCode:13, which:13, bubbles:true, cancelable:true }});
    editor.dispatchEvent(eDown);
    editor.dispatchEvent(eUp);
    return true;
  }} catch (e) {{ return false; }}
}})();
";
                    var r = (await _web.CoreWebView2.ExecuteScriptAsync(js))?.Trim();
                    return string.Equals(r, "true", StringComparison.OrdinalIgnoreCase);
                }
                catch { return false; }
            }
        }

        private async Task<bool> SendAndEmitAsync(string text)
        {
            var ok = await SendMessageAsyncDevTools(text);
            if (ok) OnCommandSent?.Invoke(text);
            return ok;
        }

        // Exposed helper to send immediately (used by Start button)
        public async Task<bool> SendImmediateAsync(string text)
        {
            var ok = await SendMessageAsyncDevTools(text);
            if (ok)
            {
                OnCommandSent?.Invoke(text);
                if (string.Equals(text?.Trim(), "rpg cd", StringComparison.OrdinalIgnoreCase))
                    _lastManualCdUtc = DateTime.UtcNow;
            }
            return ok;
        }

        private async Task<string> CheckLastMessageAsync()
        {
            if (_web.CoreWebView2 == null) return string.Empty;
            string js = @"
(() => {
  const items = Array.from(document.querySelectorAll('li[id^=""chat-messages-""]'));
  if (items.length === 0) return JSON.stringify({ id: '', text: '' });
  const last = items[items.length - 1];
  const obj = { id: (last.id || ''), text: (last.innerText || '') };
  return JSON.stringify(obj);
})();
";
            try
            {
                var json = await _web.CoreWebView2.ExecuteScriptAsync(js);
                var s = UnquoteJson(json);
                var id = ExtractField(s, "id");
                var text = ExtractField(s, "text");

                if (string.IsNullOrEmpty(id) || id == _lastMessageId) return string.Empty;
                _prevMessageId = _lastMessageId;
                _lastMessageId = id;
                OnMessageSeen?.Invoke(text);
                return text ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string ExtractField(string json, string field)
        {
            if (string.IsNullOrEmpty(json)) return null;
            var key = $"\"{field}\":";
            int idx = json.IndexOf(key, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return null;
            idx += key.Length;
            while (idx < json.Length && (json[idx] == ' ')) idx++;
            bool quoted = idx < json.Length && json[idx] == '\"';
            if (quoted) idx++;
            int start = idx;
            if (quoted)
            {
                while (idx < json.Length)
                {
                    if (json[idx] == '\\')
                    {
                        idx += 2;
                        continue;
                    }
                    if (json[idx] == '\"') break;
                    idx++;
                }
                var raw = json.Substring(start, Math.Max(0, idx - start));
                return raw.Replace("\\n", "\n").Replace("\\r", "\r").Replace("\\t", "\t").Replace("\\\"", "\"").Replace("\\\\", "\\");
            }
            else
            {
                while (idx < json.Length && json[idx] != ',' && json[idx] != '}' && !char.IsWhiteSpace(json[idx])) idx++;
                return json.Substring(start, Math.Max(0, idx - start)).Trim();
            }
        }

        private static string UnquoteJson(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            s = s.Trim();
            if (s.Length >= 2 && s[0] == '\"' && s[s.Length - 1] == '\"')
            {
                s = s.Substring(1, s.Length - 2);
                s = s.Replace("\\n", "\n").Replace("\\r", "\r").Replace("\\t", "\t").Replace("\\\"", "\"").Replace("\\\\", "\\");
            }
            return s;
        }

        private void EventCheck(string message)
        {
            var msg = message ?? string.Empty;

            // Trigger by alternate guard phrase requested for testing:
            // Look for "EPIC GUARD: stop there," in the last two messages.
            const string GuardAlt = "EPIC GUARD: stop there,";
            bool inCurrent = msg?.IndexOf(GuardAlt, StringComparison.OrdinalIgnoreCase) >= 0;
            bool inPrev = (!string.IsNullOrEmpty(_prevMessageText) && _prevMessageText.IndexOf(GuardAlt, StringComparison.OrdinalIgnoreCase) >= 0);

            if (inCurrent || inPrev)
            {
                Debug.WriteLine("Guard seen (alt phrase)");
                OnSolverInfo?.Invoke("Captcha detected (alt phrase).");
                var targetId = inCurrent ? _lastMessageId : _prevMessageId;
                _ = SolveCaptchaAsync(targetId); // fire-and-forget
            }
            else if (msg.IndexOf("TEST", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _ = SendAndEmitAsync("I hear you: " + DateTime.Now);
            }
            else if (msg.IndexOf("BOT HELP", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _ = SendAndEmitAsync("Change work - chop / axe / bowsaw / chainsaw ");
                _ = SendAndEmitAsync("Change farm - farm / potato / carrot / bread");
                _ = SendAndEmitAsync("bot farming - will start farming");
            }
            else if (msg.IndexOf("STOP", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                StopTimers();
            }
            else if (msg.IndexOf("START", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _ = Task.Run(async () =>
                {
                    await SendAndEmitAsync("rpg cd");
                    await SafeDelay(2001);
                    await SendAndEmitAsync(_hunt);
                    await SafeDelay(2001);
                    await SendAndEmitAsync(_work);
                    await SafeDelay(2001);
                    if (_area >= 4)
                        await SendAndEmitAsync(_farm);
                });
                StartTimers();
            }
            else if (msg.IndexOf("wait at least", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                // cooldown info; ignore
            }
            else if (msg.IndexOf("CHANGE WORK", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                if (msg.IndexOf("CHOP", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    _ = SendAndEmitAsync("I am chopping treez");
                    _work = "rpg chop";
                }
                else if (msg.IndexOf("AXE", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    _ = SendAndEmitAsync("I am using an axe");
                    _work = "rpg axe";
                }
                else if (msg.IndexOf("BOWSAW", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    _ = SendAndEmitAsync("I am using a bowsaw");
                    _work = "rpg bowsaw";
                }
                else if (msg.IndexOf("CHAINSAW", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    _ = SendAndEmitAsync("I am using a chainsaw");
                    _work = "rpg chainsaw";
                }
            }
            else if (msg.IndexOf("CHANGE FARM", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                if (msg.IndexOf("FARM FARM", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    _farm = "rpg farm";
                    _ = SendAndEmitAsync("I am farming normally");
                }
                else if (msg.IndexOf("CARROT", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    _farm = "rpg farm carrot";
                    _ = SendAndEmitAsync("I am farming carrots");
                }
                else if (msg.IndexOf("POTATO", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    _farm = "rpg farm potato";
                    _ = SendAndEmitAsync("I am farming potatoes");
                }
                else if (msg.IndexOf("BREAD", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    _farm = "rpg farm bread";
                    _ = SendAndEmitAsync("I am farming bread");
                }
            }
            else if (msg.IndexOf("BOT FARMING", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _ = SendAndEmitAsync("I am farming");
                if (!_farmT.IsEnabled)
                {
                    _farmT.Interval = TimeSpan.FromMilliseconds(_farmCooldown);
                    _farmT.Start();
                }
            }
            else if (msg.IndexOf("You were about to hunt a defenseless monster, but then you notice a zombie horde coming your way", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _ = SendMessageAsyncDevTools("RUN");
            }
            else if (msg.IndexOf("megarace boost", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _ = SendMessageAsyncDevTools("yes");
            }
            else if (msg.IndexOf("AN EPIC TREE HAS JUST GROWN", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _ = SendMessageAsyncDevTools("CUT");
            }
            else if (msg.IndexOf("A MEGALODON HAS SPAWNED IN THE RIVER", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _ = SendMessageAsyncDevTools("LURE");
            }
            else if (msg.IndexOf("IT'S RAINING COINS", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _ = SendMessageAsyncDevTools("CATCH");
            }
            else if (msg.IndexOf("God accidentally dropped an EPIC coin", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                RespondFirstPresent(msg,
                    "I SHALL BRING THE EPIC TO THE COIN",
                    "MY PRECIOUS",
                    "WHAT IS EPIC? THIS COIN",
                    "YES! AN EPIC COIN",
                    "OPERATION: EPIC COIN"
                );
            }
            else if (msg.IndexOf("OOPS! God accidentally dropped", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                RespondFirstPresent(msg,
                    "BACK OFF THIS IS MINE!!",
                    "HACOINA MATATA",
                    "THIS IS MINE",
                    "ALL THE COINS BELONG TO ME",
                    "GIMME DA MONEY",
                    "OPERATION: COINS"
                );
            }
            else if (msg.IndexOf("EPIC NPC: I have a special trade today!", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                RespondFirstPresent(msg,
                    "YUP I WILL DO THAT",
                    "I WANT THAT",
                    "HEY EPIC NPC! I WANT TO TRADE WITH YOU",
                    "THAT SOUNDS LIKE AN OP BUSINESS",
                    "OWO ME!!!"
                );
            }
            else if (msg.IndexOf("A LOOTBOX SUMMONING HAS", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _ = SendAndEmitAsync("SUMMON");
            }
            else if (msg.IndexOf("A LEGENDARY BOSS JUST SPAWNED", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _ = SendAndEmitAsync("TIME TO FIGHT");
            }

            // Track last message text so we can check the last two messages for guard triggers
            _prevMessageText = msg;
        }

        private void RespondFirstPresent(string msg, params string[] options)
        {
            try
            {
                if (string.IsNullOrEmpty(msg) || options == null || options.Length == 0) return;
                foreach (var opt in options)
                {
                    if (string.IsNullOrEmpty(opt)) continue;
                    if (msg.IndexOf(opt, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        _ = SendMessageAsyncDevTools(opt);
                        return;
                    }
                }
            }
            catch { }
        }

        private async Task<string> GetCaptchaImageUrlForMessageIdAsync(string messageId)
        {
            if (_web.CoreWebView2 == null) return null;
            if (string.IsNullOrWhiteSpace(messageId)) return null;

            string js = $@"
(() => {{
  const root = document.getElementById('{messageId.Replace("'", "\\'")}');
  if (!root) return '';
  // Prefer visible imgs
  const imgs = Array.from(root.querySelectorAll('img'));
  let best = '';
  for (const im of imgs) {{
    const src = im.getAttribute('src') || im.src || '';
    if (!src) continue;
    const r = im.getBoundingClientRect();
    const styles = window.getComputedStyle(im);
    const visible = r.width > 0 && r.height > 0 && styles.visibility !== 'hidden' && styles.display !== 'none';
    if (visible) {{
      best = src;
      break;
    }}
    if (!best) best = src;
  }}
  return best || '';
}})();";
            try
            {
                var res = await _web.CoreWebView2.ExecuteScriptAsync(js);
                var url = UnquoteJson(res);
                return string.IsNullOrWhiteSpace(url) ? null : url;
            }
            catch
            {
                return null;
            }
        }

        private CaptchaClassifier EnsureClassifier()
        {
            if (_classifier != null) return _classifier;

            // Resolve refs dir and threshold from env
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var defaultRefs = Path.Combine(baseDir, "CaptchaRefs");
            var refsDir = Env.Get("CAPTCHA_REFS_DIR", defaultRefs);
            int threshold = 12;
            try
            {
                var tStr = Env.Get("CAPTCHA_HASH_THRESHOLD", null);
                if (!string.IsNullOrWhiteSpace(tStr) && int.TryParse(tStr, out var t) && t > 0 && t <= 64) threshold = t;
            }
            catch { }

            try
            {
                _classifier = new CaptchaClassifier(refsDir, threshold);
                OnSolverInfo?.Invoke($"Solver initialized (refs={refsDir}, threshold={threshold}).");
            }
            catch (Exception ex)
            {
                OnSolverInfo?.Invoke($"Solver init failed: {ex.Message}");
                _classifier = null;
            }
            return _classifier;
        }

        // Capture the first visible IMG inside the specified message as a PNG via DevTools, avoiding WEBP/CORS issues.
        private async Task<byte[]> CaptureCaptchaImagePngAsync(string messageId)
        {
            if (_web.CoreWebView2 == null) return null;
            if (string.IsNullOrWhiteSpace(messageId)) return null;

            // Get IMG bounding rect in CSS pixels, adjusted for page scroll, from the specific message LI.
            string js = $@"
(() => {{
  const root = document.getElementById('{messageId.Replace("'", "\\'")}');
  if (!root) return JSON.stringify({{ ok:false }});
  const imgs = Array.from(root.querySelectorAll('img'));
  let el = null;
  for (const im of imgs) {{
    const r = im.getBoundingClientRect();
    const st = window.getComputedStyle(im);
    const vis = r.width > 1 && r.height > 1 && st.visibility !== 'hidden' && st.display !== 'none';
    if (vis) {{ el = im; break; }}
  }}
  if (!el) return JSON.stringify({{ ok:false }});
  try {{ el.scrollIntoView({{block:'center'}}); }} catch {{}}
  const r = el.getBoundingClientRect();
  const x = r.left + window.scrollX;
  const y = r.top + window.scrollY;
  return JSON.stringify({{
    ok:true,
    x:x, y:y, w:r.width, h:r.height
  }});
}})();";
            try
            {
                var json = await _web.CoreWebView2.ExecuteScriptAsync(js);
                var s = UnquoteJson(json);
                var ok = ExtractField(s, "ok");
                if (string.IsNullOrEmpty(ok) || ok.IndexOf("true", StringComparison.OrdinalIgnoreCase) < 0)
                    return null;

                double x = 0, y = 0, w = 0, h = 0;
                double.TryParse(ExtractField(s, "x"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out x);
                double.TryParse(ExtractField(s, "y"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out y);
                double.TryParse(ExtractField(s, "w"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out w);
                double.TryParse(ExtractField(s, "h"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out h);

                if (w < 2 || h < 2) return null;

                // Use CSS pixels with scale=1 and allow beyond-viewport capture so the rect is cropped to only the IMG.
                var clipX = Math.Max(0, x);
                var clipY = Math.Max(0, y);
                var clipW = Math.Max(2, w);
                var clipH = Math.Max(2, h);

                string payload =
                    $"{{\"format\":\"png\",\"fromSurface\":true,\"captureBeyondViewport\":true," +
                    $"\"clip\":{{\"x\":{clipX.ToString(System.Globalization.CultureInfo.InvariantCulture)}," +
                    $"\"y\":{clipY.ToString(System.Globalization.CultureInfo.InvariantCulture)}," +
                    $"\"width\":{clipW.ToString(System.Globalization.CultureInfo.InvariantCulture)}," +
                    $"\"height\":{clipH.ToString(System.Globalization.CultureInfo.InvariantCulture)}," +
                    $"\"scale\":1}}}}";

                var res = await _web.CoreWebView2.CallDevToolsProtocolMethodAsync("Page.captureScreenshot", payload);
                var data = ExtractField(UnquoteJson(res), "data");
                if (string.IsNullOrWhiteSpace(data)) return null;

                try
                {
                    return Convert.FromBase64String(data);
                }
                catch { return null; }
            }
            catch
            {
                return null;
            }
        }

        private HttpClient EnsureHttp()
        {
            if (_http != null) return _http;
            _http = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(4)
            };
            return _http;
        }

        private async Task SolveCaptchaAsync() => await SolveCaptchaAsync(_lastMessageId);

        private async Task SolveCaptchaAsync(string targetMessageId)
        {
            if (_captchaInProgress) return;
            _captchaInProgress = true;

            try
            {
                PauseWorkTimers();

                var clf = EnsureClassifier();
                if (clf == null)
                {
                    OnSolverInfo?.Invoke("Solver unavailable (classifier init failed).");
                    return;
                }

                var msgId = targetMessageId;
                if (string.IsNullOrWhiteSpace(msgId))
                {
                    OnSolverInfo?.Invoke("Cannot solve: message id is empty.");
                    return;
                }

                // First try capturing a PNG of the IMG via DevTools (handles WEBP/CORS).
                byte[] bytes = await CaptureCaptchaImagePngAsync(msgId);

                string url = null;
                if (bytes == null || bytes.Length == 0)
                {
                    url = await GetCaptchaImageUrlForMessageIdAsync(msgId);
                }

                if ((bytes == null || bytes.Length == 0) && string.IsNullOrWhiteSpace(url))
                {
                    // Fallback: try the other of the last two messages to support separated text/image posts
                    var altId = msgId == _lastMessageId ? _prevMessageId : _lastMessageId;
                    if (!string.IsNullOrWhiteSpace(altId))
                    {
                        OnSolverInfo?.Invoke("Primary message had no image; trying adjacent message.");
                        bytes = await CaptureCaptchaImagePngAsync(altId);
                        if (bytes == null || bytes.Length == 0)
                            url = await GetCaptchaImageUrlForMessageIdAsync(altId);
                    }
                }

                if ((bytes == null || bytes.Length == 0) && string.IsNullOrWhiteSpace(url))
                {
                    OnSolverInfo?.Invoke("Captcha image not found in selected/adjacent messages.");
                    return;
                }

                if (bytes == null || bytes.Length == 0)
                {
                    OnSolverInfo?.Invoke($"Captcha image via URL: {url}");
                    try
                    {
                        bytes = await EnsureHttp().GetByteArrayAsync(url);
                    }
                    catch (Exception ex)
                    {
                        OnSolverInfo?.Invoke($"Download failed: {ex.Message}");
                        return;
                    }
                }
                else
                {
                    OnSolverInfo?.Invoke("Captcha image captured via DevTools screenshot.");
                }

                var t0 = Stopwatch.GetTimestamp();
                var result = clf.Classify(bytes);
                var ms = (int)(1000.0 * (Stopwatch.GetTimestamp() - t0) / Stopwatch.Frequency);

                if (!result.IsMatch || string.IsNullOrWhiteSpace(result.Label))
                {
                    OnSolverInfo?.Invoke($"Classifier uncertain; closest='{(string.IsNullOrWhiteSpace(result.Label) ? "<none>" : result.Label)}' (dist={result.Distance}, method={result.Method}, {ms} ms). Skipping.");
                    return;
                }

                OnSolverInfo?.Invoke($"Classifier answer '{result.Label}' (dist={result.Distance}, method={result.Method}, {ms} ms). Sending.");
                await SendAndEmitAsync(result.Label);
            }
            catch (Exception ex)
            {
                OnSolverInfo?.Invoke($"SolveCaptcha error: {ex.Message}");
            }
            finally
            {
                ResumeWorkTimers();
                _captchaInProgress = false;
            }
        }

        private static Task SafeDelay(int ms)
        {
            return Task.Delay(ms);
        }
    }
}