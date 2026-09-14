using System.Windows.Controls;

namespace EpicRPGBot.UI.Accounts
{
    public sealed class AccountBrowserHosts
    {
        public AccountBrowserHosts(ContentControl bot, ContentControl player, ContentControl guild,
            ContentControl dungeon, ContentControl duel, Panel backgroundParking)
        {
            Bot = bot;
            Player = player;
            Guild = guild;
            Dungeon = dungeon;
            Duel = duel;
            BackgroundParking = backgroundParking;
        }

        public ContentControl Bot { get; }
        public ContentControl Player { get; }
        public ContentControl Guild { get; }
        public ContentControl Dungeon { get; }
        public ContentControl Duel { get; }
        public Panel BackgroundParking { get; }
    }
}
