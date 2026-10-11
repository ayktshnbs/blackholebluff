using System;
using BlackHoleBluff.Core.Cards;
using BlackHoleBluff.Core.GameRules;
using NUnit.Framework;

namespace BlackHoleBluff.Tests
{
    /// <summary>
    /// Hand evaluation and comparison (GDD §11). The brief is explicit that hands must
    /// not be compared by category alone or by summing card values, so kickers and the
    /// ace-low straight get direct coverage.
    /// </summary>
    public class HandEvaluatorTests
    {
        static HandResult Eval(string cards) => HandEvaluator.Evaluate(Card.ParseMany(cards));

        static void AssertCategory(string cards, HandCategory expected)
        {
            var result = Eval(cards);
            Assert.AreEqual(expected, result.Category,
                $"'{cards}' was read as {result.Category} ({result.Describe()}).");
        }

        // ---------------------------------------------------------- categories

        [Test]
        public void RoyalFlush_IsAStraightFlushWithAnAceHigh()
        {
            var result = Eval("As Ks Qs Js Ts");

            Assert.AreEqual(HandCategory.StraightFlush, result.Category);
            Assert.AreEqual(14, result.Tiebreakers[0]);
            Assert.IsTrue(result.IsRoyalFlush);
            Assert.AreEqual("Royal flush", result.Describe());
        }

        [Test]
        public void StraightFlush_IsRankedByItsHighCard()
        {
            var result = Eval("9h 8h 7h 6h 5h");

            Assert.AreEqual(HandCategory.StraightFlush, result.Category);
            Assert.AreEqual(9, result.Tiebreakers[0]);
            Assert.IsFalse(result.IsRoyalFlush);
        }

        [Test]
        public void SteelWheel_IsAFiveHighStraightFlush()
        {
            // A-2-3-4-5 all in one suit. The Ace plays low, so this is five-high and is
            // the weakest straight flush — not an ace-high monster.
            var result = Eval("Ah 2h 3h 4h 5h");

            Assert.AreEqual(HandCategory.StraightFlush, result.Category);
            Assert.AreEqual(5, result.Tiebreakers[0]);
            Assert.IsFalse(result.IsRoyalFlush);
            Assert.Less(Eval("Ah 2h 3h 4h 5h").CompareTo(Eval("7c 6c 5c 4c 3c")), 0,
                "The steel wheel must lose to a seven-high straight flush.");
        }

        [Test]
        public void FourOfAKind_CarriesQuadRankThenKicker()
        {
            var result = Eval("7c 7d 7h 7s Kd");

            Assert.AreEqual(HandCategory.FourOfAKind, result.Category);
            Assert.AreEqual(7, result.Tiebreakers[0]);
            Assert.AreEqual(13, result.Tiebreakers[1]);
        }

        [Test]
        public void FullHouse_CarriesTripsRankThenPairRank()
        {
            var result = Eval("Kc Kd Kh 3s 3d");

            Assert.AreEqual(HandCategory.FullHouse, result.Category);
            Assert.AreEqual(13, result.Tiebreakers[0]);
            Assert.AreEqual(3, result.Tiebreakers[1]);
            Assert.AreEqual("Full house, kings over threes", result.Describe());
        }

        [Test]
        public void Flush_CarriesAllFiveRanksDescending()
        {
            var result = Eval("Ac Jc 9c 6c 3c");

            Assert.AreEqual(HandCategory.Flush, result.Category);
            Assert.AreEqual(new[] { 14, 11, 9, 6, 3 }, result.Tiebreakers);
        }

        [Test]
        public void Straight_IsRankedByItsHighCard()
        {
            AssertCategory("9c 8d 7h 6s 5c", HandCategory.Straight);
            Assert.AreEqual(9, Eval("9c 8d 7h 6s 5c").Tiebreakers[0]);
        }

        [Test]
        public void Broadway_IsAnAceHighStraight()
        {
            var result = Eval("Ac Kd Qh Js Tc");

            Assert.AreEqual(HandCategory.Straight, result.Category);
            Assert.AreEqual(14, result.Tiebreakers[0]);
        }

        [Test]
        public void ThreeOfAKind_CarriesTripsThenTwoKickersDescending()
        {
            var result = Eval("Qc Qd Qh 7s 2c");

            Assert.AreEqual(HandCategory.ThreeOfAKind, result.Category);
            Assert.AreEqual(new[] { 12, 7, 2 }, result.Tiebreakers);
        }

        [Test]
        public void TwoPair_CarriesHighPairLowPairThenKicker()
        {
            var result = Eval("Jc Jd 4h 4s 9c");

            Assert.AreEqual(HandCategory.TwoPair, result.Category);
            Assert.AreEqual(new[] { 11, 4, 9 }, result.Tiebreakers);
            Assert.AreEqual("Two pair, jacks and fours", result.Describe());
        }

        [Test]
        public void OnePair_CarriesPairThenThreeKickersDescending()
        {
            var result = Eval("8c 8d Kh 6s 2c");

            Assert.AreEqual(HandCategory.OnePair, result.Category);
            Assert.AreEqual(new[] { 8, 13, 6, 2 }, result.Tiebreakers);
        }

        [Test]
        public void HighCard_CarriesAllFiveRanksDescending()
        {
            var result = Eval("Ac Qd 9h 6s 3c");

            Assert.AreEqual(HandCategory.HighCard, result.Category);
            Assert.AreEqual(new[] { 14, 12, 9, 6, 3 }, result.Tiebreakers);
            Assert.AreEqual("Ace high", result.Describe());
        }

        // ---------------------------------------------- straights that are not

        [Test]
        public void AceKingQueenJackNine_IsNotAStraight()
        {
            AssertCategory("Ac Kd Qh Js 9c", HandCategory.HighCard);
        }

        [Test]
        public void StraightsDoNotWrapAroundTheAce()
        {
            // K-A-2-3-4 is not a straight in any variant. Only A-2-3-4-5 counts.
            AssertCategory("Kc Ad 2h 3s 4c", HandCategory.HighCard);
        }

        [Test]
        public void TheWheelIsAStraightButRanksBelowSixHigh()
        {
            var wheel = Eval("Ac 2d 3h 4s 5c");

            Assert.AreEqual(HandCategory.Straight, wheel.Category);
            Assert.AreEqual(5, wheel.Tiebreakers[0], "The Ace plays low, so this is five-high.");

            Assert.Less(wheel.CompareTo(Eval("6c 5d 4h 3s 2c")), 0,
                "A wheel must lose to a six-high straight.");
            Assert.Greater(wheel.CompareTo(Eval("Ac Ad Kh Qs Jc")), 0,
                "A wheel is still a straight, so it beats a pair of aces.");
        }

        [Test]
        public void PairOfAcesIsNotMistakenForAStraight()
        {
            AssertCategory("Ac Ad 5h 4s 3c", HandCategory.OnePair);
        }

        // ---------------------------------------------------------- ordering

        // One representative hand per category, weakest first.
        static readonly (HandCategory Category, string Cards)[] Ladder =
        {
            (HandCategory.HighCard, "Ac Qd 9h 6s 3c"),
            (HandCategory.OnePair, "8c 8d Kh 6s 2c"),
            (HandCategory.TwoPair, "Jc Jd 4h 4s 9c"),
            (HandCategory.ThreeOfAKind, "Qc Qd Qh 7s 2c"),
            (HandCategory.Straight, "9c 8d 7h 6s 5c"),
            (HandCategory.Flush, "Ac Jc 9c 6c 3c"),
            (HandCategory.FullHouse, "Kc Kd Kh 3s 3d"),
            (HandCategory.FourOfAKind, "7c 7d 7h 7s Kd"),
            (HandCategory.StraightFlush, "9h 8h 7h 6h 5h")
        };

        [Test]
        public void EveryCategoryBeatsEveryWeakerCategory()
        {
            for (var strong = 0; strong < Ladder.Length; strong++)
            {
                var strongHand = Eval(Ladder[strong].Cards);
                Assert.AreEqual(Ladder[strong].Category, strongHand.Category,
                    $"Ladder entry '{Ladder[strong].Cards}' is miscategorised.");

                for (var weak = 0; weak < strong; weak++)
                {
                    var weakHand = Eval(Ladder[weak].Cards);

                    Assert.Greater(strongHand.CompareTo(weakHand), 0,
                        $"{Ladder[strong].Category} ({Ladder[strong].Cards}) should beat " +
                        $"{Ladder[weak].Category} ({Ladder[weak].Cards}).");
                    Assert.Less(weakHand.CompareTo(strongHand), 0, "Comparison must be symmetric.");
                }
            }
        }

        // ------------------------------------------------------- tie-breaking

        [Test]
        public void IdenticalRanksInDifferentSuitsTie()
        {
            // Suits never break ties in this game.
            var a = Eval("Ac Kd 9h 6s 3c");
            var b = Eval("Ad Kh 9s 6c 3d");

            Assert.AreEqual(0, a.CompareTo(b));
            Assert.IsTrue(a.Equals(b));
        }

        [Test]
        public void SamePair_IsDecidedByTheLastKicker()
        {
            // Both hold a pair of eights with K and 6. Only the final kicker differs.
            var lower = Eval("8c 8d Kh 6s 2c");
            var higher = Eval("8h 8s Kd 6c 3d");

            Assert.Less(lower.CompareTo(higher), 0,
                "A three-kicker must beat a two-kicker when everything above is equal.");
        }

        [Test]
        public void TwoPair_IsDecidedByKickerWhenBothPairsMatch()
        {
            var lower = Eval("Jc Jd 4h 4s 7c");
            var higher = Eval("Jh Js 4c 4d 9h");

            Assert.Less(lower.CompareTo(higher), 0);
        }

        [Test]
        public void TwoPair_HighPairOutranksTheLowPair()
        {
            // Queens-and-twos beats jacks-and-tens: the top pair decides first.
            Assert.Greater(Eval("Qc Qd 2h 2s 7c").CompareTo(Eval("Jc Jd Th Ts 9c")), 0);
        }

        [Test]
        public void FullHouse_TripsRankDecidesBeforeThePair()
        {
            // Threes full of twos beats twos full of aces, despite the aces.
            var threesFullOfTwos = Eval("3c 3d 3h 2s 2d");
            var twosFullOfAces = Eval("2c 2h 2s Ac Ad");

            Assert.Greater(threesFullOfTwos.CompareTo(twosFullOfAces), 0);
        }

        [Test]
        public void FourOfAKind_IsDecidedByKickerWhenQuadsMatchIsImpossible()
        {
            // Quads of the same rank cannot both occur, so quad rank always decides.
            Assert.Greater(Eval("9c 9d 9h 9s 2c").CompareTo(Eval("8c 8d 8h 8s Ac")), 0,
                "Nine quads beat eight quads regardless of kickers.");
        }

        [Test]
        public void Flush_IsComparedRankByRank()
        {
            var aceHigh = Eval("Ac Jc 9c 6c 3c");
            var kingHigh = Eval("Kd Qd Jd Td 2d");

            Assert.Greater(aceHigh.CompareTo(kingHigh), 0);

            // Equal top card, decided further down.
            var lower = Eval("Ac Jc 9c 6c 3c");
            var higher = Eval("Ad Jd 9d 6d 4d");
            Assert.Less(lower.CompareTo(higher), 0, "Fifth card must still decide a flush.");
        }

        [Test]
        public void HighCard_IsComparedRankByRank()
        {
            Assert.Less(Eval("Ac Qd 9h 6s 3c").CompareTo(Eval("Ad Qh 9s 6c 4h")), 0);
            Assert.Greater(Eval("Ac Kd 9h 6s 3c").CompareTo(Eval("Ad Qh Js Tc 8h")), 0);
        }

        [Test]
        public void ComparisonOperatorsAgreeWithCompareTo()
        {
            var strong = Eval("As Ks Qs Js Ts");
            var weak = Eval("Ac Qd 9h 6s 3c");

            Assert.IsTrue(strong > weak);
            Assert.IsTrue(weak < strong);
            Assert.IsTrue(strong >= weak);
            Assert.IsTrue(weak <= strong);
        }

        // --------------------------------------------------------- validation

        [Test]
        public void RejectsWrongNumberOfCards()
        {
            Assert.Throws<ArgumentException>(() => Eval("Ac Kd Qh Js"), "Four cards should throw.");
            Assert.Throws<ArgumentException>(() => Eval("Ac Kd Qh Js Tc 9d"), "Six cards should throw.");
        }

        [Test]
        public void RejectsDuplicateCards()
        {
            // A duplicate means a deck bug upstream; it must not be silently scored.
            var error = Assert.Throws<ArgumentException>(() => Eval("Ac Ac Kd Qh Js"));
            Assert.That(error.Message, Does.Contain("Duplicate"));
        }

        [Test]
        public void RejectsNull()
        {
            Assert.Throws<ArgumentNullException>(() => HandEvaluator.Evaluate(null));
        }

        [Test]
        public void EvaluationIsDeterministic()
        {
            var first = Eval("Kc Kd Kh 3s 3d");
            var second = Eval("Kc Kd Kh 3s 3d");

            Assert.AreEqual(0, first.CompareTo(second));
            Assert.AreEqual(first.Category, second.Category);
            Assert.AreEqual(first.Tiebreakers, second.Tiebreakers);
        }

        [Test]
        public void CardOrderWithinAHandDoesNotMatter()
        {
            Assert.AreEqual(0, Eval("Kc Kd Kh 3s 3d").CompareTo(Eval("3d Kh 3s Kc Kd")));
        }

        // --------------------------------------------------------- Compare()

        [Test]
        public void CompareHelperMatchesHandResultComparison()
        {
            var left = Card.ParseMany("As Ks Qs Js Ts");
            var right = Card.ParseMany("Ac Qd 9h 6s 3c");

            Assert.Greater(HandEvaluator.Compare(left, right), 0);
            Assert.Less(HandEvaluator.Compare(right, left), 0);
            Assert.AreEqual(0, HandEvaluator.Compare(left, left));
        }
    }
}
