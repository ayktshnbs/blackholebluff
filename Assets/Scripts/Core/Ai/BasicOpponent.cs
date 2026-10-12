using System;
using System.Collections.Generic;
using BlackHoleBluff.Core.Cards;
using BlackHoleBluff.Core.GameRules;
using BlackHoleBluff.Core.Match;

namespace BlackHoleBluff.Core.Ai
{
    /// <summary>
    /// The Phase 2 opponent (GDD §3.3). Decides from its own hand strength, the pressure
    /// on the table, both characters' distance from the black hole, its personality, and
    /// a controlled amount of randomness — and from nothing else, because a
    /// <see cref="DuelObservation"/> is all it is given.
    ///
    /// Built to make the obvious exploits lose:
    /// <list type="bullet">
    /// <item>Always wagering Pressure 3 — this opponent calls reasonable hands, and a
    /// called bluff costs the bluffer three steps, so the strategy bleeds out.</item>
    /// <item>Always folding — folding costs a step every time, so four folds is death.</item>
    /// <item>Always calling — the opponent wagers high with real hands.</item>
    /// </list>
    ///
    /// Phase 5's personalities are parameter sets for this same model, not new models.
    /// </summary>
    public sealed class BasicOpponent : IDuelOpponent
    {
        readonly OpponentPersonality _personality;
        readonly IRandomSource _random;

        public BasicOpponent(OpponentPersonality personality, IRandomSource random)
        {
            _personality = personality;
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        public OpponentPersonality Personality => _personality;

        public string Name => string.IsNullOrEmpty(_personality.Name) ? "Opponent" : _personality.Name;

        // ------------------------------------------------------------- discards

        /// <summary>
        /// Keeps whatever is already paired, or the two highest cards when nothing is.
        /// Deliberately simple: it does not chase four-card straights or flushes. Good
        /// enough to play competently, and an obvious place to add depth later.
        /// </summary>
        public int[] ChooseDiscards(DuelObservation observation)
        {
            var cards = observation.MyCards;
            if (cards == null || cards.Count != FiveCardDrawRules.HandSize)
                throw new ArgumentException("Expected a five-card hand.", nameof(observation));

            // A straight or better is already worth more than anything a redraw offers.
            if (HandEvaluator.Evaluate(cards).Category >= HandCategory.Straight)
                return Array.Empty<int>();

            var rankCounts = new int[15];
            foreach (var card in cards) rankCounts[card.Value]++;

            var keep = new bool[FiveCardDrawRules.HandSize];
            var keptAnything = false;

            for (var i = 0; i < cards.Count; i++)
            {
                if (rankCounts[cards[i].Value] < 2) continue;
                keep[i] = true;
                keptAnything = true;
            }

            if (!keptAnything) KeepTwoHighest(cards, keep);

            var discards = new List<int>(FiveCardDrawRules.MaxDiscards);
            for (var i = 0; i < keep.Length && discards.Count < FiveCardDrawRules.MaxDiscards; i++)
                if (!keep[i]) discards.Add(i);

            return discards.ToArray();
        }

        static void KeepTwoHighest(IReadOnlyList<Card> cards, bool[] keep)
        {
            var bestIndex = 0;
            var secondIndex = -1;

            for (var i = 1; i < cards.Count; i++)
                if (cards[i].Value > cards[bestIndex].Value) bestIndex = i;

            for (var i = 0; i < cards.Count; i++)
            {
                if (i == bestIndex) continue;
                if (secondIndex < 0 || cards[i].Value > cards[secondIndex].Value) secondIndex = i;
            }

            keep[bestIndex] = true;
            if (secondIndex >= 0) keep[secondIndex] = true;
        }

        // ---------------------------------------------------------------- wager

        public PressureLevel ChooseWager(DuelObservation observation)
        {
            var strength = Strength(observation);

            // Occasionally represent a hand we do not have. Without this, a player who
            // always folds to Pressure 3 would never be punished.
            var bluffing = strength < 0.35f && NextUnit() < _personality.BluffChance;
            var effective = (bluffing ? 0.75f : strength) + _personality.Aggression * 0.12f;

            var level = effective < 0.35f ? 1 : effective < 0.62f ? 2 : 3;

            // Finisher instinct: with a real hand, wager exactly enough to end the duel.
            if (strength >= 0.55f && observation.OpponentSteps > 0 && observation.OpponentSteps <= 3)
                level = Math.Max(level, observation.OpponentSteps);

            // The aggressor loses the wagered steps too. Do not pick a level that could
            // be fatal to us unless the hand genuinely justifies it.
            if (level >= observation.MySteps && strength < 0.7f)
                level = Math.Max(1, observation.MySteps - 1);

            return (PressureLevel)Math.Min(3, Math.Max(1, level));
        }

        // ------------------------------------------------------------- response

        public WagerResponse RespondToWager(DuelObservation observation)
        {
            if (!observation.PressureOnTable.HasValue)
                throw new InvalidOperationException(
                    "RespondToWager was called with no pressure on the table.");

            // If folding is itself fatal there is nothing left to protect, so call and
            // take the chance. This also makes the endgame tense for the player.
            if (observation.FoldingIsFatal) return WagerResponse.Call;

            var strength = Strength(observation);
            var pressure = (int)observation.PressureOnTable.Value;

            // Folding costs FoldCost steps for certain; calling risks only the wagered
            // steps. When the wager is no bigger than the fold cost, folding is weakly
            // dominated — the downside is identical and calling can still win. So it is
            // never right to fold, whatever the cards say.
            //
            // With the default config (fold 1, pressures 1-3) this makes Pressure 1 an
            // automatic call. See DEVLOG for why that is a notable property of the
            // current numbers rather than a bug in this method.
            if (pressure <= observation.FoldCost) return WagerResponse.Call;

            // Thresholds derived from the step arithmetic rather than from taste.
            // Folding loses FoldCost for certain; calling a wager of N loses N on a loss
            // and inflicts N on a win. Comparing the net step differential:
            //     fold  = -FoldCost
            //     call  = 2*N*p - N
            // so calling is correct once p > (N - FoldCost) / (2N):
            //     Pressure 2 -> p > 0.25      Pressure 3 -> p > 0.333
            // Those are low bars. Demanding a premium hand here is what let a player
            // win by wagering Pressure 3 every single hand, which is the exact
            // degenerate strategy GDD 3.3 forbids.
            var threshold = pressure == 2 ? 0.09f : 0.13f;
            threshold += _personality.Caution * 0.10f;

            if (observation.CallingCouldBeFatal)
                threshold += _personality.SelfPreservation * 0.25f;

            // Reading the draw. Public information, and a real tell.
            if (observation.OpponentDiscardCount >= 3) threshold -= 0.10f;
            else if (observation.OpponentDiscardCount == 0) threshold += 0.08f;

            var noise = (NextUnit() - 0.5f) * _personality.Noise * 0.30f;

            return strength + noise >= threshold ? WagerResponse.Call : WagerResponse.Fold;
        }

        // --------------------------------------------------------------- helpers

        static float Strength(DuelObservation observation) =>
            HandStrength.Estimate(HandEvaluator.Evaluate(observation.MyCards));

        /// <summary>Uniform 0–1. Derived from NextInt so IRandomSource stays minimal.</summary>
        float NextUnit() => _random.NextInt(0, 10001) / 10000f;
    }
}
