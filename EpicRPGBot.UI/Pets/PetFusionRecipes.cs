namespace EpicRPGBot.UI.Pets
{
    public static class PetFusionRecipes
    {
        // Rows are result tiers II..XX. Columns: TT 0, 10, 25, 41, 61, 91, 121.
        // The higher parent is always result tier - 1. Zero means no recommendation.
        private static readonly int[,] LowerParents = {
            {1,1,1,1,1,1,1}, {1,1,1,1,1,1,1}, {3,2,1,1,1,1,1},
            {4,3,2,1,1,1,1}, {5,4,3,2,1,1,1}, {6,5,4,3,2,2,1},
            {7,7,6,5,5,4,3}, {8,8,7,6,6,6,5}, {9,9,8,8,8,8,6},
            {0,10,9,9,9,9,8}, {0,0,11,11,10,10,9}, {0,0,0,12,11,11,11},
            {0,0,0,13,12,12,12}, {0,0,0,0,14,13,13}, {0,0,0,0,15,14,14},
            {0,0,0,0,16,16,15}, {0,0,0,0,0,17,17}, {0,0,0,0,0,18,18},
            {0,0,0,0,0,19,19}
        };

        public static int LowerParent(int resultTier, int timeTravel)
        {
            if (resultTier < 2 || resultTier > 20 || timeTravel < 0) return 0;
            var column = timeTravel >= 121 ? 6 : timeTravel >= 91 ? 5 : timeTravel >= 61 ? 4 :
                timeTravel >= 41 ? 3 : timeTravel >= 25 ? 2 : timeTravel >= 10 ? 1 : 0;
            return LowerParents[resultTier - 2, column];
        }
    }
}
