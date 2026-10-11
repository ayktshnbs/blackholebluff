using System;
using System.Collections.Generic;
using System.Linq;
using BlackHoleBluff.Core.Cards;
using NUnit.Framework;

namespace BlackHoleBluff.Tests
{
    public class CardTests
    {
        [Test]
        public void AceIsHighestValue()
        {
            Assert.AreEqual(14, new Card(Rank.Ace, Suit.Spades).Value);
            Assert.AreEqual(2, new Card(Rank.Two, Suit.Clubs).Value);
            Assert.AreEqual(10, new Card(Rank.Ten, Suit.Hearts).Value);
        }

        [TestCase("As", Rank.Ace, Suit.Spades)]
        [TestCase("Kh", Rank.King, Suit.Hearts)]
        [TestCase("Qd", Rank.Queen, Suit.Diamonds)]
        [TestCase("Jc", Rank.Jack, Suit.Clubs)]
        [TestCase("Th", Rank.Ten, Suit.Hearts)]
        [TestCase("9c", Rank.Nine, Suit.Clubs)]
        [TestCase("2d", Rank.Two, Suit.Diamonds)]
        public void ParsesShortCodes(string code, Rank expectedRank, Suit expectedSuit)
        {
            var card = Card.Parse(code);

            Assert.AreEqual(expectedRank, card.Rank);
            Assert.AreEqual(expectedSuit, card.Suit);
        }

        [Test]
        public void ParsingIsCaseInsensitiveAndRoundTrips()
        {
            Assert.AreEqual(Card.Parse("As"), Card.Parse("aS"));
            Assert.AreEqual("As", Card.Parse("As").ToString());
            Assert.AreEqual("Th", Card.Parse("th").ToString());
        }

        [TestCase("")]
        [TestCase("A")]
        [TestCase("Ass")]
        [TestCase("1s")]
        [TestCase("Ax")]
        [TestCase("10h")]
        public void RejectsMalformedCodes(string code)
        {
            Assert.IsFalse(Card.TryParse(code, out _), $"'{code}' should not parse.");
            Assert.Throws<FormatException>(() => Card.Parse(code));
        }

        [Test]
        public void ParseManyReadsAWholeHand()
        {
            var hand = Card.ParseMany("As Ks Qs Js Ts");

            Assert.AreEqual(5, hand.Length);
            Assert.AreEqual(Card.Parse("As"), hand[0]);
            Assert.AreEqual(Card.Parse("Ts"), hand[4]);
        }

        [Test]
        public void EqualityComparesRankAndSuit()
        {
            Assert.AreEqual(new Card(Rank.Ace, Suit.Spades), new Card(Rank.Ace, Suit.Spades));
            Assert.AreNotEqual(new Card(Rank.Ace, Suit.Spades), new Card(Rank.Ace, Suit.Hearts));
            Assert.AreNotEqual(new Card(Rank.Ace, Suit.Spades), new Card(Rank.King, Suit.Spades));

            Assert.IsTrue(Card.Parse("As") == Card.Parse("As"));
            Assert.IsTrue(Card.Parse("As") != Card.Parse("Ah"));
        }

        [Test]
        public void HashCodeIsUniqueAcrossTheWholeDeck()
        {
            var hashes = new HashSet<int>();

            foreach (Suit suit in Enum.GetValues(typeof(Suit)))
                for (var rank = (int)Rank.Two; rank <= (int)Rank.Ace; rank++)
                    Assert.IsTrue(hashes.Add(new Card((Rank)rank, suit).GetHashCode()),
                        $"Hash collision at {(Rank)rank} of {suit}.");

            Assert.AreEqual(52, hashes.Count);
        }
    }

    public class DeckTests
    {
        static Deck NewDeck(int seed = 1234) => new Deck(new SeededRandomSource(seed));

        [Test]
        public void NewDeckHasFiftyTwoUniqueCards()
        {
            var deck = NewDeck();

            Assert.AreEqual(52, deck.Remaining);
            Assert.AreEqual(52, deck.RemainingCards.Distinct().Count(),
                "A deck must not contain duplicates.");
        }

        [Test]
        public void NewDeckContainsEveryRankOfEverySuit()
        {
            var deck = NewDeck();

            foreach (Suit suit in Enum.GetValues(typeof(Suit)))
                for (var rank = (int)Rank.Two; rank <= (int)Rank.Ace; rank++)
                    Assert.IsTrue(deck.Contains(new Card((Rank)rank, suit)),
                        $"Missing {(Rank)rank} of {suit}.");
        }

        [Test]
        public void DrawingRemovesTheCardFromTheDeck()
        {
            var deck = NewDeck();
            var card = deck.Draw();

            Assert.AreEqual(51, deck.Remaining);
            Assert.IsFalse(deck.Contains(card), "A drawn card must leave the deck.");
        }

        [Test]
        public void DrawingTheWholeDeckYieldsNoDuplicates()
        {
            // MVP acceptance criterion 3: no card may be dealt twice.
            var deck = NewDeck();
            deck.Shuffle();

            var drawn = new List<Card>(52);
            for (var i = 0; i < 52; i++) drawn.Add(deck.Draw());

            Assert.AreEqual(0, deck.Remaining);
            Assert.AreEqual(52, drawn.Distinct().Count(), "Drew the same card twice.");
        }

        [Test]
        public void DrawingFromAnEmptyDeckThrows()
        {
            var deck = NewDeck();
            deck.Draw(52);

            Assert.Throws<InvalidOperationException>(() => deck.Draw(),
                "An empty draw must fail loudly rather than return a blank card.");
        }

        [Test]
        public void DrawingMoreThanRemainingThrowsAndLeavesTheDeckIntact()
        {
            var deck = NewDeck();
            deck.Draw(50);

            Assert.Throws<InvalidOperationException>(() => deck.Draw(3));
            Assert.AreEqual(2, deck.Remaining, "A failed bulk draw must not consume cards.");
        }

        [Test]
        public void DrawingANegativeCountThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => NewDeck().Draw(-1));
        }

        [Test]
        public void ShufflingPreservesEveryCard()
        {
            var deck = NewDeck();
            deck.Shuffle();

            Assert.AreEqual(52, deck.Remaining);
            Assert.AreEqual(52, deck.RemainingCards.Distinct().Count());
        }

        [Test]
        public void SameSeedProducesTheSameShuffle()
        {
            // Determinism is what makes failures reproducible (GDD §11).
            var first = new Deck(new SeededRandomSource(99));
            var second = new Deck(new SeededRandomSource(99));

            first.Shuffle();
            second.Shuffle();

            Assert.AreEqual(first.RemainingCards.ToArray(), second.RemainingCards.ToArray());
        }

        [Test]
        public void DifferentSeedsProduceDifferentShuffles()
        {
            var first = new Deck(new SeededRandomSource(1));
            var second = new Deck(new SeededRandomSource(2));

            first.Shuffle();
            second.Shuffle();

            Assert.AreNotEqual(first.RemainingCards.ToArray(), second.RemainingCards.ToArray());
        }

        [Test]
        public void ShufflingActuallyChangesTheOrder()
        {
            var ordered = NewDeck().RemainingCards.ToArray();

            var shuffled = NewDeck();
            shuffled.Shuffle();

            Assert.AreNotEqual(ordered, shuffled.RemainingCards.ToArray(),
                "A shuffle that returns the deck unchanged is not shuffling.");
        }

        [Test]
        public void ResetRestoresAFullDeck()
        {
            var deck = NewDeck();
            deck.Draw(20);
            Assert.AreEqual(32, deck.Remaining);

            deck.Reset();

            Assert.AreEqual(52, deck.Remaining);
            Assert.AreEqual(52, deck.RemainingCards.Distinct().Count());
        }

        [Test]
        public void ResetAndShuffleRestoresAndRandomises()
        {
            var deck = NewDeck();
            deck.Draw(30);
            deck.ResetAndShuffle();

            Assert.AreEqual(52, deck.Remaining);
            Assert.AreEqual(52, deck.RemainingCards.Distinct().Count());
        }

        [Test]
        public void RequiresARandomSource()
        {
            Assert.Throws<ArgumentNullException>(() => new Deck(null));
        }

        [Test]
        public void SeededRandomSourceStaysInRange()
        {
            var random = new SeededRandomSource(7);

            for (var i = 0; i < 1000; i++)
            {
                var value = random.NextInt(0, 52);
                Assert.GreaterOrEqual(value, 0);
                Assert.Less(value, 52);
            }
        }
    }
}
