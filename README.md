# Black Hole Bluff

A single-player, poker-inspired duel against eccentric aliens, played at a table suspended above a black hole. Every wager decides how far the loser is dragged toward the event horizon.

> Two aliens. One card table. A black hole. Every bluff could be your last.

Fictional in-game stakes only. No real-money gambling, and no purchasable currency.

---

## Status

| Phase | State |
|---|---|
| **0 — Project setup** | ✅ Complete |
| **1 — Pure card-game prototype** | ✅ Complete — **97/97 tests passing** |
| 2 — Complete playable duel | ⬜ Next |
| 3 — AI behaviour and balance | ⬜ |
| 4 — 3D vertical slice | ⬜ |
| 5–7 — Content, polish, Steam | ⬜ |

No 3D assets, UI or presentation exist yet. That is deliberate: the rules are proven first (see [docs/RULES.md](docs/RULES.md)).

## Requirements

- **Unity 6000.4.7f1** (URP)
- Windows, Git, Git LFS

## Running it

There is no game to play yet — Phase 1 delivers the rules engine. Two ways to see it work:

**1. The dev harness.** Open `Assets/Scenes/DevSandbox.unity` and press Play. It deals a hand of five-card draw, applies the configured discards, and logs both hands with the showdown result to the Console. The `DevHarness` object lets you set a seed (reproducible) or randomise it, and choose which positions each side discards.

**2. The tests.** `Window → General → Test Runner → EditMode → Run All`. 97 cases covering every hand category, tie-breaking, kickers, the ace-low straight, deck uniqueness, shuffle determinism and the discard rules.

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
      GameRules/       HandEvaluator, HandResult, FiveCardDrawRules, DrawRound
      Ai/              (Phase 3)
      Match/           (Phase 2)
    Presentation/      (Phase 4)
    UI/                (Phase 2+)
    Audio/             (Phase 4)
    Dev/               Throwaway harnesses
    Editor/            Editor tooling (menu: Black Hole Bluff / Setup)
  Tests/EditMode/      BlackHoleBluff.Tests.EditMode assembly
  Scenes/              DevSandbox.unity
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
