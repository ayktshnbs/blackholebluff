# Black Hole Bluff — Development Log

Living record. Append entries; don't rewrite history. Each entry states what was **actually verified**, not what was intended.

---

## 2026-10-12 — Q-5 resolved; Phase 2 under review

### Decision

Pressure levels cost **2 / 3 / 4** steps. Fold cost stays **1**. Starting distance rises **4 → 6**.

Rationale (from the project owner): under 1/2/3 the lowest level has no meaningful fold decision, while a 4-step loss from a 4-step start would eliminate a player on the opening hand. Six steps preserves high-stakes decisions without making hand one terminal.

Documented in `RULES.md` §4 (with an implemented-vs-approved box) and §4.3a/§4.3b. **No implementation code has been changed** — Phase 2 stays exactly as reviewed.

### Why this is strategically sound

Folding costs 1 for certain; calling a wager of N loses N on a loss and inflicts N on a win, so folding is correct below `p = (N − FoldCost) / 2N`:

| Wager | Fold correct below |
|---|---|
| 2 | p < 0.25 |
| 3 | p < 0.333 |
| 4 | p < 0.375 |

Every level now has a fold window, and because the cheapest wager (2) costs more than folding (1), the fold button finally has a reason to exist. Under 1/2/3 the bottom level was a strictly free call.

Distance maths at 6 steps: one maximum loss takes 4 and leaves 2 — survivable once. Two maximum losses are fatal, so a duel runs at least two losing hands. Six folds is also fatal, so passive play still loses, just slowly.

### Implementation touchpoints (for Phase 3, not done yet)

Mechanical, but there are traps:

| # | Location | Change |
|---|---|---|
| 1 | `MatchEnums.cs` | `PressureLevel` values 1/2/3 → 2/3/4, and **rename the members** so each name matches its value (§4.3b) |
| 2 | `MatchConfig.Default` | `(4, 1)` → `(6, 1)` |
| 3 | `DuelMatch.SubmitPlayerWager` | Validation bound `< One \|\| > Three` |
| 4 | `DuelMatch.ClampWager` | Same bounds |
| 5 | `BasicOpponent.ChooseWager` | Level mapping `1/2/3`; finisher test `OpponentSteps <= 3`; **safety clamp `Math.Max(1, MySteps − 1)` can yield 1, which will no longer be a legal level** |
| 6 | `BasicOpponent.RespondToWager` | Thresholds keyed on `pressure == 2`, and the EV comment |
| 7 | `DuelDevUI` | Three button labels, and its own `startingSteps`/`foldCost` fields |
| 8 | **`Duel.unity`** | **Serialized `startingSteps: 4` overrides the code default — changing `MatchConfig.Default` alone will NOT change the playable scene** |
| 9 | Tests | `DuelMatchTests` TestCase expectations, `GravityTrackTests` (4 → 6), `OpponentTests` threshold hands, balance probe bounds |

### Finding from the review: the config numbers live in three places

`MatchConfig.Default` (4, 1), `DuelDevUI`'s serialized field defaults (4, 1), and the **values baked into `Duel.unity`** (4, 1). The scene wins, so the first two are decorative for the playable build.

The UI also reads its own `startingSteps` and `foldCost` for display rather than `_match.Config`, which is the same duplication class as the hardcoded discard-limit constant already fixed in Phase 2.

**Recommended with the Q-5 work:** `DuelDevUI` should default to `MatchConfig.Default` and read all display values from `_match.Config`, leaving the serialized fields as an explicit opt-in override or removing them. One authority, so a rules change cannot be silently ignored by a scene.

---

## 2026-10-12 — Phase 2 complete: the duel is playable

### Verified results

```
EditMode tests : total="166" passed="166" failed="0" inconclusive="0" skipped="0"
Warnings       : 0 in project code
Windows build  : exit 0 — Build/BlackHoleBluff.exe
Balance probe  : always fold 6.0% | always Pressure 3 38.5% | always call 54.0% | plays the cards 51.5%
```

Phase 1 was committed first as `phase-1-card-rules` (`e01ed6b`, 121 files). Kara Yel was secured separately as `slice-b-controller`.

### What was built

| Type | Purpose |
|---|---|
| `MatchPhase`, `PressureLevel`, `WagerResponse` | Six phases, three pressure levels, two responses |
| `MatchConfig` | Starting steps and fold cost as data |
| `GravityTrack` | Distance per seat, clamped at zero, reports steps actually applied |
| `HandOutcome` | A resolved hand; presentation replays it at its own pace |
| `DuelMatch` | The state machine: deal, draw, wager, respond, resolve, next hand, victory |
| `DuelObservation` | **The information boundary** — all an opponent is ever given |
| `IDuelOpponent`, `OpponentPersonality`, `BasicOpponent` | One AI; Phase 5 personalities are parameter sets for the same model |
| `HandStrength` | Crude 0–1 heuristic for AI decisions only; never decides who wins |
| `DuelDevUI` | Placeholder IMGUI — a complete playable duel with zero art |

Scenes: `Duel.unity` (build scene 0, the playable duel) and `DevSandbox.unity` (the Phase 1 card harness).

### Two balance findings from the probe

The probe plays 200 full duels per strategy against the real AI, with a fixed seed, so the numbers are reproducible rather than statistical.

**1. Constant maximum pressure was winning — fixed.** The first run gave *always Pressure 3* a **66%** win rate, beating *plays the cards* at 51.5%. That is exactly the degenerate strategy GDD §3.3 forbids.

The cause was arithmetic, not taste. Folding costs `FoldCost` steps for certain; calling a wager of N loses N on a loss and inflicts N on a win. Comparing net step differential:

```
fold = -FoldCost          call = 2·N·p − N
=> calling is correct once p > (N − FoldCost) / 2N
   Pressure 2 -> p > 0.25      Pressure 3 -> p > 0.333
```

Those are low bars; my thresholds effectively demanded a flush. Retuned to match the arithmetic, and *always Pressure 3* fell to **38.5%**. Bluffing every hand now loses.

**2. The fold option is weak, and Pressure 1 is a free call.** This one is a property of the specified numbers, not a bug, and it needs a design decision.

With `FoldCost = 1` and pressures `1/2/3`:

- **Pressure 1 can never be worth folding to.** Folding costs 1 for certain; calling risks exactly 1 and can win. Folding is *weakly dominated*, so `BasicOpponent` now always calls when `pressure <= FoldCost`. One of the three levels therefore carries no fold decision at all.
- Across all levels the fold-correct window is narrow, which shows up in the probe: *always call* (54.0%) marginally outperforms *plays the cards* (51.5%). Never folding is currently a fine strategy.

**Recommendation (needs your decision):** make the pressure levels cost **2 / 3 / 4** steps while folding still costs 1. Then folding is correct below p = 0.25 / 0.333 / 0.375 respectively — a real decision at every level — and a called Pressure 3 from full distance becomes instant death, which is more dramatic, not less. Starting distance would likely need revisiting alongside it. `MatchConfig` already makes starting steps and fold cost data; the pressure-to-steps mapping is the one piece still hard-coded in the `PressureLevel` enum values.

### Decisions

**The information boundary is enforced structurally, not by convention.** `IDuelOpponent`'s every method takes only a `DuelObservation`, which carries the opponent's own cards and nothing else. Three reflection tests assert that `DuelObservation` exposes exactly one card collection named `MyCards`, that it cannot reach `DuelMatch`/`DrawRound`/`Deck`/`HandResult`, and that no opponent method accepts anything but an observation. An AI that can peek is unfair *and* impossible to balance, so this is worth a guard that fails loudly.

**Folding does not reveal cards.** `HandOutcome.PlayerHand`/`OpponentHand` are null after a fold and `DuelMatch.RevealedOpponentCards` stays null, so presentation cannot show what the rules kept hidden. This is what keeps a bluff worth making.

**The non-aggressor draws first**, so the advantage of drawing last alternates with the aggressor. The opponent's discard count is public once it has drawn, and `BasicOpponent` uses it as a tell — three cards away suggests weakness, standing pat suggests strength. `DuelObservation.Unknown` (−1) distinguishes "has not drawn" from "stood pat", which are very different reads.

**Illegal actions throw rather than being ignored**, so a double-clicked button is a loud bug instead of a silently duplicated wager. A test asserts gravity is not applied twice.

**A fresh shuffle each hand.** Simpler than a running deck and it removes any chance of exhausting 52 cards mid-duel.

**The placeholder UI is IMGUI, in `Scripts/Dev/`.** No canvas, no prefabs, no fonts, no art — a complete duel playable today, and impossible to mistake for the real interface. Expected to be deleted in Phase 4.

### Bugs and problems caught

1. **Optional parameter before an `out` parameter** in a test helper — compile error, reordered.
2. **A test assertion that tested nothing**: `Assert.IsTrue(seen.Add(card) || true)` is always true. Caught on review; replaced with a real uniqueness check.
3. **Three AI tests asserted thresholds I had not measured.** Fixed by computing the required hand strengths from the formula and choosing hands that actually straddle each threshold, rather than nudging bounds until green.
4. **A test set its scripted opponent after `StartMatch`**, by which time the opponent had already drawn. Test bug, not a match bug.
5. **A duplicated rules constant in the UI.** I had written a local `const int = 3` for the discard limit instead of using `FiveCardDrawRules.MaxDiscards` — precisely the rules-in-presentation leak the GDD warns about. Replaced.
6. **A dangling build-list entry.** The URP template's `EditorBuildSettings.asset` referenced a `SampleScene.unity` this project never copied. Unity tolerated it, but it is now pruned, and there is a menu item to prune again.

### Known limitations

- **No art, no sound, no 3D, no animation.** All Phase 4. The duel is IMGUI text and buttons.
- **No PlayMode tests.** Everything in Core is pure C#, and `DuelDevUI` is throwaway. PlayMode tests arrive with the real presentation layer.
- **`HandStrength` is not calibrated to win probability.** It is a monotonic heuristic, so the AI's thresholds are expressed in "strength" units rather than true equity. Good enough for a competent opponent; a Phase 3 improvement would be a proper equity estimate, which would also let the thresholds come straight from the EV formula.
- **The AI does not chase draws.** It keeps pairs or the two highest cards and never holds four to a straight or flush. An obvious place to add depth.
- **One personality only**, as the GDD specifies for this phase.
- **A tie cannot be forced in a test** without stacking the deck, which Core deliberately does not expose. Tie handling is covered by a unit test on `ShowdownResult` plus an invariant check across 400 real hands.
- The duel always starts with the player as aggressor on hand 1.

### Open questions

| | Question | Status |
|---|---|---|
| ~~Q-5~~ | Should pressure levels cost 2/3/4 steps instead of 1/2/3? | **Resolved 2026-10-12: yes, plus start 6.** See the entry above |
| Q-6 | Should the aggressor be able to check rather than being forced to wager 1–3? | Currently forced. Keeps every hand consequential; revisit if play feels forced |
| Q-3 | Does a tie feel right as "replay, nobody moves"? | Implemented. Confirm by playing |
| Q-1 | Company name placeholder | Still `BlackHoleBluff` |
| Q-2 | Steam title availability | Unchecked |

### Next

**Phase 3 — AI behaviour and balance.** Settle Q-5 first, since it changes every threshold. Then a calibrated strength estimate, configurable risk tolerance, and the balance probe extended across the planned personalities.

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
