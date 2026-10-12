using BlackHoleBluff.Core.Match;

namespace BlackHoleBluff.Core.Ai
{
    /// <summary>
    /// The three decisions an opponent makes in a hand. The match calls these and never
    /// knows whether a human, a script or a solver is behind them.
    ///
    /// Every method takes a <see cref="DuelObservation"/> and nothing else, which is how
    /// the information restriction in GDD §5 is enforced rather than merely intended.
    /// </summary>
    public interface IDuelOpponent
    {
        string Name { get; }

        /// <summary>
        /// Hand positions (0–4) to throw away, at most three. An empty array stands pat.
        /// </summary>
        int[] ChooseDiscards(DuelObservation observation);

        /// <summary>Which pressure level to wager when acting first.</summary>
        PressureLevel ChooseWager(DuelObservation observation);

        /// <summary>Fold or call in response to <see cref="DuelObservation.PressureOnTable"/>.</summary>
        WagerResponse RespondToWager(DuelObservation observation);
    }
}
