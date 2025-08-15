using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using EpicRPGBot.UI.Services;
using EpicRPGBot.UI.Models;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using System.Windows.Controls;

namespace EpicRPGBot.UI
{
    public partial class MainWindow : Window
    {
        private DispatcherTimer _pollTimer;
        private BotEngine _engine;

        // UI data
        private readonly InMemoryLog _log = new InMemoryLog();
        private readonly LastMessagesBuffer _last = new LastMessagesBuffer(5);
        private string _prevPolled = string.Empty;
        private int _huntCount = 0;
        private System.Windows.Controls.Grid _lastMessagesPanel;
        private System.Windows.Controls.TextBlock _huntCountText;

        // Cooldown tracking
        private DispatcherTimer _cooldownTimer;
        private readonly Dictionary<string, CooldownEntry> _cooldowns = new();
        private readonly Dictionary<string, string> _aliasToCanonical = new();
        
        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Load .env and seed UI
            Env.Load();
            ChannelUrlBox.Text = Env.Get("DISCORD_CHANNEL_URL", "https://discord.com/channels/@me");
            AreaBox.Text = Env.Get("AREA", AreaBox.Text);
            HuntCdBox.Text = Env.Get("HUNT_COOLDOWN", "21000");
            WorkCdBox.Text = Env.Get("WORK_COOLDOWN", "99000");
            FarmCdBox.Text = Env.Get("FARM_COOLDOWN", "196000");

            // Bind left panels
            StatsList.ItemsSource = _last.Items;
            ConsoleList.ItemsSource = _log.Items;
            _log.Engine("UI loaded");

            // Cache named elements for tabs/stats to avoid compile-time field generation issues
            _lastMessagesPanel = (System.Windows.Controls.Grid)FindName("LastMessagesPanel");
            _huntCountText = (System.Windows.Controls.TextBlock)FindName("HuntCountText");

            // Setup cooldown label mapping + ticking
            InitCooldownTracking();
            
            await InitializeWebViewAsync();
            await NavigateToChannelAsync(GetChannelUrl());
            StartPollingLastMessage();
        }

        private string GetChannelUrl()
        {
            var url = ChannelUrlBox.Text?.Trim();
            if (string.IsNullOrEmpty(url) && UseAtMeFallback.IsChecked == true)
                return "https://discord.com/channels/@me";
            return string.IsNullOrEmpty(url) ? "https://discord.com/channels/@me" : url;
        }

        private async Task InitializeWebViewAsync()
        {
            try
            {
                InitHint.Visibility = Visibility.Visible;
                InitHint.Text = "Initializing WebView2...";

                var dataDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "EpicRPGBot.UI", "WebView2");
                Directory.CreateDirectory(dataDir);

                var env = await CoreWebView2Environment.CreateAsync(null, dataDir);
                await Web.EnsureCoreWebView2Async(env);

                Web.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
                Web.CoreWebView2.Settings.AreDevToolsEnabled = true;
                Web.CoreWebView2.Settings.IsZoomControlEnabled = true;

                Web.NavigationCompleted += async (s, e) =>
                {
                    try { await ClickInterstitialsAsync(); } catch { }
                };

                InitHint.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                InitHint.Visibility = Visibility.Visible;
                InitHint.Text = "WebView2 init failed: " + ex.Message;
            }
        }

        private async Task NavigateToChannelAsync(string url)
        {
            try
            {
                if (Web.CoreWebView2 != null)
                {
                    Web.CoreWebView2.Navigate(url);
                }
                else
                {
                    Web.Source = new Uri(url);
                }
            }
            catch (Exception ex)
            {
                InitHint.Visibility = Visibility.Visible;
                InitHint.Text = "Navigate failed: " + ex.Message;
            }
        }

        private async Task ClickInterstitialsAsync()
        {
            if (Web.CoreWebView2 == null) return;

            string script = @"
(() => {
  const byText = (txt) => {
    const walker = document.createTreeWalker(document.body, NodeFilter.SHOW_ELEMENT, null);
    let clicked = false;
    while (walker.nextNode()) {
      const el = walker.currentNode;
      if (!(el instanceof HTMLElement)) continue;
      const t = (el.innerText || '').trim();
      if (!t) continue;
      if (t.includes(txt)) {
        try { el.click(); clicked = true; } catch {}
      }
    }
    return clicked;
  };
  let any = false;
  any = byText('Open Discord in your browser') || any;
  any = byText('Continue to Discord') || any;
  any = byText('Continue in browser') || any;
  return any;
})();
";
            try { await Web.CoreWebView2.ExecuteScriptAsync(script); } catch { }
        }

        private void StartPollingLastMessage()
        {
            _pollTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            _pollTimer.Tick += async (s, e) =>
            {
                try
                {
                    var msg = await GetLastMessageTextAsync();
                    if (!string.IsNullOrWhiteSpace(msg) && !string.Equals(msg, _prevPolled, StringComparison.Ordinal))
                    {
                        _prevPolled = msg;
                        UiDispatcher.OnUI(_last.Add, msg);
                        // Parse and apply cooldowns when the 'cooldowns' message appears
                        UiDispatcher.OnUI(() => TryParseCooldowns(msg));
                    }
                }
                catch { }
            };
            _pollTimer.Start();
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

        private async Task<string> GetLastMessageTextAsync()
        {
            if (Web.CoreWebView2 == null) return null;
            string js = @"
(() => {
  const items = Array.from(document.querySelectorAll(""li[id^='chat-messages-']""));
  if (items.length === 0) return '';
  const last = items[items.length - 1];
  return last.innerText || '';
})()
";
            var result = await Web.CoreWebView2.ExecuteScriptAsync(js);
            return UnquoteJson(result);
        }

        private async Task<bool> SendMessageAsync(string message)
        {
            if (Web.CoreWebView2 == null) return false;
            string escaped = (message ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n");
            string js = $@"
(async () => {{
  const sleep = (ms) => new Promise(r => setTimeout(r, ms));
  const editor = document.querySelector(""div[role='textbox']"") || document.querySelector(""[contenteditable='true']"");
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
            try
            {
                var r = (await Web.CoreWebView2.ExecuteScriptAsync(js))?.Trim();
                return string.Equals(r, "true", StringComparison.OrdinalIgnoreCase);
            }
            catch { return false; }
        }

        private void ReloadBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Web.CoreWebView2 != null) Web.CoreWebView2.Reload();
                else Web.Reload();
            }
            catch { }
        }

        private async void GoChannelBtn_Click(object sender, RoutedEventArgs e)
        {
            await NavigateToChannelAsync(GetChannelUrl());
        }

        private async void StartBtn_Click(object sender, RoutedEventArgs e)
        {
            _log.Info("Start button clicked");

            // If engine already running, ignore per user instruction
            if (_engine != null && (_engine.IsRunning))
            {
                _log.Info("Engine already running, Start ignored.");
                return;
            }

            int area = SafeInt(AreaBox.Text, 10);
            int hunt = SafeInt(HuntCdBox.Text, 21000);
            int work = SafeInt(WorkCdBox.Text, 99000);
            int farm = SafeInt(FarmCdBox.Text, 196000);

            _engine = new BotEngine(Web, area, hunt, work, farm);

            // Wire events BEFORE any sends to ensure we log the immediate 'rpg cd'
            _engine.OnCommandSent += (cmd) =>
            {
                UiDispatcher.OnUI(() =>
                {
                    _log.Command($"Message ({cmd}) sent");
                    if (!string.IsNullOrWhiteSpace(cmd) &&
                        cmd.Trim().StartsWith("rpg hunt", StringComparison.OrdinalIgnoreCase))
                    {
                        _huntCount++;
                        if (_huntCountText != null)
                            _huntCountText.Text = $"Hunt sent: {_huntCount}";
                    }
                });
            };

            // Immediately send "rpg cd" before starting timers
            var sent = await _engine.SendImmediateAsync("rpg cd");
            _log.Info(sent ? "Sent 'rpg cd' immediately." : "Failed to send 'rpg cd'.");

            // Then start engine timers and opening sequence (hunt/work[/farm])
            _engine.Start();
            _log.Engine("Engine started (timers running; hunt/work[/farm] scheduled)");
        }

        private void StopBtn_Click(object sender, RoutedEventArgs e)
        {
            _engine?.Stop();
            _log.Engine("Engine stopped");
        }

        private int SafeInt(string s, int def)
        {
            return int.TryParse(s, out var v) ? v : def;
        }

        // ------------------------- Cooldowns (visual -> functional) -------------------------

        private class CooldownEntry
        {
            public TextBlock Label { get; }
            public TimeSpan? Remaining { get; set; }

            public CooldownEntry(TextBlock label)
            {
                Label = label;
                Remaining = null;
            }
        }

        private void InitCooldownTracking()
        {
            TextBlock T(string name) => (TextBlock)FindName(name);

            // Canonical -> label
            // Canonical keys
            _cooldowns["daily"]      = new CooldownEntry(T("DailyCdText"));
            _cooldowns["weekly"]     = new CooldownEntry(T("WeeklyCdText"));
            _cooldowns["lootbox"]    = new CooldownEntry(T("LootboxCdText"));
            _cooldowns["card_hand"]  = new CooldownEntry(T("CardHandCdText"));
            _cooldowns["vote"]       = new CooldownEntry(T("VoteCdText"));

            _cooldowns["hunt"]       = new CooldownEntry(T("HuntCdText"));
            _cooldowns["adventure"]  = new CooldownEntry(T("AdventureCdText"));
            _cooldowns["training"]   = new CooldownEntry(T("TrainingCdText"));
            _cooldowns["duel"]       = new CooldownEntry(T("DuelCdText"));
            _cooldowns["quest"]      = new CooldownEntry(T("QuestCdText"));

            _cooldowns["work"]       = new CooldownEntry(T("WorkCdText"));
            _cooldowns["farm"]       = new CooldownEntry(T("FarmCdText"));
            _cooldowns["horse"]      = new CooldownEntry(T("HorseCdText"));
            _cooldowns["arena"]      = new CooldownEntry(T("ArenaCdText"));
            _cooldowns["dungeon"]    = new CooldownEntry(T("DungeonCdText"));

            // Aliases from message -> canonical keys
            void Map(string alias, string canonical) => _aliasToCanonical[alias] = canonical;

            Map("daily", "daily");
            Map("weekly", "weekly");
            Map("lootbox", "lootbox");
            Map("card hand", "card_hand");
            Map("vote", "vote");

            Map("hunt", "hunt");
            Map("adventure", "adventure");
            Map("training", "training");
            Map("duel", "duel");
            Map("quest", "quest");
            Map("epic quest", "quest");

            Map("chop", "work");
            Map("fish", "work");
            Map("pickup", "work");
            Map("mine", "work");

            Map("farm", "farm");

            Map("horse breeding", "horse");
            Map("horse race", "horse");

            Map("arena", "arena");

            Map("dungeon", "dungeon");
            Map("miniboss", "dungeon");

            // 1-second ticking to decrement visible timers
            _cooldownTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _cooldownTimer.Tick += (s, e) =>
            {
                foreach (var entry in _cooldowns.Values.Distinct())
                {
                    if (entry.Label == null) continue;

                    if (entry.Remaining.HasValue)
                    {
                        var left = entry.Remaining.Value - TimeSpan.FromSeconds(1);
                        if (left <= TimeSpan.Zero)
                        {
                            entry.Remaining = null; // ready
                            UpdateCooldownLabel(entry.Label, null);
                        }
                        else
                        {
                            entry.Remaining = left;
                            UpdateCooldownLabel(entry.Label, left);
                        }
                    }
                }
            };
            _cooldownTimer.Start();
        }

        private void TryParseCooldowns(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;
            if (message.IndexOf("cooldowns", StringComparison.OrdinalIgnoreCase) < 0) return;

            var lines = message.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var raw in lines)
            {
                var line = raw.Trim();
                if (string.IsNullOrEmpty(line)) continue;

                // Skip headings
                var header = line.ToLowerInvariant();
                if (header.Contains("cooldowns") || header == "rewards" || header == "experience" || header == "progress")
                    continue;

                // Extract "names (time)" where names can be "a | b | c"
                var match = Regex.Match(line, @"^\s*[~`\-\*]*\s*(.+?)\s*(?:\(([^)]+)\))?\s*$");
                if (!match.Success) continue;

                var namesPart = match.Groups[1].Value.Trim();
                var timePart = match.Groups[2].Success ? match.Groups[2].Value.Trim() : null;

                // For each alias in the names, update its canonical cooldown
                TimeSpan? dur = ParseDuration(timePart); // null => Ready
                foreach (var alias in namesPart.Split('|'))
                {
                    var a = alias.Trim().ToLowerInvariant();
                    if (_aliasToCanonical.TryGetValue(a, out var canonical) && _cooldowns.TryGetValue(canonical, out var entry))
                    {
                        entry.Remaining = dur.HasValue && dur.Value > TimeSpan.Zero ? dur : null;
                        UpdateCooldownLabel(entry.Label, entry.Remaining);
                    }
                }
            }
        }

        private static TimeSpan? ParseDuration(string time)
        {
            if (string.IsNullOrWhiteSpace(time)) return null; // Ready
            var matches = Regex.Matches(time, @"(\d+)\s*([dhms])", RegexOptions.IgnoreCase);
            if (matches.Count == 0) return null;

            int d = 0, h = 0, m = 0, s = 0;
            foreach (Match mt in matches)
            {
                var val = int.Parse(mt.Groups[1].Value);
                switch (mt.Groups[2].Value.ToLowerInvariant())
                {
                    case "d": d = val; break;
                    case "h": h = val; break;
                    case "m": m = val; break;
                    case "s": s = val; break;
                }
            }

            try { return new TimeSpan(d, h, m, s); }
            catch { return null; }
        }

        private static string FormatDuration(TimeSpan ts)
        {
            var parts = new List<string>();
            if (ts.Days > 0) parts.Add($"{ts.Days}d");
            if (ts.Days > 0 || ts.Hours > 0) parts.Add($"{ts.Hours}h");
            if (ts.Days > 0 || ts.Hours > 0 || ts.Minutes > 0) parts.Add($"{ts.Minutes}m");
            parts.Add($"{ts.Seconds}s");
            return string.Join(" ", parts);
        }

        private void UpdateCooldownLabel(TextBlock tb, TimeSpan? remaining)
        {
            if (tb == null) return;
            tb.Text = remaining.HasValue ? FormatDuration(remaining.Value) : "Ready";
        }

        // Left header buttons
        private void LastMessagesTabBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_lastMessagesPanel != null) _lastMessagesPanel.Visibility = Visibility.Visible;
            StatsPanel.Visibility = Visibility.Collapsed;
            ConsolePanel.Visibility = Visibility.Collapsed;
        }

        private void StatsTabBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_lastMessagesPanel != null) _lastMessagesPanel.Visibility = Visibility.Collapsed;
            StatsPanel.Visibility = Visibility.Visible;
            ConsolePanel.Visibility = Visibility.Collapsed;
        }

        private void ConsoleTabBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_lastMessagesPanel != null) _lastMessagesPanel.Visibility = Visibility.Collapsed;
            StatsPanel.Visibility = Visibility.Collapsed;
            ConsolePanel.Visibility = Visibility.Visible;
        }
    }
}