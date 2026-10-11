using System.Collections.Generic;
using System.Text;
using BlackHoleBluff.Core.Cards;
using BlackHoleBluff.Core.GameRules;
using UnityEngine;

namespace BlackHoleBluff.Dev
{
    /// <summary>
    /// Phase 1 development harness. Plays one hand of five-card draw and logs it to the
    /// Console — enough to see the rules working before any UI or 3D presentation exists.
    ///
    /// Disposable: delete this once the real match flow and UI arrive in Phase 2.
    /// It is also the only thing currently proving that the engine-free Core assembly
    /// can be consumed from ordinary Unity code.
    /// </summary>
    public sealed class DrawRoundDevHarness : MonoBehaviour
    {
        [Header("Shuffle")]
        [Tooltip("Fixed seed makes a round exactly reproducible. Tick Randomise Seed for variety.")]
        [SerializeField] int seed = 2026;

        [SerializeField] bool randomiseSeed;

        [Header("Discards (hand positions 0-4, up to three each)")]
        [SerializeField] int[] playerDiscards = { 0, 1, 2 };
        [SerializeField] int[] opponentDiscards = { 4 };

        [Header("Behaviour")]
        [SerializeField] bool playOnStart = true;

        void Start()
        {
            if (playOnStart) PlayRound();
        }

        /// <summary>Also available from the component's context menu in the Inspector.</summary>
        [ContextMenu("Play a round")]
        public void PlayRound()
        {
            var actualSeed = randomiseSeed ? Random.Range(int.MinValue, int.MaxValue) : seed;

            var deck = new Deck(new SeededRandomSource(actualSeed));
            deck.ResetAndShuffle();

            var round = new DrawRound(deck);
            round.Deal();

            var log = new StringBuilder();
            log.AppendLine($"=== Black Hole Bluff — draw round (seed {actualSeed}) ===");
            log.AppendLine($"Dealt    player   : {Describe(round.PlayerCards)}  [{HandEvaluator.Evaluate(round.PlayerCards).Describe()}]");
            log.AppendLine($"Dealt    opponent : {Describe(round.OpponentCards)}  [{HandEvaluator.Evaluate(round.OpponentCards).Describe()}]");

            if (!TryReplace(round, Seat.Player, playerDiscards, log)) return;
            if (!TryReplace(round, Seat.Opponent, opponentDiscards, log)) return;

            log.AppendLine($"Final    player   : {Describe(round.PlayerCards)}");
            log.AppendLine($"Final    opponent : {Describe(round.OpponentCards)}");

            var result = round.Showdown();
            log.AppendLine($"Player   : {result.PlayerHand.Describe()}");
            log.AppendLine($"Opponent : {result.OpponentHand.Describe()}");
            log.AppendLine($"Result   : {result}");
            log.AppendLine($"Deck remaining: {deck.Remaining}");

            Debug.Log(log.ToString());
        }

        bool TryReplace(DrawRound round, Seat seat, IReadOnlyList<int> discards, StringBuilder log)
        {
            var selection = discards ?? new int[0];

            if (!FiveCardDrawRules.IsValidDiscardSelection(selection, out var error))
            {
                Debug.LogError($"[DevHarness] {seat} discard selection is invalid: {error}", this);
                return false;
            }

            round.Replace(seat, selection);
            log.AppendLine($"Discard  {seat,-9}: positions [{string.Join(", ", selection)}]");
            return true;
        }

        static string Describe(IReadOnlyList<Card> cards)
        {
            var parts = new string[cards.Count];
            for (var i = 0; i < cards.Count; i++) parts[i] = cards[i].ToString();
            return string.Join(" ", parts);
        }
    }
}
