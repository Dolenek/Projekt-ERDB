using System.Collections.Generic;

namespace EpicRPGBot.UI.WorkCommands
{
    public sealed class AutoBestWorkCommandResult
    {
        private AutoBestWorkCommandResult(
            bool success,
            IReadOnlyDictionary<int, string> selections,
            string message,
            AutoBestWorkCommandMode mode,
            bool usedFallback)
        {
            Success = success;
            Selections = selections ?? new Dictionary<int, string>();
            Message = message ?? string.Empty;
            Mode = mode;
            UsedFallback = usedFallback;
        }

        public bool Success { get; }

        public IReadOnlyDictionary<int, string> Selections { get; }

        public string Message { get; }

        public AutoBestWorkCommandMode Mode { get; }

        public bool UsedFallback { get; }

        public static AutoBestWorkCommandResult Failure(string message)
        {
            return new AutoBestWorkCommandResult(
                false,
                null,
                message,
                AutoBestWorkCommandMode.None,
                false);
        }

        public static AutoBestWorkCommandResult Applied(
            IReadOnlyDictionary<int, string> selections,
            string message,
            AutoBestWorkCommandMode mode,
            bool usedFallback)
        {
            return new AutoBestWorkCommandResult(true, selections, message, mode, usedFallback);
        }
    }
}
