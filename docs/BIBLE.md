---
codex: 1
project: GridGame2026
code: GG
layer: bible
status: living
updated: 2026-10-03
---

# GridGame2026 — Project Bible

> Single source of truth for what GridGame2026 IS, is NOT, and the rules that keep it coherent.
> `README.md` says how to build/run; this says how to think about the system.
>
> The nine sections below are the L0 outline. The detailed design canon lives in
> [Appendix A — Design Canon](#gg-appendix-a) and is cited by `§`-anchor. Structured canon
> (spells, buffs, classes, enemies, item rarities) lives in [`docs/data/*.json`](data/) (L5) —
> prose cites entities by `id` rather than restating fields.

---

## 1. The one sentence {#GG-§1}

A tactical grid RPG where **movement deals zero damage** — you drag light-bearing invaders one tile
at a time across a 6×8 grid of the Undearth, **sliding** displaced actors aside, to trap enemies in
**pincers** (two heroes flanking an unbroken line) on a continuous-time **Timeline**, all in a
code-only / builder-driven Unity 6 project.

The feel target: *Final Fantasy timeline + Disgaea grid + FF8 draw economy.* See
[§North Star](#0-north-star) for the design pillars.

## 2. The product promise {#GG-§2}

- **Position is the only weapon.** Damage is dealt by the *pincer* a new position completes, never
  by the move itself. Every drop asks "does this finish the line?" (geometry → [§1.2 Pincer](#12-pincer-attacks))
- **The slide.** Dragging a hero onto an occupied tile displaces that actor — friend or foe — into
  the tile you just left. Set up flanks, eject allies, feed enemies into kill zones. ([§1.1](#11-sliding-and-displacement))
- **The Timeline is a clock you control.** Enemies "load" left→right; strike a foe whose icon sits
  in the rightmost **Pushback Zone** and their turn is shoved back toward spawn. ([§2 Timeline](#2-the-timeline))
- **Shared, visible mana.** A 12-orb colored `ManaBank` is the whole party's spell budget; pincers,
  crits, and enemy-cast interrupts refill it. ([§3.1 Mana](#31-mana-the-orb-economy), data: [`spells.json`](data/spells.json))
- **Casts ride the Timeline.** A spell with a cast time rides the timeline as a small icon below the
  line and fires when it reaches the trigger; every hit on the caster staggers it, enough stagger
  cancels it, and a rare LCK Clutch lets a dying healer let off one last miracle. ([§13.4](#134-the-interrupt-path))
- **Beyond the battlefield.** Themed campaigns of hand-built stages with a skippable story crawl per
  theme, seven vendor scenes (including the Summon Circle for roster growth), a bounty board, weapon
  durability with shatter rebound, original Undearth lore. ([§22 Macro Loop](#22-the-macro-loop), [§25 Vendors](#25-vendor-scenes))

Target pacing: a battle is 90–180s, a stage ~3–4min, a session 20–30min. ([§0.3](#03-session-shape-target-pacing))

## 3. What it is NOT {#GG-§3}

GridGame2026 is **NOT**:

- **Not turn-based in the JRPG sense** — the Timeline is continuous; thinking time is game-clock time.
- **Not a deck-builder** — AbilityBars are deliberate loadouts, not randomized hands.
- **Not a roguelike** — V1 has no permadeath, no procedural runs; stages are authored, saves persist.
- **Not a gacha / live service** — no pulls, no energy, no premium currency. Heroes are recruited
  deliberately for gold at the Summon Circle.
- **Not multiplayer** — solo offline; no co-op, PvP, or leaderboards in V1.
- **Not a grid-puzzle** — no match-3/Tetris piece-color matching; tile state is just "who's standing there".
- **Not free-form movement** — one tile at a time, cardinal only, no pathfinding or diagonals.
- **Not a stat-spreadsheet** — if a player needs a wiki to build, the design failed.
- **No dialog system, no world map.** The only narrative is a skippable per-theme text crawl
  ([§27](#27-story-crawl-no-dialog)); there is no character dialogue, branching or cutscene, and no
  Overworld — stage navigation is the scrollable StageSelect list ([§28](#28-no-overworld)).

Full rationale: [§0.2 Non-goals](#02-non-goals-what-this-game-is-not).

## 4. Architecture canon {#GG-§4}

```
                         Unity 6000.4.3f1  (C# 9, .NET Standard 2.1, root ns Scripts.*)
                                   |
   +-------------------------------+-------------------------------------------+
   |  Assets/Editor/Builders/*Builder.cs   (SOURCE OF TRUTH for every scene)   |
   |        |  BuilderAutoRebuild [InitializeOnLoad] watcher                    |
   |        v                                                                   |
   |  Assets/Scenes/*.unity   (BUILD ARTIFACTS — never hand-authoritative)      |
   +----------------------------------------------------------------------------+
                                   |  runtime
        +--------------+-----------+-----------+----------------+
        v              v                       v                v
   Managers/       Sequences/              Instances/        Canvas/
   (singletons:    (async combat/UI        (ActorInstance,   (TimelineBar,
   TurnManager,    event queue via         ActorMovement,    AbilityBar,
   PincerAttack-   SequenceManager)        Models/Actor/     ManaOrbLine,
   Manager, Mana-                          ActorStats)       DebuffIconBar)
   PoolManager)    Factories/  (the ONLY place Instantiate() is allowed)
        |
   Services/ (pure-logic: EnemyPlanner, PincerDetector, TargetShapeResolver,
              CastInterruptResolver, BossPhaseRunner)
        |
   Data/ (static defs: SpellLibrary, Buffs, ActorLibrary, ItemLibrary, StageLibrary)
   Helpers/ (GameHelper global accessor `using g = ...`)  Utilities/ (Formulas, RNG, Geometry)
```

### 4.1 Projects / assemblies

- `Scripts.csproj` — runtime game code (`Assets/Scripts/`, assembly definition `Scripts.asmdef`, so
  test assemblies can reference it).
- `Assembly-CSharp-Editor.csproj` — editor code: builders, `CliEntryPoints`, guardrails.
- `Tests.EditMode.csproj` / `Tests.PlayMode.csproj` — Unity Test Framework suites
  (`Assets/Tests/EditMode/`, `Assets/Tests/PlayMode/`).
- `README.htm` is built from `README.md` by `Tools/build-readme.ps1` (not game code).

### 4.2 Domain model — the NOUNS

The board, the timeline, and the resource economy. Structured entities are canon-as-data:

- **Actor** — `ActorInstance` (+ `ActorData` static def, `ActorStats` runtime). Hero or Enemy by
  `Tags`. Stats: STR VIT AGI SPD STA INT WIS LCK. ([§3.2](#32-hp-and-stats))
- **Hero classes** — [`docs/data/classes.json`](data/classes.json) (L5), prose [§23](#23-character-classes).
- **Enemy archetypes** — [`docs/data/enemy_archetypes.json`](data/enemy_archetypes.json) (L5), prose [§14.2](#142-enemy-archetypes-design-palette).
- **Spell / Skill** — [`docs/data/spells.json`](data/spells.json) (L5), prose [§7](#7-the-spell-catalog).
- **Buff / Debuff** — [`docs/data/buffs.json`](data/buffs.json) (L5), prose [§8](#8-buffs-and-debuffs).
- **Item rarity** — [`docs/data/item_rarities.json`](data/item_rarities.json) (L5), prose [§24](#24-equipment-items-materials-currency).
- **ManaBank** — 12 colored orbs (W U B R G C), shared by the party. ([§3.1](#31-mana-the-orb-economy))
- **Timeline / TimelineIcon** — normalized u∈[0,1]; u=1 is the trigger. ([§2](#2-the-timeline))

### 4.3 Key services — the VERBS

- `SelectionManager.Drop` — commits a hero slide; triggers the pincer scan.
- `PincerAttackManager.Check` — scans the whole board for all valid pincers, orders chains. ([§1.2](#12-pincer-attacks))
- `TurnManager` — gates the hero window / queued enemy turn / the third "resolving cast" state. ([§2.6](#26-one-timeline-two-lanes--turn-icons-above-cast-icons-below))
- `TimelineBarInstance` — advances icons, `PushbackOnAttack`, `HastenIcon`, `InterruptCastsByOwner`.
- `SpellEffectDispatcher.Cast` / `ApplyDamage` / `ApplyHeal` — resolves every spell via the
  (shape × mode × filter) triad — adding a spell never touches the dispatcher. ([§6](#6-the-spell-dispatcher))
- `EnemyPlanner.PlanStep` / `PlanCast` — positional AI + charge telegraphs. ([§14](#14-ai))
- `BuffSystem` / `BuffTickManager` — apply / tick / expire-chain buffs. ([§8.3](#83-ticking))
- `SequenceManager` — the async combat/UI event queue everything else feeds.

## 5. The Laws {#GG-§5}

This project **inherits the org-wide house rules** in
[`../MindAttic.HouseRules.md`](../../MindAttic.HouseRules.md) — do not restate them here. The most
load-bearing for this repo: [`HOUSE-LAW-1`](../../MindAttic.HouseRules.md#HOUSE-LAW-1) whole-number
versioning, [`HOUSE-LAW-2`](../../MindAttic.HouseRules.md#HOUSE-LAW-2) soft-disable over delete,
[`HOUSE-LAW-8`](../../MindAttic.HouseRules.md#HOUSE-LAW-8) verified-not-asserted done,
[`HOUSE-LAW-9`](../../MindAttic.HouseRules.md#HOUSE-LAW-9) `psst` only on request. Note GridGame2026
ships **zero LLM features** — [`HOUSE-LAW-4`](../../MindAttic.HouseRules.md#HOUSE-LAW-4) applies to
tooling (the Legion panel) only; nothing AI lands in `Assets/`.

Project-specific laws:

### {#GG-LAW-1} Movement never deals damage
Sliding a hero through or onto an enemy is **not** an attack. Damage flows ONLY through pincer
attacks ([§1.2](#12-pincer-attacks)) or spell effects ([§6](#6-the-spell-dispatcher)). The verb is
"slide", never "land on". *(Source: `ActorMovement.cs`, `PincerAttackManager.cs`.)*

### {#GG-LAW-2} A pincer is two heroes flanking an unbroken, ally-free line
Same row OR column, a contiguous file of ≥1 enemy between them, no gaps and no allies in the line.
No diagonal pincers. A 2×2 boss counts as one opponent flanked by its width. *(Source: `PincerDetector.cs`.)*

### {#GG-LAW-3} Code is the source of truth; the `.unity` is the print-out
Every scene is the regenerated output of an `Editor/Builders/*Builder.cs`. `BuilderAutoRebuild`
rebuilds on save. The reverse (`.unity` → builder) is intentionally absent; hand-edits are flagged by
`BuilderDriftChecker` (advisory) at pre-push. *(Source: `BuilderAutoRebuild.cs`, [§11.1](#111-code-only--builder-driven).)*

### {#GG-LAW-4} Guardrails are enforced at pre-push, not by convention
`SerializedFieldBan`, `ResourcesLoadBan`, `InstantiateBan` block the push (one Unity batchmode run of
`CheckCodeGuardrails`); `BuilderDriftChecker` runs in the same hook as an advisory, logged check.
Each has a curated allowlist; bypass only with `git push --no-verify` for hotfixes. No new
`[SerializeField]`, no `Resources.Load`, no `Instantiate(` outside `*Factory.cs`. *(Source:
`.githooks/pre-push` — active via `git config core.hooksPath .githooks` — and
`CliEntryPoints.CheckAllGuardrails`.)*

### {#GG-LAW-5} Every spell is a (shape × mode × filter) triad
Adding a spell picks targeting axes + damage/buff data ([`spells.json`](data/spells.json)); it never
touches the dispatcher. *(Source: `SpellEffectDispatcher.cs`, `TargetShapeResolver.cs`.)*

### {#GG-LAW-6} Mana is one shared, visible, capped pool
A single 12-orb `ManaBank` for the whole party — no per-hero MP. Over-minting past 12 fades out
(felt as "leaving value on the table"). *(Source: `ManaBank.cs`, [§3.1.4](#314-bank-full-rules-overflow).)*

### {#GG-LAW-7} Verify-then-checkpoint; the bible is the brief
Land a feature end-to-end and verify it (automated suite + play-test) before committing — no
mid-phase commits. Every new system ships a `DebugManager.Demo_*` button. If code and bible disagree,
reconcile in writing; never let drift accumulate. *(Reinforces [`HOUSE-LAW-8`](../../MindAttic.HouseRules.md#HOUSE-LAW-8);
[§17.2](#172-cadence), [§32](#32-document-discipline).)*

### {#GG-LAW-8} Portrait-mobile is locked; never stretch
The UI is locked to portrait mobile (reference 1170×2532). Off-aspect devices letterbox /
pillarbox via `AspectGuard` — the game never stretches or squashes. *(Source: `Utilities/AspectGuard.cs`,
[§26](#26-responsive-design--aspect-ratio-profile).)*

## 6. Verified state {#GG-§6}

> Status legend: ✅ done (verified) · 🟡 partial · ⬜ planned · living.

**Build/test environment:**

- **Engine:** Unity `6000.4.3f1` (authoritative in `ProjectSettings/ProjectVersion.txt`).
- **Headless verification.** Game code compiles into the `Scripts` assembly
  (`Assets/Scripts/Scripts.asmdef`) so the test assemblies can reference it.
  `tools/run-tests.ps1 -Platform EditMode|PlayMode` runs Unity's `-runTests` CLI with the Editor
  closed and gates on three signals: the results XML has zero failed test cases, the log has zero
  `error CS` lines, and Unity's exit code.
- **EditMode suite** (`Assets/Tests/EditMode/`, pure logic): `FormulasTests`, `PincerDetectorTests`,
  `SaveRoundTripTests`, `ProfilePersistenceTests`, `CampaignStagesTests`, `GoldTrackerTests`,
  `BountyFlowTests`, `AbilitySlottingTests`, `AbilitySlotProgressionTests`, `AbilityBarSlotCountTests`,
  `CombatLoadoutTests`, `SpellCastCapacityTests`, `SummonServiceTests`, `AspectGuardTests`,
  `AudioCreditsTests`, `TrapAndLineThreatTests`.
- **PlayMode suite** (`Assets/Tests/PlayMode/`): `SceneBootSmokeTests` (every live scene boots),
  `BattleLoopScenarioTests` (battle loop on the deterministic `Test-Harness` stage), `SnakeBossTests`
  (`Test-Snake` fixture), `PincerScenarioTest` (`Game_scene_boots_with_core_managers`; the
  `Pincer_drop_damages_flanked_enemy` scenario is `#if ALTTESTER`-gated and Inconclusive without the
  AltTester SDK).
- **Test hooks:** `Scripts/Helpers/TestHooks.cs`, `RNG.Seed/Unseed`,
  `FolderHelper.Folder.TestProfileRootOverride` (tests never touch real saves). Editor hooks that
  would hijack a test run stand down in batch/`-runTests` sessions: `StartSceneAuthority`,
  `DebugWindowBootstrapper`, `CustomPlayBehaviour`.
- Visual layout and game feel are verified by in-editor play-test; that evidence is a
  `DebugManager.Demo_*` button plus the cited implementation.

**Proven working** (evidence per story in [`USER_STORIES.md`](USER_STORIES.md)):

- ✅ Core combat loop: slide / displacement / pincer / supporters / pushback (`PincerAttackManager`,
  `ActorMovement`, `TimelineBarInstance`; `PincerDetectorTests`, `BattleLoopScenarioTests`).
- ✅ Casts on the timeline: below-the-line cast icons, the third "resolving" turn state, WIS/INT
  cast-time scaling (`TimelineBarInstance.SpawnSpellIcon`, `TurnManager`, `Formulas.CastTime`).
- ✅ Cast-stagger interrupt model + `ClutchSequence` (US-024/025), enemy charge casts + interrupt→orb
  mint (US-026/027), line-AoE charges and tile traps (US-138/139).
- ✅ All 10 buffs apply, tick, icon-bar, and bite gameplay (US-011..016); see [`buffs.json`](data/buffs.json).
- ✅ Mana color identity per class + wild-orb crit mint + Colorless wildcard spend + time-banked orbs
  (US-028/030/031/033/142).
- ✅ Equipment data layer: durability/shatter/repair, battle-start orbs, Sleep Dart, resistance gear
  (US-040..043).
- ✅ AI: threat tracking, retreat, supporter positioning, boss scripted phases (US-080..083);
  multi-tile 2×2 bosses (US-120); segmented snake boss (US-140).
- ✅ Macro loop: boot flow, StageSelect with bounty board, story crawl, seven vendor scenes, coins →
  gold bridge, save round-trip (US-124..132; `SceneBootSmokeTests`, `GoldTrackerTests`,
  `BountyFlowTests`, `SaveRoundTripTests`).

## 7. Active frontier {#GG-§7}

- **Remaining V1 work** — [`USER_STORIES.md`](USER_STORIES.md) Backlog: US-104 (60fps profiling pass
  on a physical mid-tier device) and the deferred items listed there.
- **Design notes / RFCs:** [`docs/rfc/0002-v2-vision.md`](rfc/0002-v2-vision.md) — the post-PoC V2
  direction (random summoning, branching story, HD art, content breadth). Nothing in it is in the V1
  build window.
- **Open design questions** — [§29](#29-open-design-questions): out-of-battle debuff carry, save
  autosave cadence, inventory cap, permadeath/NG+, tutorial.

## 8. Quality bar {#GG-§8}

Definition of done for a feature (also the per-story DoD in [`USER_STORIES.md`](USER_STORIES.md)):

1. **Compiles** with zero Console errors ([§17.3](#173-checkpoint-recipe-before-handing-back-for-commit)).
2. **Bible section updated** — no silent drift ([GG-LAW-7](#GG-LAW-7)).
3. **`DebugManager.Demo_*` method + DebugWindow button** shipped so the user can click-test, not be
   asked "does it work?"
4. **`CliEntryPoints.CheckAllGuardrails` green** (the [GG-LAW-4](#GG-LAW-4) guardrails).
5. **Verified** — the automated suite is green (`tools/run-tests.ps1`, [§6](#gg-§6)) for anything
   testable, and layout/feel is play-tested in-editor.
6. **One commit at the end** — verify-then-checkpoint, no mid-phase commits.

A story is `✅` only when 1–6 hold; otherwise `🟡`/`⬜`. This satisfies
the org-wide [`HOUSE-LAW-8`](../../MindAttic.HouseRules.md#HOUSE-LAW-8).

## 9. Glossary {#GG-§9}

- **Slide / displacement** — dragging a hero onto an occupied tile moves the displaced actor (ally
  or foe) into the tile the hero just left. ([§1.1](#11-sliding-and-displacement))
- **Pincer** — two heroes in the same row/column with a contiguous, ally-free, ≥1-enemy line between
  them; the only source of hero physical damage. ([§1.2](#12-pincer-attacks))
- **Supporter** — an ally cardinally adjacent to a pincer endpoint with clear line of sight; adds
  bonus damage. ([§1.2.3](#12-pincer-attacks))
- **Timeline / IP gauge** — horizontal strip; icons "load" left (u=0, spawn) → right (u=1, trigger).
- **Pushback Zone** — the rightmost `ZoneU` of the timeline; hitting a foe whose icon is inside it
  shoves its turn back toward spawn. ([§2.3](#23-pushback-the-interrupt-by-hitting-mechanic))
- **Hasten / Quicken** — the inverse: slide a target icon forward toward the trigger. ([§2.7.1](#271-hasten--quicken--the-inverse))
- **Cast icon** — a spell with `CastTimeSeconds > 0` rides the timeline as a small icon below the
  line; it resolves at u=1 in the third "resolving" turn state. ([§2.6](#26-one-timeline-two-lanes--turn-icons-above-cast-icons-below))
- **Clutch** — rare LCK-driven interrupt outcome: the caster shrugs the hit, the cast snaps to u=1
  and resolves on the spot. ([§13.4](#134-the-interrupt-path))
- **ManaBank / orb** — shared 12-orb colored pool (W U B R G C). ([§3.1](#31-mana-the-orb-economy))
- **Wild orb** — a Colorless orb (crit-minted) that satisfies any single color on spend. ([§3.1.6](#316-pressure-valve--colorless-wildcard))
- **Time-banked orbs** — orbs minted at the enemy-turn handoff from the time left in the hero window
  when the last hero action was taken. ([§3.1.8](#318-time-banked-orbs))
- **AbilityBar** — the Row-13 Skill / Spell / Item bar for the selected hero: 2 usable slots on a
  fresh save, unlocked by campaign progress up to 5. ([§4 AbilityBar](#4-the-abilitybar))
- **Builder** — an `Editor/Builders/*Builder.cs`; the authoritative source of a `.unity` scene. ([GG-LAW-3](#GG-LAW-3))
- **Sequence** — an async unit on the `SequenceManager` event queue (combat/UI steps).
- **Undearth** — the sunless game world the light-bearing invaders descend into (lore).
- **Story crawl** — the skippable per-theme intro text shown on first entry into a campaign theme. ([§27](#27-story-crawl-no-dialog))
- **Summon Circle** — the vendor scene where gold recruits a new hero class into the roster. ([§25.10](#2510-summon-circle))
- **Bounty** — a posted kill contract on StageSelect: accept one, track kills, claim gold + an item. ([§22.4](#224-bounty-board))

For combat/targeting/VFX sub-vocabulary see [Appendix §18 Glossary](#18-glossary).

---
<a id="gg-appendix-a"></a>

# Appendix A — Design Canon

> The detailed reference behind the nine sections above; the L0 sections and `USER_STORIES.md` cite
> it by `§` number. Structured catalogs (the spell table §7, buff catalog §8.1, class roster §23.2,
> enemy palette §14.2, rarity tiers §24.1.1) also have a machine-readable home under
> [`docs/data/`](data/).

## 0. North Star

A tactical-RPG hybrid where the player **drags heroes one tile at a time across a 6×8 grid** to flank enemies and trigger **pincer attacks**. Combat happens in a continuous-time **timeline** rather than fixed turns; enemies "load" left → right and act when ready. Magic, items, and class skills are paid for and managed through the **AbilityBar**. The feel target is *Final Fantasy timeline + Disgaea grid + FF8 draw economy*.

The game is **code-only / builder-driven**. `.unity` scene files are build artifacts of `Editor/Builders/*Builder.cs`; `BuilderAutoRebuild` regenerates them on every relevant file save. Visual content uses **Addressables** sprites loaded via `AssetHelper.LoadAsset<T>` and registered in `SpriteLibrary`. VFX use `VisualEffectLibrary` entries that point at prefabs (Addressables-backed).

### 0.1 Design pillars

1. **The verb is "slide".** Heroes never "land on" enemies. They flank into empty tiles; damage comes from the geometry that forms, not the move itself.
2. **Time is the resource the player manages.** The timeline replaces turns — every decision (cast, slide, skip) trades against the question "whose icon lands first?"
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
- **Not a gacha / live service.** No randomized character pulls (heroes are bought deliberately for gold at the Summon Circle), no daily energy, no premium currency. Single-purchase or premium-on-platform.
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

**The rule:** the displaced occupant moves to the tile the dragging hero **just left** (one tile behind the drag direction). If H drags east into A's tile, A ends up on H's previous tile. Dragging further through more occupants repeats the swap one tile at a time, so each occupant in turn shifts back by one tile.

```
before:              H A E E . .
drag H east 1 tile:  A H E E . .    (A shoved into H's old tile)
drag H east again:   A E H E . .    (the first E shoved into H's old tile)
```

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
5. After resolve: `PincerAttackManager` drops one mana orb per contributor in that hero's class color (§3.1.2) at Cleric, Knight (and supporters if any), each bouncing toward the orb line.

Suppose the goblin at (3,3) had its timeline icon inside the Prepare Zone at u=0.85. Knight just damaged it via pincer → `TimelineBarInstance.PushbackOnAttack` fires:
- Damage applied (always).
- Icon pushed left (u drops to ~0.62).
- Goblin enters `Stunned` for ~0.6s.

After the sequence finishes: `OnResolved` fires. `TurnManager` checks `HasQueuedEnemyAfterHero` — if no enemy icon has triggered yet, control returns to the player (still hero turn). If a goblin's icon reached u=1 during the drag, `EndTurnSequence → BeginEnemyTurn(goblin)` runs next.

That's the full beat: **drag → slide cascade → pincer detect → sequence resolve → orb mint → pushback → control returns or enemy turn**. Every other interaction is a variation on this — different shape, different cost, different VFX, same flow.

### 1.5 Multi-tile actors (2×2 bosses)

Most actors are 1×1, but **enemies may occupy a larger rectangle** (commonly a **2×2 boss**). **Heroes are always 1×1.** The footprint is authored on `ActorData.Footprint` (default `(1,1)`; e.g. `Cyclops00` = `(2,2)`) and carried at runtime on `ActorInstance.Footprint`.

**Representation & occupancy.** `location` is the **anchor** (lowest x,y = top-left; the board is 1-based). The actor covers `anchor + (dx,dy)` for `dx∈[0,w), dy∈[0,h)`. Occupancy is centralized: `ActorInstance.Occupies(tile)` (zero-alloc; for 1×1 it reduces to `tile == location`) and the single accessor `GameHelper.Actors.ActorAt(tile)` / `IsTileOccupied(tile)`. `TileInstance.IsOccupied`/`Occupier` forward to these, so every occupancy check (movement, pincer, targeting, spawn, support lines) is footprint-aware. The body sprite is scaled by the footprint and centered between its tiles via `Geometry.GetFootprintCenter` / `ActorInstance.CenterPosition`.

**Spawn.** A multi-tile enemy needs a whole free rectangle: `RNG.UnoccupiedFootprintAnchor` / `FootprintFitsFree` (on-board + all tiles free). `StageManager.SpawnActor` honors an explicit, fitting `StageActor.Location` (designer-placed boss) else rolls a free anchor; the 1×1 placement path is unchanged.

**The four rules** (designer-locked):
1. **Immovable anchor (vs hero slides).** A hero dragged into any boss tile is **blocked** — the slide stops at the footprint edge, exactly like the board edge (`ActorMovement.CheckLocationChanged` rejects the logical move into a multi-tile enemy). The boss is never displaced by a slide. Pincer damage still applies and pushes the boss's **timeline icon** (not its board position).
2. **Pincer by flanking its width.** A 2×2 enemy is caught when every tile strictly between the two (1×1 hero) attackers is covered by an opposing actor and none by an ally; the boss's 2 tiles in the line count as **one** opponent, not a gap (`PincerDetector.Detect`, distinct-actor predicate). Harder to set up than a 1×1 — bosses are tougher but killable by the core mechanic.
3. **Boss shoves heroes (on its turn).** A moving 2×2 enemy steps its footprint one cardinal tile and **displaces any hero in the entered tiles** into the tile it vacated on that lane — the same train-car cascade as a hero slide (`ActorInstance.StepFootprint` + `ResolveShoveChain`). The step **aborts** (nothing moves) if a required shove would push a hero off-board or into a non-shovable actor. `EnemyPlanner` only offers footprint-legal steps (`FootprintStepLegal`).
4. **One timeline icon** regardless of size; the boss melees/pincers via footprint adjacency (`Geometry.AreAdjacent`).

Demo: **"Spawn 2×2 Boss"** (DebugWindow) drops the Cyclops into a live battle. Sources: `ActorInstance.cs`, `TileInstance.cs`, `GameHelper.cs`, `PincerDetector.cs`, `ActorMovement.cs`, `EnemyPlanner.cs`, `StageManager.cs`, `RNG.cs`, `Geometry.cs`, `ActorData.cs`.

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

### 2.4 Turn order = arrival order

Icons advance independently; there is no spacing pass between neighbors. Turn order is simply the order in which icons reach the trigger (`GetSecondsRemaining`, lowest first). Pushback, Hasten, Slowed and Frozen change an icon's `u` or speed directly, and any reordering (one icon overtaking another) follows from that.

### 2.5 Auto-skip / Shield

The Shield button (right edge of the timeline, `ShieldButton`) fast-forwards the timeline to the next enemy trigger. Pressing it:
- Calls `ManaPoolManager.OnBankButtonClicked()`, which advances the timeline to the next enemy trigger (it grants no mana).
- Applies **Protection** to every hero — 15% incoming-damage reduction for 1 turn.

`TurnManager` also auto-presses this when remaining time until the next enemy trigger is too short for the player to react.

### 2.6 One timeline, two lanes — turn icons above, cast icons below

**Design rule:** the timeline is read on a single left→right time-axis (`u`: 0 = spawn, 1 = trigger). **Two lanes share that one axis** so the player sees, at a glance, **when each enemy acts AND when each spell fires — relative to each other, on the same timeline**:

- **Above the line — actor turn icons (LARGE):** each actor's **portrait** loads toward the trigger; reaching `u = 1` is that actor's turn. Large, because turn order is the dominant read.
- **Below the line — cast icons (SMALL, ~¼ the size of a turn icon):** a spell with `CastTimeSeconds > 0` rides the *same* axis as a quarter-size icon **below** the line; its position is how close the cast is to resolving (`u = 1` = it fires). Color = the spell's dominant mana-cost color (`ManaOrbLine.ColorFor`).

A cast's **position is its progress read**, lining up directly under the enemy turn-icons. The same below-the-line lane is where an **enemy charge-cast** icon rides (US-026). Player casts spawn through `TimelineBarInstance.SpawnSpellIcon` from `AbilityBar.HandleSpell`.

**Shared continuous clock — does a cast fire "off-turn"? Yes, and that's the point.** This is the Grandia IP-gauge model (§0): there is *one* clock, not alternating turns. Both lanes advance together as the timeline progresses, and **whatever icon reaches the trigger first resolves** — an enemy icon at `u = 1` takes its turn; a cast icon at `u = 1` fires its spell, even if that lands *between* enemy turns. Resolution runs through the input-suspending **`Resolving` third state** (§2.2) and then hands control back to wherever the clock was. The two-lane layout is exactly what keeps this legible rather than confusing: you literally *see* your Fireball's small icon racing the Goblin's portrait toward the trigger and read "my spell lands just before it acts." So casting time and enemy turns are **related — same axis, same clock** — and a cast resolving off any particular turn is intended, not a bug. *(If we ever wanted casts gated to a turn boundary instead, that would be a departure from the IP-gauge pillar — flagged, not assumed.)*

Lifecycle: up to 4 hero casts load at once — a further cast-time spell is refused before any orbs are spent (§4.4); orbs are spent when the target is confirmed; the icon spawns at `u = 1 − castTime × pace` (the bar's canonical Speed-10 pace, so "3 s left" reads the same as on any actor icon) and advances only while the timeline advances. On reaching `u = 1` it parks in `Resolving`, `TurnManager.BeginCastResolution` suspends input, the effect resolves, and control returns. A spell with no cast time resolves immediately with no icon. Interrupts (hero casts and enemy charges alike) follow the cast-stagger rule, §13.4.

```
        ╭─────╮       ╭─────╮                      ← ABOVE: large actor turn icons
        │ 👹  │       │ 👤  │                          (enemy / hero portraits)
  ┃═════╪═════╪═══════╪═════╪══════════════[🛡]┃   ← the timeline (u: spawn→trigger)
        │ ⬤R │   │ ⬤W │                            ← BELOW: small cast icons
        ╰─────╯   ╰─────╯                              (Fireball-red, Heal-white …)
   left (just cast / fresh) ───────────────→ right (about to fire / act)
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

### 2.7.1 Hasten / Quicken — the inverse

The mirror of pushback: **Quicken** (`SpellLibrary.Quicken`, `SpellDefinition.HastenU`) slides a target's timeline icon **forward** (toward the trigger) by a fixed `u` amount via `TimelineIcon.Hasten` / `TimelineBarInstance.HastenIcon`. A higher `u` means a sooner turn, so the hastened icon can **overtake** icons that were ahead of it.

Overtaking is emergent from the forward `u` bump (turn order is arrival order, §2.4). Hasten clears a `Queued`/`Stunned`/`PushedBack` icon to `Approaching` so the push lands, then clamps `u ≤ 1` (pushed all the way to the trigger → the icon fires its turn). Cast on an enemy to bait its turn early or out of a forming pincer; on a charging ally to rush a cast.

### 2.8 Spawn rules

| Trigger | Where the icon spawns |
|---|---|
| Battle start | All actor icons spawn at `u = 0` staggered by `Queued` delay |
| Enemy reinforcement (scripted mid-battle) | `u = 0` |
| Spell cast by hero | A **small cast icon BELOW the timeline line** (§2.6), riding the shared `u`-axis toward the trigger at the cast-time rate — *not* a separate stacked bar |
| Enemy charge spell (US-026) | A small cast icon in the **same below-the-line lane** as hero casts, advancing via the cast-time formula |
| Pushback / displacement / Hasten | Existing icon's u changes directly; turn order follows from arrival-at-trigger order (§2.4) |

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
- **Pincer completion**: each hero attacker contributes 1 orb of their **class color** (US-030, via `ManaColorAffinity.For`). Each supporter also contributes 1 (its own color).
- **Enemy charge interruption** (US-027): a hero's landing hit on a charging enemy interrupts its cast via the US-024 stagger model (hooked centrally in `ActorInstance.DamageRoutine` — covers pincers, magic, shield); when the accumulated stagger **cancels** the charge, `TimelineBarInstance.MintInterruptOrb` drops one orb of the charge's color (`EnemyChargeCatalog.ColorFor`) via `ManaOrbFactory.Drop` — it bounces into the team bank. This is **how off-palette colors flow in** (§3.1.2). Clutch is hero-only (enemies never instant-resolve a charge on being hit).
- **Critical hits** (US-031): a hero's critical hit (pincer or magic — any damage routed through `ActorInstance.DamageRoutine`) mints one **Colorless "wild" orb** to the bank. Wild orbs render as an all-colors-at-once orb that **flashes through the spectrum** in the line (`ManaOrbLine.AnimateWildOrbs`), distinguishing them from the static elemental orbs. This is a primary off-palette source.
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

Possible tuning knob (not built): a higher-rarity Mage Robe might raise the cap for that battle (`BattleStartManaOrbs` and a parallel `BankCapacityBonus`).

#### 3.1.5 Spend ordering (which orb leaves first)

When `Bank.Spend(recipe)` removes colored orbs, the **leftmost orb of each demanded color** is consumed. Visually the surviving orbs shift left to close the gap so the bank always renders left-packed. This means a player can predict cast cost by reading left-to-right.

`AllowAnyColor` (dev flag) consumes purely leftmost regardless of color — useful for early-stage testing before colors lock in.

#### 3.1.6 Pressure valve — Colorless wildcard

The escape valve so an off-color bank isn't dead weight: **a Colorless "wild" orb (minted by crits, §3.1.2/US-031) satisfies any single colored requirement when spending.** `ManaBank.CanAfford`/`Spend` pay each cost with its own color first (leftmost, §3.1.5) and fall back to Colorless wilds for any shortfall; an explicit Colorless requirement is paid only by Colorless. There is **no manual color-to-color converter** — the wildcard was chosen over a 2-for-1 trade because it keeps class colors meaningful (off-color orbs stay off-color — the cost of poor positioning), ties the valve to skilled play (crits), and isn't exploitable. Reversible: a converter can be added later if the valve proves too tight.

#### 3.1.7 At-a-glance mint cadence

| Event | Orbs minted | Color source |
|---|---|---|
| Hero completes a 2-hero pincer (no supporters, 1 enemy in line) | 2 | each hero's class color (US-030) |
| Hero pincer with 2 supporters | 4 | 2 attackers + 2 supporters |
| Hero pincer with 1 supporter, 3 enemies in line | 3 | 2 attackers + 1 supporter |
| Steal/Mug skill, 3 adjacent enemies, 2 succeed | 2 | random per roll |
| Critical hit (US-031) | 1 | Colorless "wild" orb (flashes every color) |
| Cancel an enemy charge (US-027) | 1 | matches enemy charge color |
| Battle start (Mage Robe equipped, count=1) | 2 | random (per `BattleStartManaOrbs`) |
| Enemy-turn handoff with 9+ s banked (US-142) | 3 (cap) | Blue |

The math here is the **design lever for spell tuning**: a 2-orb spell should feel like 1 pincer's worth; a 4-orb spell should require a multi-supporter or multi-pincer chain.

#### 3.1.8 Time-banked orbs

Acting decisively is a resource play. At each hero drop, `SelectionManager` calls `ManaPoolManager.RecordHeroActionForTimeBank()`, which records the seconds still remaining before the next enemy reaches the trigger. When the enemy turn begins (`TurnManager.BeginEnemyTurn`), `MintTimeBankedOrbs()` converts that remainder into **Blue** orbs — one per `SecondsPerTimeBankOrb` (3 s), capped at `MaxTimeBankOrbsPerWindow` (3) per window — through the same bouncing `ManaOrbFactory.Drop` path as pincer mints, and posts a "Banked N mana" combat-feed line. The record is reset each new hero window, so idling banks nothing; overflow past the 12-orb cap fades like any other mint ([GG-LAW-6](#GG-LAW-6)).

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

Row 13 of the HUD. The bar renders **5 slot buttons** (`AbilityBarFactory.Slots` = `AbilitySlotProgression.MaxSlots`); campaign progress makes **2 to 5** of them usable (§4.7), so a fully progressed save can use every slot. *(Verified by `AbilityBarSlotCountTests`.)* Each slot holds one `ManaAbility` (which is one of three kinds).

> **Invariant — one `ManaAbility` per spell.** For Spell-kind abilities, `AbilityBar.ResolveSpell` looks the `SpellDefinition` up by *ability reference* and returns the first match in `SpellLibrary.All`. A `ManaAbility` must therefore back exactly one `SpellDefinition` — reusing a single instance across spells silently resolves to whichever is declared first (this caused the Heal→Sleep bug). Every entry in the §7 catalog has its own dedicated ability.

### 4.1 The three kinds

| Kind | Cost | Cast time | Frame color |
|---|---|---|---|
| **Skill** | free, "costs the player's turn" (timeline auto-advances after resolve); locked by a per-skill cooldown after use | instant | green |
| **Spell** | colored mana orbs from `ManaBank` | cast icon on the timeline (§2.6), or instant | cool blue |
| **Item** | per-slot stack charge | instant | warm leather |

Slot kind is set by the constructor used on `ManaAbility`. Cost icons label: Skills show `Free`, Items show `current/MaxStackSize`, Spells show `(W)(R)…`.

### 4.1.1 Skill cooldowns

A Skill is free to use but, once cast, is **locked for `ManaAbility.CooldownTurns` turn-cycles**. Current values: **Steal 3, Mug 2, Teleport 3** (`Data/ManaAbilities.cs`). The remaining countdown is tracked **per hero** by `SkillCooldownManager` (a `BuffSystem`-style static dictionary) — *not* on the `ManaAbility`, because Skill ability instances are shared statics across multiple class loadouts.

- **Set:** `AbilityBar.HandleSkill` (and the Teleport flow) call `SkillCooldownManager.Begin(hero, skill)` when the skill actually fires.
- **Tick:** `TurnManager.BeginHeroWindow` calls `SkillCooldownManager.TickAll()` once per turn-cycle (each time the player regains control); a skill reactivates when its counter reaches 0. `TurnManager.Initialize` clears all cooldowns at battle start.
- **UI:** while on cooldown the bar slot **fades out** and its cost label shows the **turns-remaining number**; the button is non-interactable and `HandleSkill` refuses an early click. (`AbilityBar.Refresh`.)
- **Debug:** Debug Window → *Lock Skill CDs* / *Tick CD (1 turn)* (`DebugManager.Demo_LockSkillCooldowns` / `Demo_TickSkillCooldowns`).

### 4.2 Per-hero loadouts

The bar **follows the selected hero**. `AbilityBar.Update` binds `Services/CombatLoadouts.For(characterClass)` for the selected hero each frame. When no hero is selected, slots hide.

`CombatLoadouts.For` picks the bar in this order:

1. **The player's saved bar** — `HeroEquipmentSave.AbilityBarSlots`, edited in the Abilities scene (§25.6) — when it has at least one filled slot. Positions are kept. An item slot becomes a per-slot consumable stack linked to its item (`ManaAbilities.NewConsumable`, so `OnUseSpellName` items still cast their spell) with charges = owned count capped at the item's `MaxStack`; a named slot resolves to the `ManaAbilities` entry of that name; a weapon slot, or a name/item with no combat-bar entry, shows empty.
2. **The class preset** — `HeroLoadouts.For(characterClass)` (table below); classes without one fall through to `ManaAbilities.Slots` (Heal, Fireball, Frost, Bolt, Potion).

Resolved bars are cached per class for the battle (so item charges survive re-selecting a hero); `AbilityBar.Awake` clears the cache. The Debug Window's random-abilities button replaces a hero's bar for the current battle (`CombatLoadouts.SetBattleOverride`). *(Verified by `CombatLoadoutTests`.)*

Add per-class presets to `HeroLoadouts.perClass` via `HeroLoadouts.Set(class, loadout)`; a preset has at most 5 entries.

Seeded loadouts:

| Class | Slots |
|---|---|
| Cleric | Heal, Heal, Frost, NewPotion(3) |
| Paladin | Heal, Fireball, NewPotion(3) |
| Barbarian | Fireball, Bolt, NewPotion(3) |
| Alchemist | Frost, NewPotion(5), Steal, NewPotion(5), Sleep Dart (×5) |
| Assassain | Steal, Mug, Bolt, NewPotion(3) |
| GreenNinja | Teleport, Steal, Fireball, NewPotion(3) |
| RedNinja | Teleport, Mug, Bolt, NewPotion(3) |

### 4.3 Item stacks (per-slot)

Items are **per-slot instances** — each call to `ManaAbilities.NewPotion(stackSize)` mints a new `ManaAbility` with its own `Charges` and `MaxStackSize`. Two 5-stack slots = 10 total uses split across two independent bars. Buying another stack at the vendor fills the next free slot.

`TryConsumeCharge()` decrements; `Refill(amount)` clamps to `MaxStackSize`.

### 4.4 Click flow per kind

**Item**: if the item declares `OnUseSpellName` (e.g. Sleep Dart), route through that spell's targeting flow → `SpellEffectDispatcher.Cast`, spending one charge **on confirm** + costing a turn (US-042, via `ManaAbility.SourceItemId`); otherwise `TryConsumeCharge` → log. Instant (no cast icon).

**Skill**: `TargetingMode.Begin` → on confirm, dispatch (or run the Skill's bespoke flow), then call `ManaPoolManager.OnBankButtonClicked()` to advance the timeline ("costs a turn"). Free.

**Spell**:
1. `Bank.CanAfford(cost)` precheck — no deduction yet. A spell with a cast time is also refused when **4** hero casts are already loading on the timeline (`Services/SpellCastCapacity.MaxHeroCastsInFlight`, counted by `TimelineBarInstance.HeroCastsInFlight`; enemy charges don't count): "Too many spells!" pops over the caster and no orbs are spent. Instant spells are never refused.
2. `TargetingMode.Begin` → user picks (or auto-resolves for Mode=Auto).
3. On **confirm**, the cast cap is re-checked (another cast may have started while targeting was open), then `Bank.Spend(cost)` (orbs deducted). A spell with a cast time spawns its cast icon (`TimelineBarInstance.SpawnSpellIcon`, §2.6) and dispatches per target when the icon resolves; an instant spell dispatches immediately.
4. On **cancel**, zero orbs spent.

This means **mana is consumed AT CAST START** (after target chosen, before the icon), per the project rule "MP consumed upfront; interruption refunds nothing." Cancel during targeting is free.

### 4.5 Slot visual states

Each slot in the AbilityBar can be in one of these visual states, driven by `AbilityBar.RefreshSlot(int i)`:

| State | Visual | When |
|---|---|---|
| **Empty** | grey dashed outline | no `ManaAbility` assigned (or slot index ≥ loadout length) |
| **Ready** | full color frame, icon at 100% opacity | affordable + not on cooldown |
| **Unaffordable** | full color frame, icon at 40% opacity, cost text red | spell — `Bank.CanAfford(cost) == false` |
| **Out-of-charges** | item frame, "0/N" overlay, icon at 30% opacity, slot is non-interactive | item with `Charges == 0` |
| **Selected / targeting** | thick yellow outline + slight scale-up | `TargetingMode.IsActive && AbilityBar.SelectedSlot == i` |
| **Cooldown** | greyscale icon + radial sweep (`CooldownSweep` Image, `Radial360`) | skill `CooldownTurns > 0` and remaining > 0; countdown ticked per hero window (`SkillCooldownManager`, US-092) |
| **Disabled (Silenced)** | solid-red blocked frame on Spell slots, non-interactable | caster has `silenced` debuff; `AbilityBar.HandleSpell` refuses the cast with "Silenced!" popup (US-012) |
| **Locked** | dark frame, "Locked" label, non-interactable | slot index ≥ `AbilitySlotProgression.UnlockedSlotsForCurrentSave()` (§4.7) |

**Hover/long-press**: shows name, cost, cast time, and charge count in a `Tooltip` positioned above the slot (US-091).

### 4.6 Cancel paths

A targeting flow can be aborted by:
- Tapping the **selected slot a second time** (cancel from the bar itself).
- Tapping the **Cancel button** in the `TargetPickerOverlay`.
- Pressing **Escape** (debug keyboard binding; mobile gets a hardware-back equivalent).
- `TargetPickerOverlay.OnDestroy` safety net — destroying the overlay (e.g., a scene transition) fires `OnCancelled` so `TargetingMode.IsActive` never gets stuck true (see §17.1 #4).

### 4.7 Progressive slot unlock

`Services/AbilitySlotProgression` (pure) decides how many slots are usable: **2** on a fresh save, +1 each when `StageSaveData.HighestClearedStageIndex` reaches **0, 2 and 5** (`UnlockThresholds`), hard max **5** — the same marker StageSelect unlock gating uses, so the two progressions agree. `AbilityBar` renders slots past the limit as Locked and `OnSlotClicked` ignores them; `AbilitiesManager` renders locked slot buttons in the loadout scene and only assigns into unlocked slots. *(Verified by `AbilitySlotProgressionTests`.)*

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
       │  (precheck: affordable, not silenced, not on CD)     │
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
       (cast icon spawns, or skill resolves, or nothing)
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
| `g.Actors.All` is null (scene transition during cast) | `TargetShapeResolver` null-guards `actors` and returns an empty list — dispatcher iterates nothing. |
| AOE shape includes the caster (e.g. Cross(r=1) on self) | `TargetFilter.EnemyOnly` strips caster + allies; `AllyOnly` keeps caster (heal-self is valid); `Any` keeps everyone including caster. |
| Spell hits the same target twice (overlapping shapes) | Each tile resolves once. AllEnemies + AOE never double-stack because the actor collection dedupes by `ActorInstance` reference. |
| Wet + Fire on the same hit | Wet is stripped BEFORE damage (stage 7), so the lightning ×1.5 multiplier on stage 10 does NOT see a Wet target the same hit applied. Order matters. |
| Lightning on a Wet target | Wet stays (it's not stripped by Lightning); the ×1.5 multiplier applies and the 30% Blind roll fires after damage. |
| Steal on a target with no orbs to give | Roll succeeds → bank still gains a random-color orb (Steal is "from the world", not the target's MP). |

---

## 7. The Spell Catalog

All entries in `Data/SpellLibrary.cs`. Cost references `ManaAbilities.<Name>`.

| Spell | Cost | Shape / Mode / Filter | Motion | Effect | Strategic role |
|---|---|---|---|---|---|
| **Heal** | (W) | SingleActor / PickActor / AllyOnly | None | +25 HP | Spot-heal; cheap to repeat |
| **Mass Heal** | (W) | AllAllies / Auto / AllyOnly | None | +12 HP everyone | Emergency top-up; bigger gross but smaller per-target |
| **Antidote** | (W) | SingleActor / PickActor / AllyOnly | None | strips ALL debuffs | Cleanse — pair after a debuff-heavy enemy turn |
| **Scan** | (W) | SingleActor / PickActor / EnemyOnly | Straight | reveals HP/STR/VIT/AGI/INT via AnnouncementWindow + flags Bestiary Seen (US-077) | Recon — info, not damage |
| **Fire (Fireball)** | (R)(R) | SingleActor / PickActor / EnemyOnly | Twist | 18 Fire dmg + `burning` | High burst single target; DOT continues after |
| **Ice (Frost)** | (U)(U) | Square(r=1) / PickTile / EnemyOnly | Bezier | 10 Ice dmg + `frozen` | Crowd-control AOE; frozen halts timeline (§8.4) |
| **Lightning (Bolt)** | (R)(R)(U) | Row / PickActor / EnemyOnly | Strike | 14 Lightning dmg (×1.5 if target Wet) + 30% chance `blinded` | Row clear; pair after Frost expires → Wet for combo |
| **Poison** | (U)(U) | Cross(r=1) / PickTile / EnemyOnly | Bezier | 6 Poison dmg + `poisoned` | Low burst, big tail (tick damage) |
| **Sleep** | (W) | SingleActor / PickActor / EnemyOnly | Homing | `sleep` (breaks on damage / move) | Hard CC on a priority target; do NOT also attack them |
| **Slow** | (U)(U) | Row / PickActor / EnemyOnly | Bezier | `slowed` — icon speed ×0.5 (`TimelineIcon.GetEffectiveUPerSec`, US-011) | Row-wide tempo control; buys time across a rank |
| **Quicken** | (U) | SingleActor / PickActor / Any | Straight | slides the target's timeline icon forward 0.30u (`HastenU`, §2.7.1) — no damage | Tempo tool: bait an enemy's turn early or rush an ally's cast |
| **Silence** | (W) | SingleActor / PickActor / EnemyOnly | Straight | `silenced` — Spell slots blocked; casts refused with "Silenced!" popup (US-012) | Lock down enemy casters before they fire |
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

Turn-unit buffs decrement via `BuffSystem.TickTurn(actor)`, called at the **END** of the bearer's turn (US-016, wired into `TurnManager.NextTurn`). End-of-turn (not turn-start) so a "2 Turns" debuff affects the bearer for 2 of its *own* turns — ticking before the actor acts would burn one turn to off-by-one. `NextTurn` is the single turn boundary: when an enemy turn just ended it ticks that enemy (`lastEnemy`); when the hero window just ended it ticks every playing hero once (heroes share one free-form window, so there is no per-hero turn to tick — this is the closest boundary).

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
       │  • Item use (`OnUseSpellName`, e.g. Sleep Dart US-042) / passive │
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
- **Between battles**: buffs are per-battle state. `BuffSystem` is cleared at battle start (`TurnManager.Initialize`) and on a mid-battle restart, so no status carries from one battle to the next (§30.3).

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
│ Row 4    [combat feed: last 7 events; cast icons ride below the timeline line]
│ Row 5         ┌─────────────────┐  │
│ Row 6         │                  │  │
│ Row 7         │   6 × 8 Board    │  │  ← rows 4–12; world-space camera viewport
│ Row 8         │  (heroes, enemies)│  │
│ Row 9         │                  │  │
│ Row 10        │                  │  │
│ Row 11        │                  │  │
│ Row 12        └─────────────────┘  │
├─────────────────────────────────────┤
│ Row 13  [Heal][Fire][Frost][Bolt][Pot][🔒]  ← AbilityBar (6 buttons, 2–5 usable)
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
| 13 | AbilityBar — 5 slot buttons, 2–5 usable by campaign progress (§4.7) | `AbilityBarFactory` (runtime, parented to `Canvas/AbilityButtonContainer` placed by `GameBuilder`) |
| 14 | 12-slot mana orb belt — screen-wide "tray", sits just **above** the ability bar | `ManaOrbLineFactory` (runtime) |
| 15 | `ActorPanel` — tabbed **Stats / Equipment / Lore** (contextual: selected hero or scanned enemy). Hero ◀▶ cycle arrows in the tab bar. | root in `GameBuilder`; tab UI built at runtime by `ActorPanel` |

**Cast icons** — small spell-sprite icons that travel left→right on a lane **below** the timeline bar line (§2.6). Spawned via `TimelineBarInstance.SpawnSpellIcon`. (`SpellCastBar` / `SpellCastBarFactory` are marked `[Obsolete]` and have no live caller.)

**Combat feed** — `Canvas/CombatFeed` (built by `CombatFeedFactory`) shows the last 7 combat events as aging lines under the ActionTitle banner, newest at the bottom, raycast-transparent. Every `AnnouncementWindow.Announce` is mirrored into it, plus feed-only lines for damage (attacker / target / amount, crits colored), status ticks, heals, supporter assists and time-banked orbs. Inline icons come from the `CombatFeedIcons` TMP sprite asset (authored by `CombatFeedSpriteAssetAuthor`: spell icons, tag icons and one glyph per buff); `CombatFeed.Icon(name)` emits a `<sprite>` tag only for glyphs that exist.

**Combat text popups** (red damage, green heal, "Miss" / "Steal!" / "Steam!") float up from the actor's world position via `CombatTextManager.Spawn(text, position, styleKey)`.

**Debuff icons** (3-cell radial-ring strip) anchor inside the upper-right corner of each actor's tile via `WorldFollow` (§8.5).

---

## 10. Equipment (summary — see §24 for the full spec)

**See §24 "Equipment, Items, Materials, Currency"** for the comprehensive treatment — types, slots, ItemDefinition, inventory, durability, drops, crafting, currency, and the specific user-spec'd items (Mage Robe / Wizard Robe / Sleep Dart).

Quick recap: `Inventory/PartyLoadout` keys `HeroLoadout` per `CharacterClass`; each loadout is a `Dictionary<EquipmentSlot, ItemDefinition>`; `Formulas.ComputeEquipmentBonus(loadout)` aggregates stat bonuses into combat stats.

---

## 11. Scene Architecture

### 11.1 Code-only / builder-driven

Every scene is reproducible from its `Editor/Builders/*Builder.cs`, including `Game.unity` (`GameBuilder.cs`).

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
| Hand-edit `.unity` | **NOT detected** — `BuilderDriftChecker` flags it (advisory) at push time |

### 11.2 Scene list

Scenes in the build (`ProjectSettings/EditorBuildSettings.asset`):

- `SplashScreen` (boot scene — `StartSceneConfig.StartScene`) → `TitleScreen` → `ProfileSelect` / `ProfileCreate` / `SaveFileSelect` → `StageSelect` → `Game` (battle) → `PostBattleScreen` → back to `StageSelect`. `LoadingScreen`, `Settings` and `Credits` support the flow.
- `StoryCrawl` — the skippable per-theme intro shown on first entry into a campaign theme (§27).
- Vendor scenes: `Vendor`, `Alchemist`, `Blacksmith`, `Equip`, `Party`, `Abilities`, `Summon`. Each has its own scene + builder + manager + `PlayerInventory` hydration, reached through the `VendorNavBar` (§25.0).
- `Bestiary` — swipe-navigable encyclopedia of enemy classes (name, portrait, stats, abilities, lore; unseen classes are silhouettes). Reachable from `TitleScreen → Bestiary` (`TitleScreenManager.OnBestiaryButtonClicked → SceneHelper.Fade.ToBestiary()`); back → `BestiaryView.OnBackButtonClicked → SceneHelper.Fade.ToTitleScreen()`.

`Hub.unity` and `Overworld.unity` (and their builders) remain on disk but are not in the build list and nothing routes to them (soft-disabled per HOUSE-LAW-2).

#### 11.2.1 Scene transition graph

```
   SplashScreen ──▶ TitleScreen ◀──────────▶ Bestiary
                        │
                        ▼
        ProfileSelect / ProfileCreate / SaveFileSelect
                        │
                        ▼
                  ┌─────────────┐  VendorNavBar  ┌──────────────────────────────┐
             ┌──▶ │ StageSelect │ ◀────────────▶ │ Vendor · Alchemist · Smith · │
             │    └──────┬──────┘                │ Equip · Party · Abilities ·  │
             │           │ confirm stage         │ Summon (hop between freely)  │
             │           ▼                       └──────────────────────────────┘
             │    StoryCrawl (first entry into a theme only)
             │           │
             │           ▼
             │         Game ──battle end──▶ PostBattleScreen
             │                                     │
             └─────────────────────────────────────┘
```

### 11.3 Scene navigation

`SceneHelper.Switch.ToX()` (instant) and `SceneHelper.Fade.ToX()` (with FadeOverlay) are the two flavors.

**Fade speed: 125 ms.** `FadeOverlayInstance` fades out/in at **0.125 s** each way — snappy, not languid. Scene-to-scene navigation should feel near-instant; the fade exists only to hide the load seam, not to be a transition flourish. (Set the duration constant in `FadeOverlayInstance`; don't pad it.)

**Rule:** for any UI Button → scene transition, the click handler must be a real `MonoBehaviour` method (not a lambda). Persistent `UnityEvent` listeners require a `UnityEngine.Object` target — lambdas via the `<>c` closure class do not qualify and are silently dropped. Example pattern: `BestiaryView.OnBackButtonClicked()` on the scene's view component, wired by the builder via `UnityEventTools.AddPersistentListener`.

### 11.4 Build settings caveat

When a new scene is created by `SceneBuilderHelper.OpenScene` (auto-creates if missing), it must be **manually added** to `File → Build Settings → Scenes in Build` to be playable from gameplay scene transitions.

### 11.5 The visual language — UiKit / HubTheme / UiFonts

**One visual language for every scene** (FFBE-inspired mobile JRPG): simple boxes with thin steel
borders, navy panels, gold accents, two fonts (US-123).

**The three pillars:**
- **`Scripts/Hub/HubTheme.cs`** — the palette (PanelBg, HeaderBg, NavIdle/Active/Hover, Accent
  gold, PanelBorder steel, ListBg, RowBg/RowSelected/RowLocked, ScrollTrack/Handle, Danger,
  Success, Text tiers) + `ButtonColors` (the ONE ColorBlock every button uses). **Never hand-type
  a color that exists here.**
- **`Scripts/Hub/UiFonts.cs`** — the two-font system: **Attic = display** (scene titles, headers,
  announcements), **Outfit = body** (buttons, rows, stats, descriptions). Resolves via FontLibrary
  (Addressables) at runtime and AssetDatabase in edit mode, so builders and runtime factories
  render identically. **Every runtime-created TMP must set a UiFonts font** — an unfonted TMP
  falls back to LiberationSans and instantly breaks the language.
- **`Assets/Editor/Builders/UiKit.cs`** — the component factory every scene builder composes
  from: `Header` (96px bar + 3px gold rule + Attic 48 bold gold title), `HeaderRightLabel`,
  `Panel`/`Border` (flat fill + 2px steel edge frame), `Button` (Primary gold / Secondary navy /
  Tab / Danger), `BackButton` (the ONE convention: bottom-left (24,24) 220×64 "← Label"),
  `ScrollList` (bordered well + visible 12px themed scrollbar; hierarchy is exactly
  `{name}/Viewport/Content` so manager paths keep resolving), `Label`/`DisplayLabel`.

**Conventions the kit enforces:** every screen opens with the standard Header; one gold Primary
button per screen (the commit action); all lists scroll with a visible themed scrollbar; canvases
author at the §26.2 reference (1170×2532, match 0.5) so the editor preview matches the
AspectGuard-normalized device; the `CutoutOverlay` black bar is Game-scene-only (the Clock docks
there) — meta scenes use the Header instead.

**Rule:** a new screen is composed from UiKit components; a new runtime row sets `UiFonts.Body`
and HubTheme constants. Don't hand-roll per-scene buttons/labels/scroll-lists. *(Sources: `UiKit.cs`, `HubTheme.cs`, `UiFonts.cs`,
`SceneBuilderHelper.cs` — EnsureCanvas/EnsureTitle/EnsureBackButton/EnsureButton/EnsureLabel/
EnsureScrollView all route through the kit.)*

---

## 12. Asset Pipeline

### 12.0 Audio — authored tracks with a chiptune fallback

The game is never silent: every event has a sound.
- **Music.** `Jukebox.PlayMusic` plays an authored royalty-free track when `MusicTrackLibrary` has one for the scene's MusicDirector key, else a chiptune loop: "Teller of the Tales" (Title / Bestiary), "Minstrel Guild" (Vendor / StageSelect), "Crusade" (Battle) — Kevin MacLeod, CC BY 4.0; "Triumph" (Pixabay) for Victory; "Melancholy Lull" for Defeat. Full attribution (author, title, license, URL) lives in `Data/AudioCredits` and renders in the Credits scroll; entries whose origin is untracked are flagged there to verify before a commercial release. *(Verified by `AudioCreditsTests`.)*
- **SFX.** `AudioManager.Play`/`PlayAndThen` use an authored clip if present, else chiptune; via the battle `SoundSource` else the cross-scene **`Jukebox`**.
- **Chiptune fallback.** `Utilities/ChiptuneSynth` synthesizes `AudioClip`s (tones with ADSR + pitch slide, jingles, tileable loops); `Libraries/ChiptuneBank` caches a semantic SFX vocabulary (`Click`/`Hit`/`Cast`/`Charge`/`Heal`/`Pincer`/`Orb`/`Crit`/`Pushback`/`Debuff`/`Enrage`/`Clutch`/`Death`/`Victory`/`Defeat`/…) and resolves **any unknown key** to a deterministic hash-pitched blip, so there is no "sound not found" error.
- **`Managers/Jukebox`** owns persistent DontDestroyOnLoad audio sources (works in vendor scenes too); **`MusicDirector`** (`[RuntimeInitializeOnLoadMethod]` + `activeSceneChanged`) picks the music key per scene with no per-scene wiring.
- **Addressables.** `Editor/AudioAddressableRegistrar` (idempotent) registers the SFX pack and music as Addressables (`MusicTracks/<name>`, `SoundEffects/<name>`).

### 12.0.1 AnnouncementWindow — cadenced event callouts

Game events are announced in a dedicated **`Canvas/AnnouncementWindow`** (auto-spawned in the battle HUD by `AnnouncementWindowFactory` via `ManaPoolManager.Start`): "X casts Ice", "Cyclops is ENRAGED!", "Slime is poisoned", etc. **Cadence is mandatory — never an instant text swap:** each announcement is **queued** and played one at a time with a rapid **flash**, a readable **hold**, then a **fade**, plus an "Announce" chiptune sting. Call `AnnouncementWindow.Announce(text)` from anywhere (no-op outside battle). Wired for hero casts, enemy charges, boss phase transitions and debuff application; every announcement is also mirrored into the combat feed (§9). See the effect-cadence rule (no instant anythings).

### 12.0.2 PacingConfig — centralized combat-feel beats

Every "breathing room" duration in the combat flow lives in **`Data/Config/PacingConfig.cs`** —
the single tuning surface for game feel (sibling of `TimelineBarConfig`/`ActionTitleConfig`). The
`Intermission.Before.*` accessors in `Common.cs` read from it: enemy turns have a telegraph beat
(move 0.35s, attack 0.45s), pincer damage gets a 0.2s pre-hit beat, floating combat text holds fully
readable for 0.45s before a 0.30s fade, the "Counter!" callout holds 0.35s, the forced-drop settle is
0.25s, and the strike/dodge animation beats are floored at the ~0.12s perception threshold. The
Victory/Defeat 1.2s banner hold and the timeline queue-delay formula
(`TimelineBarConfig.QueueDelayFromSpeed`) also live in config. Debug: **"Log Pacing"** button →
`DebugManager.Demo_LogPacing()`. Per the effect-cadence rule: nothing the player must read may
resolve in a single frame.

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
| Music track | `MusicTracks/<name>` | `MusicTracks/Crusade` |
| Sound effect | `SoundEffects/<name>` | `SoundEffects/<clip>` |

**Rule:** when adding a new asset, register the address using the matching convention; missing/typo'd addresses surface as the magenta error sprite (the author's last-resort fallback).

### 12.2 Procedural placeholders

`Editor/SpriteAssetAuthor.cs` builds placeholder sprites at edit-time:
- **Mana orbs**: `orb-body.png` (radial gradient white→transparent, 256×256) + `orb-glass.png` (white highlight upper-left, 256×256).
- **Spell icons**: `<SpellName>.png` (64×64 colored disk + first-letter glyph via tiny 5×7 pixel font), one per `SpellLibrary` entry.
- **Timeline tag icons**: `<TagName>.png` (64×64 colored **diamond** + two-letter code — diamond so
  tag icons read differently from circular spell icons), one per tag in
  `SpriteLibrary.GetActorTagIcon`'s priority list. Gap-fill only (never overwrites the hand-made
  icons), so every priority tag has an icon.

Each save:
1. Writes the PNG to `Assets/Sprites/Mana/` or `Assets/Sprites/Spells/`.
2. Sets `TextureImporter` to Sprite + Bilinear + AlphaIsTransparency.
3. Adds the asset to the project's default Addressables group with the address `Sprites/Mana/orb-body`, `Sprites/Spells/Fireball`, etc.

Run via `Tools/Sprites/Author Mana Orb Sprites` and `Tools/Sprites/Author Spell Icons (Placeholders)`. Real art swaps in by overwriting the same PNG — address stays the same.

### 12.3 VFX prefabs

VFX are the documented EXCEPTION to "no prefabs" because particle systems have dozens of tightly-coupled modules best authored as prefab. Authoring still stays in code (`Editor/VfxPrefabAuthor.cs`) — each `Tools/VFX/Author '<Name>'` menu builds a `ParticleSystem` GameObject programmatically (Main / Emission / Shape / Velocity / Size / Color modules), saves a `.prefab` to `Assets/VisualEffects/`, deterministic + regeneratable.

Per-spell custom VFX in the catalog: IcyWind, FlamingTwist, ShockBolt, SleepDust, HealAura, PoisonCloud, AntidoteSparkle, ScanRays, SlowShimmer, SilenceMute. All 10 prefabs exist in `Assets/VisualEffects/`, are registered in `VisualEffectLibrary`, and `SpellLibrary` references each from its themed spell (FlamingTwist = Fire's twist projectile, IcyWind = Frost impact, ShockBolt = Bolt impact, SleepDust/SlowShimmer/SilenceMute = status impacts, HealAura = Heal/MassHeal impact, AntidoteSparkle = Antidote impact, ScanRays = Scan impact, PoisonCloud = Poison linger). `VfxPrefabAuthor.SavePrefab` auto-registers the Addressable. Visual tuning happens in play-test; regenerating is idempotent.

Shader fallback chain in the author: URP → built-in → Sprites/Default → magenta error, so render-pipeline switches don't silently break.

---

## 13. Combat Resolution

### 13.1 Damage formulas

**Two damage paths**: physical (pincer attacks) and spell (dispatcher).

#### 13.1.1 Physical pincer damage

Resolved by `Formulas.CalculateAttackResult(attacker, opponent)`. Stat-derived inputs:
- `Offense(attacker)` = function of `Strength` + weapon bonus.
- `Defense(opponent)` = function of `Vitality` + armor bonus.
- `MagicOffense / MagicDefense` = parallel pair using `Intelligence / Wisdom` (dispatcher spells do not route here — §13.1.2).
- Crit chance scales with `Luck`.
- Miss chance scales inversely with attacker `Agility` vs target `Agility`; a `Blinded` attacker's hit chance is multiplied by `Buffs.BlindedAccuracyMultiplier` (0.5, `Formulas.CalculateHitType`, US-013).

Resulting `AttackResult` carries `Damage`, `IsCrit`, `IsMiss`, `HpDelta`. Supporters add their own `Offense`-derived chunks via `PincerAttackSupportSequence`.

**Both endpoints hit the whole line.** One `AttackResult` is built per trapped enemy *per attacker* (`PincerAttackManager`), so every enemy in the line takes damage from both flanking heroes. When a single drop spawns chained pincers, an enemy killed by an earlier link is `IsDying` (HP 0, not yet despawned) when the next link resolves; `AttackHelper.SingleAttackRoutine` skips any non-`IsPlaying` target, so the dead enemy is passed over while the survivors behind it still take their hit.

**Respite** is cosmetic only: if an attacker's *entire* trapped line is already dead by the time its link resolves, it plays a little victory spin + "Respite" text (`Spin360AndWaitRoutine`) instead of swinging at corpses. It never suppresses damage — an attacker with even one living target performs the real attack (`PincerAttackSequence`).

#### 13.1.2 Spell damage (dispatcher)

Computed inline in `SpellEffectDispatcher.ApplyDamage`:

```
raw       = spell.BaseDamage
resMult   = ActorData.ResistanceMultiplier(spell.DamageType)   // 0..2+ (per-class)
          × Π(equipped item.ResistanceModifiers[type])         // US-043: gear folds in, multiplicative
elemBonus = (spell.DamageType == Lightning && BuffSystem.Has(target, "wet"))
             ? Buffs.LightningWhenWetMultiplier
             : 1.0f
buffMult  = BuffSystem.GetIncomingDamageMultiplier(target)     // Protection 0.85, others 1.0
final     = round( raw × resMult × elemBonus × buffMult )

if (spell.BaseDamage > 0 && resMult > 0) final = max(1, final)   // min-1 floor (§3.2)
target.Stats.HP = clamp(target.Stats.HP − final, 0, MaxHP)
BuffSystem.OnDamaged(target)                                    // breaks Sleep, etc.
```

Dispatcher spell damage does **not** route through `Formulas.CalculateAttackResult`, so crit/miss/blind do not apply to it (unifying the paths is a backlog item in `USER_STORIES.md`).

#### 13.1.3 Healing

```
final = max(0, round(spell.BaseHeal))
target.Stats.HP = clamp(target.Stats.HP + final, 0, MaxHP)
```

No resistance applies; heals never crit.

### 13.2 HP delta

`ActorInstance.Stats.HP = Mathf.Clamp(Stats.HP - dmg, 0, Stats.MaxHP)`. Then `BuffSystem.OnDamaged(target)` to break sleep-like buffs.

### 13.3 Death

HP reaching 0 = death. Handled by existing `DeathHelper` / death sequence (separate from spell dispatcher). Coins drop on enemy death via existing `CoinManager`.

### 13.4 The interrupt path

Any landing hit on a casting actor — a hero mid-cast, or an enemy charging a spell — interrupts through one **cast-stagger** rule. Hits are routed centrally from `ActorInstance.DamageRoutine` (pincers, magic, shield) to `TimelineBarInstance.InterruptCastsByOwner(owner, attacker)`, which is a no-op unless the owner has a cast in flight; `CastInterruptResolver` decides the outcome.

**Cast stagger.** A Miss never interrupts. Each landing hit pushes the cast back on the timeline — its remaining cast time increases (`CastingState.AccumulatedInterruptDelay`, `TimelineIcon.DelayCast`; the small below-the-line cast icon, §2.6, slides away from the trigger). The delays **accumulate**; **once the accumulated delay exceeds the spell's original cast time, the cast is CANCELLED** (`CastingState.Interrupt()` — MP stays consumed, no effect, icon removed). Otherwise the cast survives, just later.

**Two stats matter.** **Wisdom = caster poise**: higher WIS both reduces the push-back per hit and gives a chance to **shrug a hit off entirely** (no delay). Attacker **Strength** increases the push. **Luck = Clutch** (hero casts only): a rare LCK-driven Clutch is rolled **first** (≈ `LCK/200`, capped 20%). Order per hit: **Clutch (LCK) → WIS-shrug → add stagger delay → cancel if total ≥ cast time.**

**Clutch** (US-025). On a Clutch proc, `InterruptCastsByOwner` pauses the spell icon and `AddFirst`-queues `Sequences/ClutchSequence`: a white full-screen flash + "Heal" SFX + "Clutch!" combat text, then `TimelineIcon.ForceResolve()` snaps the icon to `u = 1` and fires the **same** resolution closure a natural arrival uses (EnterResolvingMode → suspend input → apply effect → end turn). The dying-healer miracle save: the cast shrugs the hit AND resolves on the spot. Demo: "Clutch! (Force)". Enemies never Clutch (`CastInterruptResolver.Resolve(..., allowClutch: false)`) — a hit must never instant-resolve an enemy charge.

**Enemy charges** (US-026). A Caster (tagged `Magic`) that isn't cardinally adjacent to a hero telegraphs a charge: the pure `EnemyPlanner.PlanCast` decides, `EnemyTakeTurnSequence` queues `EnemyChargeSequence` in place of the move/attack chain, and the charge spawns a cast icon through the team-agnostic `SpawnSpellIcon`. At u=1 it resolves into a `MagicAttackSequence` with NO `EndTurnSequence` — it resolves on the shared clock, not as a turn. `EnemyChargeCatalog` derives the element from affinity tags (IceMauler is an Ice caster). Fire-affinity casters lock a cardinal **line** instead (§14.2). **Cancelling** a charge mints one orb of the charge's color (`MintInterruptOrb`, US-027, §3.1.2).

---

## 14. AI

### 14.1 Enemy planning

`Services/EnemyPlanner.PlanStep(enemy, actors, tileMap)`:
1. If `BuffSystem.IsImmobile(enemy)` → stay put.
2. Pick best target hero (nearest + HP-weighted, minus an INT-scaled threat term — US-080).
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

The −100 self-flank avoidance outweighs the +50 pincer-seek, which means **enemies will not walk into a hero pincer even to form their own pincer**. That's intentional: enemies pick safe pincers, not suicide pincers. If a designer wants a kamikaze Bruiser archetype, tweak the score weights via a new `archetype` tag.

#### 14.1.2 Score weight cheat sheet

| Factor | Weight | Reasoning |
|---|---|---|
| Distance to target | −1 per tile | Closer is better (advance) |
| Cardinal-adjacent to target | +2 | "In range to swing next turn" |
| Would be flanked by heroes here | −100 | Hard avoid (single hardest signal) |
| Stay put | −0.5 | Mild bias to keep advancing |
| Forms Humanoid pincer here | +50 | Pincer-seek beats positional ties, loses to flank-avoidance |
| **Target's threat (US-080)** | −(threat∕maxThreat) × INT × 0.8 | Smart enemies hunt whoever's hurt them. Applied to *target choice*, not the step score: high-INT enemies prefer the top damage-dealer; low-INT barely weight it. Threat = damage dealt this battle (`ThreatTracker`). |
| **Wounded retreat (US-081)** | flips advance→flee below 0.30 HP | A badly wounded enemy maximizes distance from the target instead of minimizing it, and drops adjacency/pincer-seek bonuses (flank-avoid still applies). |
| **Support ally pincer (US-082)** | +25 | Move makes this enemy a supporter of *another* ally's Humanoid pincer (`FindSupporters`). Below its own +50 pincer-seek; suppressed when fleeing. |

These weights are the **only tuning knobs**; they live as constants in `EnemyPlanner`. Adding new behaviors (e.g. range-keep) means new factor branches.

### 14.2 Enemy archetypes (design palette)

Different enemies should *feel* different by their `ActorData.Tags` + base stat distribution + which `ActorTag.Humanoid`-style mechanics they engage in. Suggested archetypes for designers:

| Archetype | Stat lean | Tags | Behavior the planner expresses |
|---|---|---|---|
| **Rusher** | high SPD, low VIT | `Humanoid, Beast` | Loads timeline fast; closes the distance every turn. Wants to be adjacent and swing. |
| **Bruiser** | high STR + VIT | `Humanoid, Soldier` | Slower load but high HP and threat. Best target for a pincer. |
| **Flanker** | mid STR, high AGI | `Humanoid` | Seeks pincer formation with another Flanker. Devastating if ignored. |
| **Ranged** | high AGI, mid INT | `Humanoid` | Wants distance — keeps gap from heroes; attacks across tiles (future: requires a Line shape) |
| **Caster** | high INT, low VIT | `Humanoid, Magic` | When not adjacent to a hero, telegraphs an affinity charge-cast on the timeline (`EnemyPlanner.PlanCast` → `EnemyChargeSequence`) that resolves into a magic hit at u=1; high reward for interrupting (US-027) |
| **Beast** | varies | `Beast` (no Humanoid) | Can NOT pincer. Just rushes. Cheaper threat density. |
| **Mechanical** | high VIT, status-immune | `Mechanical, Boss` | Status immunities (Resistances 0 for Poison/Sleep). Pure HP race. |
| **Boss** | very high everything | `Humanoid, Boss, Elite` | Authored HP-threshold phases via `BossScriptLibrary` (data-driven) → `BossPhaseRunner` fires a one-time `BossPhaseTransitionSequence` on phase entry, queued through `SequenceManager` (US-083). Per-phase `PrefersCharge` knob. Cyclops enrages (Quicken) below 50% HP. Often a 2×2 (§1.5). |
| **Line caster** | as Caster | `Magic, FireAffinity` | `EnemyChargeCatalog.ShapeFor` → `ChargeShape.Line`: at telegraph time the charge locks a **cardinal line** from beside the caster to the board edge (`Services/LineThreat` — dominant axis, X on ties). `LineTelegraph` glows the threatened tiles red for the whole charge; at u=1 every hero still on the line takes a `MagicAttackSequence`, an empty line posts "dodged!". Slide out of the line, or interrupt (US-138). Edge-pinned degenerate lines fall back to single-target. |
| **Trap-layer** | varies | (e.g. Scorpion) | `TrapCatalog` (Scorpion: Venom Snare, 6 dmg + Poisoned). 45% roll per turn when no hero is adjacent: `PlaceTrapSequence` arms an unoccupied cardinal-adjacent tile (glows purple). `TrapManager` (per-battle static) owns state; a trap fires on BOTH a hero slide (`ActorMovement.CheckLocationChanged`) and a displacement (`HandleOverlap`) — enemies weaponize your slides. Traps are visible by design (US-139). |
| **Segmented snake** | boss | `Naga00` | Every spawned `Naga00` is a chain head: `StageManager.LoadWave` grows 3 body segments behind it (`SnakeBossManager.CreateChain`). The head keeps the timeline icon and turns; segments have no icon and follow the head's vacated trail front-to-back. **Tail-first:** a member is armored (all damage zeroed, "Armored!") while any later member lives. Chain members are immovable walls to drags (US-140). |

### 14.3 AI hooks

- **Casting enemies** (US-026/US-027): telegraph a charge on the timeline (`EnemyChargeSequence`); a hero's landing hit staggers it (§13.4) and, on cancel, drops an orb of the charge's color into the team bank (§3.1.2).
- **Supporter positioning** (US-082): an enemy is rewarded (+25) for moving to a tile where it becomes a §1.2.3 **supporter** of another ally's Humanoid pincer (`EnemyPlanner.WouldSupportAllyPincer`, reusing `PincerDetector.FindSupporters`); own-pincer formation still wins (+50). Demo: "Log Enemy Plans".
- **Boss scripted phases** (US-083): `BossScriptLibrary` (per-class ordered phases {HpThreshold, PrefersCharge, transition `SequenceEvent` factory}); `BossPhaseRunner.AdvancePhasesAndCollectTransitions` fires on threshold crossings; `EnemyTakeTurnSequence` queues transitions + honors `PrefersCharge`. Demo: "Trigger Boss Enrage".
- **Threat tracking** (US-080): `ThreatTracker` tallies hero→enemy damage; `EnemyPlanner` subtracts a normalized-threat × INT × 0.8 term from each candidate's target score, so **smarter (high-INT) enemies prefer the top damage-dealer**. Cleared at battle start. Demo: "Log Threat".
- **Coordinated retreat** (US-081): below `RetreatHpThreshold` (0.30 HP fraction) an enemy flees the target (maximizes distance) and drops its adjacency/pincer-seek biases; flank-avoidance still applies. Demo: "Test Enemy Retreat".

---

## 15. Save / Profile

### 15.1 Data model

```
Profile                       (one per player — Models/Profile.cs)
├─ Key, Folder
├─ Settings          ProfileSettings   (volumes, mutes, ColorblindMode, ReduceMotion, …)
├─ CurrentSave       SaveState
└─ SaveStates[]      SaveState         (newest first; LatestSave = SaveStates[0])
       ├─ Global      GlobalSaveData    (TotalCoins lifetime ticker, SeenStoryCrawls)
       ├─ Stage       StageSaveData     (CurrentStage, CurrentWave, HighestClearedStageIndex)
       ├─ Roster      RosterSaveData    (Members: CharacterLevelPair[])
       ├─ Party       PartySaveData     (Members: CharacterLevelPair[] — the active squad)
       │                 CharacterLevelPair = CharacterClass, TotalXP, HpCurrent
       ├─ Inventory   InventorySaveData (Gold wallet; Items: ItemId, Count,
       │                                 CurrentDurability, RepairCount)
       ├─ Equipment   EquipmentSaveData (Heroes: HeroEquipmentSave — WeaponId, ArmorId,
       │                                 Relic1..3Id, durability/repair counts,
       │                                 AbilityBarSlots[])
       ├─ Bounty      BountySaveData    (ActiveBountyId, Progress)
       ├─ Bestiary    BestiarySaveData  (Entries: CharacterClass, Seen, Defeated, TimesDefeated)
       └─ Training, CraftJobs, Overworld (further sections; Overworld is unused — §28)
```

XP is stored as `TotalXP`; level + currentXP are **derived** via `ExperienceHelper.DeriveFromTotalXP(totalXP)` — no migration when the curve changes. **Wounds carry between battles** (US-053): victory persists each party hero's HP in `CharacterLevelPair.HpCurrent` (`0` = full), which is hydrated on spawn; defeat resets the party to full. Recovery is the Alchemist's "Heal Party" service — 0.5g per missing HP (US-122, §25.3). The serializer cannot do Dictionaries, so keyed collections (Bestiary, Inventory) are lists.

### 15.2 Persistence flow

| Trigger | Saved to disk? |
|---|---|
| Vendor purchase/sale | Yes (commit-on-vendor-exit) |
| AbilityBar reassign (Abilities scene) | Yes (commit on scene exit) |
| Equip change (Equip scene) | Yes (commit on scene exit) |
| Mid-battle stat/hp changes | No (held in `ActorInstance.Stats`) |
| End of battle | Yes — **victory** persists each party hero's current HP (`HpCurrent`; wounds carry forward, a hero who fell in a won battle revives at 1 HP); **defeat** resets the whole party to full; XP/loot committed (US-053) |

### 15.3 Per-hero ability bar and Bestiary

- `HeroEquipmentSave.AbilityBarSlots` holds the per-hero bar edited in the Abilities scene, with a full hydrate/persist round-trip through `HeroLoadout.LoadFromSave` / the save path (`HeroLoadout.cs`). Slots resolve by ability name (`AbilityLibrary.Get`), item id or weapon id. The combat bar reads the same slots through `CombatLoadouts` (§4.2). *(Verified by `AbilitySlottingTests`, `SaveRoundTripTests`, `CombatLoadoutTests`.)*
- `SaveState.Bestiary` records each enemy class **Seen** on spawn (`StageManager.SpawnActor`) or Scan, and **Defeated/TimesDefeated** on death (`ActorInstance.DieRoutine`), persisted at battle end. The Bestiary view reveals seen classes and shows the rest as silhouettes (US-093).

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

6. **`ManaAbility` cannot be renamed to `Ability`.** The legacy `Ability` class lives in `Instances/AbilityButton.cs`. Keep the new bar-data class as `ManaAbility` to avoid type collision. The legacy `Ability` class is still heavily referenced.

7. **`TileManager.Reset()` NREs during builder rebuild.** Unity's `Reset()` magic method fires when a builder calls `AddComponent<TileManager>()`. At that moment `g.Tiles` is null (no GameManager initialized). Always null-guard `g.X` accessors in `Reset()` methods.

8. **Builders must clear roots first.** Every builder (including `GameBuilder.Build()`) calls `SceneBuilderHelper.ClearAllRootObjectsSilent()` at the top, matching `CliEntryPoints.InvokeBuilderCreate`; skipping it produces "Can't add component X — already exists" warning spam that hides real errors.

9. **`AddressableAssetSettings` missing.** If the project's Addressables aren't initialized, `AssetHelper.LoadAsset<T>` returns null and `SpriteAssetAuthor` logs a warning. Open `Window → Asset Management → Addressables → Groups` once to seed the default group.

10. **`LayerMask.NameToLayer("UI")` can return -1.** Any camera `cullingMask` built from it must guard: `uiLayer >= 0 ? (1 << uiLayer) : ~0` (as `BestiaryBuilder` does).

11. **Every Canvas uses the §26.2 scaler, every ScrollRect is fully wired.** `CanvasScaler.referenceResolution = (1170, 2532)` with `ScaleWithScreenSize` + match 0.5 (a zero reference resolution mis-scales everything); a `ScrollRect` needs `viewport`, `content`, `vertical = true`, `horizontal = false` or the list cannot scroll. `UiKit.ScrollList` and `SceneBuilderHelper.EnsureCanvas` do both.

### 17.2 Cadence

- Keep going until the whole feature works end-to-end before committing ([[feedback_commit_granularity]]).
- Ship a `DebugManager.Demo_*` method + Debug-Window button with every new system so the user can test by clicking, not by being asked "does it work?" ([[feedback_debug_window_demos]]).
- Run the automated suites headless with `tools/run-tests.ps1 -Platform EditMode|PlayMode` (the Editor must be closed — project lock); feel and layout still need an in-editor play-test.
- Never `taskkill` Unity.exe to recover from a stuck batchmode — ask the user to close the editor cleanly ([[feedback_force_close_unity]]).
- Don't suggest `/Run` to launch the game; that routes to `/loop`. Use the PS1 console Option 1 ([[feedback_no_run_slash_command]]).

### 17.3 Checkpoint recipe (before handing back for commit)

When work feels done, run this sequence — don't ship until each is green:

```
1. Source mtime > Library/ScriptAssemblies/Scripts.dll mtime?
   → Yes: Unity recompiled. Continue.
   → No:  Recompile didn't fire. Ask user to focus the Editor.

2. Unity Console errors?
   → Yes: Diagnose; do NOT push.
   → No:  Continue.

3. New gameplay rule?
   → Yes: docs/BIBLE.md updated? If no, update it. (§32)
   → No:  Continue.

4. New system?
   → Yes: DebugManager.Demo_* + DebugWindow button added?
   → No:  Continue.

5. Touched a guardrail (SerializedField, Resources.Load,
   Instantiate outside Factory.cs, scene drift)?
   → Yes: regenerate the allowlist (CliEntryPoints) or
          fix the violation.
   → No:  Continue.

6. Automated suites green (tools/run-tests.ps1, Editor closed),
   then play-test in Editor (user clicks Play; reports back).
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
| **Add a new debuff** | `Data/Buffs.cs` (catalog row) + `Canvas/DebuffIconBar.ColorFor` + `LetterFor` | Add its gameplay hook in the relevant system if it should affect formulas; add a glyph to the combat-feed sprite asset |
| **Add a new spell** | §19 checklist (`ManaAbilities`, `SpellLibrary`, `HeroLoadouts`) | Update §7 catalog table |
| **Add a new enemy class** | §20 checklist (`CharacterClass` enum, `Data/Actor/<X>.cs`, `ActorLibrary`) | Stage + drop table separately |
| **Add a new sprite/asset** | PNG on disk in `Assets/Sprites/...` + `SpriteAssetAuthor` (procedural) OR Addressables config | `AssetHelper.LoadAsset<T>(address)` to consume; register in `SpriteLibrary` if reused |
| **Add a new VFX prefab** | `Editor/VfxPrefabAuthor.cs` menu item → registers prefab → paste registration into `VisualEffectLibrary` | Choose CastVfx vs Linger vs Impact slot carefully (looping = linger only) |
| **Tweak pincer damage** | `Utilities/Formulas.CalculateAttackResult` | Update §13.1.1 if formula shape changes |
| **Tweak spell damage** | `SpellDefinition.BaseDamage` in `SpellLibrary` | Per-spell change, no formula rewrite needed |
| **Tweak enemy AI weight** | `Services/EnemyPlanner.cs` (constants section) | Update §14.1.2 table |
| **Add a vendor screen item** | The vendor's manager (`VendorManager`, `BlacksmithManager`, …) + recipe/data registration in `Data/` | Run the scene's builder to refresh layout if UI rows added |
| **Hook a new Save field** | the matching `*SaveData` class in `Models/Profile.cs` (including its copy constructor) + the write site (`ProfileHelper.Save`) | Migration: handle old saves missing the field; lists, not Dictionaries (serializer limit) |
| **Add a Debug Window demo button** | `Assets/Editor/DebugWindow.Demos.cs` (or add a new `Demo_*` method to `DebugManager`) | Follow [[feedback_debug_window_demos]] — every new system gets one |
| **Change scene transition** | `Helpers/SceneHelper.cs` (`ToX` methods) + any builders that wire the button to it | Persistent UnityEvent → wire to a real MonoBehaviour method |
| **Add a buff cross-effect** | `Data/Buffs.cs` (constants like `LightningWhenWetMultiplier`) + the relevant dispatcher stage | Update §8.2 interaction matrix |

---

## 18. Glossary

### Combat / mechanics

- **AbilityBar** — Row-13 bar (6 buttons, 2–5 usable by campaign progress). Holds Skills / Spells / Items per the selected hero.
- **ManaBank** — party-wide line of 12 colored orbs (WUBRG palette).
- **Pincer** — two Humanoid heroes flanking a contiguous enemy line on a single row or column; deals damage to every enemy in the line.
- **Supporter** — ally cardinally adjacent to a pincer endpoint with unbroken line of sight; adds bonus damage to that endpoint.
- **Slide / displace** — what dragging a hero through an occupied tile does to the occupant (single tile, into the tile the dragger just left).
- **Prepare Zone** — rightmost 25–35% of the timeline (`u ≥ 1 - ZoneU`); in-Zone icons crawl at a uniform pace and are the prime interrupt window.
- **Pushback** — leftward shove applied to a damaged icon **only if** it was in the Prepare Zone; followed by `Stunned` mode while it stops.
- **Cast icon** — small spell-sprite icon (≈¼ normal size) on a lane **below** the timeline bar line; it spawns at `u = 1 − castTime × pace` and resolves on reaching u=1 (§2.6).
- **Interrupt outcomes** (stagger model, §13.4) — *Clutch* (rare LCK pre-check: cast shrugs the hit and snaps to the trigger to resolve — US-025), *Shrug* (WIS poise ignores the hit), *Stagger* (cast-time delay added; accumulates), *Cancel* (total delay ≥ cast time → cast canceled, MP gone).

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

- **Skill** — costs the hero's turn but no mana; locked by a per-skill cooldown after use (§4.1.1).
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
    ManaAbilities.NewPotion(3), null
});

// 4. (no new debuff needed)

// 5. Tools/Sprites/Author Spell Icons (Placeholders)
//    → generates Sprites/Spells/Quake.png with the brown "Q" glyph + registers Addressable.

// 6. (optional) Tools/VFX/Author 'RockBurst' to author a custom prefab.

// 7. Add row to §7 catalog:
//    | Quake | (R)(B) | Square(r=1) / PickTile / EnemyOnly | None | 16 Physical AOE | Barbarian's anti-clump tool |
```

What you DON'T touch: the dispatcher, the picker, the cast icon, the orb bank, the mana economy, the HUD layout. Those are stable and parametric — only data files change.

---

## 20. How to Add a New Enemy — Checklist

1. Add a `CharacterClass` enum entry in `Helpers/CharacterClass.cs`.
2. Create a `Data/Actor/<Name>.cs` with `Data()` factory returning `new ActorData { ... }`.
3. Set `Tags`: include `Enemy`; include `Humanoid` if the enemy can pincer; add `BeastFlying`/etc. as appropriate.
4. Set `Resistances` dict for elemental profile (omit = 1.0 neutral).
5. Register in `Libraries/ActorLibrary.cs`.
6. Add to a `StageLibrary` wave / `DropTableLibrary` table for an actual encounter.
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
//    (StageLibrary) add CharacterClass.FrostWolf to a stage wave's actor list
//    DropTableLibrary.For(CharacterClass.FrostWolf) = new[] {
//        new DropEntry(ItemData_Materials.WolfPelt, weight: 70),
//        new DropEntry(ItemData_Materials.IceShard, weight: 30),
//    };
```

EnemyPlanner treats it as a Rusher-Beast: closes distance, swings in melee, never tries to flank. The Bestiary lists it automatically as a silhouette until it is first seen.

---

## 22. The Macro Loop

The game's beat-to-beat shape:

```
SplashScreen → TitleScreen
   ↓ (Continue / New Game → ProfileCreate → Title)
ProfileSelect / ProfileCreate / SaveFileSelect
   ↓
StageSelect  ◀════════════════════════════════╗   (scrollable level list, §22.3;
   │  ↕ VendorNavBar (hamburger)              ║    BountyBar contract strip, §22.4)
   │   → Vendor / Blacksmith / Alchemist /     ║   (the same NavBar rides every vendor
   │     Equip / Party / Abilities / Summon    ║    scene — hop freely, "Campaign" = home)
   ↓  Confirm stage                            ║
StoryCrawl (first entry into a theme only)     ║
   ↓                                           ║
Game.unity — clear ALL waves of the stage      ║   (waves spawn in sequence)
   ↓                                           ║
PostBattleScreen — XP + items + GOLD awarded   ║   (gold = coins collected in-battle,
   ↓                                           ║    bridged by GoldTracker, §24.9)
└════════ back to StageSelect (next stage now unlocked, on top) ═╝
```

- **Stage = waves.** A stage runs its waves in sequence (`StageLibrary`); clearing the **last** wave ends the battle → PostBattle. Beating a stage unlocks the next, which appears **on top** of the list (§22.3).
- **Reward beat.** PostBattle awards XP, items, and gold (coins collected in-battle, committed by `GoldTracker` — §24.9), commits the save, then returns to StageSelect.
- **VendorNavBar is the vendor navigation** (§25.0): the floating hamburger dropdown on StageSelect and every vendor scene hops directly between vendors; its "Campaign" entry returns home.
- **Failure path**: all heroes die in Game → PostBattleScreen with "Defeat" → StageSelect (no permadeath; the stage can be retried).
- **No Overworld.** There is no world-map / exploration scene; stage navigation is the scrollable level list in StageSelect (§22.3).

`SceneHelper.Fade.ToX()` / `SceneHelper.Switch.ToX()` are the canonical scene-switch entry points.

### 22.1 Per-transition state contract

Each scene transition has a **side-effect contract** — what state must be committed before the switch, what hydrates in the next scene's `Awake`. Listing the contracts keeps state from leaking into the wrong scope:

| From → To | Commit before | Hydrate on arrival |
|---|---|---|
| `Game` → `PostBattleScreen` | The static session-tracker trio carries the result: `ExperienceTracker` (XP gains), `LootTracker` (drop-table items), `GoldTracker` (coins collected this battle) — all sessions started in `StageManager.Initialize()` | `PostBattleManager` plays the XP phase, then the loot phase (Gold row first), commits via `*.CommitToInventory()` + `ProfileHelper.Save(true)` |
| `PostBattleScreen` → vendor (any) | Saved profile already has the new XP/HP/inventory | Vendor scene Awake: `PlayerInventory.HydrateFromCurrentSave()` |
| Vendor → Vendor (via NavBar) | Active vendor commits its inventory mutations to `ProfileHelper.CurrentProfile.CurrentSave` | New vendor hydrates from same save |
| Vendor → `StageSelect` | Commit (same as above) | StageSelect reads `Stage.HighestClearedStageIndex` for unlocks |
| `StageSelect` → `Game` | `StageSelectManager.ConfirmLaunch` — the only surface that sets `Stage.CurrentStage` / `CurrentWave` and the post-battle return scene — routes through `StoryCrawl` on first entry into a theme | `StageManager.Initialize()` reads `CurrentSave` and spawns the stage's waves from `StageLibrary`; enemy levels are floored to `CampaignStages.RecommendedLevel` (§22.3) |

### 22.2 Failure path detail

When all heroes hit `HP <= 0`:
1. `StageManager.CheckBattleLost()` detects party wipe → queues `BattleLostSequence` → PostBattleScreen.
2. `PostBattleScreen` shows "Defeat" banner, plays sad fanfare, and does **not** apply XP or rewards (per V1 — defeat is a retry, not a permanent loss).
3. Heroes' HP is restored to MaxHP (since defeat doesn't carry wounds).
4. "Continue" → `SceneHelper.Fade.ToStageSelect()`.

(No permadeath; a roguelike/NG+ mode is an open question, §29.1 #1.)

### 22.3 StageSelect — the scrollable level list

Stage navigation is a **vertically scrollable list of levels**, same look-and-feel as the load/save-file screen (`SaveFileSelect`) but for picking the next battle. It is the *only* navigation surface — there is no world map.

**Behavior:**
- **Newest-on-top.** Each newly-unlocked level is **prepended to the top** of the list; older/earlier levels scroll below. The most recent frontier is always the first thing the player sees.
- **All unlocked levels stay replayable.** Clearing a stage unlocks the next but does **not** consume or grey out the cleared one. The player can scroll down and re-enter any previously-beaten level at will.
- **Farming is the point.** Because cleared stages remain replayable and each enemy class has its own drop table (§24.7), the player goes back to a specific stage to farm a specific material an enemy there drops — e.g. re-run the Frost stage for Ice Shards to fund a Blacksmith upgrade. This is the intended grind loop; the list is built to support it, not to lock progress behind one-shot stages.
- **Unlock gating (linear frontier, open backtrack).** A stage is unlocked when the prior stage is cleared (`HighestClearedStageIndex`, `CampaignStages.IsUnlocked`). So progression is linear *forward* (you can't skip ahead), but fully *open backward* (every unlocked stage is freely re-enterable). Locked (not-yet-reached) stages render dimmed/disabled.

**Row content (per level):** star prefix, name, cleared marker, lock suffix, and a second-line `drops: X, Y` hint built from the wave enemies' drop tables, so the player knows where to farm what. Rows run themes in reverse (newest theme first) and stages within a theme in reverse. The detail panel shows "Recommended level: N". Confirm → `StageSelectManager.ConfirmLaunch` → (`StoryCrawl` on first entry into a theme) → `Game`.

**Difficulty curve** (US-135): `CampaignStages.RecommendedLevel(stage)` maps campaign stage N to level N; `StageManager.SpawnActor` floors each enemy's level to it (authored higher levels win; `Test-*` and Endless stages are unaffected). XP and coin rewards scale with enemy level, so the reward curve follows. *(Verified by `CampaignStagesTests`.)*

Implementation lives in `StageSelectManager` / `StageSelectBuilder`; unlock data in `StageLibrary` + `CampaignStages`.

### 22.4 Bounty board

A **BountyBar** strip on StageSelect (`StageSelectBuilder.BuildBountyBar`; `StageSelectManager` via `BountyHelper`) lists the posted contracts from `BountyLibrary` / `BountyData_Hunts`. The player can browse, **Accept** one (single active slot, `BountySaveData.ActiveBountyId`), watch kill progress (`RecordKill` is called at enemy death), **Abandon**, and **Claim** the gold + reward item once complete. Changes persist via `ProfileHelper.Save`. *(Verified by `BountyFlowTests`: accept / track / claim / refuse-early / abandon.)*

## 23. Character Classes

Heroes and enemies share the `CharacterClass` enum in `Helpers/CharacterClass.cs`. What separates a hero from an enemy is the `ActorData.Tags` flag (`Hero` vs `Enemy`) plus their AI/control path.

### 23.1 Class identity (what a class encodes)

- **Base stats** (`ActorData.BaseStats`) — STR, VIT, AGI, SPD, STA, INT, WIS, LCK.
- **Stat growth** (`StatGrowth` per level + `MilestoneStatGrowth` at fixed levels).
- **Portrait** + `ThumbnailSettings` + `CanvasThumbnailSettings`.
- **Tags** (`Hero / Enemy / Humanoid / Soldier / Beast / Boss / Mechanical` + elemental affinity flags).
- **Elemental resistance** (`ActorData.Resistances`) — per-`DamageType` multiplier.
- **Default AbilityBar loadout** — `HeroLoadouts.perClass` keyed by `CharacterClass`.
- **Color affinity** — when this hero contributes to a pincer harvest, the dropped orb is this color. `ManaColorAffinity.For(class)` in `PincerAttackManager` (US-030). See §23.2 for the per-class map.
- **No dialogue** — heroes have no lines; the only narrative is the per-theme story crawl (§27).

### 23.2 Hero classes with seeded loadouts

| Class | Identity | Stat lean | Color affinity | Loadout |
|---|---|---|---|---|
| **Cleric** | white-magic healer; sustain-focused; reads enemy intentions | INT/WIS high, STR low | White | Heal, Heal, Frost, Potion(3) |
| **Paladin** | front-line tank with healing on the side | VIT/STR high, mid WIS | **White** (US-030) | Heal, Fireball, Potion(3) |
| **Barbarian** | high-damage front line; brute force | STR/VIT high, low INT/WIS | Red | Fireball, Bolt, Potion(3) |
| **Alchemist** | utility / consumable stacks / non-magical control | INT/AGI mid, high LCK | **Green** (US-030) | Frost, Potion(5), Steal, Potion(5), Sleep Dart(5) |
| **Assassain** | high-damage flanker; rogue toolkit | AGI/LCK high | Black | Steal, Mug, Bolt, Potion(3) |
| **GreenNinja** | mobility specialist; thief variant | AGI/LCK high | Green | Teleport, Steal, Fireball, Potion(3) |
| **RedNinja** | mobility + striker | AGI/STR high | Red | Teleport, Mug, Bolt, Potion(3) |

Completing a pincer drops an orb of each participating hero's color via `ManaColorAffinity.For(class)` in `PincerAttackManager` (US-030). The full map: Cleric **W**, Paladin **W**, Barbarian **R**, Alchemist **G**, Assassin **B**, GreenNinja **G**, RedNinja **R**; unlisted classes default Blue.

A new save's roster is the starting trio — Paladin, Barbarian, Cleric (`ProfileHelper.DefaultRoster`); other classes join through the Summon Circle (§25.10).

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

Every other entry in the `CharacterClass` enum falls through to the default `ManaAbilities.Slots` (Heal/Fireball/Frost/Bolt/Potion). Add entries via `HeroLoadouts.Set(class, list)` to give them distinct kits. Candidates:

- **BlackNinja / BlueNinja / WhiteNinja / YellowNinja / ChromaNinja** — variants of the Ninja archetype; each should feel different (poison-specialist, ice-specialist, etc.).
- **Bruiser** — slow brute, even more lopsided than Barbarian; STR/VIT maxed.
- **Captain** — buffing leader; gives allies Protection at battle start (passive).
- A "Druid" / nature-class would give Green a third class (alongside Alchemist and GreenNinja).

### 23.3 Enemy classes

Enemies aren't playable. Their `ActorData` defines stats, drop table, abilities (per `Ability` legacy class, see §6 + §14), AI behavior. `EnemyPlanner.PlanStep` drives tile-by-tile moves. **Humanoid enemies actively seek pincers** (§14.1); non-Humanoid use the straight-line approach.

### 23.4 Roster + party composition

The save keeps a **roster** of recruited classes (`SaveState.Roster`) and an active **party** (`SaveState.Party`). A new save starts with the trio above; the Summon Circle (§25.10) adds classes to the roster for gold. The Party scene (§25.5) moves heroes between roster and party, capped at 4 (`PartyManager.MaxPartySize`); `ProfileHelper.AddToParty` / `RemoveFromParty` preserve XP across the move.

## 24. Equipment, Items, Materials, Currency

### 24.1 Item types (`ItemType` enum)

- `Equipment` — wearable; takes an `EquipmentSlot`. Relics are equipment in one of the three Relic slots (`ItemDefinition.IsRelic`).
- `Consumable` — single-use stackable (potions, scrolls, throwables).
- `CraftingMaterial` — recipe input (monster fang, iron ingot, fire essence, mana shard…).
- `QuestItem` — not sold or consumed.

Gold is not an item; it is tracked on the save (§24.9).

#### 24.1.1 Rarity tiers

`ItemRarity` enum (`Junk`, then Common → Legendary) drives drop weight, stat range, and `HubItemRowFactory.RarityColor`; the cost multiplier is the authoring guide for an item's `BaseCost`:

| Rarity | Color | Cost multiplier | Stat range (per primary stat) | Drop weight bucket |
|---|---|---|---|---|
| **Common** | white-grey | 1.0× | +1 to +3 | most enemies, all stages |
| **Uncommon** | green | 2.5× | +3 to +6 | mid-tier enemies + low-tier vendors |
| **Rare** | blue | 6× | +5 to +10 | elite enemies, mid vendors |
| **Epic** | purple | 15× | +8 to +15 | bosses, high-tier crafting |
| **Legendary** | gold | 40× | +12 to +25 (sometimes unique passives) | end-game bosses, ultimate crafting |

Total stat-budget per piece scales roughly geometrically; epic+ pieces tend to carry one *named passive* (e.g., "+1 mana orb at battle start" on Mage Robe Epic variant) instead of pure stats.

### 24.2 Equipment slots

`EquipmentSlot` enum: `Weapon, Armor, Relic1, Relic2, Relic3` (plus `None`). A hero's `HeroLoadout` is persisted in `HeroEquipmentSave` (`WeaponId`, `ArmorId`, `Relic1..3Id`, durability + repair counts). `Formulas.ComputeEquipmentBonus(loadout)` aggregates the equipped pieces' stat bonuses into the hero's combat stats.

### 24.3 ItemDefinition fields

```
Id, DisplayName, Description, Type, Rarity (Junk, Common→Legendary), Slot, WeaponType,
BaseCost, SellValue (-1 = BaseCost/2), MaxStack, Durability,
BaseHealing, BaseDamage, BonusDamageVsTag, BonusDamageMultiplier, MaxUsesPerBattle,
Strength, Vitality, Agility, Stamina, Intelligence, Wisdom, Luck,
RequiredTags, SalvageComponents,
BattleStartManaOrbs : int        // Mage/Wizard Robe → adds N random orbs to bank at battle start
OnUseSpellName     : string      // Sleep Dart etc. → consumable that triggers a spell on use
ResistanceModifiers: Dict<DamageType, float>  // elemental gear
```

The last three default to `0` / `null` / empty and are consumed by `ManaPoolManager.ApplyBattleStartManaOrbs` (US-041), `AbilityBar.HandleItem` (US-042) and `SpellEffectDispatcher.ApplyDamage` (US-043).

### 24.4 Inventory

`PlayerInventory` — item ID → `Entry(count, durability)`. Per save file. Vendor scenes hydrate from `ProfileHelper.CurrentProfile.CurrentSave` on Awake, persist on commit.

### 24.5 Weapon durability

Per the locked rule ([[project_weapon_durability_rule]]) — **all built** in `WeaponDurabilityHelper.cs`:
- A weapon takes durability damage per use.
- At durability 0, it **shatters** — deals damage to **both** the target (×1.5 bonus) AND the wielder (15% MaxHP self-damage), then clears the slot (`WeaponDurabilityHelper.cs:37-103`).
- Each repair drops effective max durability by 1 (`EffectiveMaxDurability = max(1, Durability − repairCount)`) and per-point repair cost escalates ×1.6, so gear naturally retires (`WeaponDurabilityHelper.cs:105-138`).

### 24.6 Crafting recipes

`CraftingRecipe`: list of `(materialId, count)` + gold cost → `ItemDefinition` result.
- `RecipeLibrary.All()` catalogs them.
- `CraftingRecipe.CanCraft(inventory)` / `.Execute(inventory)` for atomic check + commit.

#### 24.6.1 Example tier ladder (design target): Iron Sword → Steel Sword → Mythril Blade

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

The ladder is the design target for tiering. Upgrade recipes exist as data (`UpgradeLibrary` / `UpgradeRecipe`) but no UI consumes them; the Blacksmith's live tabs are Forge, Salvage and Repair (§25.2).

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

- **Mage Robes** — armor, Uncommon (`eq_armor_mage`). `BattleStartManaOrbs = 2`. Stacks per hero wearing (US-041).
- **Wizard Robe** — armor, Rare (`eq_armor_wizard`). `BattleStartManaOrbs = 3`. Stacks per hero wearing (US-041).

Battle-start grant: `ManaPoolManager.ApplyBattleStartManaOrbs` (run once at battle start via `GameReady`) sums `BattleStartManaOrbs` across every equipped item on the active party and adds that many **random-color** orbs (WUBRG, not Colorless) to the team bank, clamped to the 12-orb cap (§3.1.4).
- **Sleep Dart** — consumable, per-slot stack (`MaxStack = 5`, `cons_sleep_dart`). `OnUseSpellName = "Sleep"` — on use, opens the Sleep spell's targeting flow and consumes one charge (US-042). It is seeded in slot 5 of the Alchemist's per-class bar. The bar slot carries `ManaAbility.SourceItemId` so `HandleItem` recovers the item and routes the cast (first item-casts-a-spell path; generalizes to any consumable with `OnUseSpellName`).

### 24.9 Currency

Gold is the universal currency, and the save carries **two distinct fields**:

- **`Inventory.Gold`** — the wallet. The only field vendors read/spend: purchases / Blacksmith
  forging & repair / Alchemist brewing deduct it; selling at vendor returns ~50% `BaseCost`;
  bounty claims credit it.
- **`Global.TotalCoins`** — the lifetime pickup ticker behind the in-battle CoinCounter HUD.
  A stat, never spent, never reset.

The bridge between them is **`GoldTracker`**: it snapshots `TotalCoins` at battle start and commits
the per-battle delta (the coins the player actually collected via `CoinManager` pickups) into
`Inventory.Gold` at the PostBattle loot phase, shown as the leading "Gold +N" row. *(Verified by `GoldTrackerTests`, including no double-count across battles.)*

## 25. Vendor Scenes

Seven dedicated scenes — Vendor, Blacksmith, Alchemist, Equip, Party, Abilities, Summon — each with its own `<X>Builder.cs` + `<X>Manager.cs` + `PlayerInventory` hydration ([[project_scene_per_section_migration]]).

### 25.0 Vendor navigation — VendorNavBar

The floating **`VendorNavBar`** hamburger dropdown is the only vendor navigation. It is built into StageSelect and every vendor scene (`VendorNavBarBuilder.Build`) and lists every entry in `VendorNavBar.Entries` — Vendor, Alchemist, Party, Abilities, Equip, Blacksmith, Summon, and "Campaign" (StageSelect). The active scene's row is highlighted and inert. A new vendor scene is added by appending to `VendorNavBar.Entries` and rebuilding.

### 25.1 Vendor (general merchant)

`Vendor.unity` / `VendorManager.cs`. A classic JRPG shop with a **Buy / Sell** mode toggle.

**Layout (portrait):** header (hamburger + Merchant title) · mode toggle · a scrollable list of rows · footer with the running cost/value label and the commit button.

**Rows and carts.** Each row shows the item (rarity-colored name), unit price, and a `[−] N [+]` quantity stepper. Buy and Sell each keep their own cart; switching modes preserves the other mode's pending quantities until commit. The footer shows `Pay` (Buy) or `Receive` (Sell) for the cart total and turns red when the player can't afford a Buy; the action button (`Buy` / `Sell`) commits the whole cart at once.

**Pricing and stock:**
- **Buy** — stock is `ItemLibrary.VendorMaterials()` plus the basic Healing Potion, at `BaseCost` per unit; a row's stepper is capped at `MaxStack − owned`.
- **Sell** — every owned item with `BaseCost > 0`, at `floor(BaseCost × 0.5)` (min 1) per unit.

**Layout discipline:** every vendor Canvas uses the §26.2 CanvasScaler and the UiKit components (§11.5); all colors come from `HubTheme`, never hand-typed `new Color(...)` (§17.1 #11).

### 25.2 Blacksmith

`Blacksmith.unity` / `BlacksmithManager.cs`. Three workflows:
1. **Forge** — combine materials + gold → new weapon/armor per `CraftingRecipe`. Pulls from `RecipeLibrary.All().Where(r => r.ResultItemId is equipment)`.
2. **Salvage** — break an inventory equipment piece into 50% of its recipe's ingredients (floor, min 1). `UpgradeLibrary`/`UpgradeRecipe` data exists but no UI consumes it — stat-improvement is covered by **Enchant** at the Alchemist.
3. **Repair** (US-121) — third tab listing every hero's equipped weapon/armor with a durability pool (worn pieces first). Cost from `WeaponDurabilityHelper.RepairCost` (per-point price ×1.6 per prior repair); restores to the effective max (factory − prior repairs, §24.5), then the ceiling drops 1 for next time. The detail pane warns when repairing costs as much as a new copy (`IsUneconomical`). Demo: "Wear Gear −5".

### 25.3 Alchemist

`Alchemist.unity` / `AlchemistManager.cs`. Brews consumables from materials + gold per recipes. Pulls `RecipeLibrary.All().Where(r => r.ResultItemId is consumable)`.

- **Enchant** (`EnchantLibrary.cs`) — apply one of 4 elemental affinities (Flame/Frost/Spark/Shadow) to a base weapon; each recipe = 1 element-essence + 2 ArcaneDust + 150g, elevates rarity and adds element-themed stats.
- **Heal service** (US-122) — the game's only out-of-battle recovery: a green "Heal Party" button beside Mix. Prices the party's total missing HP at the Healing Potion's rate (`HealGoldPerHp = 0.5` g/HP — 25g heals 50); paying clears every party member's `CharacterLevelPair.HpCurrent` to 0 (= spawn at full). Shows "Party Healthy" (disabled) when nobody is wounded. Test: "Wound Party 50%" in battle, win, visit the Alchemist.

### 25.4 Equip

`Equip.unity` / `EquipManager.cs`. Pick a hero → drag/select items from inventory into `EquipmentSlot`s. Live-previews stat changes via `Formulas.ComputeEquipmentBonus`.

### 25.5 Party

`Party.unity` / `PartyManager.cs`. Lists every roster hero with the selected hero's stats; Add/Remove toggles membership in the active battle squad, capped at 4 (`PartyManager.MaxPartySize`). See §23.4.

### 25.6 Abilities

`Abilities.unity` / `AbilitiesManager.cs`. Per-hero ability-bar editor over `HeroEquipmentSave.AbilityBarSlots` (5 slots, `AbilitiesManager.SlotCount`). The assignables list leads with the hero's own active abilities (`ActorData.Abilities`, under "Skills & Spells"), then "Items". Tapping assigns by name via `AbilityBarSlotSave.AbilityName` into the first empty **unlocked** slot (§4.7); a duplicate-on-bar guard prevents assigning the same ability twice; locked slots render locked. *(Verified by `AbilitySlottingTests`.)*

### 25.7 Cross-vendor utilities

- `Hub/HubTheme.cs` — shared palette, `FormatGold(amount)`, `ColorByAffordable(cost, gold)`.
- `Hub/HubToast.cs` — transient notifications ("Item bought", "Not enough gold").
- `Factories/HubItemRowFactory.Create(container)` — standard row layout for buy/sell lists; `HubItemRowFactory.RarityColor(rarity)` for the rarity tint.
- `Editor/Builders/VendorNavBarBuilder.cs` — the floating hamburger nav bar (`VendorNavBar`) at the top of every vendor scene; click → fade to another vendor.

### 25.8 Per-screen UI sketches

Each vendor follows the same skeleton: NavBar at top, scene-specific body in the middle, action row at bottom. Sketches below show the body for clarity.

**Vendor (§25.1)**
```
┌──[≡]  Merchant ──────────────────────────────┐
│  ┌ Buy ┐┌ Sell ┐                              │  ← mode toggle (separate carts)
│ ┌──────────────────────────────────────────┐ │
│ │ Health Potion          25g   [−]  2  [+] │ │  ← name(rarity) · price · stepper
│ │ Iron Ore               12g   [−]  0  [+] │ │     scrollable list
│ │ Wood                    8g   [−]  3  [+] │ │
│ │  ...                                       │ │
│ └──────────────────────────────────────────┘ │
│  Pay: 74g                            [ Buy ] │  ← cart total (red if unaffordable) + commit
└────────────────────────────────────────────────┘
   Sell mode → "Receive: Ng" at 50% BaseCost.
```

**Blacksmith**
```
┌──[≡ NavBar]──────────────────────────────┐
│ ┌─ Forge ─┐┌─ Salvage ─┐┌─ Repair ─┐      │
│ │ Recipe: Iron Sword                  │
│ │   Inputs: ×2 Iron, ×1 Wood, 100g    │
│ │   Owned:  ×3 Iron, ×0 Wood ✗        │  ← red on missing
│ │ [Craft] (disabled)                  │
│ └────────────────────────────────────┘    │
└──────────────────────────────────────────┘
   Repair tab → "Paladin — Iron Sword 3/9  12g" rows; worn gear first;
   repairs to (factory − prior repairs), price ×1.6 per prior repair.
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
│ Bar: [Heal] [Antidote] [—] [🔒] [🔒]     │  ← 5 slots, locked until unlocked (§4.7)
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

### 25.9 The merged hub (long-term goal)

**Intent:** optionally fold the vendor scenes into a **single composed hub `.unity`** — one screen where the player switches between vendors as tabs/panels rather than separate scene loads.

**Sequencing — deliberately not yet.** Each vendor stays its **own** scene + builder + manager until it is individually stable. Rationale:
- Merging unstable screens multiplies the surface area of any one bug across all of them.
- The shared utilities (`HubTheme`, `HubToast`, `HubItemRowFactory`, `VendorNavBar`, UiKit) already give a consistent look, so the eventual merge is layout composition, not a rewrite ([[project_scene_per_section_migration]]).

**Known hazard:** building shopping-interface scenes via the `*Builder.cs` pipeline is disproportionately fiddly relative to how simple a buy/sell list seems. Keep each builder minimal, lean on the shared factories, and fix a misbehaving vendor builder in isolation rather than touching neighbors.

### 25.10 Summon Circle

`Summon.unity` / `SummonManager.cs` ("Summon Circle"), reachable from the NavBar. The player spends **gold** to recruit a hero class into the roster — a deliberate purchase, not a pull (§3). Rules live in the pure `Services/SummonService`: the pool is GreenNinja, RedNinja, Pugilist, Ronin, Sellsword, Thief, Vampire; the price is **250 + 250 × recruits so far**; already-owned classes and unaffordable rows are refused. A recruit deducts `Inventory.Gold`, appends to `save.Roster`, persists, and the hero appears in the Party scene. *(Verified by `SummonServiceTests`.)*

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

`Utilities/AspectGuard.cs` self-installs on every scene load (`[RuntimeInitializeOnLoadMethod(AfterSceneLoad)]`, no per-builder insertion):

1. **Snap.** `ClosestValidAspect` picks the valid portrait aspect closest to the device's — 9:21, 9:20, 9:19.5, 1:2, 9:16, 10:16 or 3:4.
2. **Letterbox / pillarbox.** `LetterboxRect` fits the largest centered rect of that aspect inside the screen and assigns it to `Camera.main.rect` — fit, never crop, bars on at most one axis. A persistent black background camera fills the bars.
3. **Scaler.** Every `CanvasScaler` is normalized to 1170×2532, match 0.5 (§26.2).
4. **Safe area.** Any Canvas child named `SafeArea` gets `SafeAreaAnchors(Screen.safeArea, …)` normalized anchors, re-applied on change (§26.5).

The game **never** stretches to fill — it would ruin the HUD's vertical-row composition. The 1170×2532 reference is 9:19.477, a hair off 9:19.5, so it gets sub-1.5% bars by the fit-never-crop contract. *(Verified by `AspectGuardTests` across a device matrix — iPhone 14, 20:9 / 19.5:9 Androids, SE 16:9, iPad 3:4, 16:10 tablet, 1:2, 9:21, fold inner ~5:6, landscape: viewport centered, bars on at most one axis, rendered aspect exactly the snapped target, safe-area anchors normalized.)*

### 26.4 Camera framing

- The world camera is orthographic. `orthographicSize` is set so the 6×8 board fits with a configured margin inside the AspectGuard.
- Camera viewport rect = AspectGuard screen-rect (normalized).
- Camera `clearFlags = SolidColor, backgroundColor = black` so anything outside the viewport renders black (the letterbox/pillarbox).
- `GameBuilder` stacks a URP Base camera (depth −1, clearFlags SolidColor) and an Overlay camera (depth +1, clearFlags Nothing) that renders UI and shares the viewport.

### 26.5 Safe area (notch / cutout)

Modern phones have rounded corners + camera notches. AspectGuard anchors any Canvas child named `SafeArea` to `Screen.safeArea` so HUD content inside it respects them. The letterbox bars + background art can still extend to the screen edge.

## 27. Story crawl (no dialog)

The only narrative layer is a skippable **story crawl** (US-131): the `StoryCrawl` scene (builder + manager) plays a Star-Wars-style upward crawl (26 s, unscaled time) that the player skips with the button or a tap anywhere. Text lives in `Data/StoryCrawlData.cs`, one crawl per campaign theme (the Lightbearers descend the Undearth chasing the stolen dawn) — writers edit that file only. `StageSelectManager.ConfirmLaunch` routes through the crawl on the **first** entry into a theme (`GlobalSaveData.SeenStoryCrawls`, per save), then fades to `Game`.

There is no dialog system: no character dialogue, no branching, no cutscenes, no shopkeeper voice. (A branching story is a V2 idea — [`rfc/0002-v2-vision.md`](rfc/0002-v2-vision.md).)

## 28. No Overworld

There is no world-map / exploration scene. Stage navigation is the **scrollable level list** in StageSelect (§22.3): newest-on-top, every unlocked level freely replayable for farming. (`Overworld.unity` and `OverworldBuilder.cs` remain on disk, out of the build list.)

## 29. Open Design Questions

The bible is the resolved answer; this section is the **queue** of decisions still pending. Resolve a question → write its answer into the relevant section above and delete the entry here.

### 29.1 Macro loop / run structure

1. **Permadeath / roguelike / NG+** — today a defeat bounces back to StageSelect with the party at full HP and the campaign difficulty is flat per stage (§22.3). Is there ever a mode where defeat ends the run, or a New Game+ loop?
6. **Tutorial / onboarding** — does the player get a guided first battle?

### 29.3 Content economy

14. **Save autosave cadence** — the save is written at PostBattle and on vendor commits (§15.2); should entering a vendor or StageSelect also autosave?
19. **Inventory cap** — inventory is unbounded apart from per-item `MaxStack`; is a total cap wanted?

### 29.5 How to resolve a question

1. Pick a question; talk it through.
2. Land the decision in the matching section above as **prose**, not a question — delete the question here.
3. If the decision implies code work, add a story to `USER_STORIES.md`.
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

**Static state hygiene.** Per-battle static stores keyed by `ActorInstance`
(`BuffSystem.active`, `ThreatTracker`, `SkillCooldownManager`, `TrapManager`) survive scene loads and actor
teardown — destroyed actors linger as ghost keys carrying buffs/cooldowns/threat into the next
run. Rule: every such store exposes a static `Clear()` and is cleared in BOTH
`TurnManager.Initialize()` (battle start) and `StageManager.RestartStage()` (mid-battle
restart, which intentionally does NOT call `Initialize()` — `BeginHeroWindow` is not
idempotent). When adding a new per-battle static store, wire its `Clear()` into both sites.
Related teardown rule: any `g.TimelineBar` (or similar scene-bound singleton) use that can run
during scene unload needs a null-guard, and event subscribers on cross-scene singletons must
unsubscribe in `OnDestroy` (see `TargetModeOverlay`).

### 30.4 Mobile-specific constraints

- **No `Resources.Load`** (guardrail) — everything via Addressables; eliminates startup hitches.
- **Texture atlas the HUD.** `CliEntryPoints.BuildHudAtlas` creates `Assets/Sprites/HudAtlas.spriteatlas` (14 HUD sprite folders) and registers it as Addressable address `HudAtlas`, label `UI` (US-103). Run it once in the editor to materialize the atlas; the draw-call reduction is measured in the device profiling pass (US-104).
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
- ✅ **Colorblind palette toggle** — `ProfileSettings.ColorblindMode` (persisted); `ColorblindHelper.cs` substitutes Okabe-Ito colors for Red/Green mana orbs and debuff icons; `SettingsManager` toggle live-applies. (US-094)

### 31.2 Motion / VFX sensitivity

- **No screen-shaking by default** beyond mild impact hits (configurable via `VisualEffectManager.IntensityScale`).
- ✅ **Reduce-motion toggle** — `ProfileSettings.ReduceMotion` (persisted, default false); `VisualEffectManager.IntensityScale = 0` suppresses all particle VFX at the single spawn choke-point; `ProjectileMotionEval.ReduceMotion` collapses projectile arcs to straight lerps; `MotionSettingsHelper.Apply()` pushes the flag to both; `SettingsManager` toggle live-applies per scene change. (US-095)
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
- ✅ **Volume sliders + mute** — `ProfileSettings.{MusicVolume, SfxVolume, MuteMusic, MuteSfx}` (persisted; defaults 0.6/0.85/false/false); `AudioSettingsHelper.Apply()` folds mute into effective volume and pushes to `Jukebox` (music/vendor SFX) and `g.SoundSource` (battle SFX); `SettingsManager` sliders/toggles live-apply; `MusicDirector.Apply` re-applies per scene change. UI audio folds into the SFX channel. (US-096)

### 31.6 Status

- §31.1 color-blindness: ✅ colorblind toggle built (US-094).
- §31.2 motion: ✅ reduce-motion toggle built (US-095).
- §31.3 touch targets: design commitment, verify at min resolution during device testing.
- §31.4 readability: design commitment, ongoing.
- §31.5 audio: ✅ volume + mute sliders built (US-096); music and SFX per §12.0.

---

## 32. Document Discipline

The bible is the **connective membrane**. From this point forward:

### 32.1 When to update

- Every gameplay-affecting code change must update the bible — add, amend, or verify ([[feedback_game_bible]]).
- New mechanic → write the section first if it's complex, then code against the spec.
- Numeric tuning the bible records → update the table when the number changes.
- Removed mechanic → delete its text. The bible states current truth only; git history is the record.
- Open questions live in §29; resolving one moves the resolution into the right section AND deletes the question.

### 32.2 Bible vs memory

| Type | Lives in | Survives |
|---|---|---|
| "What the game IS" (rules, formulas, mechanics) | This bible | All future sessions |
| "What we discussed / why" (rationale, ratified decisions) | `memory/*.md` ([[feedback_game_bible]]) | All future sessions |
| "What's pending now" | `USER_STORIES.md` backlog + §29 here | Same |
| "What this turn debugged" | Conversation only | Until compaction |

If a feature is in a memory entry but not here, the **memory** wins for "what was discussed" and the **bible** wins for "what's locked in." Resolve disagreement by promoting the memory's resolved bits into the bible.

### 32.3 Who reads this

- **The user** — for sanity-checking that I understand the design.
- **Future-me** (next session) — to skip re-learning.
- **Legion / unattended sessions** — as the spec.
- **A new contributor** — as onboarding.

If a section can't be read by one of those audiences without context, that's a bug — fix it.

— end of document —

