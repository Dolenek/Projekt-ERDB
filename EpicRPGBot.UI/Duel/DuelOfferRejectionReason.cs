namespace EpicRPGBot.UI.Duel
{
    public enum DuelOfferRejectionReason
    {
        None = 0,
        MissingMessageId = 1,
        MissingAuthorId = 2,
        MissingTimestamp = 3,
        TooOld = 4,
        OwnMessage = 5,
        BlockingReaction = 6,
        NotCf = 7,
        InvalidLevel = 8,
        OutsideRewardRange = 9
    }
}
