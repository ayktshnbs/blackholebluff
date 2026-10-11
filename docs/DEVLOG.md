# Black Hole Bluff — Development Log

Living record. Append entries; don't rewrite history. Each entry states what was **actually verified**, not what was intended.

---

## 2026-10-12 — Phase 0 and Phase 1 complete

### Verified results

Run with Unity 6000.4.7f1 in batch mode on this machine:

```
EditMode tests : total="97" passed="97" failed="0" inconclusive="0" skipped="0"
Compile        : 0 errors in project code
Assemblies     : BlackHoleBluff.Core, BlackHoleBluff.Tests.EditMode, Assembly-CSharp
Windows build  : exit 0 — Build/BlackHoleBluff.exe produced (652 KB + data)
```

### Phase 0 — project setup

- New Unity project at `C:\Users\Aykut\Desktop\blackholebluff`, entirely separate from Kara Yel. Nothing in that project was touched.
- Built from the editor's own URP template (`com.unity.template.urp-blank`), so URP pipeline assets and graphics settings are pre-wired rather than converted after the fact.
- Settings: Linear colour space, Force Text serialization, Visible Meta Files, .NET Standard 2.1, Active Input Handling = Both, fast play-mode entry (domain + scene reload off).
- Git repo initialised with a Unity `.gitignore` (including `*.slnx`, which the standard Unity gitignore misses) and LFS attributes for models, textures, audio and fonts. UnityYAMLMerge configured as the merge driver for scenes and prefabs.
- `Assets/Scenes/DevSandbox.unity` created and registered as build scene 0.
- Windows build confirmed working.

**Package versions were taken from the editor's own authoritative manifest** at `Editor/Data/Resources/PackageManager/Editor/manifest.json`, not from the template. This matters: the template declares `"unity": "2023.3"` and its pinned versions (`inputsystem 1.12.0`, `timeline 1.8.6`) fail to compile on a 6000.4 editor. Learned the hard way on the other project.

Deliberately lean package set — six packages. No Addressables, Localization, Timeline, Visual Scripting, AI Navigation or Newtonsoft, because nothing needs them yet.

### Phase 1 — card rules

Implemented in `Assets/Scripts/Core/`:

| Type | Purpose |
|---|---|
| `Card`, `Rank`, `Suit` | Readonly value struct. Short codes (`As`, `Th`) with `Parse` / `ParseMany` |
| `IRandomSource`, `SeededRandomSource` | Injected randomness, so shuffles are reproducible |
| `Deck` | 52 cards, Fisher–Yates shuffle, draws remove cards, loud failure when empty |
| `HandCategory`, `HandResult` | Category plus ordered tie-break values; `IComparable`; `Describe()` for UI |
| `HandEvaluator` | Deterministic five-card evaluation, with validation |
| `FiveCardDrawRules` | The fixed numbers: five cards, one draw, max three discards |
| `DrawRound` | Deal, one discard-and-replace per seat, showdown |

Plus `Assets/Scripts/Dev/DrawRoundDevHarness.cs` — a throwaway MonoBehaviour that logs a round to the Console. Delete it when real UI arrives in Phase 2.

### Decisions

**`BlackHoleBluff.Core` is declared `noEngineReferences: true`.** The compiler now refuses any `UnityEngine` reference from the rules — no `GameObject`, no `MonoBehaviour`, no `UnityEngine.Random`, no `Debug.Log`. The GDD asks for rules independent of presentation; this makes it unbreakable instead of merely intended, and the whole suite runs in EditMode in about a second.

Consequence to accept: ScriptableObject-based opponent personalities (Phase 3) cannot live in Core. They belong in a thin data assembly that reads Core types, with the personality *parameters* as plain structs inside Core.

**Only two assembly definitions.** Core and the EditMode tests. Presentation, UI and Dev code sit in `Assembly-CSharp`, which auto-references Core. More assemblies when they earn their place.

**A royal flush is not its own category** — it is a straight flush with an ace high, which is automatically unbeatable. Uniform comparison beats a special case.

**Ties move nobody** and the hand is replayed (`docs/RULES.md` §4.4). Specified now because the alternative — an arbitrary tiebreak on a Pressure 3 wager — would be unreadable to the player.

**`DrawRound` is deliberately not a state machine.** It only knows how cards move. Betting, Gravity Pressure, fold/call and match flow are Phase 2 and belong in the match layer. Ordering is enforced with guards that throw, not with a phase enum that would pre-empt Phase 2's design.

### Two bugs caught during this session

1. **`static DrawRound NewRound(int seed = 2026, out Deck deck)`** — C# forbids an optional parameter before an `out` parameter. Compile error, fixed by reordering.
2. **A test assertion that tested nothing.** `Assert.IsTrue(seen.Add(card) || true)` is always true. Caught on self-review before the run; replaced with a real uniqueness assertion that names the offending card.

### Known limitations

- **No game yet.** No betting, no Gravity Pressure, no AI, no match flow, no UI, no 3D. All Phase 2+.
- `DrawRound` exposes both hands. Hiding the opponent's cards is the presentation layer's job; keeping them from the AI is the AI layer's job.
- No PlayMode tests. Nothing needs them yet — all Phase 1 logic is pure C#. They arrive with the match state machine.
- No art, no sound, no scene beyond the dev sandbox.
- The dev harness runs on `Start`, so opening `DevSandbox.unity` and pressing Play logs one round and then sits idle. That is all it is for.

### Open questions

| | Question | Recommendation |
|---|---|---|
| Q-1 | **Company name** is a placeholder (`BlackHoleBluff`). It sets the save path, so changing it later orphans any save data | Decide before persistent progression exists |
| Q-2 | Working title availability on Steam | GDD §2 flags this. Check before any store page work; do not leave it until Phase 7 |
| Q-3 | Does a tie consume the wager, or is the hand simply replayed with no movement? | Specified as "replay, nobody moves". Confirm it feels right once Phase 2 is playable |
| Q-4 | Should the first-acting side be able to check (wager nothing)? | Currently no — the acting side must pick Pressure 1–3. Simpler, and keeps every hand consequential. Revisit if play feels forced |

### Next

**Phase 2 — complete playable duel.** Match state machine, Gravity Pressure resolution, fold/call, victory and defeat, one basic AI opponent, restart flow. Plain UI, no 3D.

Nothing is committed yet; the first commit awaits a diff review.
