using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BlackHoleBluff.Core.Ai;
using BlackHoleBluff.Core.Cards;
using BlackHoleBluff.Core.GameRules;
using BlackHoleBluff.Core.Match;
using NUnit.Framework;

namespace BlackHoleBluff.Tests
{
    /// <summary>
    /// Guards the information restriction in GDD §5 structurally, so it cannot be
    /// weakened by accident later. An opponent that can see the player's cards is both
    /// unfair and impossible to balance.
    /// </summary>
    public class OpponentInformationBoundaryTests
    {
        static bool MentionsCard(Type type)
        {
            if (type == typeof(Card)) return true;
            if (type.IsArray) return MentionsCard(type.GetElementType());
            return type.IsGenericType && type.GetGenericArguments().Any(MentionsCard);
        }

        [Test]
        public void ObservationExposesExactlyOneCardCollectionAndItIsTheObserversOwn()
        {
            var members = typeof(DuelObservation)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(p => (p.Name, Type: p.PropertyType))
                .Concat(typeof(DuelObservation)
                    .GetFields(BindingFlags.Public | BindingFlags.Instance)
                    .Select(f => (f.Name, Type: f.FieldType)))
                .Where(m => MentionsCard(m.Type))
                .ToList();

            Assert.AreEqual(1, members.Count,
                "DuelObservation must carry exactly one set of cards. Found: " +
                string.Join(", ", members.Select(m => $"{m.Name}:{m.Type.Name}")));

            Assert.AreEqual("MyCards", members[0].Name,
                "The only cards an opponent may see are its own.");
        }

        [Test]
        public void ObservationCannotReachTheMatchOrTheRound()
        {
            var forbidden = new[]
            {
                typeof(DrawRound), typeof(DuelMatch), typeof(ShowdownResult),
                typeof(GravityTrack), typeof(HandResult), typeof(Deck)
            };

            var leaks = typeof(DuelObservation)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(p => (p.Name, Type: p.PropertyType))
                .Concat(typeof(DuelObservation)
                    .GetFields(BindingFlags.Public | BindingFlags.Instance)
                    .Select(f => (f.Name, Type: f.FieldType)))
                .Where(m => forbidden.Contains(m.Type))
                .ToList();

            Assert.IsEmpty(leaks,
                "An observation that can reach the match can reach the player's hand. Found: " +
                string.Join(", ", leaks.Select(m => $"{m.Name}:{m.Type.Name}")));
        }

        [Test]
        public void OpponentsAreOnlyEverHandedAnObservation()
        {
            var offenders = typeof(IDuelOpponent)
                .GetMethods()
                .SelectMany(m => m.GetParameters().Select(p => (Method: m.Name, p.ParameterType)))
                .Where(x => x.ParameterType != typeof(DuelObservation))
                .ToList();

            Assert.IsEmpty(offenders,
                "Every decision must take only a DuelObservation. Found: " +
                string.Join(", ", offenders.Select(x => $"{x.Method}({x.ParameterType.Name})")));
        }
    }

    public class BasicOpponentTests
    {
        /// <summary>Fully deterministic: no bluffing, no noise.</summary>
        static OpponentPersonality Predictable(float caution = 0.35f, float selfPreservation = 0.6f) =>
            new OpponentPersonality("Predictable", 0.45f, 0f, caution, selfPreservation, 0f);

        static BasicOpponent NewOpponent(OpponentPersonality? personality = null, int seed = 11) =>
            new BasicOpponent(personality ?? Predictable(), new SeededRandomSource(seed));

        static DuelObservation Observe(
            string cards, int mySteps = 4, int opponentSteps = 4,
            PressureLevel? pressure = null, int opponentDiscards = 2) =>
            new DuelObservation(
                Card.ParseMany(cards), mySteps, opponentSteps, pressure,
                handNumber: 1, myDiscardCount: 0, opponentDiscardCount: opponentDiscards, foldCost: 1);

        // ------------------------------------------------------------- discards

        [Test]
        public void StandsPatOnAStraightOrBetter()
        {
            var opponent = NewOpponent();

            Assert.IsEmpty(opponent.ChooseDiscards(Observe("9c 8d 7h 6s 5c")), "straight");
            Assert.IsEmpty(opponent.ChooseDiscards(Observe("Ac Jc 9c 6c 3c")), "flush");
            Assert.IsEmpty(opponent.ChooseDiscards(Observe("Kc Kd Kh 3s 3d")), "full house");
            Assert.IsEmpty(opponent.ChooseDiscards(Observe("As Ks Qs Js Ts")), "royal flush");
        }

        [Test]
        public void KeepsAPairAndThrowsTheOtherThree()
        {
            // 8c 8d Kh 6s 2c -> keep positions 0 and 1.
            var discards = NewOpponent().ChooseDiscards(Observe("8c 8d Kh 6s 2c"));

            Assert.AreEqual(new[] { 2, 3, 4 }, discards);
        }

        [Test]
        public void KeepsTwoPairAndThrowsTheKicker()
        {
            // Jc Jd 4h 4s 9c -> only position 4 is spare.
            Assert.AreEqual(new[] { 4 }, NewOpponent().ChooseDiscards(Observe("Jc Jd 4h 4s 9c")));
        }

        [Test]
        public void KeepsTripsAndThrowsTwo()
        {
            Assert.AreEqual(new[] { 3, 4 }, NewOpponent().ChooseDiscards(Observe("Qc Qd Qh 7s 2c")));
        }

        [Test]
        public void WithNoPairKeepsTheTwoHighestCards()
        {
            // Ac Qd 9h 6s 3c -> keep the ace (0) and the queen (1), throw 2, 3, 4.
            Assert.AreEqual(new[] { 2, 3, 4 }, NewOpponent().ChooseDiscards(Observe("Ac Qd 9h 6s 3c")));
        }

        [Test]
        public void NeverReturnsAnIllegalDiscardSelection()
        {
            var random = new SeededRandomSource(99);
            var opponent = NewOpponent();
            var deck = new Deck(random);

            for (var i = 0; i < 500; i++)
            {
                deck.ResetAndShuffle();
                var hand = deck.Draw(5);

                var discards = opponent.ChooseDiscards(
                    new DuelObservation(hand, 4, 4, null, 1, 0, 0, 1));

                Assert.IsTrue(FiveCardDrawRules.IsValidDiscardSelection(discards, out var error),
                    $"Hand {string.Join(" ", hand.Select(c => c.ToString()))}: {error}");
            }
        }

        [Test]
        public void RejectsAHandThatIsNotFiveCards()
        {
            Assert.Throws<ArgumentException>(
                () => NewOpponent().ChooseDiscards(Observe("Ac Kd Qh")));
        }

        // ---------------------------------------------------------------- wagers

        [Test]
        public void WagersHigherWithAStrongerHand()
        {
            var opponent = NewOpponent();

            var weak = opponent.ChooseWager(Observe("7c 5d 3h 2s 9c"));
            var strong = opponent.ChooseWager(Observe("As Ks Qs Js Ts"));

            Assert.Less((int)weak, (int)strong,
                "A royal flush must not be wagered as timidly as a junk hand.");
        }

        [Test]
        public void DoesNotWagerItsOwnLifeOnAWeakHand()
        {
            // Two steps left: losing a called Pressure 2 or 3 is fatal, so a junk hand
            // should not reach for one. The aggressor risks the wager too.
            var level = NewOpponent().ChooseWager(Observe("7c 5d 3h 2s 9c", mySteps: 2));

            Assert.AreEqual(PressureLevel.One, level);
        }

        [Test]
        public void ReachesForTheFinisherWhenTheOpponentIsNearlyGone()
        {
            // Strong hand, opponent two steps from the horizon -> wager exactly two.
            var level = NewOpponent().ChooseWager(Observe("Kc Kd Kh 3s 3d", opponentSteps: 2));

            Assert.GreaterOrEqual((int)level, 2);
        }

        [Test]
        public void AlwaysWagersALegalLevel()
        {
            var random = new SeededRandomSource(7);
            var opponent = new BasicOpponent(OpponentPersonality.Default, random);
            var deck = new Deck(random);

            for (var i = 0; i < 400; i++)
            {
                deck.ResetAndShuffle();
                var hand = deck.Draw(5);
                var steps = random.NextInt(1, 5);

                var level = opponent.ChooseWager(
                    new DuelObservation(hand, steps, random.NextInt(1, 5), null, 1, 0, 0, 1));

                Assert.That((int)level, Is.InRange(1, 3));
            }
        }

        // -------------------------------------------------------------- responses

        [Test]
        public void CallsWhenFoldingWouldItselfBeFatal()
        {
            // One step left and folding costs one, so folding is death. Calling is the
            // only line that can survive, whatever the cards say.
            var response = NewOpponent().RespondToWager(
                Observe("7c 5d 3h 2s 9c", mySteps: 1, pressure: PressureLevel.Three));

            Assert.AreEqual(WagerResponse.Call, response);
        }

        [Test]
        public void FoldsAJunkHandToHeavyPressure()
        {
            var response = NewOpponent().RespondToWager(
                Observe("7c 5d 3h 2s 9c", pressure: PressureLevel.Three));

            Assert.AreEqual(WagerResponse.Fold, response);
        }

        [Test]
        public void CallsAStrongHandEvenAtMaximumPressure()
        {
            var response = NewOpponent().RespondToWager(
                Observe("As Ks Qs Js Ts", pressure: PressureLevel.Three));

            Assert.AreEqual(WagerResponse.Call, response);
        }

        [Test]
        public void AlwaysCallsWhenTheWagerIsNoBiggerThanTheFoldCost()
        {
            // With the default numbers, folding costs one step and Pressure 1 risks one
            // step. The downside is identical, so folding can only lose value — it is
            // weakly dominated and must never be chosen, even holding junk.
            var opponent = NewOpponent();

            Assert.AreEqual(WagerResponse.Call,
                opponent.RespondToWager(Observe("8c 8d Kh 6s 2c", pressure: PressureLevel.One)));

            Assert.AreEqual(WagerResponse.Call,
                opponent.RespondToWager(Observe("7c 5d 3h 2s 9c", pressure: PressureLevel.One)),
                "Even the worst hand should call a wager it cannot lose value to.");
        }

        [Test]
        public void SelfPreservationMakesAFatalCallHarder()
        {
            var reckless = new BasicOpponent(
                Predictable(selfPreservation: 0f), new SeededRandomSource(5));
            var careful = new BasicOpponent(
                Predictable(selfPreservation: 1f), new SeededRandomSource(5));

            // Two pair sits between the two thresholds: above the ordinary bar for
            // Pressure 2, below it once the self-preservation penalty for a potentially
            // fatal call is added.
            var observation = Observe("Jc Jd 4h 4s 9c", mySteps: 2, pressure: PressureLevel.Two);

            Assert.AreEqual(WagerResponse.Call, reckless.RespondToWager(observation));
            Assert.AreEqual(WagerResponse.Fold, careful.RespondToWager(observation),
                "Self-preservation should raise the bar when losing means death.");
        }

        [Test]
        public void ReadsTheDrawAsATell()
        {
            var opponent = NewOpponent();

            // A small pair sits just either side of the Pressure 2 bar once the tell is
            // applied, so the read is the only thing that decides the hand.
            var threwThree = Observe("5c 5d Kh 8s 2c", pressure: PressureLevel.Two, opponentDiscards: 3);
            var stoodPat = Observe("5c 5d Kh 8s 2c", pressure: PressureLevel.Two, opponentDiscards: 0);

            Assert.AreEqual(WagerResponse.Call, opponent.RespondToWager(threwThree),
                "Three cards away suggests weakness, so a marginal hand is worth a call.");
            Assert.AreEqual(WagerResponse.Fold, opponent.RespondToWager(stoodPat),
                "Standing pat suggests strength, so the same hand should back off.");
        }

        [Test]
        public void RespondingWithNoWagerOnTheTableThrows()
        {
            Assert.Throws<InvalidOperationException>(
                () => NewOpponent().RespondToWager(Observe("8c 8d Kh 6s 2c")));
        }

        [Test]
        public void RequiresARandomSource()
        {
            Assert.Throws<ArgumentNullException>(
                () => new BasicOpponent(OpponentPersonality.Default, null));
        }

        [Test]
        public void BluffingIsPossibleSoAlwaysFoldingCanBePunished()
        {
            // With a weak hand and a high bluff chance, the opponent must sometimes
            // reach for a big wager. Otherwise a player who always folds to Pressure 3
            // would never be exploited (GDD §3.3).
            var bluffer = new BasicOpponent(
                new OpponentPersonality("Bluffer", 0.5f, 1f, 0.3f, 0.3f, 0f),
                new SeededRandomSource(3));

            var level = bluffer.ChooseWager(Observe("7c 5d 3h 2s 9c"));

            Assert.Greater((int)level, 1, "A guaranteed bluff should raise the pressure.");
        }
    }
}
