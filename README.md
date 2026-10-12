# Black Hole Bluff

A single-player, poker-inspired duel against eccentric aliens, played at a table suspended above a black hole. Every wager decides how far the loser is dragged toward the event horizon.

> Two aliens. One card table. A black hole. Every bluff could be your last.

Fictional in-game stakes only. No real-money gambling, and no purchasable currency.

---

## Status

| Phase | State |
|---|---|
| **0 — Project setup** | ✅ Complete |
| **1 — Pure card-game prototype** | ✅ Complete |
| **2 — Complete playable duel** | ✅ Complete — **166/166 tests passing** |
| 3 — AI behaviour and balance | ⬜ Next |
| 4 — 3D vertical slice | ⬜ |
| 5–7 — Content, polish, Steam | ⬜ |

**The duel is playable end to end** — wagers, bluffing, fold/call, victory and defeat, restart. No art, sound or 3D yet, and the UI is deliberately throwaway IMGUI. That ordering is on purpose: the game has to be worth looking at before it is worth decorating.

## Requirements

- **Unity 6000.4.7f1** (URP)
- Windows, Git, Git LFS

## Playing it

**Open `Assets/Scenes/Duel.unity` and press Play.** That is the whole game as it currently stands:

1. You and the opponent are each dealt five cards, four steps from the black hole.
2. Click up to three of your cards to throw away, then draw.
3. Either set a pressure level (1–3) or answer the opponent's — fold, or call.
4. Fold and you lose one step. Call and lose the showdown, and you lose the whole wager.
5. First to the event horizon loses. **New duel** restarts without leaving Play mode.

The `Duel` object exposes starting steps, fold cost, and a seed — set the seed to a fixed value to replay the same duel exactly, or leave it at 0 for a new one each run.

`Assets/Scenes/DevSandbox.unity` still holds the Phase 1 card harness, which logs a single hand to the Console.

## The tests

`Window → General → Test Runner → EditMode → Run All` — 166 cases covering hand evaluation and tie-breaking, deck integrity, the discard rules, gravity, every match state transition, the AI's decisions, the information boundary, and a balance probe that plays 200 full duels per strategy.

From the command line:

```bash
"C:/Program Files/Unity/Hub/Editor/6000.4.7f1/Editor/Unity.exe" -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testResults results.xml -logFile tests.log
```

## Project layout

```
Assets/
  Scripts/
    Core/              <- BlackHoleBluff.Core assembly, NO UnityEngine (see below)
      Cards/           Card, Rank, Suit, Deck, IRandomSource
      GameRules/       HandEvaluator, HandResult, HandStrength, FiveCardDrawRules, DrawRound
      Ai/              DuelObservation, IDuelOpponent, OpponentPersonality, BasicOpponent
      Match/           DuelMatch, GravityTrack, MatchConfig, HandOutcome
    Presentation/      (Phase 4)
    UI/                (Phase 4 — the real UI; the placeholder lives in Dev/)
    Audio/             (Phase 4)
    Dev/               Throwaway harnesses and the placeholder IMGUI duel
    Editor/            Editor tooling (menu: Black Hole Bluff / Setup)
  Tests/EditMode/      BlackHoleBluff.Tests.EditMode assembly
  Scenes/              Duel.unity (playable), DevSandbox.unity (card harness)
  Settings/            URP pipeline assets
  _TEMP_/              Placeholder art; gitignored, never ships
```

### The one architectural rule worth knowing

`BlackHoleBluff.Core` is declared with **`noEngineReferences: true`**. The compiler therefore *refuses* to let the rules reference `UnityEngine` at all — no `GameObject`, no `MonoBehaviour`, no `UnityEngine.Random`, no `Debug.Log`.

That makes the GDD's separation of rules from presentation unbreakable rather than merely intended, and it is why the whole rules suite runs in EditMode in about a second. Randomness is injected via `IRandomSource` so any shuffle can be reproduced from a seed.

There are deliberately only **two** assembly definitions. More will be added when they earn their place.

## Documentation

| File | Contents |
|---|---|
| [docs/RULES.md](docs/RULES.md) | The authoritative rules: five-card draw, hand rankings and tie-breaks, Gravity Pressure |
| [docs/DEVLOG.md](docs/DEVLOG.md) | Living development log — what was built, what was verified, what is still open |

## Related

Kara Yel: The Forty Passes is a **separate project** at `../steamgame` and shares nothing with this one.
