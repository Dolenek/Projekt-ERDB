using System;
using System.Threading;
using System.Threading.Tasks;
using EpicRPGBot.UI.Models;

namespace EpicRPGBot.UI.Services
{
    public sealed class GuildRaidCoordinator : IDisposable
    {
        private readonly IDiscordChatClient _chatClient;
        private readonly Func<AppSettingsSnapshot> _getCurrentSettings;
        private readonly ChatMessagePoller _poller;
        private readonly GuildRaidTriggerProcessor _processor = new GuildRaidTriggerProcessor();
        private readonly SemaphoreSlim _sendGate = new SemaphoreSlim(1, 1);

        private string _activeChannelUrl = string.Empty;
        private string _watchState = string.Empty;
        private AppSettingsSnapshot _currentSettings;
        private bool _started;

        public GuildRaidCoordinator(
            IDiscordChatClient chatClient,
            Func<AppSettingsSnapshot> getCurrentSettings,
            TimeSpan? interval = null)
        {
            _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
            _getCurrentSettings = getCurrentSettings ?? throw new ArgumentNullException(nameof(getCurrentSettings));
            _poller = new ChatMessagePoller(_chatClient, interval);
            _poller.MessageDetected += OnMessageDetected;
            _currentSettings = _getCurrentSettings();
        }

        public event Action<string> OnInfo;

        public async Task StartAsync()
        {
            if (_started)
            {
                return;
            }

            _started = true;
            await ApplySettingsAsync(_getCurrentSettings());
            _poller.Start();
        }

        public void Stop()
        {
            _poller.Stop();
            _started = false;
        }

        public async Task ApplySettingsAsync(AppSettingsSnapshot settings)
        {
            _currentSettings = settings ?? AppSettingsSnapshot.Default;
            if (!_started)
            {
                return;
            }

            if (!_currentSettings.IsGuildRaidConfigured())
            {
                _activeChannelUrl = string.Empty;
                _processor.Reset();
                await _poller.CaptureCurrentMessageAsBaselineAsync();
                ReportState("idle-incomplete", "Guild raid watcher idle: channel URL and trigger text are required.");
                return;
            }

            if (!_currentSettings.TryResolveGuildRaidChannelUrl(out var channelUrl))
            {
                _activeChannelUrl = string.Empty;
                _processor.Reset();
                await _poller.CaptureCurrentMessageAsBaselineAsync();
                ReportState("idle-invalid-url", "Guild raid watcher idle: enter a Discord channel URL.");
                return;
            }

            if (!string.Equals(_activeChannelUrl, channelUrl, StringComparison.OrdinalIgnoreCase))
            {
                await _chatClient.NavigateToChannelAsync(channelUrl);
                _activeChannelUrl = channelUrl;
                _processor.Reset();
                _poller.ResetCursor();
                _poller.SkipNextDetectedMessage();
            }

            ReportState("watching", "Guild raid watcher ready.");
        }

        public void Dispose()
        {
            Stop();
            _sendGate.Dispose();
        }

        private async void OnMessageDetected(DiscordMessageSnapshot snapshot)
        {
            try
            {
                await HandleMessageDetectedAsync(snapshot);
            }
            catch
            {
            }
        }

        private async Task HandleMessageDetectedAsync(DiscordMessageSnapshot snapshot)
        {
            await _sendGate.WaitAsync();
            try
            {
                if (!_started ||
                    !_currentSettings.IsGuildRaidConfigured() ||
                    !_currentSettings.TryResolveGuildRaidChannelUrl(out var channelUrl) ||
                    string.IsNullOrWhiteSpace(_activeChannelUrl) ||
                    !string.Equals(_activeChannelUrl, channelUrl, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                if (!_processor.ShouldTrigger(_currentSettings, snapshot))
                {
                    return;
                }

                var sent = await _chatClient.SendMessageAsync("rpg guild raid");
                if (sent)
                {
                    OnInfo?.Invoke($"Matched message {snapshot.Id} and sent 'rpg guild raid'.");
                    return;
                }

                OnInfo?.Invoke($"Matched message {snapshot.Id}, but failed to send 'rpg guild raid'.");
            }
            finally
            {
                _sendGate.Release();
            }
        }

        private void ReportState(string state, string message)
        {
            if (string.Equals(_watchState, state, StringComparison.Ordinal))
            {
                return;
            }

            _watchState = state;
            OnInfo?.Invoke(message);
        }
    }
}
