using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using EpicRPGBot.UI.Models;
using EpicRPGBot.UI.Services;

namespace EpicRPGBot.UI.Pets
{
    public sealed class DiscordPetGateway : IPetGateway
    {
        private readonly IDiscordChatClient _client;
        private readonly Func<string, CancellationToken, Task<ConfirmedCommandSendResult>> _send;
        private string _owner;

        public DiscordPetGateway(IDiscordChatClient client,
            Func<string, CancellationToken, Task<ConfirmedCommandSendResult>> send, string owner)
        {
            _client = client;
            _send = send;
            _owner = owner;
        }

        public async Task<PetInventory> LoadAsync(CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_owner)) await ReadOwnerAsync(cancellationToken);
            var reply = await SendCheckedAsync("rpg pets", cancellationToken);
            var first = ParsePage(reply);
            if (first == null || first.Number != 1 || first.Owner != _owner)
                throw new InvalidOperationException("Cannot identify the first pet page for this player.");
            var collected = new List<PetRecord>(first.Pets);
            for (var page = 2; page <= first.Pages; page++)
            {
                reply = await NextPageAsync(reply, page, cancellationToken);
                var parsed = ParsePage(reply);
                if (parsed.Owner != first.Owner || parsed.Total != first.Total || parsed.Pages != first.Pages)
                    throw new InvalidOperationException("Pet inventory changed while paging.");
                collected.AddRange(parsed.Pets);
            }
            if (collected.Count != first.Total || collected.Select(p => p.Id).Distinct().Count() != first.Total)
                throw new InvalidOperationException("Pet inventory is incomplete or contains duplicate IDs.");
            return new PetInventory { Owner = first.Owner, Pets = collected, Complete = true };
        }

        public async Task FuseAsync(PetFusionStep step)
        {
            var reply = await SendCheckedAsync(step.Command, CancellationToken.None);
            if (!PetFusionReplyParser.IsSuccess(reply.Text, _owner))
                throw new InvalidOperationException("Unrecognized fusion reply. No retry was sent; inspect Discord before continuing.");
        }

        private async Task ReadOwnerAsync(CancellationToken token)
        {
            var reply = await SendCheckedAsync("rpg p", token);
            if (!ProfileMessageParser.TryParsePlayerName(reply.RenderedText, out _owner))
                throw new InvalidOperationException("Cannot identify player from rpg p. Configure the player name first.");
        }

        private async Task<DiscordMessageSnapshot> SendCheckedAsync(string command, CancellationToken token)
        {
            var result = await _send(command, token);
            var reply = result?.ReplyMessage;
            var outgoingId = result?.OutgoingMessage?.Id;
            if (!string.IsNullOrWhiteSpace(outgoingId) && !PetReplyChronology.IsAfter(reply?.Id, outgoingId))
                reply = await PetReplyChronology.WaitAsync(_client, outgoingId, token);
            if (!PetReplyChronology.IsAfter(reply?.Id, outgoingId) || !PetReplyIdentity.IsEpic(reply))
                throw new InvalidOperationException("Missing verified EPIC RPG reply for " + command +
                    ". Outgoing: " + (outgoingId ?? "missing") +
                    "; reply: " + (reply?.Id ?? "missing") +
                    "; author: '" + (reply?.Author ?? "missing") + "'. Operation stopped.");
            return reply;
        }

        private async Task<DiscordMessageSnapshot> NextPageAsync(DiscordMessageSnapshot current, int expected, CancellationToken token)
        {
            var next = current.Buttons.FirstOrDefault(b => Regex.IsMatch(b.Label.Trim(),
                @"^(▶\ufe0f?|►|➡\ufe0f?|→|>|next|next page|:arrow_forward:|:?epic_arrow_right:?)$", RegexOptions.IgnoreCase));
            if (next == null || !await _client.ClickMessageButtonAsync(current.Id, next.RowIndex, next.ColumnIndex, token))
                throw new InvalidOperationException("Cannot find or click the next pet page button.");
            for (var poll = 0; poll < 40; poll++)
            {
                await Task.Delay(250, token);
                var messages = await _client.GetRecentMessagesAsync(30);
                var updated = messages.FirstOrDefault(m => m.Id == current.Id && m.RenderedText != current.RenderedText);
                var page = updated == null ? null : ParsePage(updated);
                if (page != null && page.Number == expected) return updated;
            }
            throw new InvalidOperationException("Pet page did not update; inventory was not loaded.");
        }

        private static PetPage ParsePage(DiscordMessageSnapshot message) =>
            PetPageParser.Parse(message.Text) ?? PetPageParser.Parse(message.RenderedText);

    }
}
