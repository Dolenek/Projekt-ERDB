using EpicRPGBot.UI.Models;

namespace EpicRPGBot.Tests.Duel;

internal static class DuelTestMessages
{
    public static DiscordMessageSnapshot Message(
        string id,
        string text,
        DateTimeOffset? createdAtUtc = null,
        string author = "opponent",
        string authorId = "200",
        IReadOnlyList<DiscordMessageButton>? buttons = null,
        IReadOnlyList<DiscordMessageReaction>? reactions = null)
    {
        return new DiscordMessageSnapshot(
            id,
            text,
            author,
            text,
            buttons,
            authorId: authorId,
            createdAtUtc: createdAtUtc,
            reactions: reactions);
    }

    public static DiscordMessageButton Button(string label, int columnIndex = 0)
    {
        return new DiscordMessageButton(label, 0, columnIndex);
    }
}
