# Black Hole Bluff — Rules

The authoritative specification. Where this file and the code disagree, that is a bug in one of them; fix both in the same commit.

Sections marked **(Phase 1 — implemented)** are built and covered by tests. Sections marked **(Phase 2 — specified)** are designed but not yet coded.

---

## 1. The deck — (Phase 1 — implemented)

A standard **52-card deck, no jokers**. Four suits (clubs, diamonds, hearts, spades), thirteen ranks (2–10, J, Q, K, A).

- An Ace is worth **14**, except in the ace-low straight (§3.3).
- Suits never break ties. They exist only for flushes and for visual variety.
- A drawn card is physically removed from the deck, so a card cannot be dealt twice without an explicit reset. Drawing from an empty deck throws rather than returning a blank card.
- Shuffling is Fisher–Yates over an injected `IRandomSource`, so any shuffle is reproducible from a seed.

Cards are written as two-character codes throughout the code and tests: rank (`2`–`9`, `T`, `J`, `Q`, `K`, `A`) then suit (`c`, `d`, `h`, `s`). So `As` is the ace of spades, `Th` the ten of hearts.

## 2. Five-card draw — (Phase 1 — implemented)

1. Each side is dealt **five cards**.
2. Each side gets **exactly one** opportunity to discard **up to three** cards and draw replacements.
3. Replacements come off the top of the same deck, so they can never duplicate a card already in play.
4. Final hands are evaluated and compared.

**Standing pat** — discarding nothing — is legal, and it still consumes that side's single draw. There is no second discard round.

## 3. Hand rankings — (Phase 1 — implemented)

Weakest to strongest:

| # | Category | Example |
|---|---|---|
| 1 | High card | `Ac Qd 9h 6s 3c` |
| 2 | One pair | `8c 8d Kh 6s 2c` |
| 3 | Two pair | `Jc Jd 4h 4s 9c` |
| 4 | Three of a kind | `Qc Qd Qh 7s 2c` |
| 5 | Straight | `9c 8d 7h 6s 5c` |
| 6 | Flush | `Ac Jc 9c 6c 3c` |
| 7 | Full house | `Kc Kd Kh 3s 3d` |
| 8 | Four of a kind | `7c 7d 7h 7s Kd` |
| 9 | Straight flush | `9h 8h 7h 6h 5h` |

A **royal flush** is not a separate category: it is a straight flush with an ace high (`As Ks Qs Js Ts`), which is automatically the best possible hand. Treating it uniformly keeps comparison logic simple.

### 3.1 How ties are broken

Hands are **never** compared by category alone, and **never** by summing card values. Evaluation produces a category plus an ordered list of tie-break values, compared left to right:

| Category | Tie-break values, most significant first |
|---|---|
| Straight flush | high card of the run |
| Four of a kind | quad rank, then the kicker |
| Full house | trips rank, then pair rank |
| Flush | all five ranks, descending |
| Straight | high card of the run |
| Three of a kind | trips rank, then two kickers descending |
| Two pair | high pair, low pair, kicker |
| One pair | pair rank, then three kickers descending |
| High card | all five ranks, descending |

Consequences worth stating explicitly, because each one is a test:

- **Threes full of twos beats twos full of aces.** The trips rank decides before the pair.
- Two hands with the same pair and the same first two kickers are still decided by the **last** kicker.
- A flush is compared rank by rank, so the **fifth** card can decide it.
- Identical ranks in different suits are a **genuine tie**. The match layer must handle ties (§4.4).

### 3.2 What is not a straight

- `Ac Kd Qh Js 9c` — A-K-Q-J-9 is a high-card hand.
- `Kc Ad 2h 3s 4c` — straights **do not wrap around** the ace. K-A-2-3-4 is nothing.

### 3.3 The ace-low straight (the wheel)

`A-2-3-4-5` **is** a straight, and it is the **weakest** one: its high card is **5**, not 14. It loses to a six-high straight and ties another wheel.

The same applies in one suit — the "steel wheel" `Ah 2h 3h 4h 5h` is a five-high straight flush, the weakest straight flush, not an ace-high monster.

This is the single place in the game where an ace is not worth 14.

---

## 4. Gravity Pressure

The signature mechanic. **The numbers are changing** — this section documents both the
version in the code today and the approved replacement.

> ### ⚠️ Numbers: implemented vs approved
>
> | | Implemented today | **Approved (Q-5), not yet implemented** |
> |---|---|---|
> | Starting distance | 4 steps | **6 steps** |
> | Pressure levels cost | 1 / 2 / 3 steps | **2 / 3 / 4 steps** |
> | Fold cost | 1 step | 1 step (unchanged) |
>
> Approved 2026-10-12. Rationale: under 1/2/3 the lowest level has no meaningful fold
> decision (§4.3a), while a 4-step loss from a 4-step start would end the duel on the
> opening hand. Six steps keeps the top wager genuinely dangerous without making hand
> one terminal.
>
> Phase 3 implements this. Until then the code, tests and balance figures below the
> warning box describe the **1 / 2 / 3 and start-4** version.

### 4.1 Distance

Each character starts a duel a fixed number of steps from the black hole — **4 today, 6 once Q-5 lands**. Reaching **zero** steps means being consumed, and losing the duel.

### 4.2 Wagering

The side acting first chooses one of three pressure levels. The number shown to the player **is** the number of steps at risk.

| Level | Meaning | Steps today | **Steps after Q-5** |
|---|---|---|---|
| Lowest | A cautious wager | 1 | **2** |
| Middle | A significant risk | 2 | **3** |
| Highest | A dangerous, high-stakes wager | 3 | **4** |

The other side may **fold** or **call**.

### 4.3 Resolution

- **Fold** — the folding side loses the hand and moves **one** step closer, regardless of the pressure level. Cards are not revealed.
- **Call** — both hands are revealed. The loser moves **the wagered number of steps** closer.

So a called Pressure 3 moves the loser three steps — from a full four, that is one hand from death.

The asymmetry is the whole point: folding always costs exactly one step, so a big wager against a weak hand is a real threat *and* a real risk. Bluffing is profitable only while opponents still fold.

### 4.3a Why the numbers are changing — Q-5, resolved

Folding costs `FoldCost` for certain. Calling a wager of **N** loses N on a loss and inflicts N on a win. Comparing net step differential:

```
fold = -FoldCost          call = 2·N·p − N
=> calling is correct once p > (N − FoldCost) / 2N
```

With the specified `FoldCost = 1` and pressures `1 / 2 / 3`:

| Wager | Fold is correct below |
|---|---|
| Pressure 1 | **never** — folding is weakly dominated |
| Pressure 2 | p < 0.25 |
| Pressure 3 | p < 0.333 |

Two consequences, both confirmed by the balance probe:

- **Pressure 1 carries no fold decision at all.** The downside of calling is identical to folding, so calling can only gain. `BasicOpponent` therefore always calls when `pressure <= FoldCost`.
- **Folding is rarely correct anywhere.** Across 200 duels per strategy, "always call" (54.0%) slightly outperforms a strategy that folds weak hands (51.5%).

**Resolution (approved 2026-10-12):** pressures cost **2 / 3 / 4** steps, folding stays at **1**, starting distance rises to **6**.

Under the new numbers folding becomes a real decision at every level:

| Wager | Fold is correct below | Equivalent |
|---|---|---|
| 2 steps | p < 0.25 | fold with the bottom quarter of hands |
| 3 steps | p < 0.333 | fold with the bottom third |
| 4 steps | p < 0.375 | fold with the bottom three eighths |

And the distance maths stays tense without being abrupt:

- A single maximum loss takes 4 of 6 steps — survivable once, leaving 2.
- **Two** maximum losses are fatal, so a duel is at least two losing hands long.
- Folding six times is also fatal, so passive play still loses — just slowly.
- Folding (1) is now clearly cheaper than the cheapest wager (2), which is what gives the fold button a reason to exist.

### 4.3b Implementation note: the number shown must equal the steps at risk

After Q-5 the three levels cost 2, 3 and 4 steps. Labelling them "Pressure 1/2/3" would mean "Pressure 1" costs two steps, which is needlessly confusing.

**Label each level by its step cost** — Pressure 2, Pressure 3, Pressure 4 — so the number on the button is always the number of steps the loser is dragged. The `PressureLevel` enum members should be renamed to match their values for the same reason.

### 4.4 Ties

A tie moves **neither** character. The hand is simply replayed. (Without this, a tie on a Pressure 3 would need an arbitrary tiebreak the player cannot reason about.)

### 4.5 Turn order

Duels **alternate** which side acts first each hand. When the AI acts first, the player's only decision is fold or call.

### 4.6 Explicitly out of scope for the first version

No side pots, no multiple raises, no re-raises, no variable bet sizing beyond the three levels, no all-in mechanics. Three levels and two responses is the whole betting system.

---

## 5. Information rules — (Phase 2 — implemented)

The AI **must not** read the player's hidden cards. It may use only:

- its own hand strength
- the pressure level on the table
- both characters' current gravity distance
- its own personality and risk preference
- a controlled amount of randomness

This is an architectural requirement, not a politeness: an AI that peeks is both unfair and impossible to balance. Phase 3 adds a test asserting the restriction.

Randomness must make the opponent unpredictable without making it feel arbitrary. If one simple strategy (always fold, always call, always Pressure 3) dominates in playtesting, the fix is adjusting decision thresholds and incentives — **not** adding more randomness.

---

## 6. Match end — (Phase 2 — implemented)

A duel ends when either character reaches zero steps. Gravity distance must never become negative or otherwise invalid without ending the match — that is MVP acceptance criterion 6.

A new duel must be startable without restarting Unity.
