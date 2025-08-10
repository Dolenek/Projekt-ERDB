using System;

namespace EpicRPGBot.UI.Models
{
    public sealed class MessageItem
    {
        public DateTime At { get; }
        public string Text { get; }

        public MessageItem(string text)
        {
            At = DateTime.Now;
            Text = text ?? string.Empty;
        }

        public override string ToString() => $"[{At:HH:mm:ss}] {Text}";
    }
}