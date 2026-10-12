using System.Collections.Generic;
using BlackHoleBluff.Core.Ai;
using BlackHoleBluff.Core.Cards;
using BlackHoleBluff.Core.GameRules;
using BlackHoleBluff.Core.Match;
using UnityEngine;

namespace BlackHoleBluff.Dev
{
    /// <summary>
    /// Placeholder UI for the Phase 2 playable duel — deliberately IMGUI.
    ///
    /// Why IMGUI: it needs no canvas, no prefabs, no fonts and no art, so a complete
    /// duel is playable today and nothing here can be mistaken for the real interface.
    /// The real UI (and all presentation) is Phase 4; this file is expected to be deleted
    /// then, which is why it lives in Dev rather than UI.
    ///
    /// It only ever reads <see cref="DuelMatch"/> and calls its Submit methods. No rules
    /// live here — if a decision looks wrong, it is wrong in Core.
    /// </summary>
    public sealed class DuelDevUI : MonoBehaviour
    {
        [Header("Duel")]
        [SerializeField] int startingSteps = 4;
        [SerializeField] int foldCost = 1;

        [Tooltip("Leave at 0 to use a time-based seed each run.")]
        [SerializeField] int seed;

        [Header("Layout")]
        [SerializeField] int fontSize = 18;

        DuelMatch _match;
        readonly List<int> _selected = new List<int>();
        string _lastError;

        void Start() => NewDuel();

        void NewDuel()
        {
            var random = seed == 0 ? new SeededRandomSource() : new SeededRandomSource(seed);
            var opponent = new BasicOpponent(OpponentPersonality.Default, random);

            _match = new DuelMatch(new MatchConfig(startingSteps, foldCost), opponent, random);
            _match.StartMatch();

            _selected.Clear();
            _lastError = null;
        }

        void OnGUI()
        {
            if (_match == null) return;

            GUI.skin.label.fontSize = fontSize;
            GUI.skin.button.fontSize = fontSize;
            GUI.skin.box.fontSize = fontSize;

            GUILayout.BeginArea(new Rect(24, 24, Mathf.Min(760, Screen.width - 48), Screen.height - 48));

            DrawHeader();
            GUILayout.Space(10);
            DrawGravity();
            GUILayout.Space(10);
            DrawOpponent();
            GUILayout.Space(10);
            DrawPlayerHand();
            GUILayout.Space(10);
            DrawActions();
            GUILayout.Space(10);
            DrawResult();

            if (!string.IsNullOrEmpty(_lastError))
            {
                GUILayout.Space(8);
                GUILayout.Label($"<!> {_lastError}");
            }

            GUILayout.Space(16);
            if (GUILayout.Button("New duel", GUILayout.Height(32))) NewDuel();

            GUILayout.EndArea();
        }

        void DrawHeader()
        {
            GUILayout.Label($"BLACK HOLE BLUFF  —  hand {_match.HandNumber}  —  {Describe(_match.Phase)}");
            GUILayout.Label($"Opponent: {_match.OpponentName}      " +
                            $"Acting first: {(_match.Aggressor == Seat.Player ? "you" : "opponent")}");
        }

        /// <summary>Distance as a row of markers, with the black hole on the left.</summary>
        void DrawGravity()
        {
            GUILayout.Box($"  [ BLACK HOLE ]{Track(Seat.Opponent)}  opponent   " +
                          $"{_match.Gravity.StepsRemaining(Seat.Opponent)} step(s) left");
            GUILayout.Box($"  [ BLACK HOLE ]{Track(Seat.Player)}  you        " +
                          $"{_match.Gravity.StepsRemaining(Seat.Player)} step(s) left");
        }

        string Track(Seat seat)
        {
            var remaining = _match.Gravity.StepsRemaining(seat);
            var consumed = startingSteps - remaining;

            var bar = new System.Text.StringBuilder(" ");
            for (var i = 0; i < consumed; i++) bar.Append(". ");
            bar.Append('#');
            for (var i = 0; i < remaining; i++) bar.Append(" -");
            return bar.ToString();
        }

        void DrawOpponent()
        {
            var revealed = _match.RevealedOpponentCards;

            GUILayout.Label(revealed != null
                ? $"Opponent's hand:  {string.Join("  ", revealed)}"
                : "Opponent's hand:  [ ? ]  [ ? ]  [ ? ]  [ ? ]  [ ? ]");

            var discards = _match.OpponentDiscardCount;
            GUILayout.Label(discards == DuelObservation.Unknown
                ? "Opponent has not drawn yet."
                : discards == 0
                    ? "Opponent stood pat — drew no cards."
                    : $"Opponent drew {discards} new card(s).");
        }

        void DrawPlayerHand()
        {
            var cards = _match.PlayerCards;
            if (cards == null) return;

            var selecting = _match.Phase == MatchPhase.DiscardSelection;

            GUILayout.Label(selecting
                ? $"Your hand — click up to {FiveCardDrawRules.MaxDiscards} cards to throw away:"
                : "Your hand:");

            GUILayout.BeginHorizontal();
            for (var i = 0; i < cards.Count; i++)
            {
                var chosen = _selected.Contains(i);
                var label = chosen ? $"[{cards[i]}]" : $" {cards[i]} ";

                if (!selecting)
                {
                    GUILayout.Box(label, GUILayout.Width(78), GUILayout.Height(54));
                    continue;
                }

                if (GUILayout.Button(label, GUILayout.Width(78), GUILayout.Height(54)))
                    ToggleSelection(i);
            }
            GUILayout.EndHorizontal();

            if (_match.LastOutcome?.PlayerHand != null && _match.Phase != MatchPhase.DiscardSelection)
                GUILayout.Label($"You held: {_match.LastOutcome.PlayerHand.Describe()}");
        }

        void ToggleSelection(int index)
        {
            if (_selected.Remove(index)) return;
            if (_selected.Count >= FiveCardDrawRules.MaxDiscards) return;

            _selected.Add(index);
        }

        void DrawActions()
        {
            _lastError = null;

            switch (_match.Phase)
            {
                case MatchPhase.DiscardSelection:
                    if (GUILayout.Button(
                            _selected.Count == 0 ? "Stand pat (draw nothing)" : $"Draw {_selected.Count} card(s)",
                            GUILayout.Height(36)))
                        Submit(() => { _match.SubmitPlayerDiscards(_selected); _selected.Clear(); });
                    break;

                case MatchPhase.AwaitingWager:
                    GUILayout.Label("Choose your wager. If called, the loser is pulled that many steps.");
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Pressure 1\n(cautious)", GUILayout.Height(52)))
                        Submit(() => _match.SubmitPlayerWager(PressureLevel.One));
                    if (GUILayout.Button("Pressure 2\n(risky)", GUILayout.Height(52)))
                        Submit(() => _match.SubmitPlayerWager(PressureLevel.Two));
                    if (GUILayout.Button("Pressure 3\n(dangerous)", GUILayout.Height(52)))
                        Submit(() => _match.SubmitPlayerWager(PressureLevel.Three));
                    GUILayout.EndHorizontal();
                    break;

                case MatchPhase.AwaitingResponse:
                    GUILayout.Label(
                        $"Opponent wagers PRESSURE {(int)_match.PressureOnTable.Value}. " +
                        $"Folding costs you {foldCost} step(s); " +
                        $"losing a called hand costs {(int)_match.PressureOnTable.Value}.");
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Fold", GUILayout.Height(44)))
                        Submit(() => _match.SubmitPlayerResponse(WagerResponse.Fold));
                    if (GUILayout.Button("Call", GUILayout.Height(44)))
                        Submit(() => _match.SubmitPlayerResponse(WagerResponse.Call));
                    GUILayout.EndHorizontal();
                    break;

                case MatchPhase.HandComplete:
                    if (GUILayout.Button("Next hand", GUILayout.Height(36)))
                        Submit(() => { _match.BeginNextHand(); _selected.Clear(); });
                    break;

                case MatchPhase.MatchOver:
                    GUILayout.Label(_match.Victor == Seat.Player
                        ? ">>> The opponent is dragged past the event horizon. YOU WIN. <<<"
                        : ">>> Gravity takes you. The black hole wins. <<<");
                    break;
            }
        }

        void DrawResult()
        {
            var outcome = _match.LastOutcome;
            if (outcome == null) return;

            GUILayout.Box(outcome.Summary());
        }

        /// <summary>
        /// Submits through the match and surfaces a refusal instead of crashing. The match
        /// throws on an out-of-order action, which should be impossible from this UI —
        /// so if this ever shows a message, the UI and the state machine disagree.
        /// </summary>
        void Submit(System.Action action)
        {
            try
            {
                action();
            }
            catch (System.Exception e)
            {
                _lastError = e.Message;
                Debug.LogError($"[DuelDevUI] {e.Message}", this);
            }
        }

        static string Describe(MatchPhase phase) => phase switch
        {
            MatchPhase.DiscardSelection => "choose your discards",
            MatchPhase.AwaitingWager => "your wager",
            MatchPhase.AwaitingResponse => "fold or call",
            MatchPhase.HandComplete => "hand over",
            MatchPhase.MatchOver => "duel over",
            _ => phase.ToString()
        };
    }
}
