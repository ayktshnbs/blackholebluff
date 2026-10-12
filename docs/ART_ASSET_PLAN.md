# Black Hole Bluff — Art Asset Plan

Visual direction, prioritised asset inventory, production order, and technical standards.

**Status:** plan approved for Stage 1. No final art assets exist yet.

> Every number in this document is a **starting value to build against and then tune in
> engine**, not a law. Where a number is load-bearing — card dimensions, the atlas layout,
> the shared skeleton — it says so explicitly.

---

## 1. Visual direction and reference boundaries

### The premise, in one image

Two eccentric aliens play a dangerous card game on a scrap platform in open space. A black
hole fills the view beyond and below them. When one loses, their seat releases and they are
dragged off the platform into it.

### The three things a first screenshot must communicate

Everything in this plan serves these, in this order:

1. **They are playing in open space** — not a casino. Stars and void on every side, no walls, no floor beyond the platform edge.
2. **The black hole is a constant, visible threat** — large in frame, always present, never a background detail you could crop out.
3. **Losing has a dramatic consequence** — readable even in a still: restraints, warning lights, the empty space past the platform edge.

If a screenshot fails any of the three, the composition is wrong regardless of asset quality.

### Palette and atmosphere

| Role | Colour | Where |
|---|---|---|
| Environment base | Deep blue-violet, near-black | Void, distant nebula |
| Primary accent | Violet / magenta | Accretion disk, black-hole light |
| Secondary accent | Cyan | Table edge lights, UI, cool rim light |
| Warning accent | Amber / orange | Wreckage hazard lights, seat restraints |
| Characters | Saturated, contrasting with each other and the void | Skin, clothing |

**The environment stays dark.** Cards, the table surface, the wager indicators and the
black hole are the only things that read bright. If anything else competes, dim it.

### Style

Stylised, not photoreal. Exaggerated proportions, readable silhouettes, controlled palettes,
clean shapes with worn surface detail. Think *built from scrap by someone with taste*: the
arena is salvaged, not derelict.

### Reference boundaries — read this before referencing anything

The open-space atmosphere reference is **Breathedge**: the feeling of being surrounded by
void, floating wreckage, industrial sci-fi hardware, and cold light.

What that means in practice:

- ✅ **Take**: the general atmosphere of open-space survival — exposed vacuum, salvaged
  industrial hardware, debris fields, cold cosmic light, the sense of being somewhere you
  should not survive.
- ❌ **Do not take**: its characters, its assets, its interface, its typography, its humour
  voice, or any design distinctive enough to be recognised as from that game.

The test: if someone familiar with the reference could point at an asset and name its
source, redesign it. Atmosphere is not ownable; specific designs are.

---

## 2. Prioritised asset inventory

### Arena coordinate convention

Every dimension below derives from this. Fix it first and nothing has to move later.

```
Platform floor plane ....... Y = 0
Table top surface .......... Y = 0.78
Seat pan ................... Y = 0.45
Seated eye line ............ Y = 1.22   (0.44 above the table — faces must clear it)
Player seat ................ Z = -0.95
Opponent seat .............. Z = +0.95
Table centre ............... origin, long axis along X
Camera ..................... behind the player, at -Z, looking +Z and slightly down
```

The player sits at **−Z**, the opponent at **+Z**, and the black hole lies beyond and below
**+Z**. One Unity unit is one metre throughout.

---

### PRIORITY 1 — first visual prototype

#### A. Space background

**Asset 1 — Space background.** A skybox or layered backdrop, *not* modelled geometry.

| | |
|---|---|
| Form | Cubemap skybox (6×2048) or a panoramic 4096×2048 equirectangular |
| Contents | Star field, one subtle nebula band, one distant planet or moon, optional far wreckage silhouettes |
| Not included | The black hole — that is real geometry (Asset 2), because it needs motion, light and parallax |
| Brightness | Dark. Stars are the brightest thing in it, and they are still dim next to the table |

A distant planet gives the void a sense of place and costs nothing. Put it off-axis so it
does not fight the black hole for attention.

#### B. Black hole

**Asset 2 — Main black-hole visual.** The most important single element in the game.

Built from parts, deliberately cheap:

| Part | Form | Notes |
|---|---|---|
| Event horizon | Low-poly sphere, pure black, unlit | No lighting response at all — it must read as absence |
| Accretion disk | Flat or shallow-cone ring mesh, 200–800 tris | Scrolling emissive material, two-sided |
| Inner glow | A second, smaller ring or billboard | Hotter colour at the inner edge |
| Particles | Slow inward-spiralling motes | A few hundred, no collision |
| Debris | A handful of Priority-2 debris pieces on slow orbital paths | Reuses existing meshes |

**Starting transform:** disk outer diameter **130 m**, centre near **(0, −170, 300)**, disk
plane tilted roughly 25° off the camera's view direction so it reads as a disk rather than
a line or a circle. From the gameplay camera this subtends about 19° — large enough to
dominate, small enough to leave the table readable.

**Do not** begin with physically accurate lensing. A black sphere, a bright rotating ring
and good colour choices will sell it. Distortion can come later as a full-screen effect if
it earns its place.

**It is also a light source.** The violet accent light on the characters comes from here, so
its colour is a lighting decision as much as a material one.

#### C. Floating arena and wreckage

**Asset 3 — Main floating platform.** Compact, irregular, assembled from modular hull
pieces. Roughly **5 × 5 m** with a 0.3–0.6 m thickness and a deliberately ragged edge.
Open to space on all sides — no railings, no walls. The edge matters: it is the thing the
loser is dragged over.

**Asset 4 — Main poker table.** The focal point after the characters.

| | |
|---|---|
| Top surface | **1.35 (X) × 0.90 (Z) m**, 0.06 m thick, top at Y = 0.78 |
| Player card area | Centred Z = −0.28 |
| Opponent card area | Centred Z = +0.28 |
| Central area | Z ∈ [−0.12, +0.12] — showdown cards and wager indicators |
| Body | Worn metal, visible mechanism, a few cyan luminous accents |
| Budget | 3,000–6,000 tris |

Those card areas are derived, not arbitrary: a five-card hand at the card size in Asset 8
fans to about 0.26 m wide and 0.1125 m deep, which fits inside a 0.90 m depth with room for
the central area between.

**Asset 5 — Seating.** One seat model, two instances at Z = ±0.95, seat pan at Y = 0.45.
Industrial and improvised — bolted-on salvage, with a visible restraint or magnetic clamp,
because the restraint *releasing* is the first beat of the loss sequence.

> **The seat and the character must be separate objects.** The losing character is pulled
> away while the seat stays bolted to the platform. Nothing about the character should be
> modelled as part of the chair.

Budget: 1,000–2,000 tris.

#### D. Characters

**Assets 6 and 7 — two original aliens.** One player, one opponent. Full technical
specification in §4.2 — read it before modelling.

Priorities, in order: **silhouette**, **eyes**, **face proportion**, **palette**. Surface
detail is last and barely matters; these characters are judged on reactions.

The two must be distinguishable as black shapes at thumbnail size. If both are "upright
with a big head", one of them is wrong — vary mass, height, shoulder line, head shape.

> **Build and fully validate the first character before starting the second.** Rig, export,
> import, materials, proportions against the table, framing in the camera, and one test
> animation. Discovering a pipeline problem once is cheap; discovering it twice is not.

#### E. Cards

**Asset 8 — Reusable 3D card mesh.** One mesh. Never 52 models.

| | |
|---|---|
| Dimensions | **0.075 (X) × 0.1125 (Z) m**, 0.002 m thick — aspect 1.5 |
| Orientation | Lies in the XZ plane, face up along **+Y** |
| Pivot | Geometric centre, so flipping is a rotation about X or Z |
| Budget | ≤ 24 tris |

The 1.5 aspect is slightly squarer than a real playing card (1.4) and slightly oversized for
its scale. Both are deliberate: it keeps the atlas cells clean and the ranks legible from the
fixed camera.

**Card faces come from one atlas, and its layout is dictated by code.**

| | |
|---|---|
| Atlas | **4096 × 1536**, 16 columns × 4 rows, cell **256 × 384** px |
| Columns 0–12 | Ranks, in the order of `Rank.Two` (2) … `Rank.Ace` (14) → column = rank − 2 |
| Rows 0–3 | Suits, in the order of the `Suit` enum: **0 Clubs, 1 Diamonds, 2 Hearts, 3 Spades** |
| Columns 13–15 | Spare: card back, highlight state, future specials |

That row and column order is **not a preference** — it matches `Core/Cards/Card.cs` so a UV
offset can be computed directly from a `Card` with no lookup table. The art must represent
the cards the rules actually produce; the rules are the source of truth.

Also required: an original card-back texture, reusable rank glyphs and suit symbols (the
source material the atlas is built from), a card-face material and a card-back material.

#### F. Shared material library

Small, and reused everywhere. URP Lit unless stated.

| Material | Key properties | Used by |
|---|---|---|
| `M_Hull_Worn` | Metallic ~0.8, smoothness ~0.35, normal + mask | Platform, props, table body |
| `M_Hull_Painted` | Same base, painted colour and stencil decals | Platform highlights, crates |
| `M_Rubber_Black` | Metallic 0, smoothness ~0.2 | Cables, seals, grips |
| `M_Alien_Skin_A` / `_B` | Light base colour, low smoothness | Characters |
| `M_Accent_Emissive` | Emission in HDR colour, near-black base | Warning lights, table edge, restraints |
| `M_CardFace` | Bright base, slight emission, low smoothness | Cards |
| `M_CardBack` | As above, back texture | Cards |
| `M_AccretionDisk` | Unlit or emissive, two-sided, scrolling UV | Black hole |

Two URP facts worth knowing before authoring skin:

- **URP Lit has no subsurface scattering.** Translucent-looking alien skin has to be faked
  with base colour, a soft normal map and rim light. Do not author textures expecting SSS.
- **Cards must stay readable in a dark scene.** A small emission on `M_CardFace` is the
  cheap fix; going fully Unlit also works but loses the tabletop's light response. Start
  with Lit plus modest emission and judge it in the scene.

---

### PRIORITY 2 — polished vertical slice

#### G. Space debris and props

A modular set, reused by rotating, scaling and repositioning rather than modelling
individually:

- 2–3 broken hull pieces (500–1,500 tris each)
- 1–2 detached panels
- A small cable/pipe set
- 1 damaged antenna or satellite fragment
- 1–2 industrial crates or gas cylinders
- 3 asteroid variants, different sizes (300–800 tris each)
- Several tiny debris pieces for background motion (50–200 tris each)

**Distant objects get much simpler geometry and share the hull atlas.** Nothing more than
~15 m from the camera needs its own texture.

#### H. Character animation

Minimum practical set, in rough order of need:

| State | Notes |
|---|---|
| Idle | Looping, with subtle breathing/drift |
| Thinking | Played while the opponent decides |
| Reacting to a wager | Short, readable, personality-defining |
| Placing a wager | Reaches to the table |
| Winning | Short |
| Losing | Short |
| **Restraint release / pulled from seat** | The signature moment, part 1 |
| **Falling toward the black hole** | The signature moment, part 2 — tumbling, looping |

Much of the personality can come from facial expression swaps and small head movement
rather than full body animation. See §4.2 for the face approach — it is deliberately cheap.

Both characters share one skeleton template, so every clip authored for the first works on
the second.

#### I. Black-hole presentation effects

| Effect | Trigger |
|---|---|
| Accretion-disk rotation | Always |
| Glowing particles, drifting debris | Always |
| Light shift on high-pressure wagers | Pressure 3 and 4 |
| Rising environmental agitation as a character nears elimination | Gravity step count |
| Character pull and acceleration | Loss of the final step |
| Consumption sequence | Match end |

> **The rules decide; the presentation shows.** `DuelMatch` resolves a hand deterministically
> and hands back a `HandOutcome` with the steps applied and the victor. Effects read that
> record and play it at whatever pace looks good. Nothing in the effect system may influence
> an outcome, and nothing in the rules may wait on an animation.

#### J. Lighting and composition

One polished setup for the arena:

| Light | Role |
|---|---|
| Cool cosmic fill | Very low ambient, blue-violet, so nothing is pure black |
| Violet accent from the black hole | Directional or area, from +Z and below |
| Cyan rim | Behind/above the characters, separates them from the void |
| Amber practicals | Warning lights on wreckage, seat restraints |
| Table key | Tight, controlled, keeps cards and tabletop readable |

Keep bloom restrained and skip heavy distortion. If the cards or the gravity indicators get
hard to read, the lighting is wrong — readability wins over spectacle every time.

---

### PRIORITY 3 — later, and must not block the first scene

Additional characters · alternative character materials and accessories · extra table
designs · more asteroid and wreckage variants · elaborate facial animation · extra
environmental particles · additional arena backdrops · cosmetic variations.

**Do not build a library before one scene is assembled and tested.**

---

## 3. Production order

### Stage 1 — Greybox composition *(primitives only)*

Table, two character positions, basic background, placeholder black hole, fixed gameplay
camera. Verify that the table, cards, characters and black hole all coexist in one readable
frame before any real asset exists.

**Camera starting values:** position `(0, 1.75, −2.0)`, looking at `(0, 1.00, 0.15)` — about
19° down — vertical FOV 45°, near clip 0.05, far clip 2000.

> **Recommended deviation from the stated order:** build a *rough* black hole during Stage 1
> rather than waiting for Stage 5. It is a black sphere, a ring and an emissive material —
> an hour's work — and it is the dominant light source and the whole premise. Every asset
> after it gets authored and judged in the real environment instead of a grey room.

### Stage 2 — First character

Full specification in §4.2. Validate rig, export, import, materials, proportions against
the table, and camera framing before starting anything else.

### Stage 3 — Table and platform

First polished table plus a small set of supporting wreckage. Verify scale, pivots,
materials, lighting and framing in engine.

### Stage 4 — Second character

Different silhouette and palette, same skeleton and pipeline.

### Stage 5 — Cards and black hole

Card mesh and atlas, final black hole, card motion, environmental effects, and the
pull-away sequence.

### Stage 6 — Vertical-slice polish

Remaining props, materials, lighting, animation, sound. Then judge whether it holds up in a
screenshot and a short gameplay capture.

---

## 4. Technical standards

### 4.1 General

| Rule | Detail |
|---|---|
| Scale | **1 Blender unit = 1 metre = 1 Unity unit.** Scene unit scale 1.0, Metric, Length Meters |
| Source files | Keep every `.blend`. They are the asset; the FBX is a build artifact |
| Source location | **Outside `Assets/`** — see §5 |
| Transforms | Apply before export. Object scale `(1,1,1)`, rotation zero, mesh data in final orientation |
| Pivots | Deliberate and documented per asset type (§4.4) |
| Materials | Reused. Create them in Unity, never rely on imported Blender materials |
| Textures | Sized to on-screen need, not to habit. 2048 for hero assets, 1024 for props, 512 for distant debris |
| Distant debris | Cheap geometry, shared atlas, no unique textures |
| Verification | Check transparency and emissive **in the actual URP scene** — they look wrong everywhere else |
| Packages | Add none without a clear need |

### 4.2 Character specification — read before modelling

This is the answer to "what should I build first and to what spec".

**Proportions**

| | Value |
|---|---|
| Nominal standing height | **1.90 m** |
| Head height | **~0.34 m** (≈1:5.6 head-to-body — stylised, not realistic 1:7.5) |
| Seated eye line | **Y = 1.22 m**, i.e. 0.44 m above the tabletop |
| Eyes | Large — roughly 40% of face width combined. The single most important feature |
| Silhouette | Must be identifiable as a black shape at thumbnail size |

Seated eye height is the number that actually matters. These characters are seated for the
entire game, so legs are nearly irrelevant and the face clearing the table is everything.
Build the character **in a seated pose reference** against a 0.78 m box before committing.

**Rig — Generic, on a shared skeleton template**

Use **Generic**, not Humanoid. Humanoid's retargeting is the usual reason to choose it, but
both characters will share one authored skeleton anyway, so that benefit is small — and
Humanoid normalises proportions, which fights deliberately non-human shapes and would
constrain the second character's design.

Shared bone hierarchy (≈26 bones). Names and hierarchy must match **exactly** between the
two characters so clips are interchangeable:

```
Root
└── Hips
    ├── Spine_01 → Spine_02 → Chest
    │   ├── Neck → Head
    │   │   ├── Jaw                (optional)
    │   │   ├── Eye_L  Eye_R
    │   ├── Shoulder_L → UpperArm_L → LowerArm_L → Hand_L
    │   │   ├── Thumb_L  Index_L   (minimum for holding cards)
    │   └── Shoulder_R → UpperArm_R → LowerArm_R → Hand_R
    │       ├── Thumb_R  Index_R
    ├── UpperLeg_L → LowerLeg_L → Foot_L
    └── UpperLeg_R → LowerLeg_R → Foot_R
```

`Root` sits at the origin and is the bone the pull-away sequence drives. Keep it separate
from `Hips` so the body can be animated independently of where the whole character is
being dragged.

**Faces — expression swaps, not blendshapes**

Put eyes and mouth on their own material with a **4×4 atlas of expressions** (eyes and mouth
in separate atlases, or one combined), driven by a material property / UV offset. Sixteen
expressions per feature costs one texture and reads enormously better than a handful of
blendshapes, which is the right trade for a solo developer on a stylised game.

Add shape keys only for things an atlas cannot do — a jaw drop, a body squash.

**Pivot and orientation**

| | |
|---|---|
| Pivot | Floor contact point, between the feet |
| Origin | `(0, 0, 0)`, standing on the ground plane |
| Facing | **+Z** |

The opponent is then placed with a 180° Y rotation. Facing +Z matches Unity's forward, so
`transform.forward` means what it says.

**Budgets**

| | |
|---|---|
| Triangles | 8,000–15,000 |
| Body textures | 2048 — `_BC`, `_N`, `_MAS` |
| Face atlas | 1024 |
| Materials per character | 2 (body, face) |

**Unity import settings**

*Model tab*

| Setting | Value |
|---|---|
| Scale Factor | 1 |
| Convert Units | ✓ |
| Import BlendShapes | only if shape keys are used |
| Mesh Compression | Off |
| Read/Write Enabled | Off |
| Optimize Mesh | ✓ |
| Normals | **Import** (author them in Blender) |
| Tangents | Calculate Mikktspace |
| **Materials → Import Materials** | **Off** — build URP materials by hand |

*Rig tab*

| Setting | Value |
|---|---|
| Animation Type | **Generic** |
| Avatar Definition | Create From This Model |
| Root node | `Root` |
| Skin Weights | Standard (4 bones) |
| **Optimize Game Objects** | **Off** — the bone transforms are needed to parent cards to hands |

*Animation tab* — Off on the mesh FBX. Animations come in as separate files.

**File layout**

One FBX for the mesh and skeleton, one per animation clip. Mixing them makes every clip
re-import the mesh:

```
SK_Alien_A.fbx            mesh + skeleton, no animation
A_AlienA_Idle.fbx
A_AlienA_Thinking.fbx
A_AlienA_PulledFromSeat.fbx
```

**Armature export settings (Blender FBX)**

| Setting | Value |
|---|---|
| **Add Leaf Bones** | **Off** — leaf bones pollute the rig and break clip sharing |
| Primary Bone Axis | Y |
| Secondary Bone Axis | X |
| Apply Scalings | FBX All |
| Forward / Up | −Z Forward / Y Up |
| Apply Unit | ✓ |
| Smoothing | Normals Only |
| Bake Animation | only on animation exports |

### 4.3 Naming

| Prefix | Meaning | Example |
|---|---|---|
| `SM_` | Static mesh | `SM_PokerTable` |
| `SK_` | Skinned mesh | `SK_Alien_A` |
| `A_` | Animation clip | `A_AlienA_Idle` |
| `M_` | Material | `M_Hull_Worn` |
| `T_` | Texture | `T_Hull_Worn_BC` |
| `PF_` | Prefab | `PF_DuelArena` |
| `VFX_` | Effect | `VFX_AccretionDisk` |

Texture suffixes: `_BC` base colour · `_N` normal · `_MAS` packed mask · `_EM` emissive.

`_MAS` packing, chosen to suit URP Lit: **R = Metallic, G = Occlusion, A = Smoothness.**
Set the material's Smoothness Source to **Metallic Alpha** to read it.

### 4.4 Pivots by asset type

| Asset | Pivot |
|---|---|
| Character | Floor contact between the feet, facing +Z |
| Table, seat, crate | Base centre, so it sits on a surface at Y = 0 |
| Card | Geometric centre, face up +Y |
| Platform / hull piece | Base centre of its connecting face, on-grid where modular |
| Asteroid, debris | Volumetric centre, so it tumbles about itself |
| Accretion disk | Centre of the ring |

### 4.5 Scene budget

| | Tris |
|---|---|
| 2 characters | ~30,000 |
| Table + 2 seats | ~10,000 |
| Platform | ~9,000 |
| 10 cards | ~250 |
| Black hole | ~800 |
| Props and debris | ~5,000 |
| **Total** | **~55,000** |

Comfortable for a fixed-camera PC scene with two characters. The constraint on this project
is authoring time, not the GPU — so spend polygons where they show and nowhere else.

---

## 5. Unity folder structure

The project currently has flat, **empty** `Models/`, `Materials/`, `Textures/` and
`Animations/` folders from the Phase 0 scaffold. They are retired in favour of an `Art/`
hierarchy; nothing has to be migrated because nothing is in them.

```
Assets/
  Art/
    Characters/       SK_, rigs, per-character materials and textures
    Environment/      Platform, hull pieces, skybox
    Props/            Debris, crates, asteroids, cables
    Cards/            Card mesh, atlas, card materials
    Materials/        Shared material library
    Textures/         Shared textures and atlases
    Animations/       Clips and controllers
    VFX/              Particles, black-hole effects, shaders
  Audio/              (existing)
  Data/               (existing — ScriptableObjects)
  Prefabs/            (existing — assembled prefabs)
  Scenes/             (existing)
  Scripts/            (existing)
  Settings/           (existing — URP assets)
  Tests/              (existing)
  UI/                 (existing)
  _TEMP_/             Placeholders; gitignored, never ships

ArtSource/            **Outside Assets/** — .blend files, texture sources
```

Two deliberate choices:

- **`.blend` sources live outside `Assets/`.** Unity imports `.blend` files if it finds them,
  which means it needs Blender installed to open the project and re-imports on every save.
  Keeping sources in a sibling `ArtSource/` folder avoids both. They are still version
  controlled, and `.gitattributes` already routes `.blend` through LFS.
- The folders are **documented here but not yet created**, to keep this task
  documentation-only. One command creates them when Stage 2 begins.

---

## 6. Dependencies between assets

What blocks what. Arrows point from prerequisite to dependant.

```
Arena coordinate convention (§2)
  ├──> Table dimensions ──> Card area layout ──> Card dimensions
  ├──> Seated eye line ──> Character proportions
  └──> Camera framing  <──(iterate)── Table + characters

Rules code (Core/Cards/Card.cs)
  └──> Card atlas row/column order        ← HARD dependency, not a preference

Shared skeleton template (§4.2)
  ├──> Character A ──> all animation clips
  └──> Character B ──> reuses every clip

Hull material + atlas
  ├──> Platform
  ├──> Table body
  └──> Props and debris

Black hole (even rough)
  ├──> Arena lighting ──> character rim light ──> material judgement
  └──> Pull-away sequence target

Seat (separate object from character)
  └──> Pull-away sequence
```

Three of these are worth stating plainly because getting them wrong costs rework:

1. **The card atlas layout is dictated by the `Rank` and `Suit` enums.** Build the atlas to
   match the code, not the other way round.
2. **Lighting depends on the black hole**, which is why building it rough and early is worth
   breaking the stage order for. Judging a skin material in a grey room tells you nothing.
3. **The character must be a separate object from the seat**, decided at modelling time. It
   cannot be fixed later without re-rigging.

---

## 7. Acceptance criteria — first polished scene

The scene is done when all of the following are true. These are deliberately testable.

**Contents**

- [ ] Two original alien characters, visually distinct in silhouette and palette
- [ ] One poker table and two seats
- [ ] One small floating wreckage platform, open to space on all sides
- [ ] A stylised open-space background
- [ ] A prominent black hole
- [ ] Cards rendering the actual hands the rules deal
- [ ] Gravity Pressure presented on the table
- [ ] The losing character's pull-away sequence
- [ ] Basic sound and lighting feedback

**Readability**

- [ ] Both players' card ranks and suits are legible at 1080p without zooming
- [ ] Each character's remaining gravity steps are readable at a glance
- [ ] The current pressure level is unambiguous
- [ ] Nothing competes with the cards, table or black hole for attention

**The three-second test** — show a still to someone who has never seen the game:

- [ ] They say it is set in space, unprompted
- [ ] They identify the black hole as dangerous, unprompted
- [ ] They can tell which character is losing

**Technical**

- [ ] 1080p60 on the target machine with the full scene and effects running
- [ ] No asset relies on imported Blender materials
- [ ] Every `.blend` source retained, outside `Assets/`
- [ ] No gameplay logic in presentation code; effects only read `HandOutcome`
- [ ] A full duel can be played and restarted without leaving Play mode

**Explicitly out of scope:** additional characters, multiple arenas, tournament content,
cosmetic variations.

---

## 8. Asset checklist

Legend: ⬜ planned · 🟡 in progress · ✅ complete

### Priority 1 — first visual prototype

| | Asset | Owner | State |
|---|---|---|---|
| 1 | Space background (skybox) | artist | ⬜ |
| 2 | Black hole — sphere, disk, particles | artist | ⬜ |
| 3 | Floating platform (modular hull) | artist | ⬜ |
| 4 | Poker table | artist | ⬜ |
| 5 | Seat (one model, two instances) | artist | ⬜ |
| 6 | Alien character A — player | artist | ⬜ |
| 7 | Alien character B — opponent | artist | ⬜ |
| 8 | Card mesh | artist | ⬜ |
| 8a | Card face atlas (code-dictated layout) | artist | ⬜ |
| 8b | Card back texture | artist | ⬜ |
| F | Shared material library (8 materials) | artist | ⬜ |
| — | Greybox arena + fixed camera | **claude** | ⬜ |
| — | `Assets/Art/` folder structure | **claude** | ⬜ |
| — | Card atlas UV mapping from `Card` | **claude** | ⬜ |

### Priority 2 — polished vertical slice

| | Asset | Owner | State |
|---|---|---|---|
| G | Hull pieces ×3, panels ×2, cables, antenna, crates ×2 | artist | ⬜ |
| G | Asteroids ×3, small debris set | artist | ⬜ |
| H | Animation set — idle, thinking, react, wager, win, lose | artist | ⬜ |
| H | Animation — restraint release, pulled from seat, falling | artist | ⬜ |
| H | Face expression atlases | artist | ⬜ |
| I | Black-hole effects — rotation, particles, agitation, consumption | artist + claude | ⬜ |
| J | Arena lighting setup | artist | ⬜ |
| — | Presentation layer reading `HandOutcome` | **claude** | ⬜ |
| — | Card dealing and flipping motion | **claude** | ⬜ |
| — | Gravity Pressure indicator presentation | **claude** | ⬜ |
| — | Sound hooks | **claude** | ⬜ |

### Priority 3 — later

All ⬜ and explicitly not blocking: extra characters, accessories, alternative tables, more
debris variants, elaborate facial animation, extra particles, additional backdrops,
cosmetics.

---

## Related documents

| File | Contents |
|---|---|
| [RULES.md](RULES.md) | The authoritative game rules — the source of truth art must represent |
| [DEVLOG.md](DEVLOG.md) | Living development log |
| [../README.md](../README.md) | Project overview and status |
