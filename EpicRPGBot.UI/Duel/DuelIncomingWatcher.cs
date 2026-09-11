using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EpicRPGBot.UI.Models;
using EpicRPGBot.UI.Services;

namespace EpicRPGBot.UI.Duel
{
    public sealed class DuelIncomingWatcher
    {
        private const int RecentMessageCount = 40;
        private const int MentionPollDelayMs = 300;
        private const int PromptPollDelayMs = 250;
        private static readonly TimeSpan PromptWaitTimeout = TimeSpan.FromSeconds(10);
        private readonly IDuelDiscordClient _chatClient;
        private readonly DuelMessageParser _messageParser;
        private readonly Func<DateTimeOffset> _utcNow;

        public DuelIncomingWatcher(
            IDuelDiscordClient chatClient,
            DuelMessageParser messageParser,
            Func<DateTimeOffset> utcNow = null)
        {
            _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
            _messageParser = messageParser ?? throw new ArgumentNullException(nameof(messageParser));
            _utcNow = utcNow ?? (() => DateTimeOffset.UtcNow);
        }

        public async Task<IDictionary<string, int>> CaptureMentionBaselineAsync(
            IReadOnlyList<DiscordChannelReference> channels,
            CancellationToken cancellationToken)
        {
            await _chatClient.DiscoverCategoryChannelsAsync(
                DuelChannelCatalog.CategoryId,
                cancellationToken);
            var channelIds = channels.Select(channel => channel.Id).ToArray();
            var current = await _chatClient.GetChannelMentionCountsAsync(channelIds, cancellationToken);
            return channelIds.ToDictionary(
                channelId => channelId,
                channelId => ReadCount(current, channelId),
                StringComparer.Ordinal);
        }

        public async Task<DuelIncomingRequest> WaitForRequestAsync(
            IReadOnlyList<DiscordChannelReference> channels,
            DiscordChannelReference parkingChannel,
            IDictionary<string, int> mentionBaseline,
            string playerName,
            DateTimeOffset notBeforeUtc,
            ISet<string> processedSignatures,
            Action<string> report,
            CancellationToken cancellationToken)
        {
            while (true)
            {
                var signaledChannels = await WaitForNewMentionsAsync(
                    channels, mentionBaseline, cancellationToken);
                report?.Invoke("New mention badge detected in " +
                               string.Join(", ", signaledChannels.Select(channel => "#" + channel.Name)) + ".");
                var request = await WaitForPromptAsync(
                    signaledChannels,
                    playerName,
                    notBeforeUtc,
                    processedSignatures,
                    cancellationToken);
                if (request != null)
                {
                    report?.Invoke($"Incoming duel found in #{request.Channel.Name}.");
                    return request;
                }

                report?.Invoke("Mention did not produce an active duel request; continuing to wait.");
                await EnsureChannelLoadedAsync(parkingChannel, cancellationToken);
            }
        }

        private async Task<IReadOnlyList<DiscordChannelReference>> WaitForNewMentionsAsync(
            IReadOnlyList<DiscordChannelReference> channels,
            IDictionary<string, int> baseline,
            CancellationToken cancellationToken)
        {
            var channelIds = channels.Select(channel => channel.Id).ToArray();
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var current = await _chatClient.GetChannelMentionCountsAsync(channelIds, cancellationToken);
                var signaled = channels.Where(channel =>
                    ReadCount(current, channel.Id) > ReadBaselineCount(baseline, channel.Id)).ToArray();
                foreach (var channelId in channelIds)
                {
                    baseline[channelId] = ReadCount(current, channelId);
                }

                if (signaled.Length > 0)
                {
                    return signaled;
                }

                await Task.Delay(MentionPollDelayMs, cancellationToken);
            }
        }

        private async Task<DuelIncomingRequest> WaitForPromptAsync(
            IReadOnlyList<DiscordChannelReference> channels,
            string playerName,
            DateTimeOffset notBeforeUtc,
            ISet<string> processedSignatures,
            CancellationToken cancellationToken)
        {
            var deadline = _utcNow() + PromptWaitTimeout;
            var currentChannelId = string.Empty;
            do
            {
                var requests = new List<DuelIncomingRequest>();
                foreach (var channel in channels)
                {
                    if (!string.Equals(currentChannelId, channel.Id, StringComparison.Ordinal))
                    {
                        await EnsureChannelLoadedAsync(channel, cancellationToken);
                        currentChannelId = channel.Id;
                    }

                    requests.AddRange(await ReadCurrentRequestsAsync(
                        channel, playerName, notBeforeUtc, processedSignatures));
                }

                var first = requests
                    .OrderBy(request => request.Classification.Message.CreatedAtUtc ?? DateTimeOffset.MaxValue)
                    .ThenBy(request => request.Classification.Message.Id, StringComparer.Ordinal)
                    .FirstOrDefault();
                if (first != null)
                {
                    return first;
                }

                await Task.Delay(PromptPollDelayMs, cancellationToken);
            }
            while (_utcNow() < deadline);

            return null;
        }

        private async Task<IReadOnlyList<DuelIncomingRequest>> ReadCurrentRequestsAsync(
            DiscordChannelReference channel,
            string playerName,
            DateTimeOffset notBeforeUtc,
            ISet<string> processedSignatures)
        {
            var messages = await _chatClient.GetRecentMessagesAsync(RecentMessageCount);
            var requests = new List<DuelIncomingRequest>();
            foreach (var message in messages)
            {
                var classification = _messageParser.Parse(message);
                if (classification.Kind != DuelMessageKind.IncomingRequest ||
                    !classification.Targets(playerName) ||
                    !message.CreatedAtUtc.HasValue || message.CreatedAtUtc.Value < notBeforeUtc)
                {
                    continue;
                }

                var signature = DuelMessageSequence.Signature(classification);
                if (processedSignatures.Add(signature))
                {
                    requests.Add(new DuelIncomingRequest(channel, classification));
                }
            }

            return requests;
        }

        private async Task EnsureChannelLoadedAsync(
            DiscordChannelReference channel,
            CancellationToken cancellationToken)
        {
            if (!await _chatClient.NavigateToChannelAndWaitAsync(channel.Url, cancellationToken))
            {
                throw new InvalidOperationException($"Duel channel '#{channel.Name}' did not load.");
            }
        }

        private static int ReadCount(IReadOnlyDictionary<string, int> counts, string channelId)
        {
            return counts != null && counts.TryGetValue(channelId, out var count) ? count : 0;
        }

        private static int ReadBaselineCount(IDictionary<string, int> counts, string channelId)
        {
            return counts != null && counts.TryGetValue(channelId, out var count) ? count : 0;
        }
    }
}
