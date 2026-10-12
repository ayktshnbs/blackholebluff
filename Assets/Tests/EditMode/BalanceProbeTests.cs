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
    /// Plays whole duels with scripted player strategies to check that no trivial line
    /// dominates (GDD §3.3). Deterministic: every run uses a fixed seed, so these are
    /// real assertions rather than flaky statistics.
    ///
    /// These are balance *probes*, not correctness tests. If one fails, the answer is
    /// usually to adjust decision thresholds and wager incentives — not to loosen the
    /// bound until it goes green.
    /// </summary>
    public class BalanceProbeTests
    {
        interface IPlayerStrategy
        {
            string Name { get; }
            PressureLevel Wager(float strength, int mySteps, int opponentSteps);
            WagerResponse Respond(float strength, PressureLevel pressure, int mySteps, int opponentSteps);
        }

        /// <summary>Folds to everything it is allowed to fold to.</summary>
        sealed class AlwaysFold : IPlayerStrategy
        {
            public string Name => "always fold";
            public PressureLevel Wager(float s, int m, int o) => PressureLevel.One;
            public WagerResponse Respond(float s, PressureLevel p, int m, int o) => WagerResponse.Fold;
        }

        /// <summary>Maximum pressure every hand, and never backs down.</summary>
        sealed class AlwaysMaximumPressure : IPlayerStrategy
        {
            public string Name => "always Pressure 3, always call";
            public PressureLevel Wager(float s, int m, int o) => PressureLevel.Three;
            public WagerResponse Respond(float s, PressureLevel p, int m, int o) => WagerResponse.Call;
        }

        /// <summary>Never folds, never raises the stakes.</summary>
        sealed class AlwaysCall : IPlayerStrategy
        {
            public string Name => "always Pressure 1, always call";
            public PressureLevel Wager(float s, int m, int o) => PressureLevel.One;
            public WagerResponse Respond(float s, PressureLevel p, int m, int o) => WagerResponse.Call;
        }

        /// <summary>Bets its hand and folds weak hands to heavy pressure.</summary>
        sealed class PlaysTheCards : IPlayerStrategy
        {
            public string Name => "plays the cards";

            public PressureLevel Wager(float strength, int mySteps, int opponentSteps)
            {
                var level = strength < 0.30f ? 1 : strength < 0.55f ? 2 : 3;
                if (level >= mySteps && strength < 0.7f) level = Math.Max(1, mySteps - 1);
                return (PressureLevel)Math.Min(3, Math.Max(1, level));
            }

            public WagerResponse Respond(float strength, PressureLevel pressure, int mySteps, int opponentSteps)
            {
                // Folding is never right when it costs as much as calling risks.
                if ((int)pressure <= 1) return WagerResponse.Call;
                if (mySteps <= 1) return WagerResponse.Call;

                var bar = pressure == PressureLevel.Two ? 0.33f : 0.48f;
                return strength >= bar ? WagerResponse.Call : WagerResponse.Fold;
            }
        }

        /// <summary>Plays `matches` duels and returns how many the player won.</summary>
        static int PlayerWins(IPlayerStrategy strategy, int matches, int seed)
        {
            var random = new SeededRandomSource(seed);
            var discardBrain = new BasicOpponent(OpponentPersonality.Default, random);
            var wins = 0;

            for (var i = 0; i < matches; i++)
            {
                var opponent = new BasicOpponent(OpponentPersonality.Default, random);
                var match = new DuelMatch(MatchConfig.Default, opponent, random);
                match.StartMatch();

                var guard = 0;

                while (match.Phase != MatchPhase.MatchOver && guard++ < 500)
                {
                    var mine = match.PlayerCards;
                    var mySteps = match.Gravity.StepsRemaining(Seat.Player);
                    var oppSteps = match.Gravity.StepsRemaining(Seat.Opponent);

                    // Every strategy discards the same way, so only betting differs.
                    var observation = new DuelObservation(
                        mine, mySteps, oppSteps, null, match.HandNumber, 0,
                        match.OpponentDiscardCount, match.Config.FoldCost);

                    match.SubmitPlayerDiscards(discardBrain.ChooseDiscards(observation));

                    var strength = HandStrength.Estimate(HandEvaluator.Evaluate(match.PlayerCards));

                    if (match.Phase == MatchPhase.AwaitingWager)
                        match.SubmitPlayerWager(strategy.Wager(strength, mySteps, oppSteps));
                    else
                        match.SubmitPlayerResponse(strategy.Respond(
                            strength, match.PressureOnTable.Value, mySteps, oppSteps));

                    if (match.Phase == MatchPhase.HandComplete) match.BeginNextHand();
                }

                Assert.AreEqual(MatchPhase.MatchOver, match.Phase,
                    $"'{strategy.Name}' match {i} never finished.");

                if (match.Victor == Seat.Player) wins++;
            }

            return wins;
        }

        const int Matches = 200;

        [Test]
        public void NoTrivialStrategyDominatesAndPlayingTheCardsIsBest()
        {
            var fold = PlayerWins(new AlwaysFold(), Matches, 20261012);
            var max = PlayerWins(new AlwaysMaximumPressure(), Matches, 20261012);
            var call = PlayerWins(new AlwaysCall(), Matches, 20261012);
            var cards = PlayerWins(new PlaysTheCards(), Matches, 20261012);

            var report =
                $"win rates over {Matches} duels each — " +
                $"always fold {Percent(fold)}, " +
                $"always Pressure 3 {Percent(max)}, " +
                $"always call {Percent(call)}, " +
                $"plays the cards {Percent(cards)}";

            TestContext.WriteLine(report);

            // Always folding is strictly dominated: it costs a step every time it is
            // chosen and can never win a hand.
            Assert.Less(fold, Matches / 2, $"Always folding should not break even. {report}");

            // No blunt line should be a winning solution, or there is no game here.
            Assert.Less(max, (int)(Matches * 0.85f), $"Always Pressure 3 must not dominate. {report}");
            Assert.Less(call, (int)(Matches * 0.85f), $"Always calling must not dominate. {report}");

            // And the opponent must be beatable, or there is no game either.
            Assert.Greater(cards, (int)(Matches * 0.15f),
                $"A sensible strategy must win a meaningful share. {report}");

            // Reading the hand should beat ignoring it.
            Assert.Greater(cards, fold, $"Playing the cards should beat always folding. {report}");
        }

        static string Percent(int wins) => $"{100f * wins / Matches:0.0}%";
    }
}
