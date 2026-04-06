using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using EpicRPGBot.UI.Models;

namespace EpicRPGBot.UI.Training
{
    public sealed class TrainingPromptParser
    {
        private static readonly Regex EmojiTokenRegex = new Regex(@":[^:\s]+:", RegexOptions.Compiled);
        private static readonly Regex FishOptionRegex = new Regex(@"^(?<number>\d+)\s*-\s*(?<name>.+)$", RegexOptions.Compiled);
        private static readonly Regex CountQuestionRegex = new Regex(@"How many\s+(?<item>:[^:\s]+:)\s+do you see", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex LetterQuestionRegex = new Regex(@"What's the\s+(?<ordinal>[a-z0-9]+(?:st|nd|rd|th)?)\s+letter of\s+(?<item>:[^:\s]+:)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex YesNoQuestionRegex = new Regex(@"Is this an?\s+(?<item>.+?)\s*\?", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Dictionary<string, int> OrdinalMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["first"] = 0,
            ["second"] = 1,
            ["third"] = 2,
            ["fourth"] = 3,
            ["fifth"] = 4,
            ["sixth"] = 5,
            ["seventh"] = 6,
            ["eighth"] = 7,
            ["ninth"] = 8,
            ["tenth"] = 9
        };

        private static readonly Dictionary<string, string> LabelAliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["diamond"] = "gem",
            ["gem"] = "gem"
        };

        public TrainingPromptResolution Parse(DiscordMessageSnapshot snapshot)
        {
            var message = snapshot?.RenderedText ?? snapshot?.Text ?? string.Empty;
            if (!LooksLikeTrainingPrompt(message))
            {
                return new TrainingPromptResolution(false, false, TrainingPromptKind.Unknown, string.Empty, string.Empty, string.Empty);
            }

            var lines = GetLines(message);
            return TryResolveFishChoice(lines) ??
                TryResolveInventoryCheck(message) ??
                TryResolveYesNoMatch(lines) ??
                TryResolveLetterQuestion(message) ??
                TryResolveCountQuestion(lines) ??
                new TrainingPromptResolution(true, false, TrainingPromptKind.Unknown, string.Empty, string.Empty, "Training prompt detected but no safe answer could be resolved.");
        }

        private static TrainingPromptResolution TryResolveFishChoice(IReadOnlyList<string> lines)
        {
            var promptLine = lines.FirstOrDefault(line => line.IndexOf("What is the name of this fish", StringComparison.OrdinalIgnoreCase) >= 0);
            if (promptLine == null)
            {
                return null;
            }

            var targetToken = EmojiTokenRegex.Match(promptLine).Value;
            if (string.IsNullOrWhiteSpace(targetToken))
            {
                return new TrainingPromptResolution(true, false, TrainingPromptKind.FishChoice, string.Empty, string.Empty, "Training fish prompt detected, but the fish emoji token was missing.");
            }

            foreach (var line in lines)
            {
                var optionMatch = FishOptionRegex.Match(line);
                if (!optionMatch.Success)
                {
                    continue;
                }

                var number = optionMatch.Groups["number"].Value.Trim();
                var optionName = optionMatch.Groups["name"].Value.Trim();
                if (!LabelsMatch(targetToken, optionName))
                {
                    continue;
                }

                return new TrainingPromptResolution(true, true, TrainingPromptKind.FishChoice, number, optionName, $"Training fish prompt resolved to option {number} ({optionName}).");
            }

            return new TrainingPromptResolution(true, false, TrainingPromptKind.FishChoice, string.Empty, string.Empty, $"Training fish prompt detected, but no option matched {targetToken}.");
        }

        private static TrainingPromptResolution TryResolveInventoryCheck(string message)
        {
            if (message.IndexOf("Do you have more than", StringComparison.OrdinalIgnoreCase) < 0)
            {
                return null;
            }

            return new TrainingPromptResolution(true, true, TrainingPromptKind.InventoryCheck, "no", "no", "Training inventory prompt resolved to 'no'.");
        }

        private static TrainingPromptResolution TryResolveYesNoMatch(IReadOnlyList<string> lines)
        {
            var questionIndex = FindYesNoQuestionIndex(lines);
            if (questionIndex < 0)
            {
                return null;
            }

            var questionText = BuildYesNoQuestionText(lines, questionIndex);
            var labelMatch = YesNoQuestionRegex.Match(questionText);
            var actualToken = FindYesNoActualToken(lines, questionIndex);
            if (!labelMatch.Success || string.IsNullOrWhiteSpace(actualToken))
            {
                return new TrainingPromptResolution(true, false, TrainingPromptKind.YesNoMatch, string.Empty, string.Empty, "Training yes/no prompt detected, but the item comparison could not be parsed.");
            }

            var expectedLabel = labelMatch.Groups["item"].Value.Trim();
            var isMatch = LabelsMatch(expectedLabel, actualToken);
            var answer = isMatch ? "yes" : "no";
            return new TrainingPromptResolution(true, true, TrainingPromptKind.YesNoMatch, answer, answer, $"Training yes/no prompt resolved to '{answer}'.");
        }

        private static int FindYesNoQuestionIndex(IReadOnlyList<string> lines)
        {
            for (var index = 0; index < lines.Count; index++)
            {
                if (lines[index].IndexOf("Is this a", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    lines[index].IndexOf("Is this an", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return index;
                }
            }

            return -1;
        }

        private static string BuildYesNoQuestionText(IReadOnlyList<string> lines, int questionIndex)
        {
            var questionLine = questionIndex >= 0 && questionIndex < lines.Count
                ? lines[questionIndex]
                : string.Empty;
            if (string.IsNullOrWhiteSpace(questionLine))
            {
                return string.Empty;
            }

            if (EmojiTokenRegex.IsMatch(questionLine) || questionIndex + 1 >= lines.Count)
            {
                return questionLine;
            }

            var nextLine = lines[questionIndex + 1];
            return EmojiTokenRegex.IsMatch(nextLine)
                ? string.Concat(questionLine, " ", nextLine)
                : questionLine;
        }

        private static string FindYesNoActualToken(IReadOnlyList<string> lines, int questionIndex)
        {
            if (questionIndex < 0 || questionIndex >= lines.Count)
            {
                return string.Empty;
            }

            var questionLineToken = EmojiTokenRegex.Matches(lines[questionIndex]).Cast<Match>().LastOrDefault()?.Value;
            if (!string.IsNullOrWhiteSpace(questionLineToken))
            {
                return questionLineToken;
            }

            if (questionIndex + 1 >= lines.Count)
            {
                return string.Empty;
            }

            return EmojiTokenRegex.Matches(lines[questionIndex + 1]).Cast<Match>().FirstOrDefault()?.Value ?? string.Empty;
        }

        private static TrainingPromptResolution TryResolveLetterQuestion(string message)
        {
            var match = LetterQuestionRegex.Match(message);
            if (!match.Success)
            {
                return null;
            }

            if (!TryParseOrdinalIndex(match.Groups["ordinal"].Value, out var index))
            {
                return new TrainingPromptResolution(true, false, TrainingPromptKind.LetterQuestion, string.Empty, string.Empty, "Training letter prompt detected, but the letter index could not be parsed.");
            }

            var normalizedItem = NormalizeLabel(match.Groups["item"].Value);
            if (index < 0 || index >= normalizedItem.Length)
            {
                return new TrainingPromptResolution(true, false, TrainingPromptKind.LetterQuestion, string.Empty, string.Empty, "Training letter prompt detected, but the requested letter index was out of range.");
            }

            var answer = normalizedItem[index].ToString();
            return new TrainingPromptResolution(true, true, TrainingPromptKind.LetterQuestion, answer, answer, $"Training letter prompt resolved to '{answer}'.");
        }

        private static TrainingPromptResolution TryResolveCountQuestion(IReadOnlyList<string> lines)
        {
            var questionIndex = -1;
            Match questionMatch = null;
            for (var index = 0; index < lines.Count; index++)
            {
                var current = CountQuestionRegex.Match(lines[index]);
                if (!current.Success)
                {
                    continue;
                }

                questionIndex = index;
                questionMatch = current;
                break;
            }

            if (questionMatch == null)
            {
                return null;
            }

            var itemLine = FindNearestItemLine(lines, questionIndex);
            if (string.IsNullOrWhiteSpace(itemLine))
            {
                return new TrainingPromptResolution(true, false, TrainingPromptKind.CountQuestion, string.Empty, string.Empty, "Training count prompt detected, but the item row was missing.");
            }

            var targetToken = questionMatch.Groups["item"].Value;
            var count = EmojiTokenRegex.Matches(itemLine)
                .Cast<Match>()
                .Count(match => LabelsMatch(match.Value, targetToken));

            return new TrainingPromptResolution(true, true, TrainingPromptKind.CountQuestion, count.ToString(), count.ToString(), $"Training count prompt resolved to {count}.");
        }

        private static bool LooksLikeTrainingPrompt(string message)
        {
            return TrainingPromptSignal.LooksLikePrompt(message);
        }

        private static IReadOnlyList<string> GetLines(string message)
        {
            return (message ?? string.Empty)
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim())
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .ToArray();
        }

        private static string FindNearestItemLine(IReadOnlyList<string> lines, int questionIndex)
        {
            for (var index = questionIndex - 1; index >= 0; index--)
            {
                var line = lines[index];
                if (EmojiTokenRegex.IsMatch(line))
                {
                    return line;
                }
            }

            return string.Empty;
        }

        private static bool TryParseOrdinalIndex(string raw, out int index)
        {
            index = -1;
            if (string.IsNullOrWhiteSpace(raw))
            {
                return false;
            }

            if (OrdinalMap.TryGetValue(raw.Trim(), out index))
            {
                return true;
            }

            var digits = Regex.Match(raw, @"\d+").Value;
            if (!int.TryParse(digits, out var ordinal))
            {
                return false;
            }

            index = ordinal - 1;
            return index >= 0;
        }

        private static bool LabelsMatch(string left, string right)
        {
            return string.Equals(NormalizeLabel(left), NormalizeLabel(right), StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeLabel(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var normalized = new string(value
                .Trim()
                .Trim(':')
                .Where(char.IsLetterOrDigit)
                .Select(char.ToLowerInvariant)
                .ToArray());

            return LabelAliases.TryGetValue(normalized, out var alias)
                ? alias
                : normalized;
        }
    }
}
