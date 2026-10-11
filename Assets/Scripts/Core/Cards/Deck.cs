using System;
using System.Collections.Generic;

namespace BlackHoleBluff.Core.Cards
{
    /// <summary>
    /// A standard 52-card deck. No jokers (GDD §3.1).
    ///
    /// Draws come off the end of the list, so a draw is O(1) and a drawn card is
    /// physically removed — a card cannot be dealt twice without an explicit
    /// <see cref="Reset"/>, which is MVP acceptance criterion 3.
    /// </summary>
    public sealed class Deck
    {
        public const int FullDeckSize = 52;

        readonly List<Card> _cards = new List<Card>(FullDeckSize);
        readonly IRandomSource _random;

        public Deck(IRandomSource random)
        {
            _random = random ?? throw new ArgumentNullException(nameof(random));
            Reset();
        }

        public int Remaining => _cards.Count;

        /// <summary>Cards still in the deck, top (next to be drawn) last.</summary>
        public IReadOnlyList<Card> RemainingCards => _cards;

        /// <summary>Restores all 52 cards in a fixed order. Does not shuffle.</summary>
        public void Reset()
        {
            _cards.Clear();

            foreach (Suit suit in Enum.GetValues(typeof(Suit)))
                for (var rank = (int)Rank.Two; rank <= (int)Rank.Ace; rank++)
                    _cards.Add(new Card((Rank)rank, suit));
        }

        /// <summary>Fisher-Yates shuffle, using the injected random source.</summary>
        public void Shuffle()
        {
            for (var i = _cards.Count - 1; i > 0; i--)
            {
                var j = _random.NextInt(0, i + 1);
                (_cards[i], _cards[j]) = (_cards[j], _cards[i]);
            }
        }

        public void ResetAndShuffle()
        {
            Reset();
            Shuffle();
        }

        /// <summary>
        /// Removes and returns the top card. Throws when empty rather than returning a
        /// default card, because a silently duplicated or blank card is far worse than
        /// a loud failure.
        /// </summary>
        public Card Draw()
        {
            if (_cards.Count == 0)
                throw new InvalidOperationException("The deck is empty. Reset it before drawing again.");

            var index = _cards.Count - 1;
            var card = _cards[index];
            _cards.RemoveAt(index);

            return card;
        }

        public Card[] Draw(int count)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), "Cannot draw a negative number of cards.");

            if (count > _cards.Count)
                throw new InvalidOperationException(
                    $"Asked for {count} cards but only {_cards.Count} remain.");

            var drawn = new Card[count];
            for (var i = 0; i < count; i++)
                drawn[i] = Draw();

            return drawn;
        }

        /// <summary>Removes a specific card. For building deterministic test scenarios.</summary>
        public bool RemoveSpecific(Card card) => _cards.Remove(card);

        public bool Contains(Card card) => _cards.Contains(card);
    }
}
