using System;
using System.Collections.Generic;
using System.Linq;
using BlackHoleBluff.Core.Cards;
using BlackHoleBluff.Core.GameRules;
using NUnit.Framework;

namespace BlackHoleBluff.Tests
{
    public class FiveCardDrawRulesTests
    {
        [Test]
        public void StandingPatIsLegal()
        {
            Assert.IsTrue(FiveCardDrawRules.IsValidDiscardSelection(new int[0], out _));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void UpToThreeDiscardsAreLegal(int count)
        {
            var indices = Enumerable.Range(0, count).ToArray();
            Assert.IsTrue(FiveCardDrawRules.IsValidDiscardSelection(indices, out var error), error);
        }

        [Test]
        public void MoreThanThreeDiscardsIsRejected()
        {
            Assert.IsFalse(FiveCardDrawRules.IsValidDiscardSelection(new[] { 0, 1, 2, 3 }, out var error));
            Assert.That(error, Does.Contain("limit is 3"));
        }

        [TestCase(-1)]
        [TestCase(5)]
        [TestCase(99)]
        public void IndicesOutsideTheHandAreRejected(int index)
        {
            Assert.IsFalse(FiveCardDrawRules.IsValidDiscardSelection(new[] { index }, out var error));
            Assert.That(error, Does.Contain("outside the hand"));
        }

        [Test]
        public void TheSameCardCannotBeDiscardedTwice()
        {
            Assert.IsFalse(FiveCardDrawRules.IsValidDiscardSelection(new[] { 2, 2 }, out var error));
            Assert.That(error, Does.Contain("more than once"));
        }

        [Test]
        public void NullSelectionIsRejected()
        {
            Assert.IsFalse(FiveCardDrawRules.IsValidDiscardSelection(null, out _));
            Assert.Throws<ArgumentException>(() => FiveCardDrawRules.ValidateDiscardSelection(null));
        }
    }

    public class ShowdownResultTests
    {
        static HandResult Eval(string cards) => HandEvaluator.Evaluate(Card.ParseMany(cards));

        [Test]
        public void StrongerPlayerHandWins()
        {
            var result = new ShowdownResult(Eval("As Ks Qs Js Ts"), Eval("Ac Qd 9h 6s 3c"));

            Assert.AreEqual(Seat.Player, result.Winner);
            Assert.IsFalse(result.IsTie);
        }

        [Test]
        public void StrongerOpponentHandWins()
        {
            var result = new ShowdownResult(Eval("Ac Qd 9h 6s 3c"), Eval("As Ks Qs Js Ts"));

            Assert.AreEqual(Seat.Opponent, result.Winner);
            Assert.IsFalse(result.IsTie);
        }

        [Test]
        public void EqualHandsTie()
        {
            var result = new ShowdownResult(Eval("Ac Kd 9h 6s 3c"), Eval("Ad Kh 9s 6c 3d"));

            Assert.IsTrue(result.IsTie);
            Assert.IsNull(result.Winner);
        }

        [Test]
        public void KickerDecidesTheWinner()
        {
            // Both hold a pair of eights with K and 6; only the last kicker differs.
            var result = new ShowdownResult(Eval("8c 8d Kh 6s 2c"), Eval("8h 8s Kd 6c 3d"));

            Assert.AreEqual(Seat.Opponent, result.Winner,
                "A showdown must be decided by kickers, not by category alone.");
        }

        [Test]
        public void RequiresBothHands()
        {
            Assert.Throws<ArgumentNullException>(() => new ShowdownResult(null, Eval("Ac Kd 9h 6s 3c")));
            Assert.Throws<ArgumentNullException>(() => new ShowdownResult(Eval("Ac Kd 9h 6s 3c"), null));
        }
    }

    public class DrawRoundTests
    {
        static DrawRound NewRound(out Deck deck, int seed = 2026)
        {
            deck = new Deck(new SeededRandomSource(seed));
            deck.Shuffle();
            return new DrawRound(deck);
        }

        static DrawRound NewDealtRound(int seed = 2026)
        {
            var round = NewRound(out _, seed);
            round.Deal();
            return round;
        }

        [Test]
        public void DealGivesFiveCardsToEachSeat()
        {
            var round = NewRound(out var deck);
            round.Deal();

            Assert.AreEqual(5, round.PlayerCards.Count);
            Assert.AreEqual(5, round.OpponentCards.Count);
            Assert.AreEqual(42, deck.Remaining, "Ten cards should have left the deck.");
            Assert.IsTrue(round.IsDealt);
        }

        [Test]
        public void DealtCardsAreAllDifferent()
        {
            var round = NewDealtRound();

            var all = round.PlayerCards.Concat(round.OpponentCards).ToList();
            Assert.AreEqual(10, all.Distinct().Count(), "The same card was dealt to both seats.");
        }

        [Test]
        public void DealingTwiceThrows()
        {
            var round = NewDealtRound();
            Assert.Throws<InvalidOperationException>(() => round.Deal());
        }

        [Test]
        public void ReplacingBeforeDealingThrows()
        {
            var round = NewRound(out _);
            Assert.Throws<InvalidOperationException>(() => round.Replace(Seat.Player, new[] { 0 }));
        }

        [Test]
        public void ShowdownBeforeDealingThrows()
        {
            var round = NewRound(out _);
            Assert.Throws<InvalidOperationException>(() => round.Showdown());
        }

        [Test]
        public void ReplacingRespectsTheThreeCardLimit()
        {
            var round = NewDealtRound();
            Assert.Throws<ArgumentException>(() => round.Replace(Seat.Player, new[] { 0, 1, 2, 3 }));
        }

        [Test]
        public void ReplacingRejectsDuplicateAndOutOfRangeIndices()
        {
            var round = NewDealtRound();

            Assert.Throws<ArgumentException>(() => round.Replace(Seat.Player, new[] { 1, 1 }));
            Assert.Throws<ArgumentException>(() => round.Replace(Seat.Player, new[] { 5 }));
            Assert.IsFalse(round.HasDrawn(Seat.Player), "A rejected draw must not count as the seat's draw.");
        }

        [Test]
        public void EachSeatGetsExactlyOneDraw()
        {
            var round = NewDealtRound();

            Assert.IsFalse(round.HasDrawn(Seat.Player));
            round.Replace(Seat.Player, new[] { 0 });
            Assert.IsTrue(round.HasDrawn(Seat.Player));

            Assert.Throws<InvalidOperationException>(() => round.Replace(Seat.Player, new[] { 1 }),
                "Five-card draw allows one discard round, not two.");

            // The other seat is unaffected.
            Assert.IsFalse(round.HasDrawn(Seat.Opponent));
            round.Replace(Seat.Opponent, new[] { 0, 1 });
            Assert.IsTrue(round.HasDrawn(Seat.Opponent));
        }

        [Test]
        public void StandingPatLeavesTheHandUntouchedButUsesTheDraw()
        {
            var round = NewRound(out var deck);
            round.Deal();

            var before = round.PlayerCards.ToArray();
            var remainingBefore = deck.Remaining;

            var replacements = round.Replace(Seat.Player, new int[0]);

            Assert.IsEmpty(replacements);
            Assert.AreEqual(before, round.PlayerCards.ToArray());
            Assert.AreEqual(remainingBefore, deck.Remaining, "Standing pat must not draw cards.");
            Assert.IsTrue(round.HasDrawn(Seat.Player), "Standing pat still consumes the draw.");
        }

        [Test]
        public void ReplacingSwapsOnlyTheSelectedSlots()
        {
            var round = NewDealtRound();
            var before = round.PlayerCards.ToArray();

            var replacements = round.Replace(Seat.Player, new[] { 0, 2 });
            var after = round.PlayerCards.ToArray();

            Assert.AreEqual(2, replacements.Length);
            Assert.AreEqual(before[1], after[1], "Slot 1 was not selected and must not change.");
            Assert.AreEqual(before[3], after[3], "Slot 3 was not selected and must not change.");
            Assert.AreEqual(before[4], after[4], "Slot 4 was not selected and must not change.");

            // Replacements come from the remaining deck, which cannot contain a dealt
            // card, so the selected slots must now hold different cards.
            Assert.AreNotEqual(before[0], after[0]);
            Assert.AreNotEqual(before[2], after[2]);
            Assert.AreEqual(replacements[0], after[0]);
            Assert.AreEqual(replacements[1], after[2]);
        }

        [Test]
        public void NoCardIsDuplicatedAfterBothSeatsDraw()
        {
            var round = NewDealtRound();

            round.Replace(Seat.Player, new[] { 0, 1, 2 });
            round.Replace(Seat.Opponent, new[] { 3, 4 });

            var all = round.PlayerCards.Concat(round.OpponentCards).ToList();
            Assert.AreEqual(10, all.Distinct().Count(),
                "Replacement must never reintroduce a card already in play.");
        }

        [Test]
        public void CardsForMatchesTheSeatProperties()
        {
            var round = NewDealtRound();

            Assert.AreEqual(round.PlayerCards, round.CardsFor(Seat.Player));
            Assert.AreEqual(round.OpponentCards, round.CardsFor(Seat.Opponent));
        }

        [Test]
        public void ShowdownProducesAValidResult()
        {
            var round = NewDealtRound();

            round.Replace(Seat.Player, new[] { 0 });
            round.Replace(Seat.Opponent, new[] { 0 });

            var result = round.Showdown();

            Assert.IsNotNull(result.PlayerHand);
            Assert.IsNotNull(result.OpponentHand);
            Assert.That(result.Winner, Is.Null.Or.EqualTo(Seat.Player).Or.EqualTo(Seat.Opponent));
            Assert.IsNotEmpty(result.PlayerHand.Describe());
        }

        [Test]
        public void TheWholeFirstMilestoneSequenceWorks()
        {
            // The exact sequence the GDD asks for in §13: start a round, receive five
            // cards, discard up to three, draw replacements, evaluate, compare.
            var deck = new Deck(new SeededRandomSource(31337));
            deck.ResetAndShuffle();

            var round = new DrawRound(deck);
            round.Deal();

            Assert.AreEqual(5, round.PlayerCards.Count);
            Assert.AreEqual(5, round.OpponentCards.Count);

            round.Replace(Seat.Player, new[] { 0, 1, 2 });
            round.Replace(Seat.Opponent, new[] { 4 });

            var result = round.Showdown();

            Assert.AreEqual(5, round.PlayerCards.Count, "The hand must still hold five cards.");
            Assert.AreEqual(5, round.OpponentCards.Count);
            Assert.AreEqual(10, round.PlayerCards.Concat(round.OpponentCards).Distinct().Count());

            // Consistent with a direct evaluation of the same cards.
            var expectedPlayer = HandEvaluator.Evaluate(round.PlayerCards);
            Assert.AreEqual(0, expectedPlayer.CompareTo(result.PlayerHand));
        }

        [Test]
        public void ManyRoundsFromOneShuffledDeckNeverDuplicateACard()
        {
            // Four rounds is 40 dealt cards plus up to 24 replacements, which exceeds 52 —
            // so this also proves the deck refuses to over-deal rather than repeating itself.
            var deck = new Deck(new SeededRandomSource(4242));
            deck.ResetAndShuffle();

            var seen = new HashSet<Card>();
            var roundsCompleted = 0;

            try
            {
                for (var i = 0; i < 4; i++)
                {
                    var round = new DrawRound(deck);
                    round.Deal();
                    round.Replace(Seat.Player, new[] { 0, 1, 2 });
                    round.Replace(Seat.Opponent, new[] { 0, 1, 2 });

                    // A drawn card never returns to the deck, so across rounds from one
                    // shuffle every card in play must be one we have not seen before.
                    foreach (var card in round.PlayerCards.Concat(round.OpponentCards))
                        Assert.IsTrue(seen.Add(card),
                            $"Card {card} appeared in two different rounds from the same deck.");

                    roundsCompleted++;
                }
            }
            catch (InvalidOperationException)
            {
                // Expected once the deck runs low: a loud refusal, not a duplicate.
            }

            Assert.GreaterOrEqual(roundsCompleted, 2, "Should manage at least two full rounds from 52 cards.");
        }

        [Test]
        public void RequiresADeck()
        {
            Assert.Throws<ArgumentNullException>(() => new DrawRound(null));
        }
    }
}
