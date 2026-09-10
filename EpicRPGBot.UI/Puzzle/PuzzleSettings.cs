using System;
using System.IO;

namespace EpicRPGBot.UI.Puzzle
{
    public sealed class PuzzleSettings
    {
        public const string DefaultModelDirectory = "artifacts/puzzle-validation-20260910";
        public const string DefaultTemplateDirectory = DefaultModelDirectory + "/Items";
        public const string DefaultPolicyFile = DefaultModelDirectory + "/policy.json";
        private PuzzleSettings() { }
        public string ItemNamesFile { get; private set; }
        public string TemplateDirectory { get; private set; }
        public string PolicyFile { get; private set; }
        public string SelfTestReplayDirectory { get; private set; }
        public bool AutomaticAnswersEnabled { get; private set; }

        public static PuzzleSettings LoadDefault() => Load(Env.Get, AppDomain.CurrentDomain.BaseDirectory);

        public static PuzzleSettings Load(Func<string, string, string> readSetting, string baseDirectory) => new PuzzleSettings
        {
            ItemNamesFile = ResolvePath(readSetting("PUZZLE_ITEM_NAMES_FILE", "items.json"), baseDirectory),
            TemplateDirectory = ResolvePath(readSetting("PUZZLE_TEMPLATES_DIR", DefaultTemplateDirectory), baseDirectory),
            PolicyFile = ResolvePath(readSetting("PUZZLE_LOCAL_POLICY_FILE", DefaultPolicyFile), baseDirectory),
            SelfTestReplayDirectory = ResolvePath(readSetting("PUZZLE_SELFTEST_REPLAY_DIR", ""), baseDirectory),
            AutomaticAnswersEnabled = string.Equals(readSetting("PUZZLE_AUTO_SEND", "1"), "1", StringComparison.Ordinal)
        };

        public string Describe() => "mode=local, items=" + ItemNamesFile + ", templates=" + TemplateDirectory;

        private static string ResolvePath(string rawPath, string baseDirectory)
        {
            if (string.IsNullOrWhiteSpace(rawPath)) return "";
            if (Path.IsPathRooted(rawPath)) return rawPath.Trim();
            var current = baseDirectory;
            for (var i = 0; i < 8; i++)
            {
                var candidate = Path.GetFullPath(Path.Combine(current, rawPath.Trim()));
                if (File.Exists(candidate) || Directory.Exists(candidate)) return candidate;
                var parent = Directory.GetParent(current);
                if (parent == null) break;
                current = parent.FullName;
            }
            return Path.GetFullPath(Path.Combine(baseDirectory, rawPath.Trim()));
        }
    }
}
