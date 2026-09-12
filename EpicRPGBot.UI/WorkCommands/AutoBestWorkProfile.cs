namespace EpicRPGBot.UI.WorkCommands
{
    public sealed class AutoBestWorkProfile
    {
        public AutoBestWorkProfile(decimal coins, decimal bank, int timeTravels)
        {
            Coins = coins;
            Bank = bank;
            TimeTravels = timeTravels;
        }

        public decimal Coins { get; }

        public decimal Bank { get; }

        public int TimeTravels { get; }

        public decimal TotalCoins => Coins + Bank;
    }
}
