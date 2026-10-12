using System;

namespace BlackHoleBluff.Core.Match
{
    /// <summary>
    /// The tunable numbers of a duel. A plain value type rather than a ScriptableObject,
    /// because Core is engine-free; a ScriptableObject wrapper can sit outside Core and
    /// hand one of these in.
    /// </summary>
    public readonly struct MatchConfig
    {
        /// <summary>Steps from the event horizon each character begins with (GDD §4.1).</summary>
        public int StartingSteps { get; }

        /// <summary>
        /// Steps the folding side loses, regardless of the pressure wagered.
        /// This asymmetry is the heart of the game: folding is always cheap, which is
        /// exactly what makes a big bluff both a threat and a risk (GDD §4.3).
        /// </summary>
        public int FoldCost { get; }

        public MatchConfig(int startingSteps, int foldCost)
        {
            if (startingSteps < 1)
                throw new ArgumentOutOfRangeException(nameof(startingSteps),
                    "A duel needs at least one step of distance.");

            if (foldCost < 0)
                throw new ArgumentOutOfRangeException(nameof(foldCost),
                    "Folding cannot move a character away from the black hole.");

            StartingSteps = startingSteps;
            FoldCost = foldCost;
        }

        /// <summary>Four steps, folding costs one (GDD §4).</summary>
        public static MatchConfig Default => new MatchConfig(4, 1);

        public override string ToString() => $"start {StartingSteps}, fold costs {FoldCost}";
    }
}
