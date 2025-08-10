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

        public LogEntry(LogKind kind, string message)
        {
            At = DateTime.Now;
            Kind = kind;
            Message = message ?? string.Empty;
        }

        public override string ToString() => $"[{At:HH:mm:ss}] {Kind}: {Message}";
    }
}