using System;
using System.Threading.Tasks;
using System.Windows.Threading;
using EpicRPGBot.UI.Models;

namespace EpicRPGBot.UI.Services
{
    public sealed class ChatMessagePoller
    {
        private readonly IDiscordChatClient _chatClient;
        private readonly DispatcherTimer _timer;
        private string _previousMessageId = string.Empty;

        public ChatMessagePoller(IDiscordChatClient chatClient, TimeSpan? interval = null)
        {
            _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
            _timer = new DispatcherTimer
            {
                Interval = interval ?? TimeSpan.FromSeconds(2)
            };

            _timer.Tick += async (sender, args) => await OnTickAsync();
        }

        public event Action<DiscordMessageSnapshot> MessageDetected;

        public void Start()
        {
            _timer.Start();
        }

        public void Stop()
        {
            _timer.Stop();
        }

        private async Task OnTickAsync()
        {
            try
            {
                var snapshot = await _chatClient.GetLatestMessageAsync();
                if (snapshot == null ||
                    string.IsNullOrWhiteSpace(snapshot.Id) ||
                    string.Equals(snapshot.Id, _previousMessageId, StringComparison.Ordinal))
                {
                    return;
                }

                _previousMessageId = snapshot.Id;
                MessageDetected?.Invoke(snapshot);
            }
            catch
            {
            }
        }
    }
}
