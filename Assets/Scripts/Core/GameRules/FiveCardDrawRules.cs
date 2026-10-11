using System;
using System.Collections.Generic;

namespace BlackHoleBluff.Core.GameRules
{
    /// <summary>
    /// The fixed numbers of five-card draw as this game plays it (GDD §3.1):
    /// five cards each, and one opportunity to discard up to three.
    /// </summary>
    public static class FiveCardDrawRules
    {
        public const int HandSize = 5;
        public const int MaxDiscards = 3;

        /// <summary>
        /// Checks a discard selection. An empty selection is valid — standing pat is a
        /// legal choice, and it still uses up the single draw opportunity.
        /// </summary>
        public static bool IsValidDiscardSelection(IReadOnlyList<int> indices, out string error)
        {
            error = null;

            if (indices == null)
            {
                error = "Discard selection was null. Pass an empty list to stand pat.";
                return false;
            }

            if (indices.Count > MaxDiscards)
            {
                error = $"Cannot discard {indices.Count} cards; the limit is {MaxDiscards}.";
                return false;
            }

            for (var i = 0; i < indices.Count; i++)
            {
                var index = indices[i];

                if (index < 0 || index >= HandSize)
                {
                    error = $"Discard index {index} is outside the hand (0-{HandSize - 1}).";
                    return false;
                }

                for (var j = i + 1; j < indices.Count; j++)
                {
                    if (indices[j] == index)
                    {
                        error = $"Card {index} was selected for discard more than once.";
                        return false;
                    }
                }
            }

            return true;
        }

        public static void ValidateDiscardSelection(IReadOnlyList<int> indices)
        {
            if (!IsValidDiscardSelection(indices, out var error))
                throw new ArgumentException(error, nameof(indices));
        }
    }
}
