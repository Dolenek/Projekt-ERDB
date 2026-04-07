using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EpicRPGBot.UI.Models;
using EpicRPGBot.UI.Services;

namespace EpicRPGBot.Tests.Dungeon
{
    internal static class DungeonTestData
    {
        public const string TestPlayerName = "testplayer";
        public const string TestDisplayMention = "@TestDisplay";
    }

    internal sealed class FakeDungeonChatClient : IDiscordChatClient
    {
        private const string ArmyHelperDm = "army-helper-dm";
        private const string DungeonChannel = "dungeon-channel";
        private const string HomeUrl = "https://discord.com/channels/@me";
        private const string ProfileReplyText = @"testplayer — profile
this is the best title
PROGRESS
Area: 7 (Max: 7)";
        private readonly List<DiscordMessageSnapshot> _armyHelperMessages = new List<DiscordMessageSnapshot>();
        private readonly List<DiscordMessageSnapshot> _dungeonMessages = new List<DiscordMessageSnapshot>();
        private readonly List<DiscordMessageSnapshot> _signupMessages = new List<DiscordMessageSnapshot>();
        private readonly Dictionary<string, string> _commandsByOutgoingId = new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly List<string> _sentCommands = new List<string>();
        private readonly List<string> _sentMessages = new List<string>();
        private readonly bool _inviteAlreadyVisible;
        private readonly bool _useSnowflakeIds;
        private int _armyHelperReadCount;
        private int _messageSequence;
        private string _location = "signup";

        public FakeDungeonChatClient(bool inviteAlreadyVisible = false, bool useSnowflakeIds = false)
        {
            _inviteAlreadyVisible = inviteAlreadyVisible;
            _useSnowflakeIds = useSnowflakeIds;
            _armyHelperMessages.Add(CreateSnapshot("Army Helper", "waiting", "Old invite placeholder"));
            if (_inviteAlreadyVisible)
            {
                _armyHelperMessages.Add(CreateTakeMeThereMessage());
            }
        }

        public bool IsReady => true;
        public bool DeleteClicked { get; private set; }
        public IReadOnlyList<string> SentCommands => _sentCommands;
        public IReadOnlyList<string> SentMessages => _sentMessages;

        public Task EnsureInitializedAsync() => Task.CompletedTask;

        public void Reload()
        {
        }

        public Task NavigateToChannelAsync(string url)
        {
            _location = string.Equals(url, HomeUrl, StringComparison.OrdinalIgnoreCase) ? HomeUrl : "signup";
            return Task.CompletedTask;
        }

        public Task<string> GetLastMessageTextAsync()
        {
            return Task.FromResult(GetVisibleMessages().LastOrDefault()?.Text ?? string.Empty);
        }

        public Task<DiscordMessageSnapshot> GetLatestMessageAsync()
        {
            return Task.FromResult(GetVisibleMessages().LastOrDefault() ?? new DiscordMessageSnapshot(string.Empty, string.Empty, string.Empty));
        }

        public Task<IReadOnlyList<DiscordMessageSnapshot>> GetRecentMessagesAsync(int maxCount)
        {
            var messages = GetVisibleMessages();
            if (_location == ArmyHelperDm)
            {
                _armyHelperReadCount++;
                if (!_inviteAlreadyVisible && _armyHelperReadCount == 2)
                {
                    _armyHelperMessages.Add(CreateTakeMeThereMessage());
                    messages = GetVisibleMessages();
                }
            }

            return Task.FromResult<IReadOnlyList<DiscordMessageSnapshot>>(messages.TakeLast(maxCount).ToArray());
        }

        public Task<DiscordMessageSnapshot> GetEpicReplyAfterMessageAsync(string outgoingMessageId)
        {
            if (!_commandsByOutgoingId.TryGetValue(outgoingMessageId ?? string.Empty, out var command))
            {
                return Task.FromResult<DiscordMessageSnapshot>(null);
            }

            if (string.Equals(command, "rpg p", StringComparison.OrdinalIgnoreCase))
            {
                var reply = CreateSnapshot("EPIC RPG", ProfileReplyText, ProfileReplyText);
                _signupMessages.Add(reply);
                return Task.FromResult(reply);
            }

            if (string.Equals(command, "rpg dung <@222>", StringComparison.OrdinalIgnoreCase))
            {
                var reply = CreateSnapshot(
                    "EPIC RPG",
                    "ARE YOU SURE YOU WANT TO ENTER?\nAll players have to say 'yes'",
                    "ARE YOU SURE YOU WANT TO ENTER?\nAll players have to say 'yes'",
                    new[]
                    {
                        new DiscordMessageButton("yes", 0, 0),
                        new DiscordMessageButton("no", 0, 1)
                    });
                _dungeonMessages.Add(reply);
                return Task.FromResult(reply);
            }

            return Task.FromResult<DiscordMessageSnapshot>(null);
        }

        public Task<bool> OpenDirectMessageAsync(string conversationName, CancellationToken cancellationToken = default)
        {
            _location = ArmyHelperDm;
            return Task.FromResult(true);
        }

        public Task<DiscordMessageSnapshot> SendMessageAndWaitForOutgoingAsync(string message, CancellationToken cancellationToken = default)
        {
            var outgoing = CreateSnapshot(DungeonTestData.TestPlayerName, message, message);
            GetMutableVisibleMessages().Add(outgoing);
            _commandsByOutgoingId[outgoing.Id] = message;
            _sentCommands.Add(message);
            return Task.FromResult(outgoing);
        }

        public Task<bool> SendMessageAsync(string message, CancellationToken cancellationToken = default)
        {
            _sentMessages.Add(message);
            _dungeonMessages.Add(CreateSnapshot(DungeonTestData.TestPlayerName, message, message));
            _dungeonMessages.Add(CreateSnapshot(
                "EPIC RPG",
                "THE xd DRAGON DIED, ALL PLAYERS WON",
                "THE xd DRAGON DIED, ALL PLAYERS WON"));
            _dungeonMessages.Add(CreateSnapshot(
                "Army Helper",
                "Thanks for using our dungeon system.",
                "Thanks for using our dungeon system.",
                new[] { new DiscordMessageButton("Delete dungeon channel", 0, 0) }));
            return Task.FromResult(true);
        }

        public Task<bool> ClickMessageButtonAsync(string messageId, int rowIndex, int columnIndex, CancellationToken cancellationToken = default)
        {
            var snapshot = GetVisibleMessages().FirstOrDefault(message => string.Equals(message.Id, messageId, StringComparison.Ordinal));
            var label = snapshot?.Buttons?.FirstOrDefault(button => button.RowIndex == rowIndex && button.ColumnIndex == columnIndex)?.Label ?? string.Empty;
            if (string.Equals(label, "Take me there", StringComparison.OrdinalIgnoreCase))
            {
                _location = DungeonChannel;
                _dungeonMessages.Add(CreateSnapshot(
                    "Army Helper",
                    "Dungeon 6 commands",
                    "Dungeon 6 commands\nPlayers listed\n" + DungeonTestData.TestDisplayMention + " - " + DungeonTestData.TestPlayerName + "\n@partner - partner",
                    mentions: new[]
                    {
                        new DiscordMessageMention(DungeonTestData.TestDisplayMention, "111"),
                        new DiscordMessageMention("@partner", "222")
                    }));
                return Task.FromResult(true);
            }

            if (string.Equals(label, "yes", StringComparison.OrdinalIgnoreCase))
            {
                _dungeonMessages.Add(CreateSnapshot(
                    "EPIC RPG",
                    "YOU HAVE ENCOUNTERED THE xd DRAGON\nit's " + DungeonTestData.TestPlayerName + "'s turn!",
                    "YOU HAVE ENCOUNTERED THE xd DRAGON\nit's " + DungeonTestData.TestPlayerName + "'s turn!",
                    new[]
                    {
                        new DiscordMessageButton("BITE", 0, 0),
                        new DiscordMessageButton("STAB", 0, 1)
                    }));
                return Task.FromResult(true);
            }

            if (string.Equals(label, "Delete dungeon channel", StringComparison.OrdinalIgnoreCase))
            {
                DeleteClicked = true;
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }

        public Task<string> GetCaptchaImageUrlForMessageIdAsync(string messageId)
        {
            return Task.FromResult(string.Empty);
        }

        public Task<byte[]> CaptureMessageImagePngAsync(string messageId)
        {
            return Task.FromResult(Array.Empty<byte>());
        }

        private IReadOnlyList<DiscordMessageSnapshot> GetVisibleMessages()
        {
            if (_location == ArmyHelperDm)
            {
                return _armyHelperMessages;
            }

            if (_location == DungeonChannel)
            {
                return _dungeonMessages;
            }

            return _signupMessages;
        }

        private List<DiscordMessageSnapshot> GetMutableVisibleMessages()
        {
            if (_location == ArmyHelperDm)
            {
                return _armyHelperMessages;
            }

            if (_location == DungeonChannel)
            {
                return _dungeonMessages;
            }

            return _signupMessages;
        }

        private DiscordMessageSnapshot CreateTakeMeThereMessage()
        {
            return CreateSnapshot(
                "Army Helper",
                "Your dungeon partner is waiting for you.",
                "Your dungeon partner is waiting for you.",
                new[] { new DiscordMessageButton("Take me there", 0, 0) });
        }

        private DiscordMessageSnapshot CreateSnapshot(
            string author,
            string text,
            string renderedText,
            IReadOnlyList<DiscordMessageButton> buttons = null,
            IReadOnlyList<DiscordMessageMention> mentions = null)
        {
            _messageSequence++;
            var id = _useSnowflakeIds ? CreateSnowflakeId(_messageSequence) : "m" + _messageSequence;
            return new DiscordMessageSnapshot(id, text, author, renderedText, buttons, mentions);
        }

        private static string CreateSnowflakeId(int sequence)
        {
            const long discordEpochMs = 1420070400000L;
            var timestampMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - discordEpochMs;
            var snowflake = ((ulong)timestampMs << 22) | (uint)sequence;
            return "chat-messages-1264680016491581502-" + snowflake;
        }
    }
}
