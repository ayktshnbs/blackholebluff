using System;
using BlackHoleBluff.Core.Cards;
using BlackHoleBluff.Core.GameRules;
using BlackHoleBluff.Core.Match;
using NUnit.Framework;

namespace BlackHoleBluff.Tests
{
    public class MatchConfigTests
    {
        [Test]
        public void DefaultIsFourStepsAndAOneStepFold()
        {
            var config = MatchConfig.Default;

            Assert.AreEqual(4, config.StartingSteps);
            Assert.AreEqual(1, config.FoldCost);
        }

        [Test]
        public void RejectsNonsenseValues()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new MatchConfig(0, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new MatchConfig(4, -1));
        }
    }

    public class GravityTrackTests
    {
        static GravityTrack NewTrack() => new GravityTrack(MatchConfig.Default);

        [Test]
        public void BothStartAtFullDistance()
        {
            var track = NewTrack();

            Assert.AreEqual(4, track.StepsRemaining(Seat.Player));
            Assert.AreEqual(4, track.StepsRemaining(Seat.Opponent));
            Assert.IsFalse(track.AnyoneConsumed);
            Assert.IsNull(track.Survivor);
        }

        [Test]
        public void MovingCloserReducesStepsForThatSeatOnly()
        {
            var track = NewTrack();
            track.MoveCloser(Seat.Player, 2);

            Assert.AreEqual(2, track.StepsRemaining(Seat.Player));
            Assert.AreEqual(4, track.StepsRemaining(Seat.Opponent));
        }

        [Test]
        public void ReachingZeroMeansConsumed()
        {
            var track = NewTrack();
            track.MoveCloser(Seat.Opponent, 4);

            Assert.AreEqual(0, track.StepsRemaining(Seat.Opponent));
            Assert.IsTrue(track.IsConsumed(Seat.Opponent));
            Assert.IsTrue(track.AnyoneConsumed);
            Assert.AreEqual(Seat.Player, track.Survivor);
        }

        [Test]
        public void StepsNeverGoNegativeAndTheMoveIsReportedHonestly()
        {
            // MVP acceptance criterion 6: distance must never become invalid.
            var track = NewTrack();
            track.MoveCloser(Seat.Player, 3);

            var applied = track.MoveCloser(Seat.Player, 3);

            Assert.AreEqual(1, applied, "Only one step was available, so only one was taken.");
            Assert.AreEqual(0, track.StepsRemaining(Seat.Player));
        }

        [Test]
        public void MovingZeroStepsIsAllowedAndChangesNothing()
        {
            var track = NewTrack();
            Assert.AreEqual(0, track.MoveCloser(Seat.Player, 0));
            Assert.AreEqual(4, track.StepsRemaining(Seat.Player));
        }

        [Test]
        public void GravityCannotPushACharacterToSafety()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => NewTrack().MoveCloser(Seat.Player, -1));
        }

        [Test]
        public void ResetRestoresFullDistance()
        {
            var track = NewTrack();
            track.MoveCloser(Seat.Player, 4);
            track.MoveCloser(Seat.Opponent, 2);

            track.Reset();

            Assert.AreEqual(4, track.StepsRemaining(Seat.Player));
            Assert.AreEqual(4, track.StepsRemaining(Seat.Opponent));
            Assert.IsFalse(track.AnyoneConsumed);
        }

        [Test]
        public void SurvivorIsNullWhileBothAreAlive()
        {
            var track = NewTrack();
            track.MoveCloser(Seat.Player, 3);

            Assert.IsNull(track.Survivor);
        }
    }

    public class HandStrengthTests
    {
        static float Strength(string cards) =>
            HandStrength.Estimate(HandEvaluator.Evaluate(Card.ParseMany(cards)));

        [Test]
        public void StaysWithinZeroToOne()
        {
            Assert.That(Strength("3c 2d 5h 4s 7c"), Is.InRange(0f, 1f));
            Assert.That(Strength("As Ks Qs Js Ts"), Is.InRange(0f, 1f));
        }

        [Test]
        public void RoyalFlushIsTheMaximum()
        {
            Assert.AreEqual(1f, Strength("As Ks Qs Js Ts"), 0.0001f);
        }

        [Test]
        public void StrongerCategoriesScoreHigher()
        {
            var ladder = new[]
            {
                "Ac Qd 9h 6s 3c",   // high card
                "8c 8d Kh 6s 2c",   // one pair
                "Jc Jd 4h 4s 9c",   // two pair
                "Qc Qd Qh 7s 2c",   // trips
                "9c 8d 7h 6s 5c",   // straight
                "Ac Jc 9c 6c 3c",   // flush
                "Kc Kd Kh 3s 3d",   // full house
                "7c 7d 7h 7s Kd",   // quads
                "9h 8h 7h 6h 5h"    // straight flush
            };

            for (var i = 1; i < ladder.Length; i++)
                Assert.Greater(Strength(ladder[i]), Strength(ladder[i - 1]),
                    $"'{ladder[i]}' should score above '{ladder[i - 1]}'.");
        }

        [Test]
        public void WithinACategoryHigherRanksScoreHigher()
        {
            Assert.Greater(Strength("Ac Ad 7h 5s 2c"), Strength("3c 3d 7h 5s 2c"),
                "A pair of aces should outscore a pair of threes.");
        }

        [Test]
        public void RejectsNull()
        {
            Assert.Throws<ArgumentNullException>(() => HandStrength.Estimate(null));
        }
    }
}
