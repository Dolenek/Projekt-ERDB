using System;
using System.Text.RegularExpressions;
using EpicRPGBot.UI.Models;

namespace EpicRPGBot.UI.Services
{
    public sealed partial class TrackedCommandScheduler
    {
        private static bool LooksLikeTrackedCommandResponse(DiscordMessageSnapshot snapshot)
        {
            var message = snapshot?.Text ?? string.Empty;
            if (string.IsNullOrWhiteSpace(message) || message.IndexOf("EPIC RPG", StringComparison.OrdinalIgnoreCase) < 0)
            {
                if (snapshot == null || snapshot.Author.IndexOf("EPIC RPG", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    return false;
                }
            }

            return message.IndexOf("EPIC GUARD", StringComparison.OrdinalIgnoreCase) < 0 &&
                   message.IndexOf("A LOOTBOX SUMMONING HAS", StringComparison.OrdinalIgnoreCase) < 0 &&
                   message.IndexOf("A LEGENDARY BOSS JUST SPAWNED", StringComparison.OrdinalIgnoreCase) < 0 &&
                   message.IndexOf("AN EPIC TREE HAS JUST GROWN", StringComparison.OrdinalIgnoreCase) < 0 &&
                   message.IndexOf("A MEGALODON HAS SPAWNED IN THE RIVER", StringComparison.OrdinalIgnoreCase) < 0 &&
                   message.IndexOf("IT'S RAINING COINS", StringComparison.OrdinalIgnoreCase) < 0 &&
                   message.IndexOf("God accidentally dropped", StringComparison.OrdinalIgnoreCase) < 0 &&
                   message.IndexOf("OOPS! God accidentally dropped", StringComparison.OrdinalIgnoreCase) < 0 &&
                   message.IndexOf("EPIC NPC: I have a special trade today!", StringComparison.OrdinalIgnoreCase) < 0 &&
                   message.IndexOf("rpg ", StringComparison.OrdinalIgnoreCase) < 0;
        }

        private static bool TryInferKind(string message, out TrackedCommandKind kind)
        {
            var msg = (message ?? string.Empty).ToLowerInvariant();

            if (msg.Contains("looked around") ||
                msg.Contains("found and killed") ||
                msg.Contains("defenseless monster") ||
                msg.Contains("zombie horde"))
            {
                kind = TrackedCommandKind.Hunt;
                return true;
            }

            if (msg.Contains("adventure") ||
                msg.Contains("you went") ||
                msg.Contains("went exploring") ||
                msg.Contains("found a cave") ||
                msg.Contains("got lost") ||
                msg.Contains("adv h"))
            {
                kind = TrackedCommandKind.Adventure;
                return true;
            }

            if (msg.Contains("is chopping") ||
                msg.Contains("is fishing") ||
                msg.Contains("is picking up") ||
                msg.Contains("is mining") ||
                msg.Contains("chainsaw") ||
                msg.Contains("bowsaw") ||
                msg.Contains("axe") ||
                msg.Contains("wooden log") ||
                msg.Contains("normie fish") ||
                msg.Contains("lootbox summoning") ||
                msg.Contains("mermaid hair"))
            {
                kind = TrackedCommandKind.Work;
                return true;
            }

            if (msg.Contains("farm") ||
                msg.Contains("working on the fields") ||
                msg.Contains("carrot") ||
                msg.Contains("potato") ||
                msg.Contains("bread"))
            {
                kind = TrackedCommandKind.Farm;
                return true;
            }

            if (msg.Contains("lootbox"))
            {
                kind = TrackedCommandKind.Lootbox;
                return true;
            }

            kind = TrackedCommandKind.Hunt;
            return false;
        }

        private static bool TryParseWaitAtLeast(string message, out TimeSpan delay)
        {
            delay = TimeSpan.Zero;

            var match = Regex.Match(message ?? string.Empty, @"wait at least\s+((?:\d+\s*[dhms]\s*)+)", RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                return false;
            }

            var total = TimeSpan.Zero;
            var units = Regex.Matches(match.Groups[1].Value, @"(\d+)\s*([dhms])", RegexOptions.IgnoreCase);
            foreach (Match unit in units)
            {
                var value = int.Parse(unit.Groups[1].Value);
                switch (unit.Groups[2].Value.ToLowerInvariant())
                {
                    case "d": total += TimeSpan.FromDays(value); break;
                    case "h": total += TimeSpan.FromHours(value); break;
                    case "m": total += TimeSpan.FromMinutes(value); break;
                    case "s": total += TimeSpan.FromSeconds(value); break;
                }
            }

            delay = total <= TimeSpan.Zero ? TimeSpan.FromSeconds(1) : total;
            return true;
        }
    }
}
