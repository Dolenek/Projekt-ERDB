namespace EpicRPGBot.UI.Dungeon
{
    public sealed class DungeonTurnActionGate
    {
        private bool _biteSentForVisibleTurn;

        public bool ShouldSendBite(bool isPlayerTurn)
        {
            if (!isPlayerTurn)
            {
                _biteSentForVisibleTurn = false;
                return false;
            }

            if (_biteSentForVisibleTurn)
            {
                return false;
            }

            _biteSentForVisibleTurn = true;
            return true;
        }
    }
}
