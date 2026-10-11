using System;
using System.Collections.Generic;
using BlackHoleBluff.Core.Cards;

namespace BlackHoleBluff.Core.GameRules
{
    public enum Seat
    {
        Player = 0,
        Opponent = 1
    }

    /// <summary>Who won the showdown, and what each side held.</summary>
    public sealed class ShowdownResult
    {
        public HandResult PlayerHand { get; }
        public HandResult OpponentHand { get; }

        /// <summary>The winning seat, or null for a genuine tie.</summary>
        public Seat? Winner { get; }

        public bool IsTie => Winner == null;

        public ShowdownResult(HandResult playerHand, HandResult opponentHand)
        {
            PlayerHand = playerHand ?? throw new ArgumentNullException(nameof(playerHand));
            OpponentHand = opponentHand ?? throw new ArgumentNullException(nameof(opponentHand));

            var comparison = playerHand.CompareTo(opponentHand);
            Winner = comparison == 0 ? (Seat?)null
                : comparison > 0 ? Seat.Player
                : Seat.Opponent;
        }

        public override string ToString() =>
            IsTie
                ? $"Tie — both hold {PlayerHand.Describe()}"
                : $"{Winner} wins: {(Winner == Seat.Player ? PlayerHand : OpponentHand).Describe()} " +
                  $"beats {(Winner == Seat.Player ? OpponentHand : PlayerHand).Describe()}";
    }

    /// <summary>
    /// One hand of five-card draw: deal five each, one discard-and-replace per seat,
    /// then a showdown.
    ///
    /// Deliberately NOT a state machine. Betting, Gravity Pressure, fold/call and the
    /// match flow are Phase 2 and belong in the match layer; this type only knows how
    /// cards move. It enforces ordering with guards so an invalid sequence throws
    /// instead of quietly producing a wrong hand.
    ///
    /// Note on hidden information: this class exposes both hands because the rules need
    /// them. Keeping the opponent's cards hidden is the presentation layer's job, and
    /// keeping them out of the AI's reach is the AI layer's job (GDD §9).
    /// </summary>
    public sealed class DrawRound
    {
        readonly Deck _deck;
        readonly Card[] _playerCards = new Card[FiveCardDrawRules.HandSize];
        readonly Card[] _opponentCards = new Card[FiveCardDrawRules.HandSize];
        readonly bool[] _hasDrawn = new bool[2];

        public DrawRound(Deck deck)
        {
            _deck = deck ?? throw new ArgumentNullException(nameof(deck));
        }

        public bool IsDealt { get; private set; }

        public IReadOnlyList<Card> PlayerCards => _playerCards;

        public IReadOnlyList<Card> OpponentCards => _opponentCards;

        /// <summary>True once this seat has used its single discard-and-replace.</summary>
        public bool HasDrawn(Seat seat) => _hasDrawn[(int)seat];

        public IReadOnlyList<Card> CardsFor(Seat seat) =>
            seat == Seat.Player ? _playerCards : _opponentCards;

        /// <summary>Deals five cards to each seat from the top of the deck.</summary>
        public void Deal()
        {
            if (IsDealt)
                throw new InvalidOperationException(
                    "This round has already been dealt. Create a new DrawRound for the next hand.");

            var needed = FiveCardDrawRules.HandSize * 2;
            if (_deck.Remaining < needed)
                throw new InvalidOperationException(
                    $"Need {needed} cards to deal but the deck has {_deck.Remaining}. Reset and shuffle first.");

            for (var i = 0; i < FiveCardDrawRules.HandSize; i++)
            {
                _playerCards[i] = _deck.Draw();
                _opponentCards[i] = _deck.Draw();
            }

            IsDealt = true;
        }

        /// <summary>
        /// Discards the cards at the given hand positions and draws replacements.
        /// Pass an empty list to stand pat — that still consumes the seat's one draw.
        /// </summary>
        /// <returns>The replacement cards, in the order they filled the discarded slots.</returns>
        public Card[] Replace(Seat seat, IReadOnlyList<int> discardIndices)
        {
            if (!IsDealt)
                throw new InvalidOperationException("Deal the round before replacing cards.");

            if (_hasDrawn[(int)seat])
                throw new InvalidOperationException(
                    $"{seat} has already drawn. Each seat gets exactly one discard-and-replace.");

            FiveCardDrawRules.ValidateDiscardSelection(discardIndices);

            if (discardIndices.Count > _deck.Remaining)
                throw new InvalidOperationException(
                    $"Need {discardIndices.Count} replacement cards but the deck has {_deck.Remaining}.");

            var hand = seat == Seat.Player ? _playerCards : _opponentCards;
            var replacements = new Card[discardIndices.Count];

            for (var i = 0; i < discardIndices.Count; i++)
            {
                var replacement = _deck.Draw();
                hand[discardIndices[i]] = replacement;
                replacements[i] = replacement;
            }

            _hasDrawn[(int)seat] = true;
            return replacements;
        }

        /// <summary>Evaluates and compares both final hands.</summary>
        public ShowdownResult Showdown()
        {
            if (!IsDealt)
                throw new InvalidOperationException("Deal the round before the showdown.");

            return new ShowdownResult(
                HandEvaluator.Evaluate(_playerCards),
                HandEvaluator.Evaluate(_opponentCards));
        }
    }
}
