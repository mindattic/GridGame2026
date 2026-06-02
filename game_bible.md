# GridGame2026 — Game Bible

**Document version:** Living. Every code change must keep this file in sync.
**Authors:** Synthesized by Claude from CLAUDE.md, project memory, and the working codebase.
**Audience:** Future-me, Legion, the user — anyone implementing or reviewing gameplay.

---

## Table of Contents

**Foundations**
- [0. North Star](#0-north-star) — pillars + feel target
- [1. The Board](#1-the-board) — grid, slide, pincer geometry
- [2. The Timeline](#2-the-timeline) — Grandia-style IP gauge, Prepare Zone, cast bars

**Resources & Systems**
- [3. Resources](#3-resources) — Mana, HP, stats, resistances
- [4. The AbilityBar](#4-the-abilitybar) — 6-slot Skill / Spell / Item bar
- [5. Targeting](#5-targeting) — Shape × Mode × Filter triad
- [6. The Spell Dispatcher](#6-the-spell-dispatcher) — 12-stage resolve coroutine
- [7. The Spell Catalog](#7-the-spell-catalog) — 17 spells with strategic role
- [8. Buffs and Debuffs](#8-buffs-and-debuffs) — interaction matrix, expire chains

**UI**
- [9. The HUD (15-row layout)](#9-the-hud-15-row-layout)
- [10. Equipment (stub)](#10-equipment-stub--see-24-for-the-full-spec) — see §24

**Tech**
- [11. Scene Architecture](#11-scene-architecture) — code-only / BuilderAutoRebuild
- [12. Asset Pipeline](#12-asset-pipeline) — Addressables conventions
- [13. Combat Resolution](#13-combat-resolution) — damage / heal formulas
- [14. AI](#14-ai) — EnemyPlanner archetypes
- [15. Save / Profile](#15-save--profile) — data model

**Process**
- [16. Open Design / Implementation TODOs](#16-open-design--implementation-todos)
- [17. Code-Only Workflow Discipline](#17-code-only-workflow-discipline) — pitfalls + cadence
- [18. Glossary](#18-glossary)
- [19. How to Add a New Spell — Checklist](#19-how-to-add-a-new-spell--checklist)
- [20. How to Add a New Enemy — Checklist](#20-how-to-add-a-new-enemy--checklist)

**Meta-Game**
- [22. The Macro Loop](#22-the-macro-loop) — battle ↔ vendor cycle
- [23. Character Classes](#23-character-classes) — identity table
- [24. Equipment, Items, Materials, Currency](#24-equipment-items-materials-currency) — rarity tiers
- [25. The Hub: Vendor Scenes](#25-the-hub-vendor-scenes) — per-screen sketches
- [26. Responsive Design & Aspect Ratio Profile](#26-responsive-design--aspect-ratio-profile)
- 27. Dialog & Story — *cut from the design (tombstone)*
- 28. Overworld — *cut; replaced by the scrollable level list (§22.3)*

**Quality**
- [29. Open Design Questions](#29-open-design-questions)
- [30. Performance Budgets](#30-performance-budgets) — 60fps target, GC pressure, coroutine hygiene
- [31. Accessibility](#31-accessibility) — color-blindness, motion, touch targets, readability
- [32. Document Discipline](#32-document-discipline-was-30--31)

---

## 0. North Star

A tactical-RPG hybrid where the player **drags heroes one tile at a time across a 6×8 grid** to flank enemies and trigger **pincer attacks**. Combat happens in a continuous-time **timeline** rather than fixed turns; enemies "load" left → right and act when ready. Magic, items, and class skills are paid for and managed through the **AbilityBar**. The feel target is *Final Fantasy timeline + Disgaea grid + FF8 draw economy*.

The game is **code-only / builder-driven**. `.unity` scene files are build artifacts of `Editor/Builders/*Builder.cs`; `BuilderAutoRebuild` regenerates them on every relevant file save. Visual content uses **Addressables** sprites loaded via `AssetHelper.LoadAsset<T>` and registered in `SpriteLibrary`. VFX use `VisualEffectLibrary` entries that point at prefabs (Addressables-backed).

### 0.1 Design pillars

1. **The verb is "slide".** Heroes never "land on" enemies. They flank into empty tiles; damage comes from the geometry that forms, not the move itself.
2. **Time is the resource the player manages.** The timeline replaces turns — every decision (cast, swap, bank) trades against the question "whose icon lands first?"
3. **Mana is a shared, visible pool.** The 12-slot ManaBank is the entire party's spell budget; pincers and enemy interrupts refill it. No per-hero MP bookkeeping.
4. **Every spell is a (shape × mode × filter) triad.** Adding a new spell never touches the dispatcher; it picks targeting axes and damage / debuff data.
5. **Code is the source of truth.** Editor hand-edits are temporary. The builder is the spec; the `.unity` is the print-out.
6. **Verify-then-checkpoint.** Land a feature end-to-end + play-test before committing; no mid-phase commits. See [[feedback_commit_granularity]].
7. **The bible is the brief.** If the code says one thing and the bible says another, fix the disagreement — don't let drift accumulate.

### 0.2 Non-goals (what this game is NOT)

What we say "no" to is as important as what we say "yes" to. Keep the loop honest:

- **Not turn-based in the JRPG sense.** No "press attack, watch animation, wait for enemy to press attack" rhythm. The timeline is continuous; thinking time is also game-clock time.
- **Not a deck-builder.** No card-draw, no per-run randomized starting hand. AbilityBars are deliberate loadouts the player builds in the Abilities vendor.
- **Not a roguelike.** V1 has no permadeath, no procedural runs, no "death is the only ending." Stages are authored, saves persist.
- **Not a gacha / live service.** No randomized character pulls, no daily energy, no premium currency. Single-purchase or premium-on-platform.
- **Not multiplayer.** No co-op, no PvP, no leaderboards (V1). Solo offline experience.
- **Not a grid-puzzle.** The board isn't a Tetris/match-3. Tile state is "who's standing there" — there's no piece-color matching, no chain combos beyond pincer-chains, no tile-clearing for clearing's sake.
- **Not free-form movement.** Heroes move one tile at a time, cardinal only, while the player drags. No path-finding, no run buttons, no diagonal moves.
- **Not a stat-spreadsheet.** Stats matter but never need a wiki. If a player has to read a forum to play a build, the design failed.

### 0.3 Session shape (target pacing)

| Beat | Target duration | Felt as |
|---|---|---|
| One **battle** | 90–180 seconds | A single decisive engagement |
| One **vendor visit** | 30–60 seconds | Quick stat tweak, not a menu trawl |
| One **stage** (battle + post-screen + vendor) | 3–4 minutes | A "round" |
| One **session** (5–8 stages) | 20–30 minutes | A mobile commute, a couch break |
| Full **playthrough** | 6–10 hours | A weekend campaign |

If a battle creeps past 5 minutes, the timeline or stage design needs tuning — combat should never feel like a chore.

---

## 1. The Board

- **6 columns × 8 rows.** Tiles are 1×1 world units. Origin at top-center per `BoardInstance.offset`.
- Each tile holds at most **one actor** (hero or enemy). Tile-coordinate space is `Vector2Int`; world conversion via `Geometry.CalculatePositionByLocation(loc)`.
- The board's `BoardInstance` lives in `Game.unity` with children `BoardOverlay`, `FocusIndicator`, `TargetModeOverlay`.

### 1.1 Sliding and displacement

The player drags a hero **one tile at a time, cardinal only** (N/E/S/W). Diagonals do not exist for movement. The drag is decomposed into discrete one-tile steps by `ActorMovement.TowardDestinationRoutine()` (X-axis leg, then Y-axis leg; the path may switch axes mid-drag).

When a drag step enters a tile that's **already occupied** by any actor (ally **or** enemy):
- The dragging hero takes that tile.
- The occupant is shoved back to the tile the dragging hero just left.
- This cascades: multiple actors in the path each slide in sequence as the hero passes through them.

**Example — slide cascade through three actors (H = dragged hero, A = ally, E = enemy, . = empty):**

```
before:  H A E E . . →  drag H east one tile
step 1:  . H E E . .   (A displaced east to H's old tile? NO — A displaced WEST? — actually H takes A's tile and A slides to H's old tile = WEST)

Correct:
before:  H A E E . .
after drag east 1:  . H E E . .   becomes   A H E E . .  (A is shoved into H's old tile)
after drag east 2:  A . H E E .   becomes   A H E E . . wait — H entered A's tile FIRST...
```

Re-stated cleanly: **the displaced occupant moves to the tile the dragging hero JUST LEFT** (one tile behind the drag direction). So if H drags east through A's tile, A ends up at H's previous tile (one west of A's original). Chain through more occupants: each one in turn shoves backward through the previous occupant's old tile.

**Edge clamp**: `ClampToBoard()` is the only hard stop. A drag step that would push an actor off the board edge is refused — the whole drag attempt for that step is canceled.

**Damage rule**: movement never deals damage. Damage flows ONLY through pincer attacks (§1.2) or spell effects (§6). A hero sliding through an enemy is not an attack.

### 1.2 Pincer attacks

A pincer is **two heroes (or two Humanoid actors) in the same row OR same column, with a contiguous line of enemies between them** — no gaps, no allies in the line, at least one enemy.

```
Valid horizontal pincer (3 enemies between two heroes):
. . . . . .
H E E E H .     ← pair (H_left, H_right), opponents [E,E,E]
. . . . . .

Valid vertical pincer (2 enemies):
. H . . . .
. E . . . .
. E . . . .
. H . . . .     ← pair (H_top, H_bottom), opponents [E,E]

INVALID — gap breaks the line:
H E . E H .     ← no pincer (empty tile between enemies)

INVALID — ally in the line:
H E A E H .     ← no pincer (ally A breaks the chain)

INVALID — diagonal (diagonals don't exist for pincers either):
H . .
. E .
. . H
```

#### 1.2.1 Trigger

`PincerAttackManager.Check(Team.Hero, droppedHero)` fires when:
- A hero finishes a drag (`SelectionManager.Drop()`).
- A Teleport skill lands its caster (§5.3).

The scan is **board-wide** — not limited to pairs involving the dropped hero. Every valid pair on the board is queued.

#### 1.2.2 Ordering of multiple pincers

When a single drop generates more than one pair, `OrderPairsByChainsThenNearest()` resolves them in this order:
1. **Chain pairs first**: if pair B's `attacker1` equals pair A's `attacker2`, A then B (the chain). Chains can extend further.
2. **Otherwise nearest-to-drop**: by Manhattan distance from the dropped hero's tile.

This gives the player the visual story "first this pincer, then the chain reaction, then the unrelated one off to the side."

#### 1.2.3 Supporters

Any ally **cardinally adjacent** to one of the pincer's endpoints, with **unbroken line of sight** along the same axis as the pincer, becomes a supporter. Each supporter adds bonus damage via `PincerAttackSupportSequence`.

```
Pincer = H₁ E E E H₂ (horizontal). Supporters check the column above/below each H, and (for horizontal) also extend along the row:

. . S . . .
. . | . . .    S₁ is a supporter of H₁ via vertical adjacency
H₁ E E E H₂ .   ← the pincer pair
. . | . . .
. . . . . .

Each supporter adds one bonus-damage hit per pincer resolution.
```

`FindSupporters(attacker)` is the lookup. Supporters do NOT need to be Humanoid (only the pair attackers do; see §1.3).

#### 1.2.4 No pincer = legal

A drop with no valid pincer is fully legal. The hero just stays where they landed; the player remains in their input window (`InputMode = PlayerTurn`, `selectedState = Idle`) until they drop again or the timeline trigger fires (§2).

### 1.3 Humanoid restriction

**Only actors tagged `ActorTag.Humanoid` can perform pincer attacks** — the geometry can match for a Beast, Mechanical, or other tag but the pair is dropped before resolution. `PincerAttackManager.IsHumanoid(actor)` is the single source of truth:
- Reads `ActorData.Tags.HasFlag(Humanoid)`.
- Falls back to `true` for `Team.Hero` so existing hero data without the explicit tag still pincers.

Humanoid enemies in `EnemyPlanner.PlanStep` **actively seek pincers** — for every candidate move, the planner simulates the move (briefly mutating `enemy.location`, restoring afterward) and checks via `PincerDetector` whether the position would form a Humanoid pincer pair. A pincer-forming move gets +50 to its score, beating positional ties.

### 1.4 A full turn, end-to-end (worked example)

Hero turn opens with Cleric `[C]` at (2,3) and Knight `[K]` at (5,3). Two enemies `[g][g]` sit at (3,3) and (4,3) — already aligned in a row.

```
Step 0: starting position (hero turn — timeline gates input)
  col:  0   1   2   3   4   5
  row 3: .   .  [C] [g] [g] [K]
```

Player taps Knight, drags **west** one tile. Knight enters (4,3) which is already occupied by `[g]` — `ActorMovement.CheckLocationChanged()` detects the overlap and calls `overlappingActor.Move.HandleOverlap(prev)`. The goblin slides **east** to the tile Knight just left (5,3).

```
Step 1: after the slide-displace
  col:  0   1   2   3   4   5
  row 3: .   .  [C] [g] [K] [g]
```

That ends the drag. `SelectionManager.Drop()` now runs:
1. `PincerAttackManager.Check(Team.Hero, Knight)` scans for pairs.
2. Geometry: `[C]` at (2,3), `[K]` at (4,3), single contiguous enemy `[g]` between them at (3,3) — **valid pincer**.
3. `FindSupporters(C)` and `FindSupporters(K)` — none here.
4. Sequence: `PincerAttackSequence` (Cleric+Knight pincer hits goblin at (3,3)), then `DeathSequence` if it dies.
5. After resolve: `PincerAttackManager` drops **Blue mana orbs** at Cleric, Knight (and supporters if any), each bouncing toward the orb line.

Suppose the goblin at (5,3) had its timeline icon inside the Prepare Zone at u=0.85. Knight just damaged it via pincer → `TimelineBarInstance.PushbackOnAttack` fires:
- Damage applied (always).
- Icon pushed left (u drops to ~0.62).
- Goblin enters `Stunned` for ~0.6s.

After the sequence finishes: `OnResolved` fires. `TurnManager` checks `HasQueuedEnemyAfterHero` — if no enemy icon has triggered yet, control returns to the player (still hero turn). If a goblin's icon reached u=1 during the drag, `EndTurnSequence → BeginEnemyTurn(goblin)` runs next.

That's the full beat: **drag → slide cascade → pincer detect → sequence resolve → orb mint → pushback → control returns or enemy turn**. Every other interaction is a variation on this — different shape, different cost, different VFX, same flow.

---

## 2. The Timeline

A horizontal strip across Row 2 of the HUD. Enemy and spell-cast icons "load" left→right over normalized u-coordinates: **u=0 is spawn (left, fresh / not loaded), u=1 is trigger (right, fully loaded — ready to fire)**.

```
u = 0.0                            (1 − ZoneU)              u = 1.0
   ┃══════════════════════════════════════║════════════════════╣
   ┃           "Open Track" (~70%)        ║   "Prepare Zone"   ║   ← (right edge = trigger)
   ┃           icons advance at own       ║   icons crawl at   ║
   ┃           uPerSec (Speed-derived)    ║   shared Zone pace ║
   ┃══════════════════════════════════════║════════════════════╣
                                          ↑
                          "loading" metaphor: the closer to right, the more "loaded"
```

The metaphor is *the icon is loading toward the trigger*; bigger Speed = faster load.

### 2.1 Speed and pace

- Each actor's `uPerSec` is derived from its Speed stat — `uPerSec ≈ Speed × someConst`. Faster actors load sooner.
- Outside the **Prepare Zone** (rightmost `TimelineBarConfig.ZoneU` ≈ 0.25–0.35 of the bar), icons advance at their own `uPerSec`.
- **Inside the Prepare Zone**, every icon crawls at the same fixed rate (`TimelineBarConfig.ZonePaceUPerSec`). This gives the player a uniform coordination window to land an in-Zone pincer regardless of how fast the enemy's stats are.
- **Trigger** fires the moment `Rect.anchoredPosition.x >= rightX - ReachTolerance` while in `Approaching` mode. The icon's *right edge* (its leading edge) crossing the right edge of the bar is the trigger event.

### 2.2 Modes (per icon)

| Mode | Behavior |
|---|---|
| `Queued` | Just spawned; waiting out a queue delay before becoming Approaching. Used when many icons enter at once (staggered visual). |
| `Approaching` | Normal forward motion. Uses `uPerSec` outside the Zone, `ZonePaceUPerSec` inside. |
| `PushedBack` | Just got knocked left by a hero's in-Zone attack (§2.3). Animating to the new u-position. |
| `Stunned` | Post-pushback or post-interrupt freeze. Doesn't advance until the stun timer expires. |
| `Resolving` | Spell-cast icon parked at u=1 while its effect plays out (§2.6). |

The `Frozen` debuff (§8) takes precedence: `TimelineIcon.UpdateApproaching` early-returns when `BuffSystem.Has(Owner, "frozen")` — the icon literally does not advance until the buff expires.

### 2.3 Pushback (the "interrupt by hitting" mechanic)

When a hero damages an enemy whose icon sits inside the Prepare Zone:
- **Damage is ALWAYS applied** — there is no "miss" because of the pushback gate; the gate only governs the position kickback.
- **Pushback amount** lerps with proximity to the trigger (higher u = more push, because the enemy was about to act) and scales with attacker Strength.
- After pushback the icon enters `Stunned` for a duration **inversely scaled by the target's Agility** (high-AGI enemies recover from stun quickly).

The strategy this creates: **form pincers around enemies whose icons are deep in the Zone**. The closer to the trigger the enemy was, the bigger the delay you buy your party.

`TimelineBarInstance.PushbackOnAttack(icon, attacker)` is the gate: returns true if `tag.GetEffectiveTargetU() >= 1 − ZoneU`.

### 2.4 Train-style overlap cascade

Each icon reserves `MinSpatialGap` to its left neighbor. When a new icon spawns OR an existing icon is displaced (pushed back), `ResolveSpatialOverlap()` walks right→left:
- If left neighbor sits within `MinSpatialGap` of the icon to its right, push it further left by the shortfall.
- The push may cascade into the NEXT neighbor (and so on).

The cascade is **order-preserving** — no speed-based reshuffling. The newest icon (or most-recent push target) keeps its rightmost slot; older icons absorb the time-cost. Visually: train cars getting bumped backward, the engine never overtaken.

### 2.5 Auto-skip / Shield

The Shield button (top-right of the timeline, replaces the old Bank button) fast-forwards the timeline to the next enemy trigger. Pressing it:
- Calls `ManaPoolManager.OnBankButtonClicked()` (kept under that name for back-compat; no longer grants mana).
- Applies **Protection** to every hero — 15% incoming-damage reduction for 1 turn.

`TurnManager` also auto-presses this when remaining time until the next enemy trigger is too short for the player to react.

### 2.6 Cast bars (under the timeline)

Spells with `CastTimeSeconds > 0` spawn a **colored shrinking bar** stacked below the timeline (`SpellCastBar`). Bar starts full, shrinks toward the right, resolves at width = 0.
- Color = the spell's **dominant mana cost color** (via `SpellCastBarFactory.ColorForSpell`, palette from `ManaOrbLine.ColorFor`).
- Multiple concurrent casts **stack vertically** (slot-managed, max `MaxConcurrent = 4`); the 5th cast is refused, orbs not spent.
- On resolve: lock `InputMode = None` for `ResolveLockSeconds = 0.30f`, fire effect, restore previous input mode.
- **Caster died mid-cast** → bar destroys itself without resolving.

```
Row 2 ┃══════ Timeline (enemy icons) ══════[🛡]┃
       ↓                                        ↓
       │ ▓▓▓▓▓▓▓▓▓░░░░░░░░  Fireball (red)      │  ← cast slot 0
       │ ▓▓▓▓▓▓░░░░░░░░░░░  Heal     (white)    │  ← cast slot 1
       │ ▓▓░░░░░░░░░░░░░░░  Frost    (blue)     │  ← cast slot 2
       │                                        │  ← slot 3 free, slot 4+ refused
       └────────────────────────────────────────┘
       width shrinks left→right; resolves at 0.
```

### 2.7 Worked example: pushback math

Say a Goblin (AGI 8) has its icon at `u = 0.92` (deep in the Zone — `ZoneU = 0.30`, so the Zone spans `u ∈ [0.70, 1.00]`). A pincer-attacking Knight (STR 18) lands a hit.

```
proximityToTrigger = u − (1 − ZoneU)         = 0.92 − 0.70 = 0.22  (of 0.30 zone width)
proximityFrac      = 0.22 / 0.30             = 0.73                 (73% of the way through)
pushbackU          = baseZonePush × (0.5 + proximityFrac) × strengthMult(STR 18)
                  ≈ 0.18 × 1.23 × 1.20      ≈ 0.265
newU               = max(0.55, 0.92 − 0.265)≈ 0.655                 (just past the Zone left edge)
stunSeconds        = baseStun / agilityMult(AGI 8)
                  ≈ 0.80 / 1.16            ≈ 0.69
```

Net: a single hit at u=0.92 buys ~0.26u of delay + ~0.7s of frozen-position stun. Hits earlier in the Zone (u closer to 0.70) buy less push but the same stun. Hits **outside** the Zone (u < 0.70) buy zero push (damage only). This is the lever the player pulls — engineer pincers around in-Zone enemies.

### 2.8 Spawn rules

| Trigger | Where the icon spawns |
|---|---|
| Battle start | All actor icons spawn at `u = 0` staggered by `Queued` delay |
| Enemy reinforcement (scripted mid-battle) | `u = 0` |
| Spell cast by hero | Cast bar (§2.6); no timeline icon for the caster's spell — the bar IS the icon |
| Enemy charge spell (Phase C, future) | Spawns a separate timeline icon next to the enemy's, advances via cast-time formula |
| Pushback / displacement | Existing icon's u changes; train-cascade resolves neighbors |

---

## 3. Resources

### 3.1 Mana (the orb economy)

**Mana is a horizontal line of colored orbs** (`ManaBank`), not a filling bar. Capacity 12 by default. The whole party shares one bank.

#### 3.1.1 The palette

Magic-style 5-color pie plus a generic:

| Letter | `ManaType` | Role (loose, designer-tunable) |
|---|---|---|
| W | White | heal / shield / order |
| U | Blue  | control / slow / freeze |
| B | Black | drain / sacrifice / decay |
| R | Red   | raw damage / aggression |
| G | Green | regen / growth / mana refund |
| C | Colorless | wild / filler; common crit drop |

**Cost icons** render via `ManaAbilities.CostIcons(ability)` as `(W)(R)(R)` etc.

#### 3.1.2 Harvesting

Orbs are minted from gameplay events:
- **Pincer completion**: each hero attacker contributes 1 orb of their class color (V1: all Blue placeholder). Each supporter also contributes 1.
- **Enemy charge interruption** (Phase C, `US-027`): interrupting a charging enemy cast cancels the attack AND drops one orb of that charge's color. Blocked on enemies not casting yet (`US-026`); the hero-side interrupt mechanic itself is wired (§13.4).
- **Critical hits** (Phase C): may drop a single Colorless orb.
- **Steal / Mug skills**: per-target LCK + 0.5 × AGI roll, success → random-color orb to the bank.

Orbs **drop visually** as bouncing UI sprites (`ManaOrbInstance`) from the source actor to the first empty slot in the line. Each commits `Bank.Add(color, 1)` on landing.

#### 3.1.3 Spending

`Bank.Spend(recipe)` either:
- Removes the exact colors the recipe demands, or
- (When `Bank.AllowAnyColor = true`, a dev/cheat flag) removes from the leftmost orbs regardless of color, as long as the total count is enough.

The flag exists so we can test mechanics before locking color identity per spell.

#### 3.1.4 Bank-full rules (overflow)

The bank holds **12 orbs total**. When a mint would push the count past capacity:

| Capacity state | Mint behavior |
|---|---|
| `Count + n ≤ 12` | All orbs land normally. |
| `Count == 12` | New orb bounces toward the line, fades out before landing (visual "you're full"), bank unchanged. |
| Partial overflow (`Count + n > 12`) | Lands the first `12 − Count` orbs; remainder fades out. |

The fade-out is intentional feedback — players need to feel they're "leaving value on the table" when they over-mint. Design lever: keeping the bank size at 12 means **a full party of mages can't infinitely stockpile**; they have to spend to make room.

Future tuning knob: a higher-rarity Mage Robe might raise the cap for that battle (`BattleStartManaOrbs` and a parallel `BankCapacityBonus`).

#### 3.1.5 Spend ordering (which orb leaves first)

When `Bank.Spend(recipe)` removes colored orbs, the **leftmost orb of each demanded color** is consumed. Visually the surviving orbs shift left to close the gap so the bank always renders left-packed. This means a player can predict cast cost by reading left-to-right.

`AllowAnyColor` (dev flag) consumes purely leftmost regardless of color — useful for early-stage testing before colors lock in.

#### 3.1.6 Conversion / pressure valve (design intent, not built)

A future alchemy step can trade N orbs of one color toward another, or treat Colorless as a wildcard at a cost. Keeps colors meaningful while giving the player an escape valve.

#### 3.1.7 At-a-glance mint cadence

| Event | Orbs minted | Color source |
|---|---|---|
| Hero completes a 2-hero pincer (no supporters, 1 enemy in line) | 2 | each hero's class color (V1: Blue placeholder) |
| Hero pincer with 2 supporters | 4 | 2 attackers + 2 supporters |
| Hero pincer with 1 supporter, 3 enemies in line | 3 | 2 attackers + 1 supporter |
| Steal/Mug skill, 3 adjacent enemies, 2 succeed | 2 | random per roll |
| Critical hit (Phase C) | 1 | Colorless |
| Interrupt enemy cast (Phase C) | 1 | matches enemy charge color |
| Battle start (Mage Robe equipped, count=1) | 2 | random (per `BattleStartManaOrbs`) |

The math here is the **design lever for spell tuning**: a 2-orb spell should feel like 1 pincer's worth; a 4-orb spell should require a multi-supporter or multi-pincer chain.

### 3.2 HP and stats

`ActorStats` (extends `BaseStats`):

| Field | Driving | Reads in |
|---|---|---|
| `HP` (float, 0..MaxHP) | live | damage / heal / death |
| `MaxHP` (float) | derived from VIT × growth | `Formulas.Health(actor)` |
| `Strength` (STR) | physical damage | `Formulas.Offense`, pincer attack |
| `Vitality` (VIT) | HP + physical defense | `Formulas.Defense`, MaxHP scaling |
| `Agility` (AGI) | dodge / pushback recovery | crit/miss roll; Stunned duration |
| `Speed` (SPD) | timeline `uPerSec` | `TimelineIcon.uPerSec` |
| `Stamina` (STA) | (future: AP regen) | reserved |
| `Intelligence` (INT) | magic damage | `Formulas.MagicOffense`; cast time scaling |
| `Wisdom` (WIS) | magic defense | `Formulas.MagicDefense`; cast time scaling |
| `Luck` (LCK) | crit + steal + Clutch interrupt | many roll-based mechanics |

Damage and heal mutate `HP` directly via the dispatcher (`SpellEffectDispatcher.ApplyDamage` / `ApplyHeal`); each posts a combat-text popup at the actor's world position.

A spell that **deals** damage with a non-zero base always does at least 1 (the only way to do 0 is when `ResistanceMultiplier == 0` — true immunity).

#### 3.2.1 Stat-to-effect cheat sheet

When designing a new class or enemy, lean the stats toward the feel you want:

| Want this feel | Bump these stats | Avoid bumping |
|---|---|---|
| Tank / bruiser | VIT, STR | INT |
| Glass cannon mage | INT, WIS, LCK | VIT |
| Fast skirmisher | AGI, SPD, LCK | (anything heavy) |
| Slow heavy hitter | STR, VIT | SPD, AGI |
| Roguish thief | LCK, AGI | INT, VIT |
| Mystic support | WIS, INT | STR |

### 3.3 Elemental resistance

`ActorData.Resistances : Dictionary<DamageType, float>` — multipliers (`1.0` neutral, `0.5` resistant, `2.0` weak, `0` immune). Per-class data files seed entries; missing entries default to `1.0`.

`DamageType` enum: `Physical / Fire / Ice / Lightning / Poison / Holy / Dark / Arcane`.

The dispatcher composes the final damage as:

```
final = base
      × ActorData.ResistanceMultiplier(spell.DamageType)
      × BuffSystem.GetIncomingDamageMultiplier(target)   // Protection etc.
      × (Lightning && target has Wet ? Buffs.LightningWhenWetMultiplier : 1)
```

Then rounds + floors-at-1 (per §3.2).

---

## 4. The AbilityBar

Row 13 of the HUD. **6 slots.** Each slot holds one `ManaAbility` (which is one of three kinds).

> **Invariant — one `ManaAbility` per spell.** For Spell-kind abilities, `AbilityBar.ResolveSpell` looks the `SpellDefinition` up by *ability reference* and returns the first match in `SpellLibrary.All`. A `ManaAbility` must therefore back exactly one `SpellDefinition` — reusing a single instance across spells silently resolves to whichever is declared first (this caused the Heal→Sleep bug). Every entry in the §7 catalog has its own dedicated ability.

### 4.1 The three kinds

| Kind | Cost | Cast time | Frame color |
|---|---|---|---|
| **Skill** | free, "costs the player's turn" (timeline auto-advances after resolve); locked by a per-skill cooldown after use | instant | green |
| **Spell** | colored mana orbs from `ManaBank` | shrinking cast bar | cool blue |
| **Item** | per-slot stack charge | instant | warm leather |

Slot kind is set by the constructor used on `ManaAbility`. Cost icons label: Skills show `Free`, Items show `current/MaxStackSize`, Spells show `(W)(R)…`.

### 4.1.1 Skill cooldowns

A Skill is free to use but, once cast, is **locked for `ManaAbility.CooldownTurns` turn-cycles**. Current values: **Steal 3, Mug 2, Teleport 3** (`Data/ManaAbilities.cs`). The remaining countdown is tracked **per hero** by `SkillCooldownManager` (a `BuffSystem`-style static dictionary) — *not* on the `ManaAbility`, because Skill ability instances are shared statics across multiple class loadouts.

- **Set:** `AbilityBar.HandleSkill` (and the Teleport flow) call `SkillCooldownManager.Begin(hero, skill)` when the skill actually fires.
- **Tick:** `TurnManager.BeginHeroWindow` calls `SkillCooldownManager.TickAll()` once per turn-cycle (each time the player regains control); a skill reactivates when its counter reaches 0. `TurnManager.Initialize` clears all cooldowns at battle start.
- **UI:** while on cooldown the bar slot **fades out** and its cost label shows the **turns-remaining number**; the button is non-interactable and `HandleSkill` refuses an early click. (`AbilityBar.Refresh`.)
- **Debug:** Debug Window → *Lock Skill CDs* / *Tick CD (1 turn)* (`DebugManager.Demo_LockSkillCooldowns` / `Demo_TickSkillCooldowns`).

### 4.2 Per-hero loadouts

The bar **follows the selected hero**. `HeroLoadouts.For(characterClass)` returns the 6-entry list for the active hero; `AbilityBar.Update` polls the selection and rebinds the slots when the class changes. When no hero is selected, slots hide.

Add per-class entries to `HeroLoadouts.perClass` via `HeroLoadouts.Set(class, loadout)`. Classes without an explicit override fall through to `ManaAbilities.Slots`.

Seeded loadouts:

| Class | Slots |
|---|---|
| Cleric | Heal, Heal, Frost, NewPotion(3) |
| Paladin | Heal, Fireball, NewPotion(3) |
| Barbarian | Fireball, Bolt, NewPotion(3) |
| Alchemist | Frost, NewPotion(5), Steal, Heal, NewPotion(5) |
| Assassain | Steal, Mug, Bolt, NewPotion(3) |
| GreenNinja | Teleport, Steal, Fireball, NewPotion(3) |
| RedNinja | Teleport, Mug, Bolt, NewPotion(3) |

### 4.3 Item stacks (per-slot)

Items are **per-slot instances** — each call to `ManaAbilities.NewPotion(stackSize)` mints a new `ManaAbility` with its own `Charges` and `MaxStackSize`. Two 5-stack slots = 10 total uses split across two independent bars. Buying another stack at the vendor fills the next free slot.

`TryConsumeCharge()` decrements; `Refill(amount)` clamps to `MaxStackSize`.

### 4.4 Click flow per kind

**Item**: `TryConsumeCharge` → log usage. Instant.

**Skill**: `TargetingMode.Begin` → on confirm, dispatch (or run the Skill's bespoke flow), then call `ManaPoolManager.OnBankButtonClicked()` to advance the timeline ("costs a turn"). Free.

**Spell**:
1. `Bank.CanAfford(cost)` precheck — no deduction yet.
2. `TargetingMode.Begin` → user picks (or auto-resolves for Mode=Auto).
3. On **confirm**, refuse if `SpellCastBar.IsAtCapacity`; otherwise `Bank.Spend(cost)` (orbs deducted), spawn the colored cast bar, on resolve dispatch per target.
4. On **cancel**, zero orbs spent.

This means **mana is consumed AT CAST START** (after target chosen, before bar), per the project rule "MP consumed upfront; interruption refunds nothing." Cancel during targeting is free.

### 4.5 Slot visual states

Each slot in the AbilityBar can be in one of these visual states, driven by `AbilityBar.RefreshSlot(int i)`:

| State | Visual | When |
|---|---|---|
| **Empty** | grey dashed outline | no `ManaAbility` assigned (or slot index ≥ loadout length) |
| **Ready** | full color frame, icon at 100% opacity | affordable + not on cooldown |
| **Unaffordable** | full color frame, icon at 40% opacity, cost text red | spell — `Bank.CanAfford(cost) == false` |
| **Out-of-charges** | item frame, "0/N" overlay, icon at 30% opacity, slot is non-interactive | item with `Charges == 0` |
| **Selected / targeting** | thick yellow outline + slight scale-up | `TargetingMode.IsActive && AbilityBar.SelectedSlot == i` |
| **Cooldown** (future) | greyscale icon + radial sweep | reserved for skills with reuse limits |
| **Disabled (Silenced)** | red diagonal stripe overlay on Spell slots | caster has `silenced` debuff (gameplay hook TODO) |

**Hover/long-press** (future): show a tooltip with name, cost, target shape preview, base damage/heal.

### 4.6 Cancel paths

A targeting flow can be aborted by:
- Tapping the **selected slot a second time** (cancel from the bar itself).
- Tapping the **Cancel button** in the `TargetPickerOverlay`.
- Pressing **Escape** (debug keyboard binding; mobile gets a hardware-back equivalent).
- `TargetPickerOverlay.OnDestroy` safety net — destroying the overlay (e.g., a scene transition) fires `OnCancelled` so `TargetingMode.IsActive` never gets stuck true (this was an actual bug — see §17.1 #5).

---

## 5. Targeting

Three orthogonal enums:

```
TargetShape  : Self / SingleActor / SingleTile / Square / Diamond / Cross / Plus / Row / Column / AllEnemies / AllAllies
TargetMode   : Auto / PickActor / PickTile
TargetFilter : Any / EnemyOnly / AllyOnly / EmptyOnly
```

Plus `int Radius` for shapes that use it.

### 5.1 Resolver

`Services/TargetShapeResolver.Resolve(anchor, shape, radius, w, h)` → `List<Vector2Int>` (clipped to board).
`CollectActors(tiles, shape, filter, caster)` → `List<ActorInstance>` filtered.

**Shape visualizations** (★ = anchor, █ = affected tile, · = unaffected):

```
Self / SingleActor / SingleTile        Square(r=1)                Diamond(r=2)
   · · · · · ·                          · · · · · ·                 · · █ · · ·
   · · ★ · · ·                          · █ █ █ · ·                 · █ █ █ · ·
   · · · · · ·                          · █ ★ █ · ·                 █ █ ★ █ █ ·
   · · · · · ·                          · █ █ █ · ·                 · █ █ █ · ·
                                        · · · · · ·                 · · █ · · ·

Cross(r=1)  (center+4 cardinals)       Cross(r=2)                 Plus (entire row ∪ column)
   · · · · · ·                          · · █ · · ·                 · · █ · · ·
   · · █ · · ·                          · · █ · · ·                 · · █ · · ·
   · █ ★ █ · ·                          █ █ ★ █ █ ·                 █ █ ★ █ █ █  ← entire row
   · · █ · · ·                          · · █ · · ·                 · · █ · · ·
   · · · · · ·                          · · █ · · ·                 · · █ · · ·     entire column

Row(anchor=(2,1))                      Column(anchor=(2,1))
   · · · · · ·                          · · █ · · ·
   █ █ ★ █ █ █                          · · ★ · · ·
   · · · · · ·                          · · █ · · ·
   · · · · · ·                          · · █ · · ·

AllEnemies / AllAllies                  (no anchor; pulled from g.Actors.Enemies / Heroes)
```

Shape resolver math:

| Shape | Tiles covered (anchor = `(ax, ay)`) |
|---|---|
| `Self`, `SingleActor`, `SingleTile` | just the anchor |
| `Square(r)` | Chebyshev ≤ r — `max(|dx|, |dy|) ≤ r` |
| `Diamond(r)` | Manhattan ≤ r — `|dx| + |dy| ≤ r` |
| `Cross(r)` | anchor + r tiles in each cardinal arm (1 + 4r tiles) |
| `Plus` | entire row of anchor ∪ entire column of anchor |
| `Row` | entire row of anchor |
| `Column` | entire column of anchor |
| `AllEnemies` / `AllAllies` | no tile shape; actor collection pulls from `g.Actors.Enemies` / `g.Actors.Heroes` directly |

### 5.2 Picker flow

`TargetingMode.Begin(spell, caster, onConfirm, onCancel)`:
- `Auto` → resolve immediately (Self → caster; AllEnemies → all enemies; etc.). If `0` actors resolve, call `onCancel`.
- `PickActor` → spawn a **gold pulsing ring** above every actor that matches the filter (via `WorldFollow`). On click, anchor = picked actor's tile; resolver collects within the shape. AOE shapes show a hover preview (`TargetShapePreview`) of the affected tiles.
- `PickTile` → spawn an invisible-but-raycastable grid of `TilePickerCell` cells (one per board tile, pinned via `WorldFollowFromTile`). Pointer-enter fires `TargetShapePreview.ShowAt(anchor, shape, radius)` — colored tile highlights repaint live. Click confirms.

**All paths**: ESC, right-click, or click on the translucent veil = cancel (no orbs spent).

**Stale state guard**: `Begin` first calls `DismissAnyActive()` — any orphan overlay is force-cancelled before the new session starts. `TargetPickerOverlay.OnDestroy` also fires its cancel callback if the GO is destroyed externally. Together these prevent `IsActive` from sticking.

### 5.3 Special targeting flows

- **Teleport** (`SpellDefinition.IsTeleport = true`) bypasses `SpellEffectDispatcher`. After tile pick: validate empty → set `caster.location` + `transform.position` → `PincerAttackManager.Check(Team.Hero, caster)` to fire any incidental pincer → advance the timeline.

### 5.4 Targeting session lifecycle

```
       ┌──────────────────────────────────────────────────────┐
       │  AbilityBar.HandleSpell / HandleSkill                │
       │  (precheck: affordable, not silenced, not at-cap)    │
       └────────────────────┬─────────────────────────────────┘
                            ▼
                    DismissAnyActive()          ← clear any orphan picker
                            │
                            ▼
       TargetingMode.Begin(spell, caster, onConfirm, onCancel)
                            │
            ┌───────────────┼────────────────┐
            ▼               ▼                ▼
         (Auto)         (PickActor)      (PickTile)
            │               │                │
            │       spawn gold rings    spawn cell grid +
            │       per matching actor  hover-preview overlay
            │               │                │
            │               │                │
            ▼               ▼                ▼
       resolve to       wait click       wait click +
       actor list       on actor         pointer-enter
            │               │                │
            └───────┬───────┴────────────────┘
                    ▼
            anchor + actor list determined
                    │
              ┌─────┴─────┐
              ▼           ▼
         onConfirm    onCancel
        (dispatch)   (no spend)
              │           │
              └─────┬─────┘
                    ▼
              IsActive = false
              overlay destroyed
                    │
                    ▼
       (cast bar spawns, or skill resolves, or nothing)
```

**Invariants:**
- `IsActive` is true for the entire window between `Begin` and `onConfirm`/`onCancel`.
- Exactly one of `onConfirm` / `onCancel` is called per `Begin` (the `OnDestroy` safety net guarantees this — if neither fired, the cancel handler runs).
- During an active session, all other AbilityBar clicks are ignored at the precheck level (so the player can't fire two spells overlapping).

---

## 6. The Spell Dispatcher

`SpellEffectDispatcher.Cast(spell, caster, target)` runs a coroutine on `VisualEffectManager` (or `ManaPoolManager` as fallback). Stages:

1. **Cast flash** at caster (`spell.CastVfxName`).
2. **Projectile** (if `Motion != None && ProjectileVfxName != null`) — flies along the motion curve.
3. **Impact** at target's current position (`spell.ImpactVfxName`).
4. **Linger** parented to the target's transform (`spell.LingerVfxName`) — persists with the actor.
5. **Validation**: skip remaining steps if target is no longer playing or HP ≤ 0.
6. **Cleanse** debuffs if `spell.RemovesDebuffs` (Antidote).
7. **Fire × Wet interaction**: a Fire-type hit on a Wet target strips Wet first ("Steam!" popup) before damage.
8. **Steal roll** if `spell.StealsMana`: chance = `clamp01((LCK + 0.5 × AGI) / 50)`, success → one random-color orb to the bank + "Steal! +X" popup.
9. **Lightning blindness roll**: Lightning-type hits roll 30% to apply `Buffs.Blinded`.
10. **Damage** (`ApplyDamage`) — see §3.3. Posts a red number combat-text popup. Notifies `BuffSystem.OnDamaged` (breaks Sleep-like buffs).
11. **Heal** (`ApplyHeal`) — posts green `+N`.
12. **Debuff** apply via `BuffSystem.Apply(target, buff)`.

### 6.1 Projectile motions

`Utilities/ProjectileMotionEval.Evaluate(motion, from, to, target, t)`:

| `ProjectileMotion` | Curve |
|---|---|
| `None` | resolves at caster (Heal, Scan, Antidote — no projectile) |
| `Straight` | linear lerp |
| `Bezier` | quadratic with vertical apex |
| `Homing` | ease-toward-live-target (target may move during flight) |
| `Spiral` | corkscrew, tightens into target |
| `Twist` | gentle weave (Fireball) |
| `Strike` | lateral → top-down drop (Lightning crashes from above) |

### 6.2 VFX pipeline

VFX live as prefabs registered in `VisualEffectLibrary` (Addressables-backed) and played via `VisualEffectManager.Spawn` / `SpawnInstance`. Per-spell custom prefabs can be generated via `Tools/VFX/Author <Name>` editor menus in `VfxPrefabAuthor.cs` — same procedural authoring pattern as the spell icons. **Looping** VFX assets (`IsLooping = true`) must NOT be used as the `CastVfx` slot or they stick on the caster permanently; they belong on `Linger`.

### 6.3 Edge cases the dispatcher handles

The 12-stage routine has to survive an actor list that mutates mid-flight. Specific safety nets:

| Edge case | Handler |
|---|---|
| Target dies before projectile lands | After-projectile validation: `if (target == null \|\| !target.IsPlaying \|\| target.Stats.HP <= 0) skip damage+linger`. Projectile still plays; linger does not parent to a corpse. |
| Target moves mid-flight (Homing) | `ProjectileMotionEval.Evaluate(Homing, …, target, t)` reads `target.transform.position` each tick; impact spawns at the moved position. |
| Caster dies mid-cast | `SpellCastBar` removed from `Active` registry; coroutine continues to resolve (MP was already spent — refund-on-death is a design call, not yet wired). |
| `g.Actors.All` is null (scene transition during cast) | `TargetShapeResolver` null-guards `actors` and returns an empty list — dispatcher iterates nothing. |
| AOE shape includes the caster (e.g. Cross(r=1) on self) | `TargetFilter.EnemyOnly` strips caster + allies; `AllyOnly` keeps caster (heal-self is valid); `Any` keeps everyone including caster. |
| Spell hits the same target twice (overlapping shapes) | Each tile resolves once. AllEnemies + AOE never double-stack because the actor collection dedupes by `ActorInstance` reference. |
| Wet + Fire on the same hit | Wet is stripped BEFORE damage (stage 7), so the lightning ×1.5 multiplier on stage 10 does NOT see a Wet target the same hit applied. Order matters. |
| Lightning on a Wet target | Wet stays (it's not stripped by Lightning); the ×1.5 multiplier applies and the 30% Blind roll fires after damage. |
| Steal on a target with no orbs to give | Roll succeeds → bank still gains a random-color orb (Steal is "from the world", not the target's MP). |
| Item-backed spell (`OnUseSpellName`) interrupted mid-cast | (Phase C) interrupt path should refund the consumed item; Fail outcome currently consumes both MP and item.|
| `SpellCastBar.IsAtCapacity` reached | New spell refused at click time (`AbilityBar.HandleSpell`); orbs are not spent. Player sees "Too many spells in flight" toast.|

---

## 7. The Spell Catalog

All entries in `Data/SpellLibrary.cs`. Cost references `ManaAbilities.<Name>`.

| Spell | Cost | Shape / Mode / Filter | Motion | Effect | Strategic role |
|---|---|---|---|---|---|
| **Heal** | (W) | SingleActor / PickActor / AllyOnly | None | +25 HP | Spot-heal; cheap to repeat |
| **Mass Heal** | (W) | AllAllies / Auto / AllyOnly | None | +12 HP everyone | Emergency top-up; bigger gross but smaller per-target |
| **Antidote** | (W) | SingleActor / PickActor / AllyOnly | None | strips ALL debuffs | Cleanse — pair after a debuff-heavy enemy turn |
| **Scan** | (W) | SingleActor / PickActor / EnemyOnly | Straight | reveals stats (TODO) | Recon — info, not damage |
| **Fire (Fireball)** | (R)(R) | SingleActor / PickActor / EnemyOnly | Twist | 18 Fire dmg + `burning` | High burst single target; DOT continues after |
| **Ice (Frost)** | (U)(U) | Square(r=1) / PickTile / EnemyOnly | Bezier | 10 Ice dmg + `frozen` | Crowd-control AOE; frozen halts timeline (§8.4) |
| **Lightning (Bolt)** | (R)(R)(U) | Row / PickActor / EnemyOnly | Strike | 14 Lightning dmg (×1.5 if target Wet) + 30% chance `blinded` | Row clear; pair after Frost expires → Wet for combo |
| **Poison** | (U)(U) | Cross(r=1) / PickTile / EnemyOnly | Bezier | 6 Poison dmg + `poisoned` | Low burst, big tail (tick damage) |
| **Sleep** | (W) | SingleActor / PickActor / EnemyOnly | Homing | `sleep` (breaks on damage / move) | Hard CC on a priority target; do NOT also attack them |
| **Slow** | (U)(U) | Row / PickActor / EnemyOnly | Bezier | `slowed` (TODO: timeline-speed mult) | Row-wide tempo control; buys time across a rank |
| **Silence** | (W) | SingleActor / PickActor / EnemyOnly | Straight | `silenced` (TODO: cast-block) | Lock down enemy casters before they fire |
| **Meteor** | (R)(R) | Diamond(r=2) / PickTile / EnemyOnly | Strike | 22 Fire dmg + `burning` | Massive AOE; the wipe-the-back-row option |
| **ShockWave** | (R)(R)(U) | Column / PickActor / EnemyOnly | Strike | 10 Lightning dmg | Vertical cleave; pair with Frost columns |
| **CrossHit** | (R)(R)(U) | Plus / PickTile / EnemyOnly | Strike | 8 Lightning dmg (board-wide +) | Hits ENTIRE row + column; spreads thin but covers ground |
| **Steal** (Skill) | Free | Cross(r=1) / Auto / EnemyOnly | Homing | LCK+AGI roll per adjacent enemy → orb | Resource grab; no damage, costs a turn |
| **Mug** (Skill) | Free | Cross(r=1) / Auto / EnemyOnly | Straight | Same roll PLUS 10 Physical dmg each | Steal + attack; the rogue's signature |
| **Teleport** (Skill) | Free | SingleTile / PickTile / EmptyOnly | None | Relocate caster; auto-resolves any new pincer | Repositioning into a flank; ninja mobility |

---

## 8. Buffs and Debuffs

`Buff` (definition) + `BuffInstance` (runtime per actor) + `BuffSystem` (central registry).

### 8.1 Catalog

| Id | Kind | Duration | Knobs | On expire |
|---|---|---|---|---|
| `protection` | Buff | 1 Turn | DR 15% | — |
| `burning` | Debuff | 5 Ticks | 4 dmg/tick | → `warm` |
| `frozen` | Debuff | 1 Turn | immobile (timeline halts) | → `wet` |
| `wet` | Debuff | 6 Ticks | (multiplier hook in formula) | — |
| `warm` | Debuff | 3 Ticks | (sleep-bonus hook in formula) | — |
| `sleep` | Debuff | 3 Turns | immobile, breaks on damage / move | — |
| `poisoned` | Debuff | 6 Ticks | 3 dmg/tick | — |
| `slowed` | Debuff | 2 Turns | timeline icon advances ×0.5 (US-011) | — |
| `silenced` | Debuff | 2 Turns | Spell clicks refused + slots blocked (US-012) | — |
| `blinded` | Debuff | 2 Turns | attacker accuracy ×0.5 (US-013) | — |

### 8.2 Cross-effect multipliers

Constants in `Data/Buffs.cs`:
- `LightningWhenWetMultiplier = 1.5f` — Lightning damage × 1.5 on a Wet target. Wired in `ApplyDamage`.
- `SleepWhenWarmMultiplier   = 1.5f` — Sleep on a Warm target lasts × 1.5 longer (US-014, wired in `SpellEffectDispatcher` debuff-apply). Applies to **duration** rather than a success roll, since Sleep has no success-chance roll today — revisit if one is added.

Also: **Fire on Wet → strips Wet** (steam) before damage applies. Wired in dispatcher.

#### 8.2.1 Interaction matrix

How active debuffs combine and react when struck by a damage type. Read left-column buff is on the target → top-row damage hits → cell describes the outcome.

| Already on target ↓ \ New hit → | **Fire** | **Ice** | **Lightning** | **Physical / Other** |
|---|---|---|---|---|
| (clean) | apply Burning | apply Frozen + Wet on expire | normal damage; 30% Blinded roll | normal damage |
| **Burning** | refresh Burning; damage | refresh Burning still ticks; cold doesn't strip it | normal | normal; existing Burning continues |
| **Frozen** | strip Frozen (Wet applies on expire normally); damage; Burning rolls | refresh Frozen; damage | normal damage; Frozen still halts timeline | damage; Frozen continues |
| **Wet** | **strip Wet ("Steam!"); damage; Burning rolls** | refresh Wet; damage | **×1.5 damage** (Lightning + Wet); Wet still ticks | normal |
| **Warm** | apply Burning (Warm doesn't block); damage | apply Frozen (cold beats Warm); damage | normal | normal |
| **Sleep** | apply Burning; damage **breaks Sleep** | damage breaks Sleep + applies Frozen | damage breaks Sleep | damage breaks Sleep |
| **Poisoned** | apply Burning (stacks tick separately); damage | apply Frozen | normal | normal |
| **Slowed** | normal Burning | refresh Frozen | normal | normal |
| **Blinded** | normal | normal | normal | normal |
| **Protection** (on heroes) | DR×0.85 then proceed | DR×0.85 then proceed | DR×0.85 then proceed | DR×0.85 then proceed |

#### 8.2.2 Expire chains

When a debuff times out, an optional follow-up debuff is applied to the target via `Buff.OnExpireApplyId`:

```
Burning  ──(expires)──> Warm     (post-fire warmth; raises Sleep success)
Frozen   ──(expires)──> Wet      (ice melts to water; raises Lightning damage)
```

Other debuffs expire silently. Designer can add more chains by editing `OnExpireApplyId` in `Data/Buffs.cs`.

#### 8.2.3 Damage-breaks rule

`Buff.BreaksOnDamage` (currently set on `Sleep` only): when the target takes any damage, this buff is force-removed before damage finalizes. Implemented via `BuffSystem.OnDamaged(target)` called by `ApplyDamage`.

`Buff.BreaksOnMove` (also Sleep): force-removed when the bearer is displaced. Wired (US-015) — `ActorMovement.HandleOverlap` calls `BuffSystem.OnMoved(instance)` at the displacement commit, so sliding a sleeping actor wakes it.

### 8.3 Ticking

`Managers/BuffTickManager` is auto-attached on Game scene start. Every `1.0s` of timeline-advancing time:
- For each playing actor, walks tick-unit buffs.
- Applies `DamagePerTick` to HP.
- Decrements duration; on expire, applies `OnExpireApplyId` chain (Fire→Warm, Frozen→Wet).

Turn-unit buffs decrement via `BuffSystem.TickTurn(actor)`, called at the **END** of the bearer's turn (US-016, wired into `TurnManager.NextTurn`). End-of-turn (not turn-start) so a "2 Turns" debuff affects the bearer for 2 of its *own* turns — ticking before the actor acts would burn one turn to off-by-one. `NextTurn` is the single turn boundary: when an enemy turn just ended it ticks that enemy (`lastEnemy`); when the hero window just ended it ticks every playing hero once (heroes share one free-form window, so there is no per-hero turn to tick — this is the closest boundary and is fine until enemy-cast debuffs on heroes exist, US-026). Decision confirmed via the Legion panel (2026-05-31).

### 8.4 Immobility hook

`BuffSystem.IsImmobile(actor)` returns true if any active buff has `Immobile = true` (Frozen, Sleep).

- **Enemy AI**: `EnemyPlanner.PlanStep` returns the enemy's current location early when immobile (no move).
- **Timeline**: `TimelineIcon.UpdateApproaching` early-returns when `BuffSystem.Has(Owner, "frozen")` — the icon literally stops advancing until the buff expires.

### 8.5 Debuff icon bar (per actor)

`Canvas/DebuffIconBar` is attached to every actor via `DebuffIconBarFactory.EnsureAttached(actor)` (idempotent; safe for mid-battle reinforcements).

- **3 cells**, upper-right of the actor's tile (world offset `(0.30, 0.30)`).
- Each cell: **disk (colored by buff id) + letter + radial yellow ring** that ticks down clockwise as the buff's remaining duration drops. When the ring empties, the cell hides.
- Overflow > 3 buffs **cycles** the visible window every 1.5s.
- Color + letter centralised in `DebuffIconBar.ColorFor / LetterFor` — add new buffs by adding an entry here.

### 8.6 Stacking rules (reapply behavior)

When a debuff with id X is applied to an actor that **already** has X active, what happens?

| Behavior | Rule | Examples |
|---|---|---|
| **Refresh** (default) | Replace the existing instance: duration resets to full, knobs unchanged. | Burning, Frozen, Wet, Poisoned, Sleep, Slowed, Silenced, Blinded — most debuffs |
| **Stack** (planned) | Keep both instances; ticks/effects double. | (Future: a "Deep Poison" upgrade that stacks) |
| **Ignore** | Do nothing — new application silently fails. | (Future: temporary immunity windows) |
| **Upgrade** | Replace with a stronger variant if the application source is stronger (high-INT caster). | (Future: tiered Poison) |

V1 uses **Refresh** uniformly via `BuffSystem.Apply(actor, buff)`: if the target has X, remove + re-add fresh. This keeps math simple and matches player intuition ("I cast Frost again, the freeze just got longer").

### 8.7 Buff lifecycle state machine

```
       ┌──────────────────────────────────────────────────┐
       │ Application source:                              │
       │  • Spell impact (SpellEffectDispatcher)         │
       │  • Buff expire-chain (Burning → Warm)           │
       │  • Item use / passive (TODO)                    │
       └────────────────────┬─────────────────────────────┘
                            ▼
                  BuffSystem.Apply(target, buff)
                            │
                  ┌─────────┴──────────┐
                  ▼                    ▼
            target has X?         target free of X
            (refresh policy)            │
                  │                    │
                  ▼                    │
            remove existing            │
                  │                    │
                  └─────────┬──────────┘
                            ▼
                  add fresh BuffInstance{remaining=full}
                  fire DebuffIconBar.RefreshSlots
                            │
                            ▼
              ┌─────────────┴──────────────┐
              ▼                            ▼
       (tick units)                  (turn units)
       BuffTickManager every 1.0s    on target's turn boundary
       advances remaining             decrement remaining
              │                            │
              │  also: if BreaksOnDamage  │
              │  and target hit → remove  │
              │                            │
              └─────────────┬──────────────┘
                            ▼
                       remaining ≤ 0
                            │
                            ▼
              BuffSystem.Remove(target, id)
                            │
                            ▼
              if (buff.OnExpireApplyId != null)
                  Apply(target, that buff)   ← chain (Burning→Warm, Frozen→Wet)
                            │
                            ▼
                        (done)
```

### 8.8 Mass-cleanse rules

- **Antidote spell** (`removesDebuffs: true`): on impact, removes ALL debuffs from the target (no expire-chain triggers). Buffs (Protection) are kept.
- **Death**: when an actor dies, all buffs are dropped silently. Expire chains do NOT trigger.
- **End-of-battle**: by current design, all buffs clear when PostBattleScreen loads (decision in §29.3 — locked: clear-on-end).

---

## 9. The HUD (15-row layout)

The full HUD layout lives in `Utilities/HudLayout.cs` (constants `Row{N}Y_FromTop/FromBot`). `GameBuilder.cs` reads them for scene-time placement; runtime factories (`ShieldButtonFactory`, `ManaOrbLineFactory`) read the same constants.

```
┌─────────────────────────────────────┐  ← y = canvas top
│ Row 1   Clock                💰0025│  ← money (CoinCounter, right-aligned)
├─────────────────────────────────────┤
│ Row 2   ──── Timeline ──── 🛡│      │  ← timeline strip + Shield button (right edge of Row 2)
├─────────────────────────────────────┤
│ Row 3       ActionTitle banner      │  ← e.g. "Cleric: Heal"
├─────────────────────────────────────┤
│ Row 4    [hero icons load left→right; cast bars stack under Row 2]
│ Row 5         ┌─────────────────┐  │
│ Row 6         │                  │  │
│ Row 7         │   6 × 8 Board    │  │  ← rows 4–12; world-space camera viewport
│ Row 8         │  (heroes, enemies)│  │
│ Row 9         │                  │  │
│ Row 10        │                  │  │
│ Row 11        │                  │  │
│ Row 12        └─────────────────┘  │
├─────────────────────────────────────┤
│ Row 13  [Heal][Fire][Frost][Bolt][Pot][—]   ← AbilityBar (6 slots)
├─────────────────────────────────────┤
│ Row 14   ●●●●●●●○○○○○                       ← 12 mana orb slots
├─────────────────────────────────────┤
│ Row 15  ┌──────────────────────────┐│
│         │ ActorPanel: [Stats][Equip][Lore] tabs ││  ← contextual: selected hero or scanned enemy
│         └──────────────────────────┘│
└─────────────────────────────────────┘  ← y = canvas bottom
```

| Row | Content | Spawned by |
|---|---|---|
| 1 | Money (CoinCounter, top-right) + optional Clock | `GameBuilder` |
| 2 | Timeline bar + Shield button at right edge | `GameBuilder` + `ShieldButtonFactory` (runtime) |
| 3 | ActionTitle banner | `GameBuilder` |
| 4–12 | 6×8 Board (world-space, camera-framed) | `GameBuilder` (BoardInstance) + ActorFactory (runtime spawn) |
| 13 | 6-slot AbilityBar | `AbilityBarFactory` (runtime, parented to `Canvas/AbilityButtonContainer` placed by `GameBuilder`) |
| 14 | 12-slot mana orb belt — screen-wide "tray", sits just **above** the ability bar | `ManaOrbLineFactory` (runtime) |
| 15 | `ActorPanel` — tabbed **Stats / Equipment / Lore** (contextual: selected hero or scanned enemy). Hero ◀▶ cycle arrows in the tab bar. | root in `GameBuilder`; tab UI built at runtime by `ActorPanel` |

**Cast bars** (`SpellCastBar`) stack vertically below Row 2 (the timeline), each its own slot via `SpellCastBar.Active` registry. Max 4 concurrent (§2.6).

**Combat text popups** (red damage, green heal, "Miss" / "Steal!" / "Steam!") float up from the actor's world position via `CombatTextManager.Spawn(text, position, styleKey)`.

**Debuff icons** (3-cell radial-ring strip) anchor inside the upper-right corner of each actor's tile via `WorldFollow` (§8.5).

---

## 10. Equipment (stub — see §24 for the full spec)

This section was the original stub. **See §24 "Equipment, Items, Materials, Currency"** for the comprehensive treatment — types, slots, ItemDefinition, inventory, durability, drops, crafting, currency, and the specific user-spec'd items (Mage Robe / Wizard Robe / Sleep Dart).

Quick recap: `Inventory/PartyLoadout` keys `HeroLoadout` per `CharacterClass`; each loadout is a `Dictionary<EquipmentSlot, ItemDefinition>`; `Formulas.ComputeEquipmentBonus(loadout)` aggregates stat bonuses into combat stats.

---

## 11. Scene Architecture

### 11.1 Code-only / builder-driven

Every scene EXCEPT `Game` and `Overworld` is reproducible from `Editor/Builders/*Builder.cs`. **`Game.unity` is also now builder-driven** via `GameBuilder.cs` (legacy CLAUDE.md note is stale). The few scenes still hand-tuned: `Overworld` (large world hierarchy, scheduled for builder migration).

- `BuilderAutoRebuild` watches `*Builder.cs` mtimes; on change → next domain reload rebuilds the matching `.unity` in-place.
- Reverse direction (scene → builder) is **not** automated — translating YAML to code needs judgment. Hand-edit a scene only if you commit to translating the change back into the builder.
- New objects always go in the builder. New UI uses factories (`Factories/*Factory.cs`). New sprites are PNGs on disk loaded via `AssetHelper.LoadAsset<Sprite>(address)` (Addressables).
- **No new `[SerializeField]`** — initialize from data-layer statics (`ItemData_*`, `ManaAbilities`, `SpellLibrary`, etc.) or factory parameters.

#### 11.1.1 The auto-rebuild loop (how it actually works)

```
                ┌──────────────────────────────┐
   You edit ── ▶│ Assets/Editor/Builders/X.cs │
                └──────────────┬───────────────┘
                               │ Unity recompiles
                               ▼
                ┌──────────────────────────────┐
                │  Domain reload completes     │
                │   [InitializeOnLoad] runs    │
                │   BuilderAutoRebuild fires   │
                └──────────────┬───────────────┘
                               │ diffs mtime against
                               │ Library/BuilderMTimes.json
                               ▼
                ┌──────────────────────────────┐
                │ For each changed builder:    │
                │  1. OpenScene("X.unity")     │
                │  2. Clear roots              │
                │  3. invoke X.Build()         │
                │  4. SaveScene                │
                │  5. update mtime cache       │
                └──────────────────────────────┘
```

- If `X` is the currently loaded scene, it is **reloaded in place** — any unsaved hierarchy edits are lost. Builders are source of truth.
- Builds are deferred during Play Mode and resume on exit.
- First launch with no cache records mtimes silently (no rebuild on fresh checkout).
- Exceptions surface via `TargetInvocationException` unwrapping (so the inner stack is logged, not the wrapper).

#### 11.1.2 What invalidates the cache

| Action | Cache reaction |
|---|---|
| Edit `*Builder.cs` body | mtime bumps → rebuild on next domain reload |
| Rename a builder file | new name = new entry → rebuilds for that scene name |
| Delete `Library/BuilderMTimes.json` | first launch silently re-records, no rebuild |
| Hand-edit `.unity` | **NOT detected** — `BuilderDriftChecker` guardrail catches at push time |

### 11.2 Scene list

- `SplashScreen` → `TitleScreen` → `ProfileSelect` / `ProfileCreate` / `SaveFileSelect` → `StageSelect` → `Game` (battle). (No Overworld — see §22.3, §28.)
- Vendor sub-scenes: `Vendor`, `Alchemist`, `Blacksmith`, `Equip`, `Party`, `Abilities`. Each has its own scene + builder + manager + `PlayerInventory` hydration. **Long-term**: once each is stable independently they merge into a single composed hub `.unity` (§25.9).
- `PostBattleScreen` after battles.
- `Bestiary` — swipe-navigable encyclopedia of every `ActorLibrary` entry (name, portrait, stats, abilities, lore). Reachable from `TitleScreen → Bestiary` (button wired to `TitleScreenManager.OnBestiaryButtonClicked → SceneHelper.Fade.ToBestiary()`). Back button → `BestiaryView.OnBackButtonClicked → SceneHelper.Fade.ToTitleScreen()`.
- `Credits`, `Settings`, `LoadingScreen`, `StageSelect`.

#### 11.2.1 Scene transition graph

```
                       ┌─────────────┐
                       │ SplashScreen│
                       └──────┬──────┘
                              ▼
                       ┌─────────────┐
                       │ TitleScreen │◀──────────────┐
                       └──┬──────┬───┘               │
                          ▼      ▼                   │ back
                  ┌───────────┐ ┌──────────┐         │
                  │ProfileSel.│ │ Bestiary │─────────┘
                  └────┬──────┘ └──────────┘
                       ▼
                  ┌───────────┐
                  │ SaveFileSel│
                  └────┬──────┘
                       ▼
                ┌────────────┐         ┌─────────────┐
                │ Overworld  │◀──────▶│ StageSelect │
                └─────┬──────┘         └─────┬───────┘
                      │ enter battle         │
                      ▼                      ▼
                ┌────────────┐         ┌─────────────┐
                │ LoadingScrn│────────▶│   Game      │
                └────────────┘         └─────┬───────┘
                                             │ battle end
                                             ▼
                ┌─────────────┐         ┌──────────────┐
                │ Vendor sub. │◀───────│ PostBattleScrn│
                └─────────────┘         └──────────────┘
                  (Vendor, Alchemist, Blacksmith,
                   Equip, Party, Abilities)
```

### 11.3 Scene navigation

`SceneHelper.Switch.ToX()` (instant) and `SceneHelper.Fade.ToX()` (with FadeOverlay) are the two flavors. `SceneHelper.Bestiary` constant + `ToBestiary()` methods added.

**Fade speed: 125 ms.** `FadeOverlayInstance` fades out/in at **0.125 s** each way — snappy, not languid. Scene-to-scene navigation should feel near-instant; the fade exists only to hide the load seam, not to be a transition flourish. (Set the duration constant in `FadeOverlayInstance`; don't pad it.)

**Rule:** for any UI Button → scene transition, the click handler must be a real `MonoBehaviour` method (not a lambda). Persistent `UnityEvent` listeners require a `UnityEngine.Object` target — lambdas via the `<>c` closure class do not qualify and are silently dropped. Example pattern: `BestiaryView.OnBackButtonClicked()` on the scene's view component, wired by the builder via `UnityEventTools.AddPersistentListener`.

### 11.4 Build settings caveat

When a new scene is created by `SceneBuilderHelper.OpenScene` (auto-creates if missing), it must be **manually added** to `File → Build Settings → Scenes in Build` to be playable from gameplay scene transitions.

---

## 12. Asset Pipeline

### 12.1 Sprites

- PNGs on disk in `Assets/Sprites/...`, configured as Sprite by `TextureImporter`.
- Loaded via `AssetHelper.LoadAsset<Sprite>(address)` which calls `Addressables.LoadAssetAsync<Sprite>`.
- Indexed in `Libraries/SpriteLibrary.cs` under category-keyed dictionaries (`Actor`, `GUI`, `Mana`, `SpellIcons`, etc.).

#### 12.1.1 Addressable address conventions

Addresses are pathlike strings without file extensions. The convention mirrors the on-disk folder:

| Category | Address format | Example |
|---|---|---|
| Actor portrait | `Sprites/Actor/<ClassName>` | `Sprites/Actor/Cleric` |
| Mana orb piece | `Sprites/Mana/<piece>` | `Sprites/Mana/orb-body` |
| Spell icon | `Sprites/Spells/<SpellName>` | `Sprites/Spells/Fireball` |
| HUD/GUI | `Sprites/GUI/<element>` | `Sprites/GUI/shield-button` |
| VFX prefab | `VFX/<EffectName>` | `VFX/IceSparkle` |
| Audio (future) | `Audio/<group>/<clip>` | `Audio/SFX/sword-swing` |

**Rule:** when adding a new asset, register the address using the matching convention; missing/typo'd addresses surface as the magenta error sprite (the author's last-resort fallback).

### 12.2 Procedural placeholders

`Editor/SpriteAssetAuthor.cs` builds placeholder sprites at edit-time:
- **Mana orbs**: `orb-body.png` (radial gradient white→transparent, 256×256) + `orb-glass.png` (white highlight upper-left, 256×256).
- **Spell icons**: `<SpellName>.png` (64×64 colored disk + first-letter glyph via tiny 5×7 pixel font), one per `SpellLibrary` entry.

Each save:
1. Writes the PNG to `Assets/Sprites/Mana/` or `Assets/Sprites/Spells/`.
2. Sets `TextureImporter` to Sprite + Bilinear + AlphaIsTransparency.
3. Adds the asset to the project's default Addressables group with the address `Sprites/Mana/orb-body`, `Sprites/Spells/Fireball`, etc.

Run via `Tools/Sprites/Author Mana Orb Sprites` and `Tools/Sprites/Author Spell Icons (Placeholders)`. Real art swaps in by overwriting the same PNG — address stays the same.

### 12.3 VFX prefabs

VFX are the documented EXCEPTION to "no prefabs" because particle systems have dozens of tightly-coupled modules best authored as prefab. Authoring still stays in code (`Editor/VfxPrefabAuthor.cs`) — each `Tools/VFX/Author '<Name>'` menu builds a `ParticleSystem` GameObject programmatically (Main / Emission / Shape / Velocity / Size / Color modules), saves a `.prefab` to `Assets/VisualEffects/`, deterministic + regeneratable.

Per-spell custom VFX in the catalog: IcyWind, FlamingTwist, ShockBolt, SleepDust, HealAura, PoisonCloud, AntidoteSparkle, ScanRays, SlowShimmer, SilenceMute. After running, paste the suggested registration line into `Libraries/VisualEffectLibrary.cs`.

Shader fallback chain in the author: URP → built-in → Sprites/Default → magenta error, so render-pipeline switches don't silently break.

---

## 13. Combat Resolution

### 13.1 Damage formulas

**Two damage paths today**: physical (pincer attacks) and spell (dispatcher).

#### 13.1.1 Physical pincer damage

Resolved by `Formulas.CalculateAttackResult(attacker, opponent)`. Stat-derived inputs:
- `Offense(attacker)` = function of `Strength` + weapon bonus.
- `Defense(opponent)` = function of `Vitality` + armor bonus.
- `MagicOffense / MagicDefense` = parallel pair using `Intelligence / Wisdom` (for spells that *do* route here, none yet).
- Crit chance scales with `Luck`.
- Miss chance scales inversely with attacker `Agility` vs target `Agility` (TODO: + `Blinded` debuff penalty).

Resulting `AttackResult` carries `Damage`, `IsCrit`, `IsMiss`, `HpDelta`. Supporters add their own `Offense`-derived chunks via `PincerAttackSupportSequence`.

**Both endpoints hit the whole line.** One `AttackResult` is built per trapped enemy *per attacker* (`PincerAttackManager`), so every enemy in the line takes damage from both flanking heroes. When a single drop spawns chained pincers, an enemy killed by an earlier link is `IsDying` (HP 0, not yet despawned) when the next link resolves; `AttackHelper.SingleAttackRoutine` skips any non-`IsPlaying` target, so the dead enemy is passed over while the survivors behind it still take their hit.

**Respite** is cosmetic only: if an attacker's *entire* trapped line is already dead by the time its link resolves, it plays a little victory spin + "Respite" text (`Spin360AndWaitRoutine`) instead of swinging at corpses. It never suppresses damage — an attacker with even one living target performs the real attack (`PincerAttackSequence`).

#### 13.1.2 Spell damage (dispatcher)

Computed inline in `SpellEffectDispatcher.ApplyDamage`:

```
raw       = spell.BaseDamage
resMult   = ActorData.ResistanceMultiplier(spell.DamageType)   // 0..2+
elemBonus = (spell.DamageType == Lightning && BuffSystem.Has(target, "wet"))
             ? Buffs.LightningWhenWetMultiplier
             : 1.0f
buffMult  = BuffSystem.GetIncomingDamageMultiplier(target)     // Protection 0.85, others 1.0
final     = round( raw × resMult × elemBonus × buffMult )

if (spell.BaseDamage > 0 && resMult > 0) final = max(1, final)   // min-1 floor (§3.2)
target.Stats.HP = clamp(target.Stats.HP − final, 0, MaxHP)
BuffSystem.OnDamaged(target)                                    // breaks Sleep, etc.
```

Currently spell damage **does not yet route through `Formulas.CalculateAttackResult`**. Future unification: route spell damage through `Formulas` too, so crit/miss/blind apply consistently.

#### 13.1.3 Healing

```
final = max(0, round(spell.BaseHeal))
target.Stats.HP = clamp(target.Stats.HP + final, 0, MaxHP)
```

No resistance applies; no crit on heal in V1 (designer call later — could add a crit-heal mechanic via Luck).

### 13.2 HP delta

`ActorInstance.Stats.HP = Mathf.Clamp(Stats.HP - dmg, 0, Stats.MaxHP)`. Then `BuffSystem.OnDamaged(target)` to break sleep-like buffs.

### 13.3 Death

HP reaching 0 = death. Handled by existing `DeathHelper` / death sequence (separate from spell dispatcher). Coins drop on enemy death via existing `CoinManager`.

### 13.4 The interrupt path (partially built)

When an enemy is in the Prepare Zone and casting/charging, a pincer or shield press should **interrupt** their charge.

**Built (Phase 1 — Fail only):** `EnemyAttackSequence.InterruptCastingHero` is wired and calls `TimelineBar.InterruptCastsByOwner(hero)` unconditionally (`EnemyAttackSequence.cs:109,133`). Today every interrupt is a **Fail** — cast cancels, MP stays consumed, no effect, spell-icon removed. (`CastingState.Interrupt()`.)

**Remaining (Phase C):**
- **Three-outcome resolver** (`US-024`): replace the flat Fail with `CastInterruptResolver.Resolve` → **Fail** | **Pushback** (cast survives, icon moves left, brief stun) | **Clutch** (LCK-rare: snap to u=1, resolve — the "miracle save"). Roll order: Clutch first; else Pushback vs Fail weighted by LCK / WIS. Clutch plays a dedicated `ClutchSequence` (`US-025`).
- **Enemies that actually cast** (`US-026`): enemies are melee-only today, so there's no enemy charge to interrupt yet.
- **Interrupting an ENEMY drops an orb** (`US-027`): cancels the charge AND drops an orb of its color to the team bank — how off-palette colors flow in (the enemy supplies what your party can't make).

---

## 14. AI

### 14.1 Enemy planning

`Services/EnemyPlanner.PlanStep(enemy, actors, tileMap)`:
1. If `BuffSystem.IsImmobile(enemy)` → stay put.
2. Pick best target hero (nearest + HP-weighted).
3. Score candidate moves (stay + 4 cardinals): distance to target, in-range bonus, walk-into-flank penalty.
4. **If Humanoid**: add +50 to any candidate that would form a pincer (simulate the move, run `PincerDetector`, restore).
5. Return best candidate.

#### 14.1.1 Decision tree

```
                  PlanStep(enemy, actors, tileMap)
                            │
                            ▼
                  ┌─────────────────────┐
                  │ IsImmobile(enemy)?  │
                  │  (Frozen / Sleep)   │
                  └────┬────────────┬───┘
                       │ yes        │ no
                       ▼            ▼
                 return loc    pick target hero
                  (no move)    (min Manhattan + HPfrac × 8)
                                    │
                                    ▼
                       enumerate candidates:
                       {self} ∪ {4 cardinals filtered on-board+free}
                                    │
                                    ▼
                       for each candidate c:
                         score  = −Manhattan(c, target)
                         if cardinal-adj(c, target): score += 2
                         if WouldBeFlanked(c, heroes): score −= 100
                         if c == self.loc:           score −= 0.5
                         if IsHumanoid(enemy) AND
                            WouldFormPincer(enemy, c):
                                                    score += 50
                                    │
                                    ▼
                          best = argmax(score)
                                    │
                                    ▼
                              return best
```

The +50 pincer-seek beats the −100 self-flank avoidance (no, it doesn't — −100 wins), which means **enemies will not walk into a hero pincer even to form their own pincer**. That's intentional: enemies pick safe pincers, not suicide pincers. If a designer wants a kamikaze Bruiser archetype, tweak the score weights via a new `archetype` tag.

#### 14.1.2 Score weight cheat sheet

| Factor | Weight | Reasoning |
|---|---|---|
| Distance to target | −1 per tile | Closer is better (advance) |
| Cardinal-adjacent to target | +2 | "In range to swing next turn" |
| Would be flanked by heroes here | −100 | Hard avoid (single hardest signal) |
| Stay put | −0.5 | Mild bias to keep advancing |
| Forms Humanoid pincer here | +50 | Pincer-seek beats positional ties, loses to flank-avoidance |

These weights are the **only tuning knobs**; they live as constants in `EnemyPlanner`. Adding new behaviors (range-keep, support-buddy) means new factor branches.

### 14.2 Enemy archetypes (design palette)

Different enemies should *feel* different by their `ActorData.Tags` + base stat distribution + which `ActorTag.Humanoid`-style mechanics they engage in. Suggested archetypes for designers:

| Archetype | Stat lean | Tags | Behavior the planner expresses |
|---|---|---|---|
| **Rusher** | high SPD, low VIT | `Humanoid, Beast` | Loads timeline fast; closes the distance every turn. Wants to be adjacent and swing. |
| **Bruiser** | high STR + VIT | `Humanoid, Soldier` | Slower load but high HP and threat. Best target for a pincer. |
| **Flanker** | mid STR, high AGI | `Humanoid` | Seeks pincer formation with another Flanker. Devastating if ignored. |
| **Ranged** | high AGI, mid INT | `Humanoid` | Wants distance — keeps gap from heroes; attacks across tiles (future: requires a Line shape) |
| **Caster** | high INT, low VIT | `Humanoid, Magic` | Telegraphs a spell in the Prepare Zone (Phase C); high reward for interrupting |
| **Beast** | varies | `Beast` (no Humanoid) | Can NOT pincer. Just rushes. Cheaper threat density. |
| **Mechanical** | high VIT, status-immune | `Mechanical, Boss` | Status immunities (Resistances 0 for Poison/Sleep). Pure HP race. |
| **Boss** | very high everything | `Humanoid, Boss, Elite` | Scripted phase changes (TODO via `SequenceManager`); large move-set; not pincerable in some phases. |

### 14.3 Future AI hooks

- **Casting enemies** (Phase C): telegraph a charge in the Prepare Zone. The icon shows a colored fill bar matching the charge type; if the player interrupts during the slow window, the cast is canceled AND drops an orb of that color into the team bank (§3.1.2).
- **AI-driven supporter positioning**: enemy turn could include a "support" mode where an enemy moves to enable an ally's pincer.
- **Boss-specific scripted moves**: per-class override in `EnemyPlanner` that swaps the generic step logic for boss-authored sequences.
- **Threat tracking**: heroes who deal more damage become preferred targets (TODO).
- **Coordinated retreat**: low-HP enemies could move *away* from heroes when wounded.

---

## 15. Save / Profile

### 15.1 Data model sketch

```
Profile                       (one per player, top-level)
├─ Name              string
├─ CreatedAt         DateTime
├─ LastPlayedAt      DateTime
├─ CurrentSaveIndex  int
└─ Saves[]           SaveState        (3 slots typical)
       ├─ Gold              long
       ├─ TotalPlaytime     TimeSpan
       ├─ StageProgress     int
       ├─ HeroSaves[]       HeroSave
       │   ├─ CharacterClass enum
       │   ├─ TotalXP        long      (level derived at runtime)
       │   ├─ HpCurrent      float     (carry-over wounds)
       │   ├─ Equipment      HeroEquipmentSave
       │   │   ├─ Weapon, Armor, …      ItemRef
       │   │   └─ AbilityBarSlots[6]    SlotRef (Skill|Spell|Item)
       │   └─ KnownSpells   string[]
       ├─ Inventory         PlayerInventorySave
       │   ├─ Entries       Dict<itemId, (count, durability)>
       │   └─ MaterialCounts Dict<materialId, count>
       └─ BestiaryProgress  Dict<class, EncounterRecord>
                          (seen, defeated, lore-unlock flags)
```

XP is stored as `TotalXP`; level + currentXP are **derived** via `ExperienceHelper.DeriveFromTotalXP(totalXP)` — no need to migrate when the curve changes. HP carry-over (wounds between battles) is intentional and lets the Inn vendor have a job (full-heal for gold).

### 15.2 Persistence flow

| Trigger | Saved to disk? |
|---|---|
| Vendor purchase/sale | Yes (commit-on-vendor-exit) |
| AbilityBar reassign (Abilities scene) | Yes (commit on scene exit) |
| Equip change (Equip scene) | Yes (commit on scene exit) |
| Mid-battle stat/hp changes | No (held in `ActorInstance.Stats`) |
| End of battle | Yes (Hp written back to HeroSave; rewards added to Inventory) |

### 15.3 Open migrations

- ~~`HeroEquipmentSave.AbilityBarSlots` migration~~ — **DONE.** It is now the source of truth for per-hero AbilityBar contents with a full hydrate/persist round-trip (`Profile.cs:376-487`, `HeroLoadout.cs:183-268`); `HeroLoadouts.perClass` is the fallback default only when a hero has no saved bar.
- `BestiaryProgress` is **unwritten** (`US-054`) — Bestiary currently shows every entry regardless of whether the player has encountered it. Future: gate by `seen` flag, show silhouettes for unseen (`US-093`).

---

## 16. Open Design / Implementation TODOs

> **The execution board is `user_stories.md`** (repo root) — a dependency-ordered backlog reconciled against the live code on 2026-05-30. This §16 is the *index of what's still open*; the board is where you pick up work, with file evidence and build order. Keep them in sync: landing a story deletes its row here AND moves any new rule into its section.
>
> **Reconciliation note (2026-05-30):** a code audit found this section had drifted badly — it listed as "TODO / not built / stub" a large amount of work that was **already implemented**. Those rows are struck below with their verifying file. The remaining open rows carry their `user_stories.md` id. **P0** = blocks core-loop feel, **P1** = enriches, **P2** = polish.

### 16.1 Combat gameplay hooks (buffs that apply but don't yet bite)

| # | US | TODO | Priority | Touch |
|---|---|---|---|---|
| ~~1~~ | — | ~~Burning / Poisoned per-tick damage~~ — **DONE** (`BuffTickManager.cs:45-60`) | — | — |
| ~~2~~ | US-011 | ~~**Slowed → timeline-speed multiplier**~~ — **DONE** 2026-05-31 (`TimelineIcon.GetEffectiveUPerSec` ×0.5, read by `TimelineBarInstance.AdvanceBySeconds`) | — | — |
| ~~3~~ | US-012 | ~~**Silenced → cast-block**~~ — **DONE** 2026-05-31 (`AbilityBar.HandleSpell` refuses + Spell slots render blocked; diagonal-stripe sprite is future polish) | — | — |
| ~~4~~ | US-013 | ~~**Blinded → hit-chance penalty**~~ — **DONE** 2026-05-31 (`Formulas.CalculateHitType` ×0.5 accuracy when attacker Blinded) | — | — |
| ~~5~~ | US-014 | ~~**SleepWhenWarmMultiplier**~~ — **DONE** 2026-05-31 (Sleep ×1.5 duration on Warm target, `SpellEffectDispatcher`) | — | — |
| ~~—~~ | US-015 | ~~**BreaksOnMove**~~ — **DONE** 2026-05-31 (`ActorMovement.HandleOverlap` → `BuffSystem.OnMoved`) | — | — |
| ~~—~~ | US-016 | ~~**Turn-unit decrement**~~ — **DONE** 2026-05-31 (`TurnManager.NextTurn` end-of-turn `BuffSystem.TickTurn`) | — | — |

### 16.2 Cast / interrupt system (Phase C)

| # | US | TODO | Priority | Touch |
|---|---|---|---|---|
| ~~10~~ | — | ~~Interrupt path — wire `InterruptCastingHero`~~ — **DONE** (Fail path live: `EnemyAttackSequence.cs:109,133` → `TimelineBarInstance.InterruptCastsByOwner`) | — | — |
| ~~11~~ | — | ~~Cast-as-timeline-icon (`Resolving` mode)~~ — **DONE** (`TimelineIcon.cs:52,833`; `TurnManager.IsResolvingCast`) | — | — |
| ~~—~~ | — | ~~Cast-time WIS/INT scaling~~ — **DONE** (`Formulas.cs:492`; `CastingState.cs:91`) | — | — |
| — | US-024 | **Clutch/Pushback/Fail resolver** — `CastInterruptResolver.Resolve` returning {Fail\|Pushback\|Clutch} (today: unconditional Fail) | P1 | new `Services/CastInterruptResolver.cs` |
| — | US-025 | **ClutchSequence** — rare LCK save: snap spell-icon to u=1 + flash/SFX | P2 | new `Sequences/ClutchSequence.cs` |
| — | US-026 | **Enemy charge/telegraph spells** — enemies cast (today: melee only) | P1 | `EnemyPlanner`, new `EnemyChargeSequence` |
| — | US-027 | **Interrupt enemy cast → drop charge-color orb** (closes off-palette economy) | P1 | `TimelineBarInstance`, `ManaPoolManager` |

### 16.3 Equipment / inventory

| # | US | TODO | Priority | Touch |
|---|---|---|---|---|
| — | US-040 | **`ItemDefinition` fields** — `BattleStartManaOrbs`, `OnUseSpellName`, `ResistanceModifiers` (all absent) | P1 | `ItemDefinition` |
| 6 | US-041 | **Mage/Wizard Robe battle-start orbs** — `MageRobes` exists; need Wizard Robe + battle-start scan | P1 | `ItemData_Armor`, `ManaPoolManager.Start` |
| 7 | US-042 | **Sleep Dart** — item routes through Sleep's targeting via `OnUseSpellName` | P1 | `AbilityBar.HandleItem`, `UseItemSequence` |
| — | US-043 | **Equipped `ResistanceModifiers` folded into damage** | P1 | `Formulas`, `SpellEffectDispatcher` |
| ~~8~~ | — | ~~Weapon shatter dual-damage~~ — **DONE** (`WeaponDurabilityHelper.cs:37-103`) | — | — |
| ~~9~~ | — | ~~Repair max-cap~~ — **DONE** (`WeaponDurabilityHelper.cs:105-138`) | — | — |

### 16.4 UI / responsive design

| # | US | TODO | Priority | Touch |
|---|---|---|---|---|
| ~~16~~ | — | ~~`ManaOrbLine` full-width~~ — **DONE** (responsive/equidistant, `ManaOrbLineFactory.cs:38`) | — | — |
| 17 | US-001 | **AspectGuard** — `Utilities/AspectGuard.cs` + insert into every Canvas + viewport math (§26.3–26.4) | P0 | new file; every `*Builder.cs` |
| 21 | US-076 | **Spell icons on bar** — render `SpriteLibrary.SpellIcons[name]` (today: glyphs only) | P2 | `AbilityBar.Refresh` |

### 16.5 Content / data layer

| # | US | TODO | Priority | Touch |
|---|---|---|---|---|
| 12 | (backlog) | **Spell-VFX per-spell prefabs** — author menus exist; run + register as art lands | P2 | `Tools/VFX/Author *`, `VisualEffectLibrary` |
| 13 | US-030 | **Per-hero color affinity** — `ColorAffinity` on ActorData; pincer drops use it (today: hardcoded Blue, `PincerAttackManager.cs:206`) | P1 | `ActorData`, `PincerAttackManager.DropOrbAt` |
| 14 | US-093 | **Bestiary enemy filter** — show only `ActorTag.Enemy` entries | P2 | `BestiaryView` |
| ~~22~~ | — | ~~Drop tables per enemy class~~ — **DONE** (16 tables, `DropTableLibrary.cs:53-68`) | — | — |

### 16.6 Save / state

| # | US | TODO | Priority | Touch |
|---|---|---|---|---|
| ~~20~~ | — | ~~AbilityBar save migration~~ — **DONE** (`Profile.cs:376-487`; `HeroLoadout.cs:183-268`) | — | — |
| — | US-053 | **HP carry-over** — no `HpCurrent` in save; needed for wounds + defeat restore | P1 | `Profile.cs`, `PostBattleManager` |
| — | US-054 | **BestiaryProgress writing** — record seen/defeated (gates US-093/US-077) | P2 | `Profile.cs`, spawn/death hooks |
| 15 | US-090 | **"No valid targets" toast** — Auto resolve with 0 actors silent-cancels (`TargetingMode.cs:81`) | P2 | `TargetingMode.Begin` |
|   | US-093 | **Bestiary unlock gating** — read `BestiaryProgress.seen` for silhouettes | P2 | `BestiaryView` |

### 16.7 Cross-reference

When you land a TODO from this list:
1. Delete the row here AND check the box in `user_stories.md`.
2. If the implementation produced a new rule, **add it to the right section** (e.g., Slow → §2/§8; Mage Robe → §24.8).
3. If it raised a new question, add it to §29.

---

## 17. Code-Only Workflow Discipline

Per the locked feedback memory ([[feedback_code_only_workflow]]):

- **Never instruct the user to open `Game.unity` and drag things.** All scene work goes through `Editor/Builders/*Builder.cs` (`BuilderAutoRebuild` regenerates) or runtime factories called from a manager's `Start`/`Awake`.
- Drag-and-drop inspector workflows are not a fallback. Persistent UnityEvent listeners (`WireOnClick`) target real methods on `MonoBehaviour` instances — never lambdas, because compiler-generated closure classes (`<>c`) don't derive from `UnityEngine.Object` and are rejected at registration.
- When the user describes a UI change, implement it in builder code with exact RectTransform anchors and the layout constants in `HudLayout`.

### 17.1 Common pitfalls (real bugs we've hit)

A running list — when you trip one of these, fix it AND amend this section so the next person doesn't.

1. **Looping VFX as `CastVfxName` → sticks on caster forever.** Any `VisualEffectAsset` with `IsLooping = true` and no `Duration` plays indefinitely. Putting it on the cast-flash slot means it parents to the caster's spawn point and never despawns. Looping VFX belong on **`LingerVfxName`** (parented to the target; ends when the target/buff ends). Non-looping VFX or VFX with explicit `Duration > 0` are safe in any slot.

2. **`WireOnClick(button, () => SomeStaticMethod())` → ArgumentException.** `SceneBuilderHelper.WireOnClick` uses `UnityEventTools.AddVoidPersistentListener`, which **rejects lambdas** because the generated closure class doesn't derive from `UnityEngine.Object`. Always wire to a `public void OnXxxClicked()` method on a `MonoBehaviour` instance that's IN the scene. If the handler needs to call a static (`SceneHelper.Fade.ToBestiary()`), wrap it in a MonoBehaviour method (`BestiaryView.OnBackButtonClicked` calls the static).

3. **`g.Actors` is a nested type, not a property.** `if (g.Actors == null)` is a compile error ("type used as expression"). Null-check the collection you actually need: `if (g.Actors.All == null) return;`.

4. **`TargetingMode.IsActive` stuck `true`.** If the picker overlay is destroyed without firing its `onConfirm`/`onCancel` callbacks (scene reload, external destroy), the static flag stays true and every subsequent ability click silently bails. Fix: `Begin` now calls `DismissAnyActive()` first; `TargetPickerOverlay.OnDestroy` also fires the cancel callback as a safety net.

5. **`BuilderAutoRebuild` masks errors behind `TargetInvocationException`.** Reflection-invoked builders throw the outer wrapper, hiding the real cause. Solution wired in `BuilderAutoRebuild.RebuildScenes` catch: unwrap `TargetInvocationException.InnerException` and log `ToString()` for the full stack.

6. **`ManaAbility` cannot be renamed to `Ability`.** The legacy `Ability` class lives in `Instances/AbilityButton.cs`. Keep the new bar-data class as `ManaAbility` to avoid type collision. The legacy `Ability` is on the path to retirement but heavily referenced.

7. **`TileManager.Reset()` NREs during builder rebuild.** Unity's `Reset()` magic method fires when a builder calls `AddComponent<TileManager>()`. At that moment `g.Tiles` is null (no GameManager initialized). Always null-guard `g.X` accessors in `Reset()` methods.

8. ~~**`Game.unity` rebuilds emit "Can't add component X — already exists" warnings.**~~ ✅ RESOLVED (US-002, 2026-05-31): `GameBuilder.Build()` now calls `SceneBuilderHelper.ClearAllRootObjectsSilent()` at the top, matching every other builder + `CliEntryPoints.InvokeBuilderCreate`. Rebuilds are warning-free.

9. **`AddressableAssetSettings` missing.** If the project's Addressables aren't initialized, `AssetHelper.LoadAsset<T>` returns null and `SpriteAssetAuthor` logs a warning. Open `Window → Asset Management → Addressables → Groups` once to seed the default group.

10. ~~**`LayerMask.NameToLayer("UI")` can return -1.**~~ ✅ RESOLVED (US-003, verified 2026-05-31): the only `cullingMask` assignment in `Assets/` is `BestiaryBuilder.cs:55`, which already guards with `uiLayer >= 0 ? (1 << uiLayer) : ~0`. Other builder cameras don't cull at all (render everything). Keep the guard pattern for any future `1 << NameToLayer` camera site.

11. **Vendor scenes look "like trash" because of a broken `CanvasScaler` — the #1 cause.** `VendorBuilder` (and the other vendor builders) set `CanvasScaler.referenceResolution = new Vector2(0f, 0f)` with `ScaleWithScreenSize`. Scaling against a **zero** reference resolution makes every element size/position nonsense — fonts, padding, buttons all wrong. **Fix:** every vendor Canvas MUST use the §26.2 baseline (`referenceResolution = (1170, 2532)`, `screenMatchMode = MatchWidthOrHeight`, `matchWidthOrHeight = 0.5`). This is *the* reason "sizing and colors and basic interface" came out broken.

12. **The vendor list doesn't scroll because the `ScrollRect` is never wired.** `VendorBuilder` calls `AddComponent<ScrollRect>()` on the Viewport but never assigns `.content`, `.viewport`, `.horizontal`/`.vertical`, or `movementType` (the "ScrollRect cross-references" comment block at the file's end is empty). Result: rows overflow/clip and the list is unusable. **Fix:** wire `scroll.viewport = Viewport`, `scroll.content = Content`, `scroll.vertical = true`, `scroll.horizontal = false`, `scroll.movementType = Clamped`. Also pull all colors from `HubTheme` (the builders hardcode `new Color(...)` and drift from the palette). The lasting fix is the shared `ShopView` (US-111) so this is solved once, not six times.

### 17.2 Cadence

- Keep going until the whole feature works end-to-end before committing ([[feedback_commit_granularity]]).
- Ship a `DebugManager.Demo_*` method + Debug-Window button with every new system so the user can test by clicking, not by being asked "does it work?" ([[feedback_debug_window_demos]]).
- Headless Unity is unlicensed in this dev env; the user runs Play tests manually ([[project_batchmode_verify_recipe]]).
- Never `taskkill` Unity.exe to recover from a stuck batchmode — ask the user to close the editor cleanly ([[feedback_force_close_unity]]).
- Don't suggest `/Run` to launch the game; that routes to `/loop`. Use the PS1 console Option 1 ([[feedback_no_run_slash_command]]).

### 17.3 Checkpoint recipe (before handing back for commit)

When work feels done, run this sequence — don't ship until each is green:

```
1. Source mtime > Assembly-CSharp.dll mtime?
   → Yes: Unity recompiled. Continue.
   → No:  Recompile didn't fire. Ask user to focus the Editor.

2. Unity Console errors?
   → Yes: Diagnose; do NOT push.
   → No:  Continue.

3. New gameplay rule?
   → Yes: bible.md updated? If no, update it. (§31)
   → No:  Continue.

4. New system?
   → Yes: DebugManager.Demo_* + DebugWindow button added?
   → No:  Continue.

5. Touched a guardrail (SerializedField, Resources.Load,
   Instantiate outside Factory.cs, scene drift)?
   → Yes: regenerate the allowlist (CliEntryPoints) or
          fix the violation.
   → No:  Continue.

6. Play-test in Editor (user clicks Play; reports back).
   → Pass: ready to commit (/commit).
   → Fail: iterate; do NOT mid-phase commit.
```

This is the "verify-then-checkpoint" rhythm ([[feedback_refactor_approach_validated]]) — no mid-phase commits, end-to-end before the hash.

### 17.4 Naming conventions worth knowing

| Pattern | Meaning | Example |
|---|---|---|
| `*Manager` (singleton MonoBehaviour) | Long-lived scene-wide system | `TurnManager`, `ManaPoolManager` |
| `*Instance` (MonoBehaviour) | Runtime per-object component | `ActorInstance`, `TimelineIcon` |
| `*Data` (static class) | Static template / definition data | `ItemData_Weapons`, `SkillData_Training` |
| `*Library` (static, lazy `Ensure()`) | Lookup over static data | `ItemLibrary`, `ActorLibrary` |
| `*Factory` (static) | The ONLY place `Instantiate` is allowed | `ActorFactory`, `ManaOrbFactory` |
| `*Sequence` (`SequenceManager.Add`) | Async event queue entry | `PincerAttackSequence`, `DeathSequence` |
| `*Builder` (`[InitializeOnLoad]`-adjacent) | Reproducer for one scene | `GameBuilder`, `VendorBuilder` |
| `*Service` (static, pure) | Logic with no Unity scene access | `PincerDetector`, `EnemyPlanner` |
| `*Helper` (static utility) | Cross-system shortcut accessors | `GameHelper`, `SceneHelper` |
| `*Definition` / `*Recipe` | Designable data shape | `SpellDefinition`, `CraftingRecipe` |

### 17.5 File-edit cookbook (where do I touch X?)

The dispatcher / dispatcher-edge-case table answers "what does the system do?"; this answers "what file do I open?"

| Goal | Primary file(s) | Side effects to remember |
|---|---|---|
| **Add a HUD button on Row 1** | `GameBuilder.cs` → new GameObject + `RectTransform` anchored via `HudLayout.Row1Y_FromTop` + Click handler on a real MonoBehaviour | If the click triggers a static, wrap in a `MonoBehaviour.OnXxxClicked()` |
| **Change HUD layout row Y** | `Utilities/HudLayout.cs` (constants only) | `GameBuilder` and every relevant factory auto-pick up; rebuild Game scene |
| **Add a new debuff** | `Data/Buffs.cs` (catalog row) + `Canvas/DebuffIconBar.ColorFor` + `LetterFor` | Add gameplay hook (TODO: §16) if it should affect formulas |
| **Add a new spell** | §19 checklist (`ManaAbilities`, `SpellLibrary`, `HeroLoadouts`) | Update §7 catalog table |
| **Add a new enemy class** | §20 checklist (`CharacterClass` enum, `Data/Actor/<X>.cs`, `ActorLibrary`) | Stage + drop table separately |
| **Add a new sprite/asset** | PNG on disk in `Assets/Sprites/...` + `SpriteAssetAuthor` (procedural) OR Addressables config | `AssetHelper.LoadAsset<T>(address)` to consume; register in `SpriteLibrary` if reused |
| **Add a new VFX prefab** | `Editor/VfxPrefabAuthor.cs` menu item → registers prefab → paste registration into `VisualEffectLibrary` | Choose CastVfx vs Linger vs Impact slot carefully (looping = linger only) |
| **Tweak pincer damage** | `Utilities/Formulas.CalculateAttackResult` | Update §13.1.1 if formula shape changes |
| **Tweak spell damage** | `SpellDefinition.BaseDamage` in `SpellLibrary` | Per-spell change, no formula rewrite needed |
| **Tweak enemy AI weight** | `Services/EnemyPlanner.cs` (constants section) | Update §14.1.2 table |
| **Add a vendor screen item** | The vendor's manager (`VendorManager`, `BlacksmithManager`, …) + recipe/data registration in `Data/` | Run the scene's builder to refresh layout if UI rows added |
| **Hook a new Save field** | `Models/HeroSave.cs` or `PlayerInventorySave.cs` + `SaveStateService` write/read paths | Migration: handle old saves missing the field |
| **Add a Debug Window demo button** | `Assets/Editor/DebugWindow.Demos.cs` (or add a new `Demo_*` method to `DebugManager`) | Follow [[feedback_debug_window_demos]] — every new system gets one |
| **Change scene transition** | `Helpers/SceneHelper.cs` (`ToX` methods) + any builders that wire the button to it | Persistent UnityEvent → wire to a real MonoBehaviour method |
| **Add a buff cross-effect** | `Data/Buffs.cs` (constants like `LightningWhenWetMultiplier`) + the relevant dispatcher stage | Update §8.2 interaction matrix |

---

## 18. Glossary

### Combat / mechanics

- **AbilityBar** — Row-13 6-slot bar. Holds Skills / Spells / Items per the selected hero.
- **ManaBank** — party-wide line of 12 colored orbs (WUBRG palette).
- **Pincer** — two Humanoid heroes flanking a contiguous enemy line on a single row or column; deals damage to every enemy in the line.
- **Supporter** — ally cardinally adjacent to a pincer endpoint with unbroken line of sight; adds bonus damage to that endpoint.
- **Slide / displace** — what dragging a hero through an occupied tile does to the occupant (single tile, into the tile the dragger just left).
- **Prepare Zone** — rightmost 25–35% of the timeline (`u ≥ 1 - ZoneU`); in-Zone icons crawl at a uniform pace and are the prime interrupt window.
- **Pushback** — leftward shove applied to a damaged icon **only if** it was in the Prepare Zone; followed by `Stunned` mode while it stops.
- **Train-cascade** — when a new/displaced icon arrives, neighbors are shoved further left in sequence to maintain `MinSpatialGap` — order-preserving.
- **Cast bar** — colored shrinking line under the timeline showing a spell's remaining cast time.
- **Interrupt outcomes** — *Fail* (cast canceled, MP gone), *Pushback* (cast delayed), *Clutch* (rare LCK-driven: cast snaps to trigger and resolves).

### Targeting

- **Shape / Mode / Filter** — the three orthogonal axes of every spell's targeting.
- **Tile-pick / Actor-pick / Auto** — the three TargetMode values.
- **Single / Square / Diamond / Cross / Plus / Row / Column / AllEnemies / AllAllies** — the TargetShape values.
- **EnemyOnly / AllyOnly / Any / EmptyOnly** — the TargetFilter values.

### VFX

- **CastVfx** — plays at the caster the moment a cast resolves (never use a looping prefab here — see §17.1).
- **ProjectileVfx** — moves from caster to target via `ProjectileMotion`.
- **ImpactVfx** — plays at target on arrival.
- **LingerVfx** — VFX parented to a target after impact; visualizes an active debuff. Must use non-looping or duration-limited VFX assets to avoid sticking forever.

### Ability kinds

- **Skill** — costs the hero's turn but no mana; defined by class.
- **Spell** — pays a mana recipe; defined by `SpellLibrary`.
- **Item** — consumes a stack of a `ConsumableItem`; effects vary.

### Tags & rules

- **Humanoid** — `ActorTag.Humanoid`; gates who can perform/seek pincers (heroes default-true, enemies must opt in).
- **Beast / Mechanical / Magic / Boss / Elite / Soldier** — other `ActorTag` flags for archetype + filter logic.

### Currency & economy

- **Gold** — universal soft currency; spent at vendors.
- **Mana orbs** — battle-scoped, color-coded; drained on cast.
- **Material** — crafting input held by Blacksmith / Alchemist recipes; not equipment.

---

## 19. How to Add a New Spell — Checklist

1. Add a `ManaAbility` to `Data/ManaAbilities.cs` (pick a ctor: Skill / Spell / Item).
2. Add a `SpellDefinition` to `Data/SpellLibrary.cs` declaring shape / mode / filter / radius / VFX names / motion / debuff / damage.
3. Optionally add a per-class loadout entry in `Data/HeroLoadouts.cs` so it actually appears on someone's bar.
4. If the spell needs a new debuff: define it in `Data/Buffs.cs` (and add letter + color in `Canvas/DebuffIconBar`).
5. Run `Tools/Sprites/Author Spell Icons (Placeholders)` to regenerate the icon set (the new spell is auto-included).
6. (Optional) Author a per-spell VFX prefab via `Editor/VfxPrefabAuthor.cs`, then update the spell's VFX name strings.
7. Update §7 of this bible with the new row.

### 19.1 Worked example: "Quake" (new spell)

Design brief: pick a tile, hit a 3×3 Square of enemies for 16 Physical damage, no debuff.

```csharp
// 1. Data/ManaAbilities.cs
public static readonly ManaAbility Quake = ManaAbility.Spell(
    name: "Quake",
    cost: new ManaRecipe { Red = 1, Black = 1 },   // 2-orb cost
    castTimeSeconds: 1.4f);

// 2. Data/SpellLibrary.cs
public static readonly SpellDefinition Quake = new SpellDefinition(
    ability: ManaAbilities.Quake,
    shape: TargetShape.Square, mode: TargetMode.PickTile,
    filter: TargetFilter.EnemyOnly, radius: 1,
    castVfx: "RockBurst", projectileVfx: null, motion: ProjectileMotion.None,
    impactVfx: "RockBurst",
    baseDamage: 16f, damageType: DamageType.Physical,
    projectileSeconds: 0f);

// + SpellLibrary.All updated to include Quake.

// 3. Data/HeroLoadouts.cs — give it to the Barbarian
HeroLoadouts.Set(CharacterClass.Barbarian, new[] {
    ManaAbilities.Quake, ManaAbilities.Fireball, ManaAbilities.Bolt,
    ManaAbilities.NewPotion(3), null, null
});

// 4. (no new debuff needed)

// 5. Tools/Sprites/Author Spell Icons (Placeholders)
//    → generates Sprites/Spells/Quake.png with the brown "Q" glyph + registers Addressable.

// 6. (optional) Tools/VFX/Author 'RockBurst' to author a custom prefab.

// 7. Add row to §7 catalog:
//    | Quake | (R)(B) | Square(r=1) / PickTile / EnemyOnly | None | 16 Physical AOE | Barbarian's anti-clump tool |
```

What you DON'T touch: the dispatcher, the picker, the cast bar, the orb bank, the mana economy, the HUD layout. Those are stable and parametric — only data files change.

---

## 20. How to Add a New Enemy — Checklist

1. Add a `CharacterClass` enum entry in `Helpers/CharacterClass.cs`.
2. Create a `Data/Actor/<Name>.cs` with `Data()` factory returning `new ActorData { ... }`.
3. Set `Tags`: include `Enemy`; include `Humanoid` if the enemy can pincer; add `BeastFlying`/etc. as appropriate.
4. Set `Resistances` dict for elemental profile (omit = 1.0 neutral).
5. Register in `Libraries/ActorLibrary.cs`.
6. Add to a `StageDataLibrary` wave / `DropTable` for actual encounter.
7. Bestiary picks it up automatically (sorted alphabetically by class).

### 20.1 Worked example: "FrostWolf" (new enemy)

Design brief: a Beast (cannot pincer), high SPD, low VIT, hits Ice damage, weak to Fire, immune to Ice.

```csharp
// 1. Helpers/CharacterClass.cs — add FrostWolf to the enum

// 2. Data/Actor/FrostWolf.cs
public static class FrostWolf {
    public static ActorData Data() => new ActorData {
        characterClass = CharacterClass.FrostWolf,
        Name = "Frost Wolf",
        PortraitAddress = "Sprites/Actor/FrostWolf",
        // 3. Tags — Beast (cannot pincer); also Enemy (default for non-Hero)
        Tags = ActorTag.Beast,
        BaseStats = new BaseStats {
            Strength=10, Vitality=8, Agility=12, Speed=14,
            Stamina=8, Intelligence=4, Wisdom=4, Luck=6
        },
        // 4. Resistances — Ice immune, Fire weak
        Resistances = new Dictionary<DamageType, float> {
            { DamageType.Ice, 0f },
            { DamageType.Fire, 2f }
        },
        XpReward = 25,
        GoldReward = 18,
    };
}

// 5. Libraries/ActorLibrary.cs (in Ensure())
Register(CharacterClass.FrostWolf, FrostWolf.Data());

// 6. Add to a stage wave + drop table
//    StageDataLibrary.Stage_FrozenPass.Waves[0].Enemies.Add(CharacterClass.FrostWolf);
//    DropTableLibrary.For(CharacterClass.FrostWolf) = new[] {
//        new DropEntry(ItemData_Materials.WolfPelt, weight: 70),
//        new DropEntry(ItemData_Materials.IceShard, weight: 30),
//    };
```

EnemyPlanner now treats it as a Rusher-Beast: closes distance, swings in melee, never tries to flank. Bestiary lists it automatically; future `seen` gate hides until first encounter.

---

## 22. The Macro Loop

The game's beat-to-beat shape. **V1 target loop** (the *connective membrane*):

```
TitleScreen
   ↓
ProfileSelect / ProfileCreate / SaveFileSelect
   ↓
StageSelect  ◀════════════════════════════════╗   (scrollable level list, §22.3)
   │  ↕ "Hub" button                          ║
   │  Hub.unity — grid of vendor buttons       ║   (§25.0; each button → a vendor
   │   → Vendor / Blacksmith / Alchemist /     ║    scene, "Back" returns to Hub)
   │     Equip / Party / Abilities → back      ║
   ↓                                           ║
Game.unity — clear ALL waves of the stage      ║   (waves spawn in sequence)
   ↓                                           ║
PostBattleScreen — XP + items + gold awarded   ║
   ↓                                           ║
└════════ back to StageSelect (next stage now unlocked, on top) ═╝
```

- **Stage = waves.** A stage runs its waves in sequence (`StageLibrary`); clearing the **last** wave ends the battle → PostBattle. Beating a stage unlocks the next, which appears **on top** of the list (§22.3).
- **Reward beat.** PostBattle awards XP, items, and gold, commits the save, then returns to StageSelect.
- **Hub is a launcher, not a mega-screen.** From StageSelect the player can open **`Hub.unity`** — a simple **grid of buttons**, one per vendor, that forwards into that vendor's own scene (§25.0). It replaces the floating `VendorNavBar` as the primary way to reach vendors. (Long-term the vendor *UIs* may compose into one screen — §25.9 — but that's separate from this lightweight launcher.)
- **Failure path**: all heroes die in Game → PostBattleScreen with "Defeat" → StageSelect (no permadeath V1; the run can be retried).
- **No Overworld.** There is no world-map / exploration scene. Stage navigation is the scrollable level list in StageSelect (§22.3). (A stray `Overworld.unity` file may linger in the project; it is dead — ignore it.)

`SceneHelper.Fade.ToX()` / `SceneHelper.Switch.ToX()` are the canonical scene-switch entry points.

### 22.1 Per-transition state contract

Each scene transition has a **side-effect contract** — what state must be committed before the switch, what hydrates in the next scene's `Awake`. Listing the contracts keeps state from leaking into the wrong scope:

| From → To | Commit before | Hydrate on arrival |
|---|---|---|
| `Game` → `PostBattleScreen` | `BattleResult` (winners, XP earned, gold dropped, items rolled) into a static carrier `BattleResultCarrier.Pending` | PostBattle reads `Pending`, plays reveal anim, then calls `SaveStateService.ApplyBattleResult(...)` (XP add, HP carry-over, inventory add) |
| `PostBattleScreen` → vendor (any) | Saved profile already has the new XP/HP/inventory | Vendor scene Awake: `PlayerInventory.HydrateFromCurrentSave()` |
| Vendor → Vendor (via NavBar) | Active vendor commits its inventory mutations to `ProfileHelper.CurrentProfile.CurrentSave` | New vendor hydrates from same save |
| Vendor → `StageSelect` | Commit (same as above) | StageSelect reads `StageProgress` for unlocks |
| `StageSelect` → `Game` | Picked stage id placed on `StageCarrier.Pending` | `GameBuilder` reads stage id at scene build, spawns the correct enemies via `EnemyDataLibrary` |

### 22.2 Failure path detail

When all heroes hit `HP <= 0`:
1. `TurnManager.CheckBattleEnd()` detects party wipe → posts `BattleResult{Outcome=Defeat}` to `BattleResultCarrier`.
2. `PostBattleScreen` shows "Defeat" banner, plays sad fanfare, and does **not** apply XP or rewards (per V1 — defeat is a retry, not a permanent loss).
3. Heroes' HP is restored to MaxHP (since defeat doesn't carry wounds).
4. "Continue" → `SceneHelper.Fade.ToStageSelect()`.

(No permadeath in V1. Future: a roguelike mode where defeat ends the run and clears the save slot.)

### 22.3 StageSelect — the scrollable level list

Stage navigation is a **vertically scrollable list of levels**, same look-and-feel as the load/save-file screen (`SaveFileSelect`) but for picking the next battle. It is the *only* navigation surface — there is no world map.

**Behavior:**
- **Newest-on-top.** Each newly-unlocked level is **prepended to the top** of the list; older/earlier levels scroll below. The most recent frontier is always the first thing the player sees.
- **All unlocked levels stay replayable.** Clearing a stage unlocks the next but does **not** consume or grey out the cleared one. The player can scroll down and re-enter any previously-beaten level at will.
- **Farming is the point.** Because cleared stages remain replayable and each enemy class has its own drop table (§24.7), the player goes back to a specific stage to farm a specific material an enemy there drops — e.g. re-run the Frost stage for Ice Shards to fund a Blacksmith upgrade. This is the intended grind loop; the list is built to support it, not to lock progress behind one-shot stages.
- **Unlock gating (linear frontier, open backtrack).** A stage is unlocked when the prior stage is cleared (`HighestClearedStageIndex`, `CampaignStages.IsUnlocked`). So progression is linear *forward* (you can't skip ahead), but fully *open backward* (every unlocked stage is freely re-enterable). Locked (not-yet-reached) stages render dimmed/disabled.

**Row content (per level):** name, theme/biome, recommended level or difficulty pip, a cleared ✓ marker, and a hint of the notable drops/enemies so the player knows where to farm what. Tapping a row → `StageCarrier`/`StageSaveData.CurrentStage` set → fade to `Game`.

Implementation lives in `StageSelectManager` / `StageSelectBuilder`; unlock data in `StageLibrary` + `CampaignStages`.

## 23. Character Classes

Heroes and enemies share the `CharacterClass` enum in `Helpers/CharacterClass.cs`. What separates a hero from an enemy is the `ActorData.Tags` flag (`Hero` vs `Enemy`) plus their AI/control path.

### 23.1 Class identity (what a class encodes)

- **Base stats** (`ActorData.BaseStats`) — STR, VIT, AGI, SPD, STA, INT, WIS, LCK.
- **Stat growth** (`StatGrowth` per level + `MilestoneStatGrowth` at fixed levels).
- **Portrait** + `ThumbnailSettings` + `CanvasThumbnailSettings`.
- **Tags** (`Hero / Enemy / Humanoid / Soldier / Beast / Boss / Mechanical` + elemental affinity flags).
- **Elemental resistance** (`ActorData.Resistances`) — per-`DamageType` multiplier.
- **Default AbilityBar loadout** — `HeroLoadouts.perClass` keyed by `CharacterClass`.
- **Color affinity** (TODO) — when this hero contributes to a pincer harvest, the dropped orb is this color. V1 placeholder: all Blue.
- **Story role / dialogue** (TODO) — placeholder.

### 23.2 V1 hero roster (seeded loadouts)

| Class | Identity | Stat lean | Color affinity (planned) | Loadout |
|---|---|---|---|---|
| **Cleric** | white-magic healer; sustain-focused; reads enemy intentions | INT/WIS high, STR low | White | Heal, Heal, Frost, Potion(3) |
| **Paladin** | front-line tank with healing on the side | VIT/STR high, mid WIS | White/Red | Heal, Fireball, Potion(3) |
| **Barbarian** | high-damage front line; brute force | STR/VIT high, low INT/WIS | Red | Fireball, Bolt, Potion(3) |
| **Alchemist** | utility / consumable stacks / non-magical control | INT/AGI mid, high LCK | Blue/Green | Frost, Potion(5), Steal, Heal, Potion(5) |
| **Assassain** | high-damage flanker; rogue toolkit | AGI/LCK high | Black | Steal, Mug, Bolt, Potion(3) |
| **GreenNinja** | mobility specialist; thief variant | AGI/LCK high | Green | Teleport, Steal, Fireball, Potion(3) |
| **RedNinja** | mobility + striker | AGI/STR high | Red | Teleport, Mug, Bolt, Potion(3) |

Color affinity is **planned** — when implemented, completing a pincer will drop an orb of the participating heroes' colors instead of all-Blue placeholders.

#### 23.2.0 Signature moves + design rationale

Each hero should have ONE thing that's distinctly theirs, and a small kit that telegraphs the fantasy. Below is what the V1 loadouts are *trying* to express — when tuning, hold these intents.

| Class | Signature | What "I'm playing X" should feel like | Anti-pattern to avoid |
|---|---|---|---|
| **Cleric** | back-line topple recovery via Heal/MassHeal/Antidote | "I keep the party alive even when it shouldn't be alive." Plays away from melee, dipping in only to flank-finish. | Cleric stuck front-line, can't reach the wounded — UI hint should suggest moving them back. |
| **Paladin** | hybrid tank-with-heals; soaks pincer hits | "I body-block and patch myself." Mid-row presence. | Paladin out-DPS'ing the Barbarian; means STR scaling too high. |
| **Barbarian** | row-clearing Fireball + Bolt; high HP pool | "I delete the enemy front rank." | Barbarian taking too many turns to set up — needs to feel decisive. |
| **Alchemist** | per-slot stack economy + Steal | "I never run out of resources." Carries doubled Potions; Steal makes mana stretch. | Alchemist outdamaging mages; should *enable* burst, not deliver it. |
| **Assassain** | Mug — Steal + damage in one click | "Every turn I both hit and harvest." Wants adjacency. | Assassain too tanky; should feel risky. |
| **GreenNinja** | Teleport into pincer formation | "I get into the right spot a turn faster than anyone." | Teleport free-flying without consequence — pincer-completion is the *reward*, not a guarantee. |
| **RedNinja** | Teleport + Mug — relocate to steal | "I'm in their backline before they finish loading." | Same as Assassain — fragility is the trade. |

#### 23.2.1 Class identity rules of thumb

- **A class should have ONE thing it does better than every other class**, plus a secondary cope.
- Stat leans should be lopsided enough that swapping a Cleric for a Barbarian *changes the feel*.
- Per-class color affinity dictates which orbs the party gathers — running 3 Paladins (W/W/W) gives a different bank profile than 1 Cleric + 1 Assassain + 1 RedNinja (W/B/R).
- Per-class abilities should reference class identity: a Cleric's bar should bias healing/cleanse; a Ninja's should bias mobility/theft.

#### 23.2.2 Classes not yet in `HeroLoadouts.perClass`

Every other entry in the `CharacterClass` enum falls through to the default `ManaAbilities.Slots` (Heal/Fireball/Frost/Bolt/Potion/—). Add entries via `HeroLoadouts.Set(class, list)` to give them distinct kits. Candidates:

- **BlackNinja / BlueNinja / WhiteNinja / YellowNinja / ChromaNinja** — variants of the Ninja archetype; each should feel different (poison-specialist, ice-specialist, etc.).
- **Bruiser** — slow brute, even more lopsided than Barbarian; STR/VIT maxed.
- **Captain** — buffing leader; gives allies Protection at battle start (passive).
- **Alchemist already done.** A "Druid" / nature-class for Green would round out the palette.

### 23.3 Enemy classes

Enemies aren't playable. Their `ActorData` defines stats, drop table, abilities (per `Ability` legacy class, see §6 + §14), AI behavior. `EnemyPlanner.PlanStep` drives tile-by-tile moves. **Humanoid enemies actively seek pincers** (§14.1); non-Humanoid use the straight-line approach.

### 23.4 Future: roster + party composition

Long-term plan: the player has a roster of unlocked classes and assembles a party of up to 4–5 from the roster per battle. The Party vendor scene (§25.5) is where this happens. Currently the active party is fixed by save state.

## 24. Equipment, Items, Materials, Currency

(Expanding §10.)

### 24.1 Item types (`ItemType` enum)

- `Equipment` — wearable; takes an `EquipmentSlot`.
- `Consumable` — single-use stackable (potions, scrolls, throwables).
- `CraftingMaterial` — recipe input (monster fang, iron ingot, fire essence, mana shard…).
- `Currency` — gold etc. (currency itself doesn't stack as item; tracked on the `SaveState`).
- `Relic` — special equipment in a Relic slot; usually grants a passive.

#### 24.1.1 Rarity tiers

`ItemRarity` enum drives shop price, drop weight, stat range, and `HubItemRowFactory.RarityColor`:

| Rarity | Color | Cost multiplier | Stat range (per primary stat) | Drop weight bucket |
|---|---|---|---|---|
| **Common** | white-grey | 1.0× | +1 to +3 | most enemies, all stages |
| **Uncommon** | green | 2.5× | +3 to +6 | mid-tier enemies + low-tier vendors |
| **Rare** | blue | 6× | +5 to +10 | elite enemies, mid vendors |
| **Epic** | purple | 15× | +8 to +15 | bosses, high-tier crafting |
| **Legendary** | gold | 40× | +12 to +25 (sometimes unique passives) | end-game bosses, ultimate crafting |

Total stat-budget per piece scales roughly geometrically; epic+ pieces tend to carry one *named passive* (e.g., "+1 mana orb at battle start" on Mage Robe Epic variant) instead of pure stats.

### 24.2 Equipment slots

`EquipmentSlot` enum: `Weapon, Armor, Helm, Boots, Relic1, Relic2, Relic3, Accessory`. A hero's `HeroLoadout : Dictionary<EquipmentSlot, ItemDefinition>` is persisted in `HeroEquipmentSave`. `Formulas.ComputeEquipmentBonus(loadout)` aggregates the equipped pieces' stat bonuses into the hero's combat stats.

### 24.3 ItemDefinition fields

```
Id, DisplayName, Description, Type, Slot, Rarity (Common→Legendary),
BaseCost, MaxStack, Durability,
Strength, Vitality, Agility, Speed, Stamina, Intelligence, Wisdom, Luck,
// Planned new fields:
BattleStartManaOrbs : int        // Mage/Wizard Robe → adds N random orbs to bank at battle start
OnUseSpellName     : string      // Sleep Dart etc. → consumable that triggers a spell on use
ResistanceModifiers: Dict<DamageType, float>  // elemental rings, etc.
```

### 24.4 Inventory

`PlayerInventory` — item ID → `Entry(count, durability)`. Per save file. Hub vendor scenes hydrate from `ProfileHelper.CurrentProfile.CurrentSave` on Awake, persist on commit.

### 24.5 Weapon durability

Per the locked rule ([[project_weapon_durability_rule]]) — **all built** in `WeaponDurabilityHelper.cs`:
- A weapon takes durability damage per use.
- At durability 0, it **shatters** — deals damage to **both** the target (×1.5 bonus) AND the wielder (15% MaxHP self-damage), then clears the slot (`WeaponDurabilityHelper.cs:37-103`).
- Each repair drops effective max durability by 1 (`EffectiveMaxDurability = max(1, Durability − repairCount)`) and per-point repair cost escalates ×1.6, so gear naturally retires (`WeaponDurabilityHelper.cs:105-138`).

### 24.6 Crafting recipes

`CraftingRecipe`: list of `(materialId, count)` + gold cost → `ItemDefinition` result.
- `RecipeLibrary.All()` catalogs them.
- `CraftingRecipe.CanCraft(inventory)` / `.Execute(inventory)` for atomic check + commit.

#### 24.6.1 Example progression: Iron Sword → Steel Sword → Mythril Blade

```
 ┌───────────────────────────────────────────────────────────────┐
 │ Common   │ Iron Sword (+3 STR, dura 30)                       │
 │          │ Forge: 2 Iron + 1 Wood + 100g                      │
 ├──────────┼────────────────────────────────────────────────────┤
 │ Uncommon │ Steel Sword (+6 STR, dura 40)                      │
 │          │ Upgrade FROM Iron Sword: 3 Iron + 1 Coal + 250g    │
 │          │   ─OR─ Forge: 4 Steel Ingot + 2 Wood + 400g        │
 ├──────────┼────────────────────────────────────────────────────┤
 │ Rare     │ Mythril Blade (+10 STR, dura 50, +5% crit)         │
 │          │ Upgrade FROM Steel Sword: 2 Mythril + 1 Star Dust  │
 │          │   + 1000g                                          │
 ├──────────┼────────────────────────────────────────────────────┤
 │ Epic     │ Mythril Blade +1 (+13 STR, dura 50, +10% crit,     │
 │          │   passive "Strikethrough" — pincer hits all in line)│
 │          │ Upgrade: 1 Phoenix Feather + 2500g                 │
 └──────────┴────────────────────────────────────────────────────┘
```

Each upgrade consumes the previous tier (you don't keep the Iron Sword after upgrading) — keeps inventory lean and gives the Blacksmith something to do all game.

### 24.7 Drops

`DropTable` per enemy class (in `DropTableLibrary`). On death (`CoinManager.TrySpawnOnDeathThreshold` during fade-out), coins spawn at the actor's position and fly to the CoinCounter; material drops appear similarly.

#### 24.7.1 Material economy at a glance

```
 ENEMIES ──────────► drop tables ──────────► PlayerInventory
   │                                              │
   │                                              ▼
   │                                  ┌─ Blacksmith ─► Equipment
   │                                  │   (forge, upgrade)
   │                                  │
   └─► Boss + Elite drop ─────────────┼─ Alchemist ──► Consumables
                                      │   (brew)
                                      │
                                      └─ Vendor ──────► Gold (sell)
                                            │
                                            ▼
                                       (buy more stuff)
```

Materials don't directly enter combat — they're the bridge between battle output and vendor-augmented loadouts. Gold is the universal solvent; materials are the rate-limit.

### 24.8 Specific items user-spec'd

- **Mage Robe** — armor, common. `BattleStartManaOrbs = 2`. Stacks per hero wearing.
- **Wizard Robe** — armor, uncommon. `BattleStartManaOrbs = 3`. Stacks per hero wearing.
- **Sleep Dart** — consumable, per-slot stack (e.g. `MaxStackSize = 5`). `OnUseSpellName = "Sleep"` — on use, opens the Sleep spell's targeting flow and consumes one charge.

### 24.9 Currency

Gold is the universal currency. Lives on `SaveState`. Coin pickups via `CoinManager`. Vendor purchases / Blacksmith forging / Alchemist brewing deduct gold; selling at vendor returns ~50% `BaseCost`.

## 25. The Hub: Vendor Scenes

Six dedicated scenes, each with its own `<X>Builder.cs` + `<X>Manager.cs` + `PlayerInventory` hydration. The old *monolithic* `Hub.unity` was deleted in the scene-per-section migration ([[project_scene_per_section_migration]]); `Hub.unity` is now **re-created as a lightweight launcher** (§25.0), not a mega-screen.

### 25.0 Hub.unity — the vendor launcher (grid of buttons)

`Hub.unity` / `HubBuilder.cs` / `HubManager.cs`. The hub is a **plain grid of buttons**, one per vendor — nothing more. Reached from StageSelect via a "Hub" button; each button fades into that vendor's own scene; the vendor's "Back" returns to the Hub.

```
┌──────────── The Hub ────────────┐
│  [  Vendor  ] [ Blacksmith ]    │
│  [ Alchemist] [   Equip    ]    │   ← grid of 6 buttons (2 cols × 3 rows)
│  [  Party   ] [ Abilities  ]    │
│            [ ← Back ]           │   → StageSelect
└─────────────────────────────────┘
```

- **Layout:** a `GridLayoutGroup` of equal-size buttons, themed via `HubTheme` (navy panels, gold accents), under the §26.2 CanvasScaler + AspectGuard. Each button: vendor icon + name.
- **Navigation:** button → `SceneHelper.Fade.To<Vendor>()`; this is the primary path to vendors and **replaces the floating `VendorNavBar`** as the main navigation. (The NavBar may stay as an in-vendor quick-jump, but the Hub grid is the canonical launcher.)
- **No shopping logic in the Hub** — it only routes. All buy/sell/craft happens in the destination vendor scene.

### 25.1 Vendor (general merchant) — the standardized shop pattern

`Vendor.unity` / `VendorManager.cs`. **This is the canonical menu-based shop** — a classic JRPG (Final Fantasy-style) flow. It is deliberately *standardized*: the same component drives buy/sell/buyback so the player learns it once and every vendor feels identical. No bespoke per-vendor layout.

**Three tabs:** `Buy` · `Sell` · `Buyback`.

**The item list (all three tabs share one layout).** A vertical, scrollable list of rows, each a clean **multi-column** read — never a single cramped string:

```
│ [icon]  Name (rarity-colored)            owned ×N     25g │
```
- **Icon** — item sprite (left).
- **Name** — rarity-colored (`HubItemRowFactory.RarityColor`).
- **Owned ×N** — how many the player already holds (right-aligned middle column).
- **Unit price** — gold, right column, colored by affordability (`HubTheme.ColorByAffordable`): gold if affordable, red if not.

**Select → quantity → confirm.** Tapping a row selects it (highlight + ▶). A **quantity stepper** appears (`− [ N ] +`, with a "Max" affordance that fills to gold-limit on Buy or owned-count on Sell). A **running total** (`Pay: 75g  |  Gold: 124g`) updates live in the footer. The footer **action button** commits the whole quantity at once (`Buy ×3` / `Sell ×3`).

**Pricing:**
- **Buy** = `BaseCost × rarity multiplier` per unit (§24.1.1). Refused (button disabled, total in red) if the player can't afford the selected quantity.
- **Sell** = **50% of `BaseCost`** per unit, rounded. Only items with `BaseCost > 0` and `count > 0` are sellable.
- **Buyback** = a **session-scoped stack** of everything sold this visit. Re-purchasing returns the item at the **exact gold it was sold for** (a friendly undo for fat-finger sells). The buyback list clears on leaving the vendor; selling pushes onto it, buying-back pops from it.

**Stock:** Buy tab lists `Inventory.All()` entries with `BaseCost > 0` (generic stock V1; future: stage-progressed). Inventory cap: TODO.

**Layout discipline (why vendors kept looking broken — see §17.1 #11/#12):** every vendor Canvas MUST use the §26.2 CanvasScaler (`referenceResolution = (1170, 2532)`, match 0.5) and sit under the AspectGuard (US-001); the scroll list's `ScrollRect` MUST be fully wired (`content`, `viewport`, `vertical = true`, `horizontal = false`, movementType Clamped); all colors come from `HubTheme`, never hand-typed `new Color(...)`. The standardized shop is built once as a shared `ShopView` (Canvas/`ShopView.cs` + factory) that every vendor instantiates with `(catalog, ownedInventory, priceFn)` — see `user_stories.md` US-111.

### 25.2 Blacksmith

`Blacksmith.unity` / `BlacksmithManager.cs`. Three workflows:
1. **Forge** — combine materials + gold → new weapon/armor per `CraftingRecipe`. Pulls from `RecipeLibrary.All().Where(r => r.ResultItemId is equipment)`.
2. **Upgrade** — improve an existing piece's stats (per `UpgradeLibrary`). Each upgrade appends `_plus` to the item id and bumps stat tiers.
3. **Repair** — restore weapon durability. Each repair erodes the max by 1 (§24.5).

### 25.3 Alchemist

`Alchemist.unity` / `AlchemistManager.cs`. Brews consumables from materials + gold per recipes. Pulls `RecipeLibrary.All().Where(r => r.ResultItemId is consumable)`.

- **Enchant** (built, `EnchantLibrary.cs:58-174`) — apply one of 4 elemental affinities (Flame/Frost/Spark/Shadow) to a base weapon; each recipe = 1 element-essence + 2 ArcaneDust + 150g, elevates rarity and adds element-themed stats.

### 25.4 Equip

`Equip.unity` / `EquipManager.cs`. Pick a hero → drag/select items from inventory into `EquipmentSlot`s. Live-previews stat changes via `Formulas.ComputeEquipmentBonus`.

### 25.5 Party

`Party.unity` / `PartyManager.cs`. Manage party composition — add/remove heroes from the active battle squad. Future cap: 4–5.

### 25.6 Abilities

`Abilities.unity` / `AbilitiesManager.cs`. Per-hero `AbilityBar` editor. Pick a hero → assign Skills / Spells / Items to their 6 slots. Currently `HeroLoadouts.perClass` is the data source; long-term this scene will hydrate from `HeroEquipmentSave.AbilityBarSlots` for true per-hero (not just per-class) loadouts.

### 25.7 Cross-vendor utilities

- `Hub/HubTheme.cs` — shared palette, `FormatGold(amount)`, `ColorByAffordable(cost, gold)`.
- `Hub/HubToast.cs` — transient notifications ("Item bought", "Not enough gold").
- `Hub/HubItemRowFactory.Create(container)` — standard row layout for buy/sell lists; `HubItemRowFactory.RarityColor(rarity)` for the rarity tint.
- `Editor/Builders/VendorNavBarBuilder.cs` — the floating hamburger nav bar (`VendorNavBar`) at the top of every vendor scene; click → fade to another vendor.

### 25.8 Per-screen UI sketches

Each vendor follows the same skeleton: NavBar at top, scene-specific body in the middle, action row at bottom. Sketches below show the body for clarity.

**Vendor (standardized FF-style shop — §25.1)**
```
┌──[≡ NavBar]─────────────────────── 💰 1,240g ─┐
│  ┌ Buy ┐┌ Sell ┐┌ Buyback ┐                   │  ← three tabs
│ ┌──────────────────────────────────────────┐ │
│ │ [▤] ▶ Health Potion        owned ×2   25g │ │  ← icon · name(rarity) · owned · price
│ │ [▤]   Mana Potion          owned ×0   35g │ │
│ │ [▤]   Iron Sword           owned ×1  200g │ │     scrollable list
│ │ [▤]   Steel Sword (rare)   owned ×0  900g │ │     (price red if unaffordable)
│ │  ...                                       │ │
│ └──────────────────────────────────────────┘ │
│  Qty:  [ − ]  3  [ + ]  [Max]                 │  ← stepper on the selected row
│  Pay: 75g  |  Gold: 1,240g          [ Buy ×3 ]│  ← live total + commit
└────────────────────────────────────────────────┘
   Sell tab → "Sell ×N" returns 50% BaseCost; sold items go to Buyback.
   Buyback tab → repurchase at the exact price you sold for (session undo).
```

**Blacksmith**
```
┌──[≡ NavBar]──────────────────────────────┐
│ ┌─ Forge ─┐┌─ Upgrade ─┐┌─ Repair ─┐      │
│ │ Recipe: Iron Sword                  │
│ │   Inputs: ×2 Iron, ×1 Wood, 100g    │
│ │   Owned:  ×3 Iron, ×0 Wood ✗        │  ← red on missing
│ │ [Craft] (disabled)                  │
│ └────────────────────────────────────┘    │
└──────────────────────────────────────────┘
```

**Alchemist** mirrors Blacksmith but for consumables.

**Equip**
```
┌──[≡ NavBar]──────────────────────────────┐
│ ┌─ Hero list ─┐  ┌─ Slots ─────────────┐  │
│ │ ▶ Cleric     │  │ Weapon: Iron Sword  │  │
│ │   Knight     │  │ Armor : Leather    │  │
│ │   Mage       │  │ Helm  : —          │  │
│ │   Ninja      │  │ Boots : —          │  │
│ └──────────────┘  └────────────────────┘  │
│            ┌─ Inventory grid ─┐           │
│            │ [Iron][Steel][Wood] …       │
│            └────────────────────────────┘ │
│  Stat delta preview: STR +3, DEF +1       │
└──────────────────────────────────────────┘
```

**Abilities** (per-hero AbilityBar editor)
```
┌──[≡ NavBar]──────────────────────────────┐
│ Hero: ▶ Cleric                          │
│ Bar: [Heal] [Antidote] [Pot] [—] [—] [—] │  ← 6 slots
│ ┌─ Known Skills / Spells / Items ──┐     │
│ │ ▶ Heal (spell)                  │     │
│ │   Antidote (spell)              │     │
│ │   Mass Heal (spell)             │     │
│ │   Health Potion (item ×5)       │     │
│ └────────────────────────────────┘      │
│  [Drag onto a slot or tap to assign]    │
└──────────────────────────────────────────┘
```

**Party** (battle roster)
```
┌──[≡ NavBar]──────────────────────────────┐
│ Active (4): [Cleric][Knight][Mage][Ninja]│
│ Reserve   : [Paladin][RedNinja]          │
│  [Tap to swap between Active/Reserve]    │
└──────────────────────────────────────────┘
```

### 25.9 The merged hub (long-term goal — distinct from §25.0)

> **Not the same as the §25.0 launcher.** §25.0 `Hub.unity` is a button-grid that *routes to* separate vendor scenes — that's the V1 navigation and it ships now. §25.9 is the *optional later* step of folding the vendor **UIs themselves** into one screen (tabs/panels, no scene loads). Build §25.0 first; §25.9 only after every vendor is independently stable.

**Intent:** eventually fold all six vendor scenes into a **single composed hub `.unity`** — one screen where the player switches between Vendor / Blacksmith / Alchemist / Equip / Party / Abilities as tabs/panels rather than separate scene loads.

**Sequencing — deliberately NOT yet.** Each vendor stays its **own** scene + builder + manager until it is individually stable and works independently. Only then do they compose. Rationale:
- Merging unstable screens multiplies the surface area of any one bug across all six.
- The shared utilities (`HubTheme`, `HubToast`, `HubItemRowFactory`, `VendorNavBar`) already give a consistent look so the eventual merge is layout composition, not a rewrite ([[project_scene_per_section_migration]]).

**Known pain (flagged by the user 2026-05-30):** building these shopping-interface scenes via the `*Builder.cs` pipeline is disproportionately fiddly/error-prone relative to how simple a buy/sell list *seems*. Treat vendor-builder churn as a first-class hazard — keep each builder minimal, lean on the shared factories, and don't attempt the merge while individual builders are still thrashing. When a vendor builder misbehaves, fix it in isolation rather than touching neighbors. (If this keeps biting, a candidate root-cause investigation is worth a dedicated pass — see `user_stories.md`.)

## 26. Responsive Design & Aspect Ratio Profile

The game's UI design is locked to **portrait mobile**. On a device with a different aspect ratio, the game must **never stretch or squash** — it letterboxes / pillarboxes to preserve the layout, the way classic console games render on modern wide-screens with side bars.

### 26.1 Reference resolution

**`1170 × 2532`** (iPhone-tall portrait, aspect ≈ 0.4625). All `HudLayout` constants and every builder's `RectTransform` math assume this. Treat it as inviolable when authoring builders.

### 26.2 CanvasScaler baseline

Every `Canvas` ScreenSpaceOverlay uses:
- `uiScaleMode = ScaleWithScreenSize`
- `referenceResolution = (1170, 2532)`
- `screenMatchMode = MatchWidthOrHeight`
- `matchWidthOrHeight = 0.5`

This keeps element sizes proportional across devices without changing layout coordinates.

### 26.3 Aspect-ratio guard ("lock to profile")

The reference aspect is `1170/2532 ≈ 0.4625`. Wider or narrower devices get pillarboxed or letterboxed:

| Device family | Resolution (typical) | Aspect | Strategy | Bars |
|---|---|---|---|---|
| iPhone 14/15 Pro (portrait) | 1179×2556 | 0.461 | direct fit | none |
| iPhone 8 / SE (portrait) | 750×1334 | 0.562 | mild **pillarbox** | ~6% each side |
| Android tall phones (portrait) | 1080×2400 | 0.450 | direct fit | none |
| Pixel Fold (unfolded, portrait) | 2208×1840 | 1.20 | heavy **pillarbox** | ~60% of width is bars |
| iPad portrait | 1668×2388 | 0.698 | **pillarbox** (significant) | ~17% each side |
| iPad landscape | 2388×1668 | 1.431 | heavy **pillarbox** | ~68% of width is bars |
| Landscape phone | 2400×1080 | 2.222 | very heavy **pillarbox** | gameplay strip in center |
| Ultra-tall portrait | 1080×2520 | 0.428 | mild **letterbox** | ~6% top/bottom |
| Square (rare) | 1:1 | 1.000 | **pillarbox** | ~54% each side |

The above is the design contract. Aspect ratios narrower than 0.4625 letterbox; wider pillarbox. The game **never** stretches to fill — it would ruin the HUD's vertical-row composition.

**Implementation plan** (planned, not yet built):
- A `Utilities/AspectGuard.cs` MonoBehaviour. Each `OnRectTransformDimensionsChange`, resize its own RectTransform to the **largest centered rectangle** that fits the parent Canvas while preserving aspect `0.4625`.
- Insert `AspectGuard` as the FIRST child under every Canvas (in every builder). All HUD content reparents under it.
- A full-screen black `Image` sits BEHIND the AspectGuard so the bars render solid black.
- The world camera (board) sets its `viewport rect` to the AspectGuard's screen rect so the board only renders within the guard, never into the bars.

### 26.4 Camera framing

- The world camera is orthographic. `orthographicSize` is set so the 6×8 board fits with a configured margin inside the AspectGuard.
- Camera viewport rect = AspectGuard screen-rect (normalized).
- Camera `clearFlags = SolidColor, backgroundColor = black` so anything outside the viewport renders black (the letterbox/pillarbox).
- A separate Overlay Camera (added by GameBuilder) renders UI-only and uses the same viewport.

### 26.5 Safe area (notch / cutout)

Modern phones have rounded corners + camera notches. AspectGuard insets its rect by `Screen.safeArea` so HUD content respects them. The letterbox bars + background art can still extend to the screen edge.

### 26.6 AspectGuard implementation sketch

When the AspectGuard TODO is picked up (§16.4 P0), the work shape:

```csharp
// Utilities/AspectGuard.cs
using UnityEngine;

namespace Scripts.Utilities
{
    /// <summary>
    /// ASPECTGUARD - Locks a child RectTransform to the reference aspect (1170/2532).
    /// Resizes itself to the LARGEST CENTERED RECT that fits its parent Canvas while
    /// preserving aspect; the leftover space pillarboxes/letterboxes (filled by a black
    /// Image behind, sized to the parent).
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public class AspectGuard : MonoBehaviour
    {
        public const float ReferenceAspect = 1170f / 2532f;  // ≈ 0.4625

        private RectTransform self;
        private RectTransform parent;

        private void Awake() {
            self = (RectTransform)transform;
            parent = self.parent as RectTransform;
            Fit();
        }

        private void OnRectTransformDimensionsChange() => Fit();

        private void Fit() {
            if (self == null || parent == null) return;
            var pSize = parent.rect.size;
            var safe  = Screen.safeArea;             // pixels in screen space

            // Convert safe area → fraction of parent
            float safeW = safe.width  / Screen.width;
            float safeH = safe.height / Screen.height;
            var availW = pSize.x * safeW;
            var availH = pSize.y * safeH;

            // Largest centered rect with ReferenceAspect
            float byW = availW;
            float byH = availW / ReferenceAspect;
            if (byH > availH) { byH = availH; byW = availH * ReferenceAspect; }

            self.anchorMin = self.anchorMax = new Vector2(0.5f, 0.5f);
            self.pivot = new Vector2(0.5f, 0.5f);
            self.sizeDelta = new Vector2(byW, byH);

            // Notify world camera to clamp viewport to our screen rect
            CameraViewportSync.SetGuardRect(self);
        }
    }
}
```

Companion `CameraViewportSync.cs` reads `AspectGuard`'s screen rect each frame and applies it to `Camera.main.rect`. Behind the AspectGuard: a full-canvas-size black `Image` so the bars render solid black.

Insertion point in every builder:
```csharp
// In each *Builder.Build(), AFTER creating the Canvas + CanvasScaler:
var guardGO = new GameObject("AspectGuard", typeof(RectTransform), typeof(AspectGuard));
guardGO.transform.SetParent(canvasGO.transform, false);
var guardRT = (RectTransform)guardGO.transform;
guardRT.anchorMin = Vector2.zero;
guardRT.anchorMax = Vector2.one;
guardRT.offsetMin = guardRT.offsetMax = Vector2.zero;
// All subsequent HUD content uses guardGO.transform as parent instead of canvasGO.transform.
```

A black-bars background image (`AspectBars`) goes BEHIND `AspectGuard` (sibling, lower index) sized to the canvas. World camera's `Camera.rect` is updated from `AspectGuard`'s screen rect on every change.

### 26.7 Status

The above is the **design intent**. Current implementation:
- §26.2 (CanvasScaler) — ✅ done in every builder.
- §26.3–§26.6 (AspectGuard, viewport math, safe area) — ❌ TODO. Tracked in §16.4 P0.

## 27. (removed) Dialog & Story — cut from the design

> **Cut 2026-05-30.** No dialog/story/cutscene system in the design. Vendors are UI-only (no shopkeeper voice); battles have no character lines. If a narrative layer is ever wanted it will be designed fresh — there is no preserved spec to build against. (Section number kept as a tombstone so later cross-refs don't shift.)

## 28. (removed) Overworld — cut from the design

> **Cut 2026-05-30.** No world-map / exploration scene. Stage navigation is the **scrollable level list** in StageSelect (§22.3): newest-on-top, every unlocked level freely replayable for farming. A stray `Overworld.unity` file may linger in the project but is dead and ungated. (Section number kept as a tombstone.)

## 29. Open Design Questions

The bible is the resolved answer; this section is the **queue** of decisions still pending. Resolve a question → move its answer into the relevant section above + delete the entry here.

### 29.1 Macro loop / run structure

1. **Permadeath vs revive cost** — does a battle loss strip the run or just bounce back to StageSelect?
2. ~~**Stage gating**~~ — **RESOLVED 2026-05-30** → §22.3: linear *forward* (next unlocks on clear), open *backward* (every unlocked stage stays freely replayable for farming).
5. **Difficulty / scaling** — flat per-stage scaling or NG+ system?
6. **Tutorial / onboarding** — does the player get a guided first battle?

### 29.2 Party / classes

7. **Party size cap** — 4 heroes? 5? Variable per stage?
8. **Color identity per class** — each hero contributes their color to a pincer. Which classes are which color?
9. **Per-hero AbilityBar (not per-class)** — when do we migrate from `HeroLoadouts.perClass` defaults to player-assigned bars saved in `HeroEquipmentSave.AbilityBarSlots`?

### 29.3 Content economy

10. **Material drop tables per enemy class** — framework exists (`DropTableLibrary`); per-enemy tables aren't fully populated.
11. **Crafting recipe completeness** — Mage Robe / Wizard Robe / Sleep Dart need recipes + Blacksmith/Alchemist menu entries.
12. **Inn / rest / healing** — between-stage healing free, gold cost, or tied to a specific vendor?
13. **Out-of-battle status** — do debuffs carry between stages or always cleared on PostBattle?
14. **Save autosave cadence** — only at PostBattle, or also on entering a vendor?

### 29.4 UI / presentation

15. **Spell-icon → ability-bar UI** — when do we wire `SpriteLibrary.SpellIcons[name]` into the AbilityBar slot's frame so the bar shows real icons instead of letter labels?
16. **Bestiary unlock gate** — only after first defeat, or always visible?
17. **AspectGuard ratification** — confirm the strategy in §26 before coding the MonoBehaviour.
18. **Soundtrack / audio system** — audio constraints sketched in §30.4 (compression, latency); full system (`AudioManager`, music transitions, SFX routing) is still TBD. Plan or defer?

### 29.5 How to resolve a question

1. Pick a question; talk it through (with Legion if needed).
2. Land the decision in the matching above-section as **prose**, not a question — strike or delete the question here.
3. If the decision implies code work, add a TODO in §16 with priority.
4. If it raises a new question, add it to the right §29 sub-section.

## 30. Performance Budgets

The game targets **60fps on a 4-year-old mid-tier phone** (iPhone 11 / mid-Snapdragon 7-series). Anything that breaks this budget is a bug, not a feature.

### 30.1 Frame budget (16.67 ms @ 60fps)

| Subsystem | Budget | Notes |
|---|---|---|
| Game logic (`Update` / coroutines) | ≤ 3 ms | Includes all manager `Update()` calls combined |
| Rendering (geometry + lighting + post) | ≤ 8 ms | Few overdrawn UI layers; particles capped |
| UI layout + canvas rebuild | ≤ 2 ms | `Canvas.SetDirty` is the cost driver — avoid setting RectTransform values per-frame |
| VFX / particles | ≤ 2 ms | Particle systems pooled; one big burst is fine, sustained per-frame is not |
| Slack | ≥ 1.67 ms | Headroom for spikes |

### 30.2 GC pressure (allocations to avoid)

`g.SequenceManager` runs many coroutines per battle. Each `new` in a hot path becomes GC pressure that surfaces as a hitch.

| Pattern to avoid | Why | Better |
|---|---|---|
| `string.Format` / `$"..."` in `Update` | Allocates per frame | Cache the result; only rebuild when source changes |
| `actor.GetComponent<X>()` in `Update` | Reflection-ish lookup | Cache on `Awake`; re-resolve only on actor swap |
| `List<T>` allocated per call | per-frame garbage | Use a private `_scratch` list, clear-then-fill |
| `LINQ` (`Where`, `Select`, `OrderBy`) in hot paths | allocates iterators | Manual `for` loops over cached lists; or accept the cost ONCE per drop event |
| `Instantiate` outside cap | Guardrail-banned; also slow + GC | Use a `*Factory` with a pool when possible |
| `Vector3` boxing via `params object[]` | autoboxing | Direct overloads |

LINQ is **fine in cold paths** (vendor scenes, `Awake`, scene transitions) — it's the dispatcher / planner / per-frame `Update` calls that matter.

### 30.3 Coroutine hygiene

`SequenceManager.ExecuteRoutine()` is the central event queue. Misuses that cause real bugs:

- **Forever-running coroutine.** A `while(true) yield return null` without an exit condition. Add a `BattleEnd` cancel.
- **Coroutine on a destroyed MonoBehaviour.** When the GameObject is destroyed mid-coroutine, Unity logs an error. Always null-guard `this` after a `yield`.
- **Stacked coroutines from the same trigger.** If a UI button starts a coroutine and the user clicks again before it ends, you get two parallel coroutines. Track via a flag (`bool isRunning`) or kill the previous one.
- **Awaiting a `null` actor.** A targeted spell coroutine that yields on the target — but the target died between yield points. Null-guard after every `yield return`.

### 30.4 Mobile-specific constraints

- **No `Resources.Load`** (guardrail) — everything via Addressables; eliminates startup hitches.
- **Texture atlas the HUD.** All UI sprites in one Addressable pack (label `UI`). Reduces draw calls.
- **Cap particle emission.** Per-spell VFX should emit ≤ 32 particles per second sustained. Bursts up to 100 are fine.
- **Avoid runtime mesh generation.** All meshes built in editor + saved to Addressables.
- **Audio compression.** Music streams (Vorbis ~96kbps), SFX `Decompress On Load` (uncompressed PCM for low-latency).

### 30.5 When in doubt, profile

Unity's Profiler (`Window > Analysis > Profiler`) is the only ground truth. `Application.targetFrameRate = 60` is pinned at app launch by `Scripts.Helpers.Bootstrap.Initialize` (a `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]`, US-004) so the editor matches build behavior from the first frame and spikes are visible immediately. `GameManager` later refines the framerate from the user's saved setting when a battle begins. (Implemented as a startup hook rather than a `Bootstrap.Awake` MonoBehaviour because the start scene is configurable — no single scene's manager is guaranteed to run at boot.)

---

## 31. Accessibility

The 5-color WUBRG mana system + tile-based combat + small mobile touch targets have specific accessibility risks. We commit to:

### 31.1 Color-blindness

- **Mana orbs carry a letter glyph** in addition to color (W/U/B/R/G/C). A red-green colorblind player reads R vs G via the letter, not just the hue.
- **Cost icons** in the AbilityBar render as `(W)(R)` glyph pairs, not pure color swatches.
- **Debuff icons** carry a unique letter in addition to color (`B`urning, `F`rozen, `P`oisoned, etc.) — see §8.5.
- **Health bars** use color + numeric overlay so "yellow vs orange" isn't the only signal.
- **Future**: add a settings toggle that swaps the palette to colorblind-safe variants (Okabe-Ito or Wong palettes).

### 31.2 Motion / VFX sensitivity

- **No screen-shaking by default** beyond mild impact hits (configurable via `VisualEffectManager.IntensityScale`).
- **Reduce-motion toggle** in Settings (planned): clamps shakes to zero, slows or skips long projectile arcs, fades transitions.
- **Avoid stroboscopic flashes.** Lightning VFX should use ≤ 3 flashes/sec and total duration ≤ 0.4s to stay below seizure thresholds.

### 31.3 Touch targets

- **Minimum tap target 44×44 dp** (per Apple HIG / Material). AbilityBar slots, shield button, and tile-pickers all clear this at reference resolution; verify at min-supported resolution (`750×1334` iPhone SE).
- **Generous drag tolerance** on hero drag — release within ½-tile of target snaps cleanly.

### 31.4 Readability

- **Font sizing** uses TextMeshPro `autoSize` so stat blocks scale with the AspectGuard rect.
- **Contrast ratio** ≥ 4.5:1 for body text per WCAG AA (white on dark UI panels is safe; light text on light backgrounds is banned).
- **Combat-text popups** scale up briefly to draw the eye (`PopInTextAnimator`) — a smaller-popup-feel toggle is on the wishlist.

### 31.5 Audio

- **Subtitled SFX** — combat-text doubles as audio-cue confirmation. No important game event is audio-only.
- **Volume sliders** for Music / SFX / UI in Settings; mute toggles independent.

### 31.6 Status

All §31 items are **design commitments** — most code-side work is pending. Track concrete tasks in §16.

---

## 32. Document Discipline (was §30 + §31)

The bible is the **connective membrane**. From this point forward:

### 32.1 When to update

- Every gameplay-affecting code change must update the bible — add, amend, or verify ([[feedback_game_bible]]).
- New mechanic → write the section first if it's complex, then code against the spec.
- Numeric tuning the bible records → update the table when the number changes.
- Removed mechanic → strike it out; never delete (design history matters).
- Open questions live in §29; resolving one moves the resolution into the right section AND deletes the question.

### 32.2 Bible vs memory

| Type | Lives in | Survives |
|---|---|---|
| "What the game IS" (rules, formulas, mechanics) | This bible | All future sessions |
| "What we discussed / why" (rationale, ratified decisions) | `memory/*.md` ([[feedback_game_bible]]) | All future sessions |
| "What's pending now" | §16 + §29 here | Same |
| "What this turn debugged" | Conversation only | Until compaction |

If a feature is in a memory entry but not here, the **memory** wins for "what was discussed" and the **bible** wins for "what's locked in." Resolve disagreement by promoting the memory's resolved bits into the bible.

### 32.3 Who reads this

- **The user** — for sanity-checking that I understand the design.
- **Future-me** (next session) — to skip re-learning.
- **Legion / unattended sessions** — as the spec.
- **A new contributor** — as onboarding.

If a section can't be read by one of those audiences without context, that's a bug — fix it.

— end of document —
