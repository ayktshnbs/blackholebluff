using BlackHoleBluff.Core.GameRules;

namespace BlackHoleBluff.Core.Match
{
    /// <summary>
    /// A finished hand, resolved deterministically before anything is animated.
    ///
    /// Presentation reads this and plays it back at whatever pace it likes; the rules
    /// never wait on an animation to decide an outcome (GDD §9, Presentation).
    ///
    /// Setters are internal: only the match builds one of these.
    /// </summary>
    public sealed class HandOutcome
    {
        /// <summary>1 for the first hand of the duel.</summary>
        public int HandNumber { get; internal set; }

        /// <summary>Who acted first and set the pressure.</summary>
        public Seat Aggressor { get; internal set; }

        /// <summary>The pressure that was wagered.</summary>
        public PressureLevel Pressure { get; internal set; }

        /// <summary>True when the hand ended in a fold rather than a showdown.</summary>
        public bool WasFolded { get; internal set; }

        /// <summary>Who folded, or null if the hand went to a showdown.</summary>
        public Seat? Folder { get; internal set; }

        /// <summary>
        /// The player's hand, or <c>null</c> when the hand was folded — folding does not
        /// reveal cards (GDD §4.3), which is what keeps a bluff worth making.
        /// </summary>
        public HandResult PlayerHand { get; internal set; }

        /// <summary>The opponent's hand, or <c>null</c> when the hand was folded.</summary>
        public HandResult OpponentHand { get; internal set; }

        /// <summary>Who lost the hand, or null on a tie.</summary>
        public Seat? Loser { get; internal set; }

        /// <summary>A tie moves nobody and the next hand is simply dealt (GDD §4.4).</summary>
        public bool IsTie => !WasFolded && Loser == null;

        /// <summary>Steps actually taken, which can be fewer than wagered on a final hand.</summary>
        public int StepsApplied { get; internal set; }

        public int PlayerStepsAfter { get; internal set; }
        public int OpponentStepsAfter { get; internal set; }

        /// <summary>Set when this hand ended the duel.</summary>
        public Seat? Victor { get; internal set; }

        public bool EndedTheMatch => Victor.HasValue;

        /// <summary>One line for a log or a placeholder UI.</summary>
        public string Summary()
        {
            if (WasFolded)
                return $"Hand {HandNumber}: {Aggressor} wagered Pressure {(int)Pressure}, " +
                       $"{Folder} folded and lost {StepsApplied} step" + (StepsApplied == 1 ? "." : "s.");

            if (IsTie)
                return $"Hand {HandNumber}: tie — both held {PlayerHand.Describe()}. Nobody moves.";

            var winner = Loser == Seat.Player ? Seat.Opponent : Seat.Player;
            var winningHand = winner == Seat.Player ? PlayerHand : OpponentHand;
            var losingHand = winner == Seat.Player ? OpponentHand : PlayerHand;

            return $"Hand {HandNumber}: called at Pressure {(int)Pressure}. " +
                   $"{winner} wins with {winningHand.Describe()} over {losingHand.Describe()}. " +
                   $"{Loser} pulled {StepsApplied} step" + (StepsApplied == 1 ? "." : "s.");
        }

        public override string ToString() => Summary();
    }
}
