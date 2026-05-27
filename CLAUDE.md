# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Conversation
- A bare "do" / "do it" / "yes" from the user means "continue", "keep going", "proceed". Resume the current task without asking for clarification.

## Project Overview

Unity 6000.4.3f1 tactical RPG — grid-based combat with 2D sprites on a 3D board. C# 9.0 targeting .NET Standard 2.1. Namespace root: `Scripts.*`. (Editor version is authoritative in `ProjectSettings/ProjectVersion.txt` — compile/batchmode against that exact version; using a different installed editor corrupts the package cache.)

## Core Game Loop

Read this section before touching anything in `Managers/`, `Sequences/`, `Instances/Actor/`, or `Canvas/TimelineBar*` — the rules below govern *every* combat-side change. When writing test plans or PR descriptions, use the vocabulary defined here. Authoritative source files are listed at the end of each rule.

### Sliding & displacement (the verb is "slide", not "drop on")

The player drags a hero across the grid one tile at a time. The drag path is **cardinal only** (N/E/S/W) — `ActorMovement.TowardDestinationRoutine()` separates X- and Y-axis legs; diagonals are not computed. As the hero crosses each new tile, `ActorMovement.CheckLocationChanged()` runs an overlap check.

If the entered tile is **already occupied** by another actor (ally **or** enemy — both displace identically), `overlappingActor.Move.HandleOverlap(previousLocation)` fires and the displaced actor is moved to **the tile the dragging hero just left** (the prior cardinal-adjacent tile). Multiple actors in the path each slide in sequence as the hero passes through them. The board edge is the only hard stop (`ClampToBoard()`).

Tiles are 1-occupant — heroes never "land on" enemies. Damage is never delivered by movement; it is delivered by the pincer that the new position completes. Sources: `Assets/Scripts/Instances/Actor/ActorMovement.cs`, `Assets/Scripts/Managers/SelectionManager.cs`.

### Pincer attack (the only way to deal hero damage)

A pincer is two heroes in the **same row OR same column** with a contiguous line of enemies between them — **no gaps and no allies in the line**, and at least one enemy required. Diagonal pincers do not exist. On hero drop, `PincerAttackManager.Check(Team.Hero, droppedHero)` scans the entire board for **all** valid hero-pair pincers, not just ones involving the dropped hero, and queues them via `OrderPairsByChainsThenNearest()` — pairs that chain (the second attacker of pincer A is the first attacker of pincer B) resolve consecutively; otherwise nearest-to-drop first.

**Supporters** are allies cardinally adjacent to either pincer endpoint with unbroken line of sight along that axis (`FindSupporters()`); each supporter adds bonus damage via `PincerAttackSupportSequence`. A drop with no pincer is legal — the hero just stays where it landed and the player remains in their turn (see Turn flow). Sources: `Assets/Scripts/Managers/PincerAttackManager.cs`, `Assets/Scripts/Sequences/PincerAttack*.cs`.

### Turn flow (hero window, then queued enemy)

`TurnManager.IsHeroTurn` gates input. During a hero window the player can select and drag **any** hero — `ActiveActor` is null and there is no per-hero turn budget (unless `TurnSelectionMode.ActiveOnly` is in force). The window ends only when the timeline trigger fires: a tag reaching the trigger (right edge) calls `OnIconReachedTrigger()` which sets `HasQueuedEnemyAfterHero` on `TurnManager`. The next hero drop checks that flag and queues `EndTurnSequence` → `BeginEnemyTurn(enemy)`.

A hero drop with no pincer and no queued enemy keeps the player in their turn (`InputMode = PlayerTurn`, `selectedState = Idle`). Sources: `Assets/Scripts/Managers/TurnManager.cs`, `Assets/Scripts/Managers/SelectionManager.cs:Drop()`, `Assets/Scripts/Sequences/EndTurnSequence.cs`.

### Timeline & Pushback Zone (Grandia 2 IP gauge — "loading" metaphor)

The TimelineBar shows a horizontal strip of icons "loading" left→right over normalized u-coordinates: **u=0.0 is spawn (left, fresh / not loaded), u=1.0 is trigger (right, fully loaded — ready to fire)**. When an enemy icon's right (leading) edge reaches u=1 the enemy's turn is queued. The **Pushback Zone** is a translucent red strip on the right, spanning `u ≥ 1 - TimelineBarConfig.ZoneU` (~the rightmost 25–35% of the bar).

`TimelineBarInstance.PushbackOnAttack()` is the **gate**: damage is always applied to the enemy, but the icon is only pushed back (leftward / toward spawn) if `tag.GetEffectiveTargetU() >= 1 - ZoneU`. Push amount lerps with proximity to the trigger (`u` — higher u = more push) and scales with attacker Strength; after pushback the icon enters `Stunned` mode for a duration scaled inversely by enemy Agility. Strategy: form pincers that include enemies whose icons sit inside the rightmost Zone to delay their turns.

**Train-style overlap cascade.** When a new icon spawns or gets displaced, `ResolveSpatialOverlap()` walks right→left. If the left neighbor sits within `MinSpatialGap` of the icon to its right, it's pushed further left by the shortfall; that push may cascade into the next neighbor, like train cars. The cascade is **order-preserving** — no speed-based reshuffling — because the newly arriving icon (most recent cast, fresh spawn at trigger, etc.) deserves its rightmost slot and existing icons absorb the time-cost of the shove. Sources: `Assets/Scripts/Canvas/TimelineBarInstance.cs`, `Assets/Scripts/Canvas/TimelineIcon.cs`, `Assets/Scripts/Data/Config/TimelineBarConfig.cs`.

### Casting & resources

Heroes accumulate Mana at 5/sec while the timeline is advancing (`ManaPoolManager.Update()`); the Bank button grants bonus equal to skipped time. Abilities cost Mana **upfront at cast start** — interruption refunds nothing. A `CastingState` with `CastTimeSeconds > 0` shows a fill bar on the caster's TimelineIcon. `CastingState.Interrupt()` sets `IsInterrupted = true`, shows "Interrupted!" combat text, and leaves MP consumed — but the interrupt *trigger path is not wired up yet*: `EnemyAttackSequence.InterruptCastingHero()` is an empty stub and `Interrupt()` has zero call-sites. HP at 0 = death; AP is overworld-side and rarely matters in combat. Sources: `Assets/Scripts/Managers/ManaPoolManager.cs`, `Assets/Scripts/Models/CastingState.cs`, `Assets/Scripts/Instances/Actor/ActorStats.cs`, `Assets/Scripts/Sequences/EnemyAttackSequence.cs`.

**Instant vs. cast-time abilities.** An ability with `CastTimeSeconds == 0f` resolves instantly on selection — no timeline icon, no input suspension, no interrupt risk. An ability with `CastTimeSeconds > 0f` (Heal, Fireball, Quicken, etc.) spawns a timeline icon that must travel to the trigger before the effect applies; during that window the caster is exposed to the interrupt mechanic below.

**Cast-as-timeline-icon (design intent — partially implemented).** A spell with `CastTimeSeconds > 0` should spawn its **own dedicated TimelineIcon** distinct from the caster's, loading left→right at a rate derived from the caster's Wisdom + Intelligence (higher = faster cast). The spell-icon's primary visual is a **progress fill bar** that fills as the icon advances along the timeline; bar-full coincides with the icon reaching u=1 ("trigger / fully loaded"), and that is the moment the cast resolves. (Pace the icon so the two events line up — `uPerSec = 1f / TotalCastTime` — so the bar is a faithful read of remaining cast time.) When the bar fills, the game enters a **third turn state — neither hero nor enemy** — that suspends all input (`InputMode = None` plus a new `IsResolvingCast` gate on TurnManager), plays the caster's animation + VFX, applies the effect, then returns control to wherever it left off (hero window or enemy turn mid-flow). The caster's icon still pulses to indicate they are casting; the *spell* icon is the one whose filling bar drives resolution.

**Hasten / Quicken (forward push).** The inverse of pushback: a spell like *Quicken* cast on an enemy (or ally) slides that target's timeline icon **toward the trigger** (u increases). If the bump lands the icon on top of a neighbor already ahead of it, the hastened icon can **overtake** — ResolveSpatialOverlap's train-cascade runs, but inverted: the hastened icon keeps its new forward u and any icon between it and its target slot gets pushed *behind* (lower u). Turn order updates accordingly; a fast enemy Quickened into the Zone may act before a previously-queued enemy.

**Cast interruption — Fail / Pushback / Clutch (design intent).** When a caster with an in-flight spell-icon takes damage, three outcomes roll based on the caster's stats (dominant factor is **Luck — LCK**; secondary: caster Wisdom/Intelligence, attacker Strength):
- **Fail (common)** — cast is interrupted; `CastingState.Interrupt()` sets `IsInterrupted = true`, MP stays consumed, the spell effect does not apply, item-backed abilities do not consume their item, and the spell-icon is removed from the timeline. This is the current `CastingState.Interrupt()` behavior.
- **Pushback (uncommon)** — cast survives but the spell-icon's u decreases (cast delayed). Amount scales inversely with LCK / WIS; may also add a brief stun equivalent to the enemy pushback flow. The filling bar rewinds to reflect the new position.
- **Clutch! (rare — LCK-driven)** — the caster shrugs off the hit, the spell-icon **snaps instantly to u=1** and resolves on the spot. Designed so a dying healer can miraculously let off one last spell before collapsing; if a Clutch heals the caster back from the brink, it should feel exciting. Trigger a dedicated `ClutchSequence` (screen flash / SFX / "Clutch!" combat text) before the normal cast resolution. Roll base rate ≈ `LCK / 200` (Luck 10 ≈ 5%, Luck 20 ≈ 10%), floored/capped by designer tuning.

Roll order on interruption: **Clutch check first** (instant resolve wins over everything), then **Pushback vs Fail** based on remaining odds. None of this is implemented yet — the interrupt trigger path itself (`EnemyAttackSequence.InterruptCastingHero`) is still an empty stub, so the first implementation phase is to wire up the basic Fail outcome (find the hero's `CastingState`, call `Interrupt()`), and the second phase is to replace that unconditional call with a `CastInterruptResolver.Resolve(caster, attacker)` helper returning `{Fail | Pushback | Clutch}`.

**Current code reality:** `CastingState` exists and tracks elapsed/total time + interruption, and `TimelineIcon.BeginCast(state)` overlays a fill bar on the **caster's existing icon** — there is no separate spell icon yet, no `Casting`/`Resolving` mode in `TimelineIconMode`, no input-suspending third state in `TurnManager`, and `TotalCastTime` reads `ability.CastTimeSeconds` raw with **no WIS/INT scaling**. When implementing: add a `Resolving` `TimelineIconMode`, a spell-only `TimelineIconFactory.CreateForCast(CastingState)` overload, a `Formulas.CastTime(baseSeconds, wis, int)` helper, and a TurnManager state that holds back the next `NextTurn()` until the cast resolves.

### Vocabulary cheat sheet

| Use | Don't use |
|---|---|
| "drag the hero to flank the enemy line" | "drop the hero on the enemy" |
| "form a pincer with another hero" | "attack with the hero" |
| "the displaced actor slides back one tile" | "the hero pushes the enemy" |
| "icon enters the Zone" | "tag enters the Zone" (post-rename: `TimelineIcon`) |

## Common Commands

The `GridGame.Console.ps1` menu is intentionally minimal — only the 6 operations the user runs by hand:

| # | Operation | Notes |
|---|---|---|
| 1 | Run Application | Launches the Unity editor. `/Run` inside Claude Code triggers Play Mode. |
| 2 | Commit and Sync | `git add -A`, commit, push. |
| 3 | Create Backup | Copies the project to `R:\Backup\GridGame` with date-stamped folders. |
| 4 | Setup | One-time (idempotent): clone/pull, activate pre-push hook, launch Unity for initial import. |
| 5 | Build Player (headless) | `CliEntryPoints.BuildStandaloneWindows` in batchmode. |
| 6 | Set Start Scene | Rewrites `StartSceneConfig.StartScene`; `StartSceneAuthority.[InitializeOnLoad]` applies to `playModeStartScene` + `EditorBuildSettings.scenes[0]`. |

**Scene building** (Builder C# → `.unity`, one direction only):
- Every `Assets/Editor/Builders/*Builder.cs` edit triggers a recompile; `BuilderAutoRebuild.cs` ([InitializeOnLoad]) detects the mtime change after the recompile and rebuilds the matching `Assets/Scenes/{Name}.unity` automatically. If the rebuilt scene is the one currently loaded, the editor reloads it in place.
- **Reverse direction (`.unity` → builder) is NOT automated.** A `.unity` is YAML; a builder is C#. Translating one to the other requires LLM judgment about which fields are meaningful, so direct edits to a `.unity` in the Editor are not synced back — ask Claude to translate the change into builder code.
- Manual escape hatch: **Tools › Scenes › Rebuild All** (or `CliEntryPoints.BuilderAllScenes` in batchmode) re-runs every builder.
- mtime cache lives at `Library/BuilderMTimes.json`; deleting it forces a no-op silent re-sync on next editor launch (the first run records mtimes without rebuilding).

### Claude's batchmode duties

Everything else in `Assets/Editor/CliEntryPoints.cs` is Claude's responsibility. Run it directly — do not surface it as a menu entry:

```
Unity -batchmode -nographics -projectPath . \
      -executeMethod CliEntryPoints.<Method> -quit -logFile -
```

Exit code `0` = success, `1` = failure. Fix failures before asking the user to commit.

| After this change | Run |
|---|---|
| Edited any `Assets/Editor/Builders/*Builder.cs` | Nothing — `BuilderAutoRebuild` auto-rebuilds on next domain reload. For batch, run `BuilderAllScenes`. |
| User reports editor-side scene hierarchy edits | Translate the YAML change into the matching `*Builder.cs` (no automatic scene→builder direction exists). |
| Builder drift is intentional (new object expected) | `RegenerateBuilderSnapshots` |
| Removed or added a `[SerializeField]` (Phase 1 work) | `RegenerateSerializedFieldAllowlist` |
| Migrated a `Resources.Load` call-site to Addressables | `RegenerateResourcesLoadAllowlist` |
| Moved an `Instantiate` call into a `*Factory.cs` | `RegenerateInstantiateAllowlist` |
| Material builder / data-layer / architecture change | `GenerateDocs` |
| About to hand work back for commit | `CheckAllGuardrails` + `RunEditTests` |

**Guardrails (auto-enforced pre-push via `.githooks/pre-push`):**
| Guardrail | What it blocks | Allowlist |
|---|---|---|
| `SerializedFieldBan` | new `[SerializeField]` fields in `Scripts/` | `Assets/Editor/SerializedFieldAllowlist.txt` |
| `ResourcesLoadBan` | new `Resources.Load*` call-sites | `Assets/Editor/ResourcesLoadAllowlist.txt` |
| `InstantiateBan` | `Instantiate(` outside `*Factory.cs` | `Assets/Editor/InstantiateAllowlist.txt` |
| `BuilderDriftChecker` | scene YAML drifting from its builder output | `Documentation/Builders/Drift/*.snapshot.txt` |

`CliEntryPoints.CheckAllGuardrails` runs all four in one batchmode session — run it before handing work back. The pre-push hook is activated automatically by **Setup (Option 4)**; bypass for hotfixes with `git push --no-verify`.

## Code-only Workflow

The project is authored to run without opening the Unity Editor UI. Every `.unity` scene is the rebuilt output of a corresponding `Assets/Editor/Builders/*Builder.cs` — the builder is the source of truth, the `.unity` is the build artifact.

**Rules when adding new content:**
- **New GameObjects** → add to the scene's builder. `BuilderAutoRebuild` will regenerate the `.unity` after the next domain reload. Do not click in the hierarchy.
- **New UI** → extend the existing factory pattern (`ActorFactory`, `HubItemRowFactory`, etc). Do not create new `.prefab` files.
- **New assets** (sprite, font, audio) → register an Addressable address and load via `AssetHelper.LoadAssetAsync<T>(address)`. Do not add inspector drag-drop references.
- **Avoid new `[SerializeField]`.** Initialize from data-layer statics (`ItemData_*`, `SkillData_*`, `ActorData_*`) or factory parameters.
- **When inspector work is unavoidable** (editing an existing prefab / a legacy `[SerializeField]`): commit the `.prefab`/`.unity` change alongside the builder-code change that would rebuild it from scratch.

**Builder → Scene auto-rebuild:**
- `BuilderAutoRebuild.cs` is an `[InitializeOnLoad]` watcher. After every domain reload it diffs each `*Builder.cs` mtime against `Library/BuilderMTimes.json`; any changed builder triggers a rebuild of its matching `Assets/Scenes/{Name}.unity` (open scene, clear roots, invoke `Build()`, save).
- If the scene being rebuilt is the one currently loaded in the Editor, it is reloaded in place (any in-editor edits are lost — builders are the source of truth).
- Rebuilds are deferred while in play mode and resume on exit.
- First launch with no cache records mtimes silently — no rebuild on a fresh checkout.
- Manual escape hatch: `Tools › Scenes › Rebuild All` rebuilds every scene with a builder.
- **Reverse direction is intentionally absent.** A `.unity` is YAML; a builder is C#; the mapping requires judgment (which fields matter, which are noise). If you've hand-edited a scene in the Editor and want to preserve the change, ask Claude to translate the YAML diff into builder code.
- The `BuilderDriftChecker` guardrail catches the case where someone hand-edits a `.unity` (e.g., merge resolution) and the builder no longer regenerates an identical scene.

## Architecture

### Global Access Pattern
```csharp
using g = Scripts.Helpers.GameHelper;
// g.TurnManager, g.Actors.Heroes, g.SequenceManager, g.TileMap, etc.
```

### Folder Layout (`Assets/Scripts/`)
| Folder | Purpose |
|---|---|
| `Data/` | Static data definitions (items, actors, skills, recipes) |
| `Models/` | Data structures, enums, `Singleton<T>` base |
| `Managers/` | Singleton game systems (51 files) |
| `Instances/` | Runtime MonoBehaviours on GameObjects |
| `Sequences/` | Async event queue for combat/UI flows |
| `Helpers/` | Static utility functions; `GameHelper` is the central accessor |
| `Libraries/` | Lazy-loaded registries with `Ensure()` pattern |
| `Factories/` | Object instantiation |
| `Canvas/` | In-game HUD and overlay UI |
| `Hub/` | Shared UI utilities (HubTheme palette, HubToast notifications) used by every vendor scene |
| `Inventory/` | Inventory and equipment models |
| `Overworld/` | Top-down exploration |
| `Effects/` | Screen-space visual effects |
| `Utilities/` | `Formulas.cs`, `RNG.cs`, `Extensions.cs`, `Geometry.cs` |

### Data Layer
- **Static data classes** define instances: `ItemData_Weapons.IronSword`, `SkillData_Training.Fireball`
- **Static libraries** register and look up data: `ItemLibrary.Get(id)`, `ActorLibrary.Get(CharacterClass.Paladin)`
- Libraries use a lazy `Ensure()` pattern with a `bool initialized` guard

### Actor System
- `ActorData` — static template (stats, abilities, portrait, stat growth)
- `ActorInstance` — runtime MonoBehaviour on the board
- `ActorStats` — mutable stat block (Strength, Vitality, Agility, Speed, Stamina, Intelligence, Wisdom, Luck, HP, AP)
- Level-scaled stats: `actorData.GetStats(level)`
- Character identity via `CharacterClass` enum

### Combat & Sequences
- Sequence-based async: `g.SequenceManager.Add(new AttackSequence(...))` then `.Execute()`
- `TurnManager` alternates hero/enemy turns; core mechanic is **pincer attack** (two heroes attack simultaneously)
- Stat formulas in `Formulas.cs`: `Health()`, `Offense()`, `Defense()`, `MagicOffense()`, etc.
- Equipment bonuses via `Formulas.ComputeEquipmentBonus(loadout)`

### Save/Persistence
- `Profile` → `SaveState` → individual save data classes
- XP stored as `TotalXP`; level/currentXP derived at runtime via `ExperienceHelper.DeriveFromTotalXP(totalXP)`
- Access current save: `ProfileHelper.CurrentProfile?.CurrentSave`

### Vendor Scenes (replaces the old monolithic Hub)
- Each vendor area is its own scene: `Vendor`, `Alchemist`, `Blacksmith`, `Equip`, `Party`, `Abilities`. (Old `Hub.unity` + `Inn.unity` and their managers are deleted; future merged hub will compose these scenes.)
- Each manages its own `PlayerInventory` hydrated from `ProfileHelper.CurrentProfile.CurrentSave` on Awake, persists on commit
- Cross-scene navigation via `VendorNavBar` (the floating hamburger dropdown — `VendorNavBarBuilder.Build(canvas, topInset, anchorLeft)`)
- Shared visual language: `HubTheme` (palette + `FormatGold` / `ColorByAffordable`), `HubToast` (notifications), `HubItemRowFactory.Create(container)` (list rows + `RarityColor`)

### Inventory & Equipment
- `PlayerInventory`: item ID → `Entry(count, durability)`
- `HeroLoadout`: `Dictionary<EquipmentSlot, ItemDefinition>` per hero
- `PartyLoadout`: all hero loadouts keyed by `CharacterClass`
- `CraftingRecipe.CanCraft(inventory)` / `.Execute(inventory)`

### Scene Builder System
- Every scene except Game and Overworld is fully reproducible from code via `Assets/Editor/Builders/`
- `SceneBuilderHelper.cs` provides shared `Ensure*()` methods — idempotent, Undo-registered
- To add new UI objects: edit the builder `.cs`, run `Checkout` on the affected scene
- Authoritative hierarchy data: `Documentation/Builders/SceneHierarchies.txt`

## Code Style

### Documentation (XML comments)
```csharp
/// <summary>
/// CLASSNAME - Brief one-line description.
/// <para>PURPOSE: 2-4 sentences explaining what this does and why.</para>
/// <para>RELATED FILES: File1.cs, File2.cs, File3.cs</para>
/// </summary>
```
- Class name in ALLCAPS in the one-liner
- Optional sections: `VISUAL APPEARANCE:`, `USAGE:`, `SEQUENCE FLOW:`, `LIFECYCLE:`, `FEATURES:`
- ASCII box-drawing for UI layout diagrams: `┌ ─ ┐ │ └ ─ ┘ ├ ┤ ┼`

### C# Conventions
- `var` for locally obvious types
- Null-safe chaining: `hub?.SharedInventory?.Gold`
- Expression-bodied members for single-line properties
- Static readonly for data definitions (not `const` for complex types)
- Private fields: `camelCase`, no underscore prefix

### Using Directives
Every `.cs` file includes the full standard `Scripts.*` using block — **do not remove "unused" usings**; this is a project convention.

## UI Patterns
- Rich text in TextMeshPro: `<color=#88CC88>`, `<b>`, `<i>`
- Rarity colors: `HubItemRowFactory.RarityColor(rarity)`
- XP bars: text-based `[████████░░░░░░░░░░░░]`
- UI child lookup: `transform.Find("ChildName")?.GetComponent<T>()`
- Layer assignment: `go.layer = LayerMask.NameToLayer("UI")`

## Unity Sorting Layers (render order)
```
Board → DottedLine → SupportLineBelow → ActorBelow → BoardOverlay
→ SupportLineAbove → ActorAbove → VFX → Coin → DamageText → PortraitPopIn → Portrait
```

## Reference Documentation
Detailed scene hierarchies and project settings are in `Documentation/` at the project root.
