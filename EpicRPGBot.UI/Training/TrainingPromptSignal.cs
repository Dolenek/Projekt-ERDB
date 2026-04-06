using System;

namespace EpicRPGBot.UI.Training
{
    internal static class TrainingPromptSignal
    {
        public static bool LooksLikePrompt(string message)
        {
            return !string.IsNullOrWhiteSpace(message) &&
                message.IndexOf("is training in", StringComparison.OrdinalIgnoreCase) >= 0 &&
                message.IndexOf("15 seconds", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
