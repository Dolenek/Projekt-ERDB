#nullable enable
using System;
using System.Text.RegularExpressions;
using EpicRPGBot.UI.Models;
using EpicRPGBot.UI.Training;

namespace EpicRPGBot.UI.Services
{
    public static class TrackedCommandResponseClassifier
    {
        public static bool LooksLikeTrackedCommandResponse(DiscordMessageSnapshot snapshot)
        {
            var message = snapshot?.Text ?? string.Empty;
            var renderedText = snapshot?.RenderedText ?? string.Empty;
            if (IsPreviousCommandBusyReply(snapshot))
            {
                return false;
            }

            var looksLikeTrainingPrompt = TrainingPromptSignal.LooksLikePrompt(renderedText) ||
                TrainingPromptSignal.LooksLikePrompt(message);
            if (!looksLikeTrainingPrompt && !HasEpicReplyContext(snapshot, renderedText, message))
            {
                return false;
            }

            var hasGuardPrompt = ContainsGuardPrompt(message);
            var hasGuardClear = ContainsGuardClear(message);
            if (hasGuardPrompt && !hasGuardClear)
            {
                return false;
            }

            if (ContainsIgnoredGlobalEvent(message))
            {
                return false;
            }

            if (message.IndexOf("rpg ", StringComparison.OrdinalIgnoreCase) >= 0 &&
                !looksLikeTrainingPrompt &&
                !TryInferKind(message, out _) &&
                !TryParseWaitAtLeast(message, out _))
            {
                return false;
            }

            return true;
        }

        public static bool IsPreviousCommandBusyReply(DiscordMessageSnapshot? snapshot)
        {
            if (snapshot == null)
            {
                return false;
            }

            var message = snapshot.Text ?? string.Empty;
            var renderedText = snapshot.RenderedText ?? string.Empty;
            return HasEpicReplyContext(snapshot, renderedText, message) &&
                   (ContainsPreviousCommandBusyReply(message) || ContainsPreviousCommandBusyReply(renderedText));
        }

        public static bool ContainsPreviousCommandBusyReply(string message)
        {
            return !string.IsNullOrWhiteSpace(message) &&
                   message.IndexOf("end your previous command", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static bool TryInferKind(string message, out TrackedCommandKind kind)
        {
            var msg = (message ?? string.Empty).ToLowerInvariant();

            if (msg.Contains("daily") &&
                (msg.Contains("ready") ||
                 msg.Contains("claimed") ||
                 msg.Contains("claim") ||
                 msg.Contains("rewards") ||
                 msg.Contains("wait at least")))
            {
                kind = TrackedCommandKind.Daily;
                return true;
            }

            if (msg.Contains("weekly") &&
                (msg.Contains("ready") ||
                 msg.Contains("claimed") ||
                 msg.Contains("claim") ||
                 msg.Contains("rewards") ||
                 msg.Contains("wait at least")))
            {
                kind = TrackedCommandKind.Weekly;
                return true;
            }

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

            if (msg.Contains("is training in"))
            {
                kind = TrackedCommandKind.Training;
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

        public static bool TryParseWaitAtLeast(string message, out TimeSpan delay)
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

        private static bool HasEpicReplyContext(DiscordMessageSnapshot? snapshot, string renderedText, string message)
        {
            if (!string.IsNullOrWhiteSpace(message) &&
                message.IndexOf("EPIC RPG", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            if (snapshot != null &&
                snapshot.Author.IndexOf("EPIC RPG", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            return renderedText.IndexOf("EPIC RPG", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool ContainsGuardPrompt(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return false;
            }

            return message.IndexOf("EPIC GUARD: stop there,", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   message.IndexOf("Select the item of the image above or respond with the item name", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool ContainsGuardClear(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return false;
            }

            return message.IndexOf("EPIC GUARD: Everything seems fine", StringComparison.OrdinalIgnoreCase) >= 0 &&
                   message.IndexOf("keep playing", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool ContainsIgnoredGlobalEvent(string message)
        {
            return message.IndexOf("A LOOTBOX SUMMONING HAS", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   message.IndexOf("A LEGENDARY BOSS JUST SPAWNED", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   message.IndexOf("AN EPIC TREE HAS JUST GROWN", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   message.IndexOf("A MEGALODON HAS SPAWNED IN THE RIVER", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   message.IndexOf("IT'S RAINING COINS", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   message.IndexOf("God accidentally dropped", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   message.IndexOf("OOPS! God accidentally dropped", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   message.IndexOf("EPIC NPC: I have a special trade today!", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
