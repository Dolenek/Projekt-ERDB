using System.Collections.Generic;

namespace EpicRPGBot.UI.WorkCommands
{
    public sealed class AutoBestWorkCommandPlan
    {
        public AutoBestWorkCommandPlan(
            IReadOnlyDictionary<int, string> selections,
            AutoBestWorkCommandMode mode)
        {
            Selections = selections;
            Mode = mode;
        }

        public IReadOnlyDictionary<int, string> Selections { get; }

        public AutoBestWorkCommandMode Mode { get; }
    }
}
