using System;

namespace EpicRPGBot.UI.Models
{
    public enum LogKind
    {
        Info,
        Command,
        Warning,
        Error,
        Engine
    }

    public sealed class LogEntry
    {
        public DateTime At { get; }
        public LogKind Kind { get; }
        public string Message { get; }
        public DiscordMessageReference MessageReference { get; }
        public bool CanNavigate => MessageReference?.IsComplete == true;

        public LogEntry(LogKind kind, string message, DiscordMessageReference messageReference = null)
        {
            At = DateTime.Now;
            Kind = kind;
            Message = message ?? string.Empty;
            MessageReference = messageReference?.IsComplete == true ? messageReference : null;
        }

        public override string ToString() => $"[{At:HH:mm:ss}] {Kind}: {Message}";
    }
}
