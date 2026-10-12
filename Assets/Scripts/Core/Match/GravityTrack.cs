using System;
using BlackHoleBluff.Core.GameRules;

namespace BlackHoleBluff.Core.Match
{
    /// <summary>
    /// How far each character is from the event horizon (GDD §4.1).
    ///
    /// Steps are clamped at zero and can never go negative — MVP acceptance criterion 6.
    /// Reaching zero means consumed.
    /// </summary>
    public sealed class GravityTrack
    {
        readonly int[] _steps = new int[2];

        public MatchConfig Config { get; }

        public GravityTrack(MatchConfig config)
        {
            Config = config;
            Reset();
        }

        public void Reset()
        {
            _steps[(int)Seat.Player] = Config.StartingSteps;
            _steps[(int)Seat.Opponent] = Config.StartingSteps;
        }

        public int StepsRemaining(Seat seat) => _steps[(int)seat];

        public bool IsConsumed(Seat seat) => _steps[(int)seat] <= 0;

        /// <summary>True once either character has been consumed.</summary>
        public bool AnyoneConsumed => IsConsumed(Seat.Player) || IsConsumed(Seat.Opponent);

        /// <summary>
        /// Drags a character toward the black hole. Clamps at zero, and returns how many
        /// steps were actually taken — which can be fewer than requested on the last hand.
        /// </summary>
        public int MoveCloser(Seat seat, int steps)
        {
            if (steps < 0)
                throw new ArgumentOutOfRangeException(nameof(steps),
                    "Gravity only pulls one way; a negative step would move a character to safety.");

            var index = (int)seat;
            var before = _steps[index];
            var after = Math.Max(0, before - steps);

            _steps[index] = after;
            return before - after;
        }

        /// <summary>
        /// The surviving seat once someone is consumed, or null while both are alive.
        /// If both were somehow consumed at once the result is null — but no rule in the
        /// game can move both characters on one hand.
        /// </summary>
        public Seat? Survivor
        {
            get
            {
                var playerOut = IsConsumed(Seat.Player);
                var opponentOut = IsConsumed(Seat.Opponent);

                if (playerOut == opponentOut) return null;
                return playerOut ? Seat.Opponent : Seat.Player;
            }
        }

        public override string ToString() =>
            $"player {StepsRemaining(Seat.Player)} / opponent {StepsRemaining(Seat.Opponent)}";
    }
}
