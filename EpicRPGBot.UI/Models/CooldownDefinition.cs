namespace EpicRPGBot.UI.Models
{
    public sealed class CooldownDefinition
    {
        public CooldownDefinition(
            string canonicalKey,
            string rowName,
            string labelName,
            CooldownCategory category,
            params string[] aliases)
        {
            CanonicalKey = canonicalKey;
            RowName = rowName;
            LabelName = labelName;
            Category = category;
            Aliases = aliases ?? new string[0];
        }

        public string CanonicalKey { get; }
        public string RowName { get; }
        public string LabelName { get; }
        public CooldownCategory Category { get; }
        public string[] Aliases { get; }
    }
}
