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
        private readonly GuildRaidOutcomeWatch _outcomeWatch = new GuildRaidOutcomeWatch();
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
        public event Action<GuardAlertNotification> OnGuardNotification;

        public async Task StartAsync()
        {
            if (_started)
            {
                return;
            }

            _started = true;
            try
            {
                await ApplySettingsAsync(_getCurrentSettings());
            }
            catch
            {
                _started = false;
                throw;
            }
        }

        public void Stop()
        {
            _poller.Stop();
            _started = false;
            ResetInactiveState();
        }

        public async Task ApplySettingsAsync(AppSettingsSnapshot settings)
        {
            await _sendGate.WaitAsync();
            try
            {
                await ApplySettingsCoreAsync(settings ?? AppSettingsSnapshot.Default);
            }
            finally
            {
                _sendGate.Release();
            }
        }

        private async Task ApplySettingsCoreAsync(AppSettingsSnapshot settings)
        {
            _currentSettings = settings;
            if (!_started)
            {
                return;
            }

            if (!TryResolveActiveChannel(out var channelUrl))
            {
                return;
            }

            if (!string.Equals(_activeChannelUrl, channelUrl, StringComparison.OrdinalIgnoreCase))
            {
                await _chatClient.NavigateToChannelAsync(channelUrl);
                _activeChannelUrl = channelUrl;
                _processor.Reset();
                _outcomeWatch.Reset();
                _poller.ResetCursor();
                _poller.SkipNextDetectedMessage();
            }

            _poller.Start();
            ReportState("watching", "Guild raid watcher ready.");
        }

        private bool TryResolveActiveChannel(out string channelUrl)
        {
            channelUrl = string.Empty;
            if (!_currentSettings.GuildRaidWatcherActive)
            {
                SetIdle("disabled", "Guild raid watcher disabled.");
                return false;
            }

            if (!_currentSettings.IsGuildRaidConfigured())
            {
                SetIdle("idle-incomplete", "Guild raid watcher idle: channel URL and trigger text are required.");
                return false;
            }

            if (!_currentSettings.TryResolveGuildRaidChannelUrl(out channelUrl))
            {
                SetIdle("idle-invalid-url", "Guild raid watcher idle: enter a Discord channel URL.");
                return false;
            }

            return true;
        }

        private void SetIdle(string state, string message)
        {
            _poller.Stop();
            ResetInactiveState();
            ReportState(state, message);
        }

        private void ResetInactiveState()
        {
            _activeChannelUrl = string.Empty;
            _processor.Reset();
            _outcomeWatch.Reset();
            _poller.ResetCursor();
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
                    !_currentSettings.GuildRaidWatcherActive ||
                    !_currentSettings.IsGuildRaidConfigured() ||
                    !_currentSettings.TryResolveGuildRaidChannelUrl(out var channelUrl) ||
                    string.IsNullOrWhiteSpace(_activeChannelUrl) ||
                    !string.Equals(_activeChannelUrl, channelUrl, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                if (TryHandlePendingOutcome(snapshot))
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
                    _outcomeWatch.Arm();
                    OnInfo?.Invoke($"Matched message {snapshot.Id} and sent 'rpg guild raid'; watching for guard or raid confirmation.");
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

        private bool TryHandlePendingOutcome(DiscordMessageSnapshot snapshot)
        {
            switch (_outcomeWatch.Observe(snapshot))
            {
                case GuildRaidOutcomeState.Inactive:
                    return false;
                case GuildRaidOutcomeState.Guarded:
                    OnInfo?.Invoke("EPIC GUARD detected on the Guild tab after 'rpg guild raid'.");
                    OnGuardNotification?.Invoke(new GuardAlertNotification(
                        GuardAlertKind.FirstDetected,
                        "EPIC GUARD detected on the Guild tab after 'rpg guild raid'."));
                    return true;
                case GuildRaidOutcomeState.Confirmed:
                    OnInfo?.Invoke("Guild raid confirmation received; guard watch cleared.");
                    return true;
                default:
                    return true;
            }
        }
    }
}
