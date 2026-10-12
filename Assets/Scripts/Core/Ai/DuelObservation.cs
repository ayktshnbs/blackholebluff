using System.Collections.Generic;
using BlackHoleBluff.Core.Cards;
using BlackHoleBluff.Core.Match;

namespace BlackHoleBluff.Core.Ai
{
    /// <summary>
    /// Everything an opponent is allowed to know (GDD §5, Information rules).
    ///
    /// This type is the information boundary. An opponent is never handed the match, the
    /// <c>DrawRound</c> or the player's hand — only one of these — so it is structurally
    /// unable to peek. <c>MyCards</c> is the opponent's own hand and is the only card
    /// collection here; there is deliberately no member that could carry the player's cards.
    ///
    /// <c>OpponentDiscardCount</c> is the player's discard count, which is public
    /// information at a real draw table and a legitimate tell — standing pat suggests
    /// strength, throwing three suggests weakness.
    /// </summary>
    public readonly struct DuelObservation
    {
        /// <summary>
        /// Value of a discard count that is not yet known, because that side has not
        /// drawn. Distinguished from 0, which means "stood pat" and is a real tell.
        /// </summary>
        public const int Unknown = -1;

        /// <summary>The observer's own five cards.</summary>
        public IReadOnlyList<Card> MyCards { get; }

        /// <summary>The observer's own remaining steps from the event horizon.</summary>
        public int MySteps { get; }

        /// <summary>The other character's remaining steps. Public information.</summary>
        public int OpponentSteps { get; }

        /// <summary>The wager to respond to, or null when choosing one's own wager.</summary>
        public PressureLevel? PressureOnTable { get; }

        /// <summary>1 for the first hand of the duel.</summary>
        public int HandNumber { get; }

        /// <summary>How many cards the observer discarded this hand.</summary>
        public int MyDiscardCount { get; }

        /// <summary>How many cards the other character discarded. Public information.</summary>
        public int OpponentDiscardCount { get; }

        /// <summary>Steps lost by folding, so the observer can tell when folding is fatal.</summary>
        public int FoldCost { get; }

        public DuelObservation(
            IReadOnlyList<Card> myCards,
            int mySteps,
            int opponentSteps,
            PressureLevel? pressureOnTable,
            int handNumber,
            int myDiscardCount,
            int opponentDiscardCount,
            int foldCost)
        {
            MyCards = myCards;
            MySteps = mySteps;
            OpponentSteps = opponentSteps;
            PressureOnTable = pressureOnTable;
            HandNumber = handNumber;
            MyDiscardCount = myDiscardCount;
            OpponentDiscardCount = opponentDiscardCount;
            FoldCost = foldCost;
        }

        /// <summary>True when folding would itself be fatal, so folding cannot be the safe choice.</summary>
        public bool FoldingIsFatal => MySteps <= FoldCost;

        /// <summary>True when losing a called showdown at the current pressure would be fatal.</summary>
        public bool CallingCouldBeFatal =>
            PressureOnTable.HasValue && MySteps <= (int)PressureOnTable.Value;
    }
}
