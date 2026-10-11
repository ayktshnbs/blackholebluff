using System;
using System.Collections.Generic;
using BlackHoleBluff.Core.Cards;

namespace BlackHoleBluff.Core.GameRules
{
    /// <summary>
    /// Deterministic five-card hand evaluation (GDD §9, Hand evaluator).
    ///
    /// Pure and side-effect free: the same five cards always produce the same result,
    /// with no dependency on Unity, scene state or time.
    /// </summary>
    public static class HandEvaluator
    {
        public const int HandSize = 5;

        /// <summary>
        /// Evaluates exactly five cards.
        /// </summary>
        /// <exception cref="ArgumentNullException">cards is null.</exception>
        /// <exception cref="ArgumentException">Wrong number of cards, or a duplicate card.</exception>
        public static HandResult Evaluate(IReadOnlyList<Card> cards)
        {
            Validate(cards);

            // Rank value (2..14) -> how many of that rank. Suit -> how many of that suit.
            var rankCounts = new int[15];
            var suitCounts = new int[4];

            foreach (var card in cards)
            {
                rankCounts[card.Value]++;
                suitCounts[(int)card.Suit]++;
            }

            var isFlush = false;
            for (var suit = 0; suit < suitCounts.Length; suit++)
                if (suitCounts[suit] == HandSize)
                    isFlush = true;

            // Ranks present, highest first. A flush or a high-card hand always has five
            // distinct ranks, because a deck cannot hold two cards of the same rank and suit.
            var distinctDescending = new List<int>(HandSize);
            for (var rank = 14; rank >= 2; rank--)
                if (rankCounts[rank] > 0)
                    distinctDescending.Add(rank);

            // Groups of equal rank, largest group first, then highest rank first.
            // This ordering is what makes kickers fall out in the right sequence.
            var groups = new List<RankGroup>(HandSize);
            foreach (var rank in distinctDescending)
                groups.Add(new RankGroup(rankCounts[rank], rank));

            groups.Sort(static (a, b) =>
                a.Count != b.Count
                    ? b.Count.CompareTo(a.Count)
                    : b.Rank.CompareTo(a.Rank));

            var isStraight = TryGetStraightHigh(distinctDescending, out var straightHigh);

            // Order matters: strongest category first.
            if (isStraight && isFlush)
                return new HandResult(HandCategory.StraightFlush, straightHigh);

            if (groups[0].Count == 4)
                return new HandResult(HandCategory.FourOfAKind, groups[0].Rank, groups[1].Rank);

            if (groups[0].Count == 3 && groups[1].Count == 2)
                return new HandResult(HandCategory.FullHouse, groups[0].Rank, groups[1].Rank);

            if (isFlush)
                return new HandResult(HandCategory.Flush, distinctDescending.ToArray());

            if (isStraight)
                return new HandResult(HandCategory.Straight, straightHigh);

            if (groups[0].Count == 3)
                return new HandResult(HandCategory.ThreeOfAKind,
                    groups[0].Rank, groups[1].Rank, groups[2].Rank);

            if (groups[0].Count == 2 && groups[1].Count == 2)
                return new HandResult(HandCategory.TwoPair,
                    groups[0].Rank, groups[1].Rank, groups[2].Rank);

            if (groups[0].Count == 2)
                return new HandResult(HandCategory.OnePair,
                    groups[0].Rank, groups[1].Rank, groups[2].Rank, groups[3].Rank);

            return new HandResult(HandCategory.HighCard, distinctDescending.ToArray());
        }

        /// <summary>
        /// Compares two five-card hands. Positive when <paramref name="left"/> wins,
        /// negative when <paramref name="right"/> wins, zero for a genuine tie.
        /// </summary>
        public static int Compare(IReadOnlyList<Card> left, IReadOnlyList<Card> right) =>
            Evaluate(left).CompareTo(Evaluate(right));

        /// <summary>
        /// Finds the high card of a straight. Returns 5 for the wheel (A-2-3-4-5),
        /// because there the Ace plays low — this is the one place the Ace is not 14.
        /// </summary>
        static bool TryGetStraightHigh(List<int> distinctDescending, out int high)
        {
            high = 0;

            // A straight needs five different ranks.
            if (distinctDescending.Count != HandSize) return false;

            // Sorted descending with no duplicates, so a span of exactly 4 means consecutive.
            if (distinctDescending[0] - distinctDescending[HandSize - 1] == 4)
            {
                high = distinctDescending[0];
                return true;
            }

            // The wheel: A, 5, 4, 3, 2 — ranked as a five-high straight, below 6-high.
            if (distinctDescending[0] == 14 &&
                distinctDescending[1] == 5 &&
                distinctDescending[2] == 4 &&
                distinctDescending[3] == 3 &&
                distinctDescending[4] == 2)
            {
                high = 5;
                return true;
            }

            return false;
        }

        static void Validate(IReadOnlyList<Card> cards)
        {
            if (cards == null) throw new ArgumentNullException(nameof(cards));

            if (cards.Count != HandSize)
                throw new ArgumentException(
                    $"A hand must contain exactly {HandSize} cards, but {cards.Count} were given.",
                    nameof(cards));

            // A duplicate card means a deck bug upstream. Fail loudly here rather than
            // silently scoring an impossible hand.
            for (var i = 0; i < cards.Count; i++)
            for (var j = i + 1; j < cards.Count; j++)
                if (cards[i] == cards[j])
                    throw new ArgumentException(
                        $"Duplicate card '{cards[i]}' in hand — the deck dealt the same card twice.",
                        nameof(cards));
        }

        readonly struct RankGroup
        {
            public readonly int Count;
            public readonly int Rank;

            public RankGroup(int count, int rank)
            {
                Count = count;
                Rank = rank;
            }
        }
    }
}
