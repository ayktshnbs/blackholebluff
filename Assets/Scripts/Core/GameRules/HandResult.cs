using System;
using System.Collections.Generic;

namespace BlackHoleBluff.Core.GameRules
{
    /// <summary>
    /// Poker hand categories, ordered weakest to strongest so the enum can be compared
    /// directly. A royal flush is a straight flush with an Ace high (GDD §3.1) rather
    /// than a separate category, which keeps comparison logic uniform.
    /// </summary>
    public enum HandCategory
    {
        HighCard = 1,
        OnePair = 2,
        TwoPair = 3,
        ThreeOfAKind = 4,
        Straight = 5,
        Flush = 6,
        FullHouse = 7,
        FourOfAKind = 8,
        StraightFlush = 9
    }

    /// <summary>
    /// The outcome of evaluating five cards: a category plus every value needed to break
    /// a tie within that category, most significant first.
    ///
    /// This is why kickers work correctly. Two hands are never compared by category
    /// alone, nor by summing card values — comparison walks the tiebreaker list.
    /// </summary>
    public sealed class HandResult : IComparable<HandResult>, IEquatable<HandResult>
    {
        readonly int[] _tiebreakers;

        public HandCategory Category { get; }

        /// <summary>
        /// Tie-break values, most significant first. Contents depend on the category:
        /// <list type="bullet">
        /// <item>StraightFlush / Straight — the high card of the run (5 for the wheel)</item>
        /// <item>FourOfAKind — quad rank, then the kicker</item>
        /// <item>FullHouse — trips rank, then pair rank</item>
        /// <item>Flush / HighCard — all five ranks, descending</item>
        /// <item>ThreeOfAKind — trips rank, then two kickers descending</item>
        /// <item>TwoPair — high pair, low pair, kicker</item>
        /// <item>OnePair — pair rank, then three kickers descending</item>
        /// </list>
        /// </summary>
        public IReadOnlyList<int> Tiebreakers => _tiebreakers;

        public HandResult(HandCategory category, params int[] tiebreakers)
        {
            Category = category;
            _tiebreakers = tiebreakers ?? Array.Empty<int>();
        }

        /// <summary>A straight flush with an Ace high.</summary>
        public bool IsRoyalFlush =>
            Category == HandCategory.StraightFlush && _tiebreakers.Length > 0 && _tiebreakers[0] == 14;

        public int CompareTo(HandResult other)
        {
            if (other is null) return 1;

            var byCategory = Category.CompareTo(other.Category);
            if (byCategory != 0) return byCategory;

            var shared = Math.Min(_tiebreakers.Length, other._tiebreakers.Length);
            for (var i = 0; i < shared; i++)
            {
                var byValue = _tiebreakers[i].CompareTo(other._tiebreakers[i]);
                if (byValue != 0) return byValue;
            }

            // Hands of the same category always produce the same number of tiebreakers,
            // so this is defensive only.
            return _tiebreakers.Length.CompareTo(other._tiebreakers.Length);
        }

        public bool Equals(HandResult other) => CompareTo(other) == 0;

        public override bool Equals(object obj) => obj is HandResult other && Equals(other);

        public override int GetHashCode()
        {
            var hash = (int)Category * 397;
            foreach (var value in _tiebreakers)
                hash = hash * 31 + value;
            return hash;
        }

        public static bool operator >(HandResult a, HandResult b) => Compare(a, b) > 0;
        public static bool operator <(HandResult a, HandResult b) => Compare(a, b) < 0;
        public static bool operator >=(HandResult a, HandResult b) => Compare(a, b) >= 0;
        public static bool operator <=(HandResult a, HandResult b) => Compare(a, b) <= 0;

        static int Compare(HandResult a, HandResult b)
        {
            if (a is null) return b is null ? 0 : -1;
            return a.CompareTo(b);
        }

        /// <summary>
        /// A player-facing description, e.g. "Full house, kings over threes".
        /// Lives here rather than in the UI because it is derived from the rules.
        /// </summary>
        public string Describe()
        {
            var t = _tiebreakers;

            switch (Category)
            {
                case HandCategory.StraightFlush:
                    return IsRoyalFlush ? "Royal flush" : $"Straight flush, {Single(t[0])}-high";

                case HandCategory.FourOfAKind:
                    return $"Four of a kind, {Plural(t[0])}";

                case HandCategory.FullHouse:
                    return $"Full house, {Plural(t[0])} over {Plural(t[1])}";

                case HandCategory.Flush:
                    return $"Flush, {Single(t[0])}-high";

                case HandCategory.Straight:
                    return $"Straight, {Single(t[0])}-high";

                case HandCategory.ThreeOfAKind:
                    return $"Three of a kind, {Plural(t[0])}";

                case HandCategory.TwoPair:
                    return $"Two pair, {Plural(t[0])} and {Plural(t[1])}";

                case HandCategory.OnePair:
                    return $"Pair of {Plural(t[0])}";

                case HandCategory.HighCard:
                    return $"{Capitalise(Single(t[0]))} high";

                default:
                    return Category.ToString();
            }
        }

        public override string ToString() =>
            $"{Describe()} [{Category} {string.Join(",", _tiebreakers)}]";

        // ---- rank naming --------------------------------------------------

        static readonly string[] Singular =
        {
            "", "", "two", "three", "four", "five", "six", "seven", "eight",
            "nine", "ten", "jack", "queen", "king", "ace"
        };

        static readonly string[] PluralNames =
        {
            "", "", "twos", "threes", "fours", "fives", "sixes", "sevens", "eights",
            "nines", "tens", "jacks", "queens", "kings", "aces"
        };

        static string Single(int rankValue) =>
            rankValue >= 2 && rankValue <= 14 ? Singular[rankValue] : rankValue.ToString();

        static string Plural(int rankValue) =>
            rankValue >= 2 && rankValue <= 14 ? PluralNames[rankValue] : rankValue.ToString();

        static string Capitalise(string text) =>
            string.IsNullOrEmpty(text) ? text : char.ToUpperInvariant(text[0]) + text.Substring(1);
    }
}
