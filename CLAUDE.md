# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Unity 6000.3.2f1 tactical RPG — grid-based combat with 2D sprites on a 3D board. C# 9.0 targeting .NET Standard 2.1. Namespace root: `Scripts.*`.

## Common Commands

**Run the game (Unity Editor):**
```
GridGame.ps1 → Option 1
```
Or via Claude Code: `/Run` triggers Unity Play Mode in the Editor.

**Commit and sync:**
```
GridGame.ps1 → Option 2  (git add, commit, push)
```

**Backup project:**
```
GridGame.ps1 → Option 3  (copies to R:\Backup\GridGame with date stamps)
```

**Scene scaffolding** (rebuild a scene's hierarchy from code):
- Unity menu: **Tools › Scenes › {SceneName} › Create Scaffolding / Clear Scene / Clear & Recreate**
- All menu items auto-switch to the correct `.unity` scene before operating

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
| `Hub/` | Town section controllers |
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

### Hub Sections
- Each section: `*SectionController : MonoBehaviour`
- Pattern: `Initialize(HubManager)` → `OnActivated()` → private `Refresh*()` methods
- `HubManager` owns `SharedInventory` (PlayerInventory) and `SharedLoadout` (PartyLoadout)
- Auto-saves when switching sections via `WriteToSave()` + `ProfileHelper.Save()`
- Scene object names in `GameObjectHelper.Hub.*` constants
- List rows via `HubItemRowFactory.Create(container)` + `SetLabel/SetSubLabel/SetIcon/SetSelected`

### Inventory & Equipment
- `PlayerInventory`: item ID → `Entry(count, durability)`
- `HeroLoadout`: `Dictionary<EquipmentSlot, ItemDefinition>` per hero
- `PartyLoadout`: all hero loadouts keyed by `CharacterClass`
- `CraftingRecipe.CanCraft(inventory)` / `.Execute(inventory)`

### Scene Scaffold System
- Every scene except Game and Overworld is fully reproducible from code via `Assets/Editor/Scaffolds/`
- `SceneScaffoldHelper.cs` provides shared `Ensure*()` methods — idempotent, Undo-registered
- To add new UI objects: edit the scaffold `.cs`, run Create Scaffolding, save scene
- Authoritative hierarchy data: `Documentation/Scaffolds/SceneHierarchies.txt`

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
