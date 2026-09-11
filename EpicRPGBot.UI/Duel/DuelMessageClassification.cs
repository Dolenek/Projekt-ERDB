using EpicRPGBot.UI.Models;

namespace EpicRPGBot.UI.Duel
{
    public sealed class DuelMessageClassification
    {
        public DuelMessageClassification(
            DuelMessageKind kind,
            DiscordMessageSnapshot message,
            string initiatorName = null,
            string targetName = null)
        {
            Kind = kind;
            Message = message;
            InitiatorName = initiatorName ?? string.Empty;
            TargetName = targetName ?? string.Empty;
        }

        public DuelMessageKind Kind { get; }

        public DiscordMessageSnapshot Message { get; }

        public string InitiatorName { get; }

        public string TargetName { get; }

        public bool Targets(string playerName)
        {
            return !string.IsNullOrWhiteSpace(playerName) &&
                   string.Equals(TargetName, playerName, System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
