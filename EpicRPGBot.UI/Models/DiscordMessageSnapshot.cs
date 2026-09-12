using System;
using System.Collections.Generic;
using System.Linq;

namespace EpicRPGBot.UI.Models
{
    public sealed class DiscordMessageSnapshot
    {
        public DiscordMessageSnapshot(
            string id,
            string text,
            string author = null,
            string renderedText = null,
            IReadOnlyList<DiscordMessageButton> buttons = null,
            IReadOnlyList<DiscordMessageMention> mentions = null,
            string authorId = null,
            DateTimeOffset? createdAtUtc = null,
            IReadOnlyList<DiscordMessageReaction> reactions = null,
            DiscordTabRole sourceTabRole = DiscordTabRole.Unknown,
            string channelUrl = null)
        {
            Id = id ?? string.Empty;
            Text = text ?? string.Empty;
            Author = author ?? string.Empty;
            RenderedText = string.IsNullOrWhiteSpace(renderedText) ? Text : renderedText;
            Buttons = buttons?.Where(button => button != null).ToArray() ?? new DiscordMessageButton[0];
            Mentions = mentions?.Where(mention => mention != null).ToArray() ?? new DiscordMessageMention[0];
            AuthorId = authorId ?? string.Empty;
            CreatedAtUtc = createdAtUtc;
            Reactions = reactions?.Where(reaction => reaction != null).ToArray() ?? new DiscordMessageReaction[0];
            SourceTabRole = sourceTabRole;
            ChannelUrl = channelUrl ?? string.Empty;
        }

        public string Id { get; }
        public string Text { get; }
        public string Author { get; }
        public string RenderedText { get; }
        public IReadOnlyList<DiscordMessageButton> Buttons { get; }
        public IReadOnlyList<DiscordMessageMention> Mentions { get; }
        public string AuthorId { get; }
        public DateTimeOffset? CreatedAtUtc { get; }
        public IReadOnlyList<DiscordMessageReaction> Reactions { get; }
        public DiscordTabRole SourceTabRole { get; }
        public string ChannelUrl { get; }
    }
}
