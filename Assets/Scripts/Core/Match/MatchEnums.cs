namespace BlackHoleBluff.Core.Match
{
    /// <summary>
    /// Where a duel currently is. Every non-terminal phase is a point where the game is
    /// waiting for the <em>player</em>; the AI acts synchronously inside transitions, so
    /// there is never a phase meaning "waiting for the opponent to think".
    ///
    /// Only the phases the game actually needs (GDD §9).
    /// </summary>
    public enum MatchPhase
    {
        /// <summary>Nothing dealt. Call StartMatch.</summary>
        NotStarted = 0,

        /// <summary>Both sides hold five cards. Waiting for the player's discards.</summary>
        DiscardSelection = 1,

        /// <summary>The player acts first this hand and must choose a pressure level.</summary>
        AwaitingWager = 2,

        /// <summary>A wager is on the table and the player must fold or call.</summary>
        AwaitingResponse = 3,

        /// <summary>The hand is resolved and gravity applied. Waiting for BeginNextHand.</summary>
        HandComplete = 4,

        /// <summary>Someone reached the event horizon. Terminal until Restart.</summary>
        MatchOver = 5
    }

    /// <summary>
    /// How far the loser of a called hand is dragged. The numeric value <em>is</em> the
    /// number of steps, so <c>(int)PressureLevel.Three == 3</c> (GDD §3.2).
    /// </summary>
    public enum PressureLevel
    {
        /// <summary>A cautious wager — one step.</summary>
        One = 1,

        /// <summary>A significant risk — two steps.</summary>
        Two = 2,

        /// <summary>A dangerous, high-stakes wager — three steps.</summary>
        Three = 3
    }

    /// <summary>The only two answers to a wager. No raises in this version (GDD §4.6).</summary>
    public enum WagerResponse
    {
        Fold = 0,
        Call = 1
    }
}
