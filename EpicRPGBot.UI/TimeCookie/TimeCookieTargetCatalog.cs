using System;

namespace EpicRPGBot.UI.TimeCookie
{
    public sealed class TimeCookieTargetDefinition
    {
        public TimeCookieTargetDefinition(TimeCookieTarget target, string displayName, string canonicalCooldownKey)
        {
            Target = target;
            DisplayName = displayName ?? string.Empty;
            CanonicalCooldownKey = canonicalCooldownKey ?? string.Empty;
        }

        public TimeCookieTarget Target { get; }
        public string DisplayName { get; }
        public string CanonicalCooldownKey { get; }
    }

    public static class TimeCookieTargetCatalog
    {
        public static TimeCookieTargetDefinition Get(TimeCookieTarget target)
        {
            switch (target)
            {
                case TimeCookieTarget.Dungeon:
                    return new TimeCookieTargetDefinition(target, "Dungeon", "dungeon");
                case TimeCookieTarget.Duel:
                    return new TimeCookieTargetDefinition(target, "Duel", "duel");
                case TimeCookieTarget.CardHand:
                    return new TimeCookieTargetDefinition(target, "Card hand", "card_hand");
                default:
                    throw new ArgumentOutOfRangeException(nameof(target), target, "Unsupported time-cookie target.");
            }
        }
    }
}
