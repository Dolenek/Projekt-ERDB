using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EpicRPGBot.UI.Models;
using EpicRPGBot.UI.Services;

namespace EpicRPGBot.UI.Duel
{
    public sealed class DuelProfileLoader
    {
        private readonly IDiscordChatClient _chatClient;
        private readonly ConfirmedCommandSender _commandSender;
        private readonly AppSettingsService _settingsService;

        public DuelProfileLoader(
            IDiscordChatClient chatClient,
            ConfirmedCommandSender commandSender,
            AppSettingsService settingsService)
        {
            _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
            _commandSender = commandSender ?? throw new ArgumentNullException(nameof(commandSender));
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        }

        public async Task<DuelProfileContext> LoadAsync(
            Action<string> report,
            CancellationToken cancellationToken)
        {
            report?.Invoke("Loading profile with rpg p in the current Bot tab channel.");
            report?.Invoke("Sending message in the current Bot tab channel: 'rpg p'.");
            var result = await _commandSender.SendAsync("rpg p", cancellationToken: cancellationToken);
            if (!result.IsConfirmed || !TryParseProfile(result.ReplyMessage, out var playerName, out var level))
            {
                report?.Invoke("Profile reply did not contain a valid player name and level.");
                return null;
            }

            var outgoing = await EnrichOutgoingAsync(result.OutgoingMessage);
            SaveProfileName(playerName);
            report?.Invoke($"Profile ready: {playerName}, level {level}.");
            return new DuelProfileContext(playerName, level, outgoing?.AuthorId, outgoing?.Author);
        }

        private static bool TryParseProfile(
            DiscordMessageSnapshot reply,
            out string playerName,
            out int level)
        {
            var text = (reply?.RenderedText ?? string.Empty) + "\n" + (reply?.Text ?? string.Empty);
            playerName = string.Empty;
            level = 0;
            var hasName = ProfileMessageParser.TryParsePlayerName(text, out playerName);
            var hasLevel = ProfileMessageParser.TryParseLevel(text, out level);
            return hasName && hasLevel;
        }

        private async Task<DiscordMessageSnapshot> EnrichOutgoingAsync(DiscordMessageSnapshot outgoing)
        {
            if (outgoing == null || string.IsNullOrWhiteSpace(outgoing.Id))
            {
                return outgoing;
            }

            var messages = await _chatClient.GetRecentMessagesAsync(20);
            return messages.FirstOrDefault(message => message.Id == outgoing.Id) ?? outgoing;
        }

        private void SaveProfileName(string playerName)
        {
            var current = _settingsService.LoadCurrent();
            if (!string.Equals(current.ProfilePlayerName, playerName, StringComparison.OrdinalIgnoreCase))
            {
                _settingsService.Save(current.WithProfilePlayerName(playerName));
            }
        }
    }
}
