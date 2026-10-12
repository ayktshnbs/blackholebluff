using System;

namespace BlackHoleBluff.Core.GameRules
{
    /// <summary>
    /// Collapses a <see cref="HandResult"/> into a single 0–1 strength figure, so the AI
    /// can reason about "how good is this?" without a full equity calculation.
    ///
    /// Deliberately crude. It is a heuristic for decision-making, not a poker solver, and
    /// it is never used to decide who wins a hand — that is always
    /// <see cref="HandResult.CompareTo"/> on the real tie-break values.
    /// </summary>
    public static class HandStrength
    {
        const int CategoryCount = 9;

        /// <summary>
        /// 0 for the worst imaginable high-card hand, 1 for a royal flush.
        /// Monotonic: a stronger hand never scores lower than a weaker one.
        /// </summary>
        public static float Estimate(HandResult result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));

            // Category contributes the whole number; the top tie-break value positions
            // the hand within its category.
            var category = (int)result.Category - 1;              // 0..8
            var top = result.Tiebreakers.Count > 0 ? result.Tiebreakers[0] : 2;
            var within = (top - 2f) / 12f;                        // rank 2..14 -> 0..1

            if (within < 0f) within = 0f;
            else if (within > 1f) within = 1f;

            var strength = (category + within) / CategoryCount;

            return strength < 0f ? 0f : strength > 1f ? 1f : strength;
        }
    }
}
