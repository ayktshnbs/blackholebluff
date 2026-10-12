namespace BlackHoleBluff.Core.Ai
{
    /// <summary>
    /// The handful of numbers that shape how an opponent plays (GDD §4).
    ///
    /// A plain value type, so the five personalities planned for Phase 5 are **data**,
    /// not five copies of the decision model. A ScriptableObject wrapper can live outside
    /// Core and hand one of these in.
    ///
    /// All fields are 0–1.
    /// </summary>
    public readonly struct OpponentPersonality
    {
        public string Name { get; }

        /// <summary>Willingness to reach for a higher pressure level with a given hand.</summary>
        public float Aggression { get; }

        /// <summary>Chance of wagering big on a weak hand.</summary>
        public float BluffChance { get; }

        /// <summary>Raises the hand strength required to call.</summary>
        public float Caution { get; }

        /// <summary>Extra reluctance to call when losing the showdown would be fatal.</summary>
        public float SelfPreservation { get; }

        /// <summary>
        /// How much randomness is mixed into decisions. Enough to be unpredictable,
        /// little enough to not feel arbitrary (GDD §3.3).
        /// </summary>
        public float Noise { get; }

        public OpponentPersonality(
            string name,
            float aggression,
            float bluffChance,
            float caution,
            float selfPreservation,
            float noise)
        {
            Name = name;
            Aggression = Clamp01(aggression);
            BluffChance = Clamp01(bluffChance);
            Caution = Clamp01(caution);
            SelfPreservation = Clamp01(selfPreservation);
            Noise = Clamp01(noise);
        }

        /// <summary>
        /// The measured, slightly cautious opponent used for Phase 2. Bluffs occasionally
        /// so that always-folding is punished, and calls readily enough that always-betting
        /// Pressure 3 is punished too.
        /// </summary>
        public static OpponentPersonality Default =>
            new OpponentPersonality("Opponent", 0.45f, 0.18f, 0.35f, 0.6f, 0.35f);

        static float Clamp01(float value) => value < 0f ? 0f : value > 1f ? 1f : value;

        public override string ToString() =>
            $"{Name} (aggression {Aggression:0.00}, bluff {BluffChance:0.00}, caution {Caution:0.00})";
    }
}
