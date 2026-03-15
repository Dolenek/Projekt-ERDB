using System;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace EpicRPGBot.UI.Services
{
    public sealed class ChatMessagePoller
    {
        private readonly IDiscordChatClient _chatClient;
        private readonly DispatcherTimer _timer;
        private string _previousMessage = string.Empty;

        public ChatMessagePoller(IDiscordChatClient chatClient, TimeSpan? interval = null)
        {
            _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
            _timer = new DispatcherTimer
            {
                Interval = interval ?? TimeSpan.FromSeconds(2)
            };

            _timer.Tick += async (sender, args) => await OnTickAsync();
        }

        public event Action<string> MessageDetected;

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
                var message = await _chatClient.GetLastMessageTextAsync();
                if (string.IsNullOrWhiteSpace(message) || string.Equals(message, _previousMessage, StringComparison.Ordinal))
                {
                    return;
                }

                _previousMessage = message;
                MessageDetected?.Invoke(message);
            }
            catch
            {
            }
        }
    }
}
