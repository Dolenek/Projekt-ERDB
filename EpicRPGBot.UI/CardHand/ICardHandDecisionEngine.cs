using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EpicRPGBot.UI.CardHand
{
    public interface ICardHandDecisionEngine
    {
        Task<CardHandDecisionResult> DecideAsync(
            CardHandState state,
            IReadOnlyCollection<CardId> ownedCards,
            CardHandRewardWeights weights,
            CancellationToken cancellationToken);
    }
}
