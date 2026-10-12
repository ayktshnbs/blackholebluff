using System;
using System.Collections.Generic;
using BlackHoleBluff.Core.Ai;
using BlackHoleBluff.Core.Cards;
using BlackHoleBluff.Core.GameRules;
using BlackHoleBluff.Core.Match;
using NUnit.Framework;

namespace BlackHoleBluff.Tests
{
    /// <summary>
    /// An opponent whose every decision is dictated by the test, so match mechanics can
    /// be checked without depending on the real AI's judgement.
    /// </summary>
    sealed class ScriptedOpponent : IDuelOpponent
    {
        public string Name => "Scripted";

        public int[] Discards = Array.Empty<int>();
        public PressureLevel Wager = PressureLevel.One;
        public WagerResponse Response = WagerResponse.Call;

        public readonly List<DuelObservation> Seen = new List<DuelObservation>();

        public int[] ChooseDiscards(DuelObservation observation)
        {
            Seen.Add(observation);
            return Discards;
        }

        public PressureLevel ChooseWager(DuelObservation observation)
        {
            Seen.Add(observation);
            return Wager;
        }

        public WagerResponse RespondToWager(DuelObservation observation)
        {
            Seen.Add(observation);
            return Response;
        }
    }

    /// <summary>
    /// Fisher-Yates with j == i every time, so the deck is left in its reset order and
    /// every deal is identical and fully known.
    /// </summary>
    sealed class IdentityShuffleRandom : IRandomSource
    {
        public int NextInt(int minInclusive, int maxExclusive) => maxExclusive - 1;
    }

    /// <summary>
    /// Fisher-Yates with j == 0 every time. Also deterministic, and happens to deal the
    /// opponent the stronger hand — which is how a defeat can be tested exactly.
    /// </summary>
    sealed class ZeroShuffleRandom : IRandomSource
    {
        public int NextInt(int minInclusive, int maxExclusive) => minInclusive;
    }

    public class DuelMatchTests
    {
        static readonly int[] StandPat = Array.Empty<int>();

        static DuelMatch NewMatch(out ScriptedOpponent opponent, IRandomSource random = null,
            MatchConfig? config = null)
        {
            opponent = new ScriptedOpponent();
            var match = new DuelMatch(
                config ?? MatchConfig.Default,
                opponent,
                random ?? new IdentityShuffleRandom());

            match.StartMatch();
            return match;
        }

        // ------------------------------------------------- pinned deterministic deals

        [Test]
        public void IdentityShuffleDealsTheExpectedKnownHands()
        {
            // Pins the assumption the deterministic tests below rely on. If Deck.Reset's
            // ordering ever changes, this fails here with a clear message instead of
            // breaking several tests mysteriously.
            var match = NewMatch(out var opponent, new IdentityShuffleRandom());
            opponent.Discards = StandPat;

            Assert.AreEqual("As Qs Ts 8s 6s", string.Join(" ", match.PlayerCards));

            match.SubmitPlayerDiscards(StandPat);
            match.SubmitPlayerWager(PressureLevel.One);

            Assert.AreEqual("Ks Js 9s 7s 5s", string.Join(" ", match.RevealedOpponentCards));
            Assert.AreEqual(Seat.Opponent, match.LastOutcome.Loser,
                "Ace-high flush should beat king-high flush.");
        }

        [Test]
        public void ZeroShuffleDealsTheOpponentTheStrongerHand()
        {
            var match = NewMatch(out var opponent, new ZeroShuffleRandom());
            opponent.Discards = StandPat;

            Assert.AreEqual("2c Ks Js 9s 7s", string.Join(" ", match.PlayerCards));

            match.SubmitPlayerDiscards(StandPat);
            match.SubmitPlayerWager(PressureLevel.One);

            Assert.AreEqual(Seat.Player, match.LastOutcome.Loser,
                "A spade flush should beat a king-high hand.");
        }

        // -------------------------------------------------------------- start state

        [Test]
        public void StartMatchDealsTheFirstHandAndWaitsForDiscards()
        {
            var match = NewMatch(out _);

            Assert.AreEqual(MatchPhase.DiscardSelection, match.Phase);
            Assert.AreEqual(1, match.HandNumber);
            Assert.AreEqual(Seat.Player, match.Aggressor, "The player acts first on hand one.");
            Assert.AreEqual(5, match.PlayerCards.Count);
            Assert.AreEqual(4, match.Gravity.StepsRemaining(Seat.Player));
            Assert.AreEqual(4, match.Gravity.StepsRemaining(Seat.Opponent));
            Assert.IsNull(match.Victor);
            Assert.IsNull(match.PressureOnTable);
            Assert.IsNull(match.RevealedOpponentCards);
        }

        [Test]
        public void TheNonAggressorDrawsFirstSoTheInformationEdgeAlternates()
        {
            // The script has to be set before StartMatch, because the opponent draws
            // during the deal when the player is the aggressor.
            var opponent = new ScriptedOpponent { Discards = new[] { 0, 1 } };
            var match = new DuelMatch(MatchConfig.Default, opponent, new IdentityShuffleRandom());
            match.StartMatch();

            Assert.AreEqual(2, match.OpponentDiscardCount,
                "The opponent draws first when the player sets the pressure.");
            Assert.IsFalse(match.PlayerHasDrawn);

            match.SubmitPlayerDiscards(StandPat);
            match.SubmitPlayerWager(PressureLevel.One);
            match.BeginNextHand();

            // Hand 2: the opponent is aggressor, so now the player draws first.
            Assert.AreEqual(Seat.Opponent, match.Aggressor);
            Assert.AreEqual(DuelObservation.Unknown, match.OpponentDiscardCount,
                "The opponent has not drawn yet, which is not the same as standing pat.");
        }

        [Test]
        public void PlayerAggressorIsAskedForAWager()
        {
            var match = NewMatch(out _);
            match.SubmitPlayerDiscards(StandPat);

            Assert.AreEqual(MatchPhase.AwaitingWager, match.Phase);
            Assert.IsNull(match.PressureOnTable);
        }

        [Test]
        public void OpponentAggressorPutsAWagerOnTheTable()
        {
            var match = NewMatch(out var opponent);
            opponent.Wager = PressureLevel.Two;

            match.SubmitPlayerDiscards(StandPat);
            match.SubmitPlayerWager(PressureLevel.One);
            match.BeginNextHand();

            Assert.AreEqual(Seat.Opponent, match.Aggressor);
            match.SubmitPlayerDiscards(StandPat);

            Assert.AreEqual(MatchPhase.AwaitingResponse, match.Phase);
            Assert.AreEqual(PressureLevel.Two, match.PressureOnTable);
        }

        // ------------------------------------------------------------------ folding

        [TestCase(PressureLevel.One)]
        [TestCase(PressureLevel.Two)]
        [TestCase(PressureLevel.Three)]
        public void FoldingAlwaysCostsExactlyOneStepWhateverWasWagered(PressureLevel wagered)
        {
            // The asymmetry that makes bluffing worth doing (GDD 4.3).
            var match = NewMatch(out var opponent);
            opponent.Response = WagerResponse.Fold;

            match.SubmitPlayerDiscards(StandPat);
            match.SubmitPlayerWager(wagered);

            Assert.IsTrue(match.LastOutcome.WasFolded);
            Assert.AreEqual(Seat.Opponent, match.LastOutcome.Folder);
            Assert.AreEqual(1, match.LastOutcome.StepsApplied);
            Assert.AreEqual(3, match.Gravity.StepsRemaining(Seat.Opponent));
            Assert.AreEqual(4, match.Gravity.StepsRemaining(Seat.Player), "The winner never moves.");
        }

        [Test]
        public void FoldingDoesNotRevealAnyCards()
        {
            var match = NewMatch(out var opponent);
            opponent.Response = WagerResponse.Fold;

            match.SubmitPlayerDiscards(StandPat);
            match.SubmitPlayerWager(PressureLevel.Three);

            Assert.IsNull(match.LastOutcome.PlayerHand, "A fold must not reveal hands.");
            Assert.IsNull(match.LastOutcome.OpponentHand);
            Assert.IsNull(match.RevealedOpponentCards,
                "Presentation must not be able to show cards the rules kept hidden.");
        }

        [Test]
        public void ThePlayerCanFoldToo()
        {
            var match = NewMatch(out var opponent);
            opponent.Wager = PressureLevel.Three;

            match.SubmitPlayerDiscards(StandPat);
            match.SubmitPlayerWager(PressureLevel.One);
            match.BeginNextHand();
            match.SubmitPlayerDiscards(StandPat);

            var before = match.Gravity.StepsRemaining(Seat.Player);
            match.SubmitPlayerResponse(WagerResponse.Fold);

            Assert.AreEqual(Seat.Player, match.LastOutcome.Folder);
            Assert.AreEqual(before - 1, match.Gravity.StepsRemaining(Seat.Player));
        }

        // ------------------------------------------------------------------ calling

        [TestCase(PressureLevel.One, 3)]
        [TestCase(PressureLevel.Two, 2)]
        [TestCase(PressureLevel.Three, 1)]
        public void ACalledWagerMovesTheLoserByThePressure(PressureLevel wagered, int expectedRemaining)
        {
            var match = NewMatch(out var opponent);
            opponent.Response = WagerResponse.Call;

            match.SubmitPlayerDiscards(StandPat);
            match.SubmitPlayerWager(wagered);

            Assert.IsFalse(match.LastOutcome.WasFolded);
            Assert.AreEqual(Seat.Opponent, match.LastOutcome.Loser);
            Assert.AreEqual((int)wagered, match.LastOutcome.StepsApplied);
            Assert.AreEqual(expectedRemaining, match.Gravity.StepsRemaining(Seat.Opponent));
            Assert.AreEqual(4, match.Gravity.StepsRemaining(Seat.Player));
        }

        [Test]
        public void ACallRevealsBothHands()
        {
            var match = NewMatch(out var opponent);
            opponent.Response = WagerResponse.Call;

            match.SubmitPlayerDiscards(StandPat);
            match.SubmitPlayerWager(PressureLevel.One);

            Assert.IsNotNull(match.LastOutcome.PlayerHand);
            Assert.IsNotNull(match.LastOutcome.OpponentHand);
            Assert.IsNotNull(match.RevealedOpponentCards);
            Assert.AreEqual(HandCategory.Flush, match.LastOutcome.PlayerHand.Category);
        }

        // -------------------------------------------------------------- match end

        [Test]
        public void TheMatchEndsWhenTheOpponentIsConsumed()
        {
            var match = NewMatch(out var opponent);
            opponent.Response = WagerResponse.Call;
            opponent.Wager = PressureLevel.Three;

            // Hand 1: player wagers 3, called, player wins -> opponent 4 -> 1.
            match.SubmitPlayerDiscards(StandPat);
            match.SubmitPlayerWager(PressureLevel.Three);
            Assert.AreEqual(1, match.Gravity.StepsRemaining(Seat.Opponent));
            Assert.AreEqual(MatchPhase.HandComplete, match.Phase);

            // Hand 2: opponent wagers 3, player calls, player wins -> opponent 1 -> 0.
            match.BeginNextHand();
            match.SubmitPlayerDiscards(StandPat);
            match.SubmitPlayerResponse(WagerResponse.Call);

            Assert.AreEqual(MatchPhase.MatchOver, match.Phase);
            Assert.AreEqual(Seat.Player, match.Victor);
            Assert.AreEqual(0, match.Gravity.StepsRemaining(Seat.Opponent));
            Assert.IsTrue(match.LastOutcome.EndedTheMatch);
            Assert.AreEqual(1, match.LastOutcome.StepsApplied,
                "Only one step remained, so only one was applied even though three were wagered.");
        }

        [Test]
        public void TheMatchEndsWhenThePlayerIsConsumed()
        {
            // Zero-shuffle deals the opponent a flush, so the player loses showdowns.
            var match = NewMatch(out var opponent, new ZeroShuffleRandom());
            opponent.Response = WagerResponse.Call;
            opponent.Wager = PressureLevel.Three;

            match.SubmitPlayerDiscards(StandPat);
            match.SubmitPlayerWager(PressureLevel.Three);
            Assert.AreEqual(1, match.Gravity.StepsRemaining(Seat.Player));

            match.BeginNextHand();
            match.SubmitPlayerDiscards(StandPat);
            match.SubmitPlayerResponse(WagerResponse.Call);

            Assert.AreEqual(MatchPhase.MatchOver, match.Phase);
            Assert.AreEqual(Seat.Opponent, match.Victor);
            Assert.AreEqual(0, match.Gravity.StepsRemaining(Seat.Player));
        }

        [Test]
        public void NothingCanBePlayedAfterTheMatchIsOver()
        {
            var match = NewMatch(out var opponent);
            opponent.Response = WagerResponse.Call;
            opponent.Wager = PressureLevel.Three;

            match.SubmitPlayerDiscards(StandPat);
            match.SubmitPlayerWager(PressureLevel.Three);
            match.BeginNextHand();
            match.SubmitPlayerDiscards(StandPat);
            match.SubmitPlayerResponse(WagerResponse.Call);

            Assert.AreEqual(MatchPhase.MatchOver, match.Phase);
            Assert.Throws<InvalidOperationException>(() => match.BeginNextHand());
            Assert.Throws<InvalidOperationException>(() => match.SubmitPlayerDiscards(StandPat));
            Assert.Throws<InvalidOperationException>(() => match.SubmitPlayerWager(PressureLevel.One));
            Assert.Throws<InvalidOperationException>(() => match.SubmitPlayerResponse(WagerResponse.Call));
        }

        // ---------------------------------------------------------------- restart

        [Test]
        public void RestartGivesABrandNewDuelWithoutRecreatingAnything()
        {
            // MVP acceptance criterion 8: a new match without restarting Unity.
            var match = NewMatch(out var opponent);
            opponent.Response = WagerResponse.Call;
            opponent.Wager = PressureLevel.Three;

            match.SubmitPlayerDiscards(StandPat);
            match.SubmitPlayerWager(PressureLevel.Three);
            match.BeginNextHand();
            match.SubmitPlayerDiscards(StandPat);
            match.SubmitPlayerResponse(WagerResponse.Call);
            Assert.AreEqual(MatchPhase.MatchOver, match.Phase);

            match.Restart();

            Assert.AreEqual(MatchPhase.DiscardSelection, match.Phase);
            Assert.AreEqual(1, match.HandNumber);
            Assert.AreEqual(Seat.Player, match.Aggressor);
            Assert.AreEqual(4, match.Gravity.StepsRemaining(Seat.Player));
            Assert.AreEqual(4, match.Gravity.StepsRemaining(Seat.Opponent));
            Assert.IsNull(match.Victor);
            Assert.IsNull(match.LastOutcome);
            Assert.IsNull(match.RevealedOpponentCards);
        }

        [Test]
        public void RestartWorksMidMatchToo()
        {
            var match = NewMatch(out _);
            match.SubmitPlayerDiscards(StandPat);

            match.Restart();

            Assert.AreEqual(MatchPhase.DiscardSelection, match.Phase);
            Assert.AreEqual(1, match.HandNumber);
        }

        // ------------------------------------------------- transitions and guards

        [Test]
        public void TheAggressorAlternatesEveryHand()
        {
            var match = NewMatch(out var opponent);
            opponent.Response = WagerResponse.Fold;
            opponent.Wager = PressureLevel.One;

            var seen = new List<Seat>();

            for (var i = 0; i < 4; i++)
            {
                seen.Add(match.Aggressor);
                match.SubmitPlayerDiscards(StandPat);

                if (match.Phase == MatchPhase.AwaitingWager) match.SubmitPlayerWager(PressureLevel.One);
                else match.SubmitPlayerResponse(WagerResponse.Fold);

                if (match.Phase != MatchPhase.HandComplete) break;
                match.BeginNextHand();
            }

            Assert.AreEqual(
                new[] { Seat.Player, Seat.Opponent, Seat.Player, Seat.Opponent },
                seen.ToArray());
        }

        [Test]
        public void ActionsOutsideTheirPhaseThrow()
        {
            var match = NewMatch(out _);

            // In DiscardSelection, only discards are legal.
            Assert.Throws<InvalidOperationException>(() => match.SubmitPlayerWager(PressureLevel.One));
            Assert.Throws<InvalidOperationException>(() => match.SubmitPlayerResponse(WagerResponse.Call));
            Assert.Throws<InvalidOperationException>(() => match.BeginNextHand());

            match.SubmitPlayerDiscards(StandPat);

            // In AwaitingWager, only a wager is legal.
            Assert.Throws<InvalidOperationException>(() => match.SubmitPlayerDiscards(StandPat));
            Assert.Throws<InvalidOperationException>(() => match.SubmitPlayerResponse(WagerResponse.Call));
        }

        [Test]
        public void ARepeatedButtonClickThrowsRatherThanActingTwice()
        {
            var match = NewMatch(out var opponent);
            opponent.Response = WagerResponse.Fold;

            match.SubmitPlayerDiscards(StandPat);
            match.SubmitPlayerWager(PressureLevel.Two);

            var stepsAfterFirst = match.Gravity.StepsRemaining(Seat.Opponent);

            Assert.Throws<InvalidOperationException>(() => match.SubmitPlayerWager(PressureLevel.Two));
            Assert.AreEqual(stepsAfterFirst, match.Gravity.StepsRemaining(Seat.Opponent),
                "A duplicate submit must not apply gravity twice.");
        }

        [Test]
        public void IllegalPlayerDiscardsAreRejected()
        {
            var match = NewMatch(out _);

            Assert.Throws<ArgumentException>(() => match.SubmitPlayerDiscards(new[] { 0, 1, 2, 3 }));
            Assert.Throws<ArgumentException>(() => match.SubmitPlayerDiscards(new[] { 7 }));
            Assert.Throws<ArgumentException>(() => match.SubmitPlayerDiscards(new[] { 1, 1 }));

            Assert.AreEqual(MatchPhase.DiscardSelection, match.Phase,
                "A rejected submission must leave the phase untouched.");
        }

        [Test]
        public void AnUnknownPressureLevelIsRejected()
        {
            var match = NewMatch(out _);
            match.SubmitPlayerDiscards(StandPat);

            Assert.Throws<ArgumentOutOfRangeException>(() => match.SubmitPlayerWager((PressureLevel)9));
        }

        [Test]
        public void AMisbehavingOpponentIsCaughtRatherThanTrusted()
        {
            var opponent = new ScriptedOpponent { Discards = new[] { 0, 1, 2, 3 } };
            var match = new DuelMatch(MatchConfig.Default, opponent, new IdentityShuffleRandom());

            var error = Assert.Throws<InvalidOperationException>(() => match.StartMatch());
            Assert.That(error.Message, Does.Contain("illegal discard"));
        }

        [Test]
        public void RequiresAnOpponentAndARandomSource()
        {
            Assert.Throws<ArgumentNullException>(
                () => new DuelMatch(MatchConfig.Default, null, new IdentityShuffleRandom()));
            Assert.Throws<ArgumentNullException>(
                () => new DuelMatch(MatchConfig.Default, new ScriptedOpponent(), null));
        }

        // ----------------------------------------------------------- config driven

        [Test]
        public void StartingStepsAndFoldCostComeFromTheConfig()
        {
            var match = NewMatch(out var opponent, new IdentityShuffleRandom(),
                new MatchConfig(startingSteps: 6, foldCost: 2));
            opponent.Response = WagerResponse.Fold;

            Assert.AreEqual(6, match.Gravity.StepsRemaining(Seat.Player));

            match.SubmitPlayerDiscards(StandPat);
            match.SubmitPlayerWager(PressureLevel.One);

            Assert.AreEqual(2, match.LastOutcome.StepsApplied, "Fold cost is configurable.");
            Assert.AreEqual(4, match.Gravity.StepsRemaining(Seat.Opponent));
        }

        // -------------------------------------------------------------- invariants

        [Test]
        public void TiesMoveNobody()
        {
            // A tie cannot be forced without stacking the deck, which Core deliberately
            // does not expose. So this checks the invariant across many real hands: every
            // tie that occurs must leave both characters exactly where they were.
            var random = new SeededRandomSource(4242);
            var opponent = new BasicOpponent(OpponentPersonality.Default, random);
            var match = new DuelMatch(MatchConfig.Default, opponent, random);

            var ties = 0;

            for (var attempt = 0; attempt < 400; attempt++)
            {
                match.Restart();
                match.SubmitPlayerDiscards(StandPat);

                var beforePlayer = match.Gravity.StepsRemaining(Seat.Player);
                var beforeOpponent = match.Gravity.StepsRemaining(Seat.Opponent);

                if (match.Phase == MatchPhase.AwaitingWager) match.SubmitPlayerWager(PressureLevel.Two);
                else match.SubmitPlayerResponse(WagerResponse.Call);

                if (match.LastOutcome == null || !match.LastOutcome.IsTie) continue;

                ties++;
                Assert.AreEqual(0, match.LastOutcome.StepsApplied, "A tie must move nobody.");
                Assert.AreEqual(beforePlayer, match.Gravity.StepsRemaining(Seat.Player));
                Assert.AreEqual(beforeOpponent, match.Gravity.StepsRemaining(Seat.Opponent));
            }

            Assert.Pass($"Checked the tie invariant on {ties} tie(s) out of 400 hands.");
        }

        [Test]
        public void EveryMatchReachesAValidEndAndNeverEntersAnInvalidState()
        {
            // Fuzz: a random player against the real AI. Proves the state machine cannot
            // deadlock or produce invalid distance (MVP criteria 6, 9).
            var random = new SeededRandomSource(1337);
            var completed = 0;

            for (var matchIndex = 0; matchIndex < 120; matchIndex++)
            {
                var opponent = new BasicOpponent(OpponentPersonality.Default, random);
                var match = new DuelMatch(MatchConfig.Default, opponent, random);
                match.StartMatch();

                var hands = 0;

                while (match.Phase != MatchPhase.MatchOver && hands < 400)
                {
                    hands++;

                    var discardCount = random.NextInt(0, 4);
                    var discards = new List<int>();
                    for (var i = 0; i < discardCount; i++) discards.Add(i);
                    match.SubmitPlayerDiscards(discards);

                    if (match.Phase == MatchPhase.AwaitingWager)
                        match.SubmitPlayerWager((PressureLevel)random.NextInt(1, 4));
                    else
                        match.SubmitPlayerResponse((WagerResponse)random.NextInt(0, 2));

                    foreach (Seat seat in Enum.GetValues(typeof(Seat)))
                    {
                        var steps = match.Gravity.StepsRemaining(seat);
                        Assert.GreaterOrEqual(steps, 0, "Distance must never go negative.");
                        Assert.LessOrEqual(steps, 4, "Distance must never exceed the start.");
                    }

                    if (match.Phase == MatchPhase.HandComplete) match.BeginNextHand();
                }

                Assert.AreEqual(MatchPhase.MatchOver, match.Phase,
                    $"Match {matchIndex} did not finish within {hands} hands.");
                Assert.IsNotNull(match.Victor, "A finished duel must have a victor.");
                Assert.IsTrue(match.Gravity.IsConsumed(
                    match.Victor == Seat.Player ? Seat.Opponent : Seat.Player));

                completed++;
            }

            Assert.AreEqual(120, completed);
        }
    }
}
