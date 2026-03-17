using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace EpicRPGBot.UI.Crafting
{
    public sealed class CraftReplyParser
    {
        private readonly CraftItemCatalog _catalog;
        private readonly Regex _missingItemsRegex;
        private readonly Regex _successRegex;

        public CraftReplyParser(CraftItemCatalog catalog)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            var itemPattern = BuildItemPattern(_catalog.AllItems);
            _missingItemsRegex = new Regex(
                $@"(?<item>(?:{itemPattern}))\s*:\s*(?<available>[\d,]+)\s*/\s*(?<required>[\d,]+)",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
            _successRegex = new Regex(
                $@"(?<amount>[\d,]+)\s+(?<item>(?:{itemPattern}))\s+successfully crafted",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
        }

        public CraftReply Parse(string replyText)
        {
            var text = replyText ?? string.Empty;

            if (_successRegex.IsMatch(text))
            {
                return new CraftReply(CraftReplyKind.Success, text);
            }

            var missingItemsMatch = _missingItemsRegex.Match(text);
            if (missingItemsMatch.Success &&
                _catalog.TryGetByDisplayName(missingItemsMatch.Groups["item"].Value, out var definition))
            {
                return new CraftReply(
                    CraftReplyKind.MissingItems,
                    text,
                    definition,
                    ParseAmount(missingItemsMatch.Groups["available"].Value),
                    ParseAmount(missingItemsMatch.Groups["required"].Value));
            }

            return new CraftReply(CraftReplyKind.Unknown, text);
        }

        private static string BuildItemPattern(IEnumerable<CraftItemDefinition> definitions)
        {
            return string.Join(
                "|",
                definitions
                    .Select(definition => Regex.Escape(definition.DisplayName))
                    .OrderByDescending(name => name.Length));
        }

        private static long ParseAmount(string rawValue)
        {
            var normalized = (rawValue ?? string.Empty).Replace(",", string.Empty);
            long.TryParse(normalized, NumberStyles.Integer, CultureInfo.InvariantCulture, out var amount);
            return amount;
        }
    }
}
