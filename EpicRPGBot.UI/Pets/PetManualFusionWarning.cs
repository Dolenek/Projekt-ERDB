using System;
using System.Linq;

namespace EpicRPGBot.UI.Pets
{
    public static class PetManualFusionWarning
    {
        public static bool Required(PetFusionRequest request) =>
            request.Mode == PetFusionMode.Manual && request.MaterialIds.Count > 3;

        public static string Describe(PetFusionRequest request) =>
            "You selected " + request.MaterialIds.Count + " pets for MANUAL fusion.\n\n" +
            "ALL of these pets will be consumed together in ONE fusion, producing ONE pet. " +
            "This is not a sequence of pairs and cannot be undone.\n\nIDs: " +
            string.Join(" ", request.MaterialIds.OrderBy(id => id, StringComparer.Ordinal)) +
            "\n\nFor sequential pairs with refreshed IDs, choose Upgrade target or Auto merge material.\n\n" +
            "Fuse all selected pets together?";
    }
}
