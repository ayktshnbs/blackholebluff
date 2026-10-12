using System;
using System.Collections.Generic;
using BlackHoleBluff.Core.Ai;
using BlackHoleBluff.Core.Cards;
using BlackHoleBluff.Core.GameRules;

namespace BlackHoleBluff.Core.Match
{
    /// <summary>
    /// A complete duel: hands of five-card draw, Gravity Pressure wagers, fold/call
    /// resolution, and a winner when one character reaches the event horizon (GDD §9).
    ///
    /// Every phase is a point where the game waits for the <em>player</em>. The opponent
    /// acts synchronously inside transitions, so there is no "AI is thinking" state and no
    /// possibility of the two sides acting at once.
    ///
    /// Illegal calls throw rather than being ignored, which is what makes a double-clicked
    /// button a loud bug instead of a silently duplicated wager.
    /// </summary>
    public sealed class DuelMatch
    {
        readonly MatchConfig _config;
        readonly IDuelOpponent _opponent;
        readonly Deck _deck;
        readonly GravityTrack _gravity;

        DrawRound _round;
        int _playerDiscardCount;
        int _opponentDiscardCount;

        public DuelMatch(MatchConfig config, IDuelOpponent opponent, IRandomSource random)
        {
            _config = config;
            _opponent = opponent ?? throw new ArgumentNullException(nameof(opponent));
            _deck = new Deck(random ?? throw new ArgumentNullException(nameof(random)));
            _gravity = new GravityTrack(config);
        }

        public MatchConfig Config => _config;
        public MatchPhase Phase { get; private set; } = MatchPhase.NotStarted;
        public GravityTrack Gravity => _gravity;
        public string OpponentName => _opponent.Name;

        /// <summary>1 for the first hand. Zero before the match starts.</summary>
        public int HandNumber { get; private set; }

        /// <summary>Who sets the pressure this hand. Alternates every hand (GDD §4.5).</summary>
        public Seat Aggressor { get; private set; } = Seat.Player;

        /// <summary>The wager awaiting a response, or null.</summary>
        public PressureLevel? PressureOnTable { get; private set; }

        /// <summary>The player's current five cards, or null before the first deal.</summary>
        public IReadOnlyList<Card> PlayerCards => _round?.PlayerCards;

        /// <summary>
        /// The opponent's cards, but only after a showdown actually revealed them.
        /// Null after a fold, so the presentation layer cannot accidentally show cards
        /// the rules say stay hidden.
        /// </summary>
        public IReadOnlyList<Card> RevealedOpponentCards { get; private set; }

        /// <summary>How many cards the player threw this hand.</summary>
        public int PlayerDiscardCount => _playerDiscardCount;

        /// <summary>
        /// How many cards the opponent threw this hand, or
        /// <see cref="DuelObservation.Unknown"/> if it has not drawn yet.
        /// </summary>
        public int OpponentDiscardCount =>
            _round != null && _round.HasDrawn(Seat.Opponent)
                ? _opponentDiscardCount
                : DuelObservation.Unknown;

        public bool PlayerHasDrawn => _round != null && _round.HasDrawn(Seat.Player);

        public HandOutcome LastOutcome { get; private set; }

        /// <summary>The surviving seat once the duel is over, otherwise null.</summary>
        public Seat? Victor { get; private set; }

        // ------------------------------------------------------------- lifecycle

        /// <summary>
        /// Starts, or restarts, a duel from full distance. Safe to call at any time —
        /// this is the restart flow, and it needs no Unity reload (MVP criterion 8).
        /// </summary>
        public void StartMatch()
        {
            _gravity.Reset();
            HandNumber = 0;
            Victor = null;
            LastOutcome = null;
            Aggressor = Seat.Player;

            DealNewHand();
        }

        /// <summary>Alias for <see cref="StartMatch"/>, for readability at call sites.</summary>
        public void Restart() => StartMatch();

        /// <summary>Deals the next hand after a completed one.</summary>
        public void BeginNextHand()
        {
            Require(MatchPhase.HandComplete, nameof(BeginNextHand));

            // Alternate who acts first. A tie counts as a hand, so it alternates too.
            Aggressor = Aggressor == Seat.Player ? Seat.Opponent : Seat.Player;

            DealNewHand();
        }

        void DealNewHand()
        {
            HandNumber++;

            // A fresh shuffle each hand. Simpler than tracking a running deck, and it
            // removes any chance of running out mid-duel.
            _deck.ResetAndShuffle();

            _round = new DrawRound(_deck);
            _round.Deal();

            _playerDiscardCount = 0;
            _opponentDiscardCount = 0;
            PressureOnTable = null;
            RevealedOpponentCards = null;

            // The side that does NOT set the pressure draws first, so the information
            // advantage of drawing last alternates along with the aggressor.
            if (Aggressor == Seat.Player) DrawForOpponent();

            Phase = MatchPhase.DiscardSelection;
        }

        // --------------------------------------------------------- player actions

        /// <summary>
        /// Discards the chosen hand positions (up to three, possibly none) and draws
        /// replacements. Advances to the wager or the response.
        /// </summary>
        public void SubmitPlayerDiscards(IReadOnlyList<int> discardIndices)
        {
            Require(MatchPhase.DiscardSelection, nameof(SubmitPlayerDiscards));

            var selection = discardIndices ?? Array.Empty<int>();
            FiveCardDrawRules.ValidateDiscardSelection(selection);

            _round.Replace(Seat.Player, selection);
            _playerDiscardCount = selection.Count;

            // When the player acts first, the opponent has already drawn.
            if (Aggressor == Seat.Opponent) DrawForOpponent();

            if (Aggressor == Seat.Player)
            {
                Phase = MatchPhase.AwaitingWager;
                return;
            }

            PressureOnTable = ClampWager(
                _opponent.ChooseWager(Observe(Seat.Opponent, null)));

            Phase = MatchPhase.AwaitingResponse;
        }

        /// <summary>The player sets the pressure; the opponent immediately answers.</summary>
        public void SubmitPlayerWager(PressureLevel level)
        {
            Require(MatchPhase.AwaitingWager, nameof(SubmitPlayerWager));

            if (level < PressureLevel.One || level > PressureLevel.Three)
                throw new ArgumentOutOfRangeException(nameof(level), $"Unknown pressure level '{level}'.");

            PressureOnTable = level;

            var response = _opponent.RespondToWager(Observe(Seat.Opponent, level));

            if (response == WagerResponse.Fold) ResolveFold(Seat.Opponent);
            else ResolveShowdown();
        }

        /// <summary>The player folds or calls the opponent's wager.</summary>
        public void SubmitPlayerResponse(WagerResponse response)
        {
            Require(MatchPhase.AwaitingResponse, nameof(SubmitPlayerResponse));

            if (response == WagerResponse.Fold) ResolveFold(Seat.Player);
            else ResolveShowdown();
        }

        // ------------------------------------------------------------- resolution

        void ResolveFold(Seat folder)
        {
            var applied = _gravity.MoveCloser(folder, _config.FoldCost);

            FinishHand(new HandOutcome
            {
                WasFolded = true,
                Folder = folder,
                Loser = folder,
                StepsApplied = applied
                // Cards stay hidden: PlayerHand and OpponentHand remain null.
            });
        }

        void ResolveShowdown()
        {
            var showdown = _round.Showdown();
            RevealedOpponentCards = _round.OpponentCards;

            var applied = 0;
            Seat? loser = null;

            if (!showdown.IsTie)
            {
                loser = showdown.Winner == Seat.Player ? Seat.Opponent : Seat.Player;
                applied = _gravity.MoveCloser(loser.Value, (int)PressureOnTable.Value);
            }

            FinishHand(new HandOutcome
            {
                WasFolded = false,
                Folder = null,
                PlayerHand = showdown.PlayerHand,
                OpponentHand = showdown.OpponentHand,
                Loser = loser,
                StepsApplied = applied
            });
        }

        void FinishHand(HandOutcome outcome)
        {
            outcome.HandNumber = HandNumber;
            outcome.Aggressor = Aggressor;
            outcome.Pressure = PressureOnTable ?? PressureLevel.One;
            outcome.PlayerStepsAfter = _gravity.StepsRemaining(Seat.Player);
            outcome.OpponentStepsAfter = _gravity.StepsRemaining(Seat.Opponent);

            Victor = _gravity.AnyoneConsumed ? _gravity.Survivor : null;
            outcome.Victor = Victor;

            LastOutcome = outcome;
            Phase = Victor.HasValue ? MatchPhase.MatchOver : MatchPhase.HandComplete;
        }

        // ---------------------------------------------------------------- opponent

        void DrawForOpponent()
        {
            var discards = _opponent.ChooseDiscards(Observe(Seat.Opponent, null)) ?? Array.Empty<int>();

            // Guard the rules against a misbehaving opponent rather than trusting it.
            if (!FiveCardDrawRules.IsValidDiscardSelection(discards, out var error))
                throw new InvalidOperationException(
                    $"Opponent '{_opponent.Name}' returned an illegal discard selection: {error}");

            _round.Replace(Seat.Opponent, discards);
            _opponentDiscardCount = discards.Length;
        }

        /// <summary>
        /// Builds the restricted view a seat is allowed to act on. The only path by which
        /// the opponent learns anything, and it never carries the player's cards.
        /// </summary>
        DuelObservation Observe(Seat seat, PressureLevel? pressure)
        {
            var other = seat == Seat.Player ? Seat.Opponent : Seat.Player;
            var ownCount = seat == Seat.Player ? _playerDiscardCount : _opponentDiscardCount;
            var otherCount = other == Seat.Player ? _playerDiscardCount : _opponentDiscardCount;

            return new DuelObservation(
                _round.CardsFor(seat),
                _gravity.StepsRemaining(seat),
                _gravity.StepsRemaining(other),
                pressure,
                HandNumber,
                _round.HasDrawn(seat) ? ownCount : DuelObservation.Unknown,
                _round.HasDrawn(other) ? otherCount : DuelObservation.Unknown,
                _config.FoldCost);
        }

        static PressureLevel ClampWager(PressureLevel level) =>
            level < PressureLevel.One ? PressureLevel.One
            : level > PressureLevel.Three ? PressureLevel.Three
            : level;

        void Require(MatchPhase expected, string action)
        {
            if (Phase == expected) return;

            throw new InvalidOperationException(
                $"{action} is not valid in phase {Phase}; it requires {expected}. " +
                "This usually means a UI action fired twice or out of order.");
        }
    }
}
