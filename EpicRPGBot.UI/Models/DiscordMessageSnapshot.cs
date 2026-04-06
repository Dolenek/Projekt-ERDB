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
            IReadOnlyList<DiscordMessageButton> buttons = null)
        {
            Id = id ?? string.Empty;
            Text = text ?? string.Empty;
            Author = author ?? string.Empty;
            RenderedText = string.IsNullOrWhiteSpace(renderedText) ? Text : renderedText;
            Buttons = buttons?.Where(button => button != null).ToArray() ?? new DiscordMessageButton[0];
        }

        public string Id { get; }
        public string Text { get; }
        public string Author { get; }
        public string RenderedText { get; }
        public IReadOnlyList<DiscordMessageButton> Buttons { get; }
    }
}
