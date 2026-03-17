namespace EpicRPGBot.UI.Models
{
    public sealed class CooldownDefinition
    {
        public CooldownDefinition(string canonicalKey, string rowName, string labelName, params string[] aliases)
        {
            CanonicalKey = canonicalKey;
            RowName = rowName;
            LabelName = labelName;
            Aliases = aliases ?? new string[0];
        }

        public string CanonicalKey { get; }
        public string RowName { get; }
        public string LabelName { get; }
        public string[] Aliases { get; }
    }
}
