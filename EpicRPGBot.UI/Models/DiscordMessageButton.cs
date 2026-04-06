namespace EpicRPGBot.UI.Models
{
    public sealed class DiscordMessageButton
    {
        public DiscordMessageButton(string label, int rowIndex, int columnIndex)
        {
            Label = label ?? string.Empty;
            RowIndex = rowIndex;
            ColumnIndex = columnIndex;
        }

        public string Label { get; }
        public int RowIndex { get; }
        public int ColumnIndex { get; }
    }
}
