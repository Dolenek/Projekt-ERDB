using System.Collections.Generic;
using System.Linq;

namespace EpicRPGBot.UI.Crafting
{
    public sealed class CraftRequest
    {
        private readonly IReadOnlyDictionary<CraftItemKey, long> _amounts;

        public CraftRequest(IReadOnlyDictionary<CraftItemKey, long> amounts)
        {
            _amounts = amounts ?? new Dictionary<CraftItemKey, long>();
        }

        public bool HasAnyAmount => _amounts.Values.Any(amount => amount > 0);

        public long GetAmount(CraftItemKey key)
        {
            return _amounts.TryGetValue(key, out var amount) ? amount : 0;
        }
    }
}
