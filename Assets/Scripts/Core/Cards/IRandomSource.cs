using System;

namespace BlackHoleBluff.Core.Cards
{
    /// <summary>
    /// Randomness is injected rather than taken from a global, so shuffles can be made
    /// exactly reproducible in tests (GDD §11). Nothing in Core may call UnityEngine.Random.
    /// </summary>
    public interface IRandomSource
    {
        /// <summary>Uniform integer in [minInclusive, maxExclusive).</summary>
        int NextInt(int minInclusive, int maxExclusive);
    }

    /// <summary>
    /// Default implementation over System.Random.
    /// Construct with an explicit seed for a deterministic sequence; use the
    /// parameterless constructor for ordinary play.
    /// </summary>
    public sealed class SeededRandomSource : IRandomSource
    {
        readonly Random _random;

        public int Seed { get; }

        public SeededRandomSource(int seed)
        {
            Seed = seed;
            _random = new Random(seed);
        }

        public SeededRandomSource()
            : this(Environment.TickCount)
        {
        }

        public int NextInt(int minInclusive, int maxExclusive)
        {
            if (maxExclusive < minInclusive)
                throw new ArgumentOutOfRangeException(
                    nameof(maxExclusive), "maxExclusive must not be less than minInclusive.");

            return _random.Next(minInclusive, maxExclusive);
        }
    }
}
