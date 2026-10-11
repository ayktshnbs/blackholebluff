using System;

namespace BlackHoleBluff.Core.Cards
{
    public enum Suit
    {
        Clubs = 0,
        Diamonds = 1,
        Hearts = 2,
        Spades = 3
    }

    /// <summary>
    /// Numeric values are the comparison values, so an Ace is 14. The ace-low straight
    /// (A-2-3-4-5) is handled as a special case in the evaluator, not by giving the Ace
    /// a second value here.
    /// </summary>
    public enum Rank
    {
        Two = 2,
        Three = 3,
        Four = 4,
        Five = 5,
        Six = 6,
        Seven = 7,
        Eight = 8,
        Nine = 9,
        Ten = 10,
        Jack = 11,
        Queen = 12,
        King = 13,
        Ace = 14
    }

    /// <summary>
    /// One playing card. A readonly struct: cards are values, compared by rank and suit,
    /// and never mutated. Deliberately knows nothing about rendering.
    /// </summary>
    public readonly struct Card : IEquatable<Card>
    {
        public Rank Rank { get; }
        public Suit Suit { get; }

        public Card(Rank rank, Suit suit)
        {
            Rank = rank;
            Suit = suit;
        }

        /// <summary>Comparison value. Ace is 14.</summary>
        public int Value => (int)Rank;

        // ---- text form ----------------------------------------------------
        // Short ASCII codes, chosen over suit glyphs so test failure messages and
        // logs are readable everywhere: "As", "Th", "2c".

        public char RankChar => Rank switch
        {
            Rank.Ten => 'T',
            Rank.Jack => 'J',
            Rank.Queen => 'Q',
            Rank.King => 'K',
            Rank.Ace => 'A',
            _ => (char)('0' + (int)Rank)
        };

        public char SuitChar => Suit switch
        {
            Suit.Clubs => 'c',
            Suit.Diamonds => 'd',
            Suit.Hearts => 'h',
            Suit.Spades => 's',
            _ => '?'
        };

        public override string ToString() => $"{RankChar}{SuitChar}";

        /// <summary>
        /// Parses a two-character code such as "As", "Th", "2c". Exists so tests can be
        /// written as readable hands instead of long enum expressions — which matters,
        /// because an unreadable poker test is a test nobody checks.
        /// </summary>
        public static Card Parse(string code)
        {
            if (!TryParse(code, out var card))
                throw new FormatException(
                    $"'{code}' is not a card code. Expected rank (2-9, T, J, Q, K, A) " +
                    "followed by suit (c, d, h, s), e.g. \"As\" or \"Th\".");

            return card;
        }

        public static bool TryParse(string code, out Card card)
        {
            card = default;
            if (string.IsNullOrEmpty(code) || code.Length != 2) return false;

            var rankChar = char.ToUpperInvariant(code[0]);
            var suitChar = char.ToLowerInvariant(code[1]);

            Rank rank;
            switch (rankChar)
            {
                case 'T': rank = Rank.Ten; break;
                case 'J': rank = Rank.Jack; break;
                case 'Q': rank = Rank.Queen; break;
                case 'K': rank = Rank.King; break;
                case 'A': rank = Rank.Ace; break;
                default:
                    if (rankChar < '2' || rankChar > '9') return false;
                    rank = (Rank)(rankChar - '0');
                    break;
            }

            Suit suit;
            switch (suitChar)
            {
                case 'c': suit = Suit.Clubs; break;
                case 'd': suit = Suit.Diamonds; break;
                case 'h': suit = Suit.Hearts; break;
                case 's': suit = Suit.Spades; break;
                default: return false;
            }

            card = new Card(rank, suit);
            return true;
        }

        /// <summary>Parses a space-separated hand, e.g. "As Ks Qs Js Ts".</summary>
        public static Card[] ParseMany(string codes)
        {
            if (string.IsNullOrWhiteSpace(codes)) return Array.Empty<Card>();

            var parts = codes.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
            var cards = new Card[parts.Length];

            for (var i = 0; i < parts.Length; i++)
                cards[i] = Parse(parts[i]);

            return cards;
        }

        // ---- equality -----------------------------------------------------

        public bool Equals(Card other) => Rank == other.Rank && Suit == other.Suit;

        public override bool Equals(object obj) => obj is Card other && Equals(other);

        // 4 suits * 15 ranks fits comfortably; this is a perfect hash for a card.
        public override int GetHashCode() => (int)Rank * 4 + (int)Suit;

        public static bool operator ==(Card left, Card right) => left.Equals(right);

        public static bool operator !=(Card left, Card right) => !left.Equals(right);
    }
}
