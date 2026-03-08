# GridGame2026 — Copilot Instructions

## Project Overview
- Unity tactical RPG (grid-based, 2D sprites on 3D board)
- C# 9.0 targeting .NET Standard 2.1
- Namespace root: `Scripts.*` (e.g. `Scripts.Hub`, `Scripts.Data.Items`, `Scripts.Managers`)
- Global access pattern: `using g = Scripts.Helpers.GameHelper;` then `g.TurnManager`, `g.Actors.Heroes`, etc.

## Scenes
- SplashScreen → TitleScreen → ProfileSelect/ProfileCreate → StageSelect → LoadingScreen → Game → PostBattleScreen
- Hub (town): Party, Shop, Blacksmith, Medical, Residence, Training, Equip, Inventory, BattlePrep
- Overworld: Top-down exploration with encounter transitions

## Code Style

### Documentation
- Class-level XML summary format: `/// CLASSNAME - Brief one-line description.` (ALLCAPS class name)
- Always include `PURPOSE:` section (2-4 sentences) and `RELATED FILES:` section (3-5 entries)
- Optional sections as needed: `VISUAL APPEARANCE:`, `USAGE:`, `SEQUENCE FLOW:`, `LIFECYCLE:`, `FEATURES:`
- Use ASCII box-drawing for UI layout diagrams: `┌ ─ ┐ │ └ ─ ┘ ├ ┤ ┼`
- Code examples in `/// ``` csharp ... ///` ``` blocks inside summaries

### C# Conventions
- Prefer `var` for locally obvious types
- Null-safe chaining: `hub?.SharedInventory?.Gold`
- Expression-bodied members for single-line properties: `public bool IsEquipment => Type == ItemType.Equipment;`
- Static readonly for data definitions (not const for complex types)
- Private fields: `camelCase`, no prefix

### Using Directives
- Every `.cs` file includes the full standard using block (all `Scripts.*` namespaces)
- This is a project convention — do not remove "unused" usings

## Architecture Patterns

### Data Layer (`Scripts.Data.*`)
- **Static data classes** define instances: `ItemData_Weapons.IronSword`, `RecipeData.IronSwordRecipe`, `SkillData_Training.Fireball`
- **Static libraries** register and look up data: `ItemLibrary.Get(id)`, `ActorLibrary.Get(class)`, `RecipeLibrary.All()`
- Libraries use lazy `Ensure()` pattern with a `bool initialized` guard
- Item types: `Equipment`, `Consumable`, `CraftingMaterial`, `QuestItem`
- Equipment slots: `Weapon`, `Armor`, `Helmet`, `Boots`, `Ring`, `Amulet`

### Save/Persistence (`Scripts.Models.Profile`)
- `Profile` → `SaveState` → individual save data classes (`InventorySaveData`, `EquipmentSaveData`, `TrainingSaveData`, etc.)
- XP stored as `TotalXP`; level and currentXP derived at runtime via `ExperienceHelper.DeriveFromTotalXP(totalXP)`
- Access current save: `ProfileHelper.CurrentProfile?.CurrentSave`

### Hub Sections (`Scripts.Hub.*`)
- Each section is a `MonoBehaviour` on its panel: `*SectionController`
- Pattern: `Initialize(HubManager)` → `OnActivated()` → private `Refresh*()` methods
- `HubManager` owns `SharedInventory` (PlayerInventory) and `SharedLoadout` (PartyLoadout)
- Auto-saves when switching sections via `WriteToSave()` + `ProfileHelper.Save()`
- UI resolved via `transform.Find("ChildName")?.GetComponent<T>()`
- Scene object names live in `GameObjectHelper.Hub.*` constants
- List rows built with `HubItemRowFactory.Create(container)` + `SetLabel/SetSubLabel/SetIcon/SetSelected`
- Vendor theming via `HubVendorFactory.Build(panel, theme)`

### Inventory/Equipment (`Scripts.Inventory.*`)
- `PlayerInventory`: item ownership (id → Entry with count + durability)
- `HeroLoadout`: per-hero equipment slots (`Dictionary<EquipmentSlot, ItemDefinition>`)
- `PartyLoadout`: groups all `HeroLoadout` instances, keyed by `CharacterClass`
- `CraftingRecipe.CanCraft(inventory)` / `.Execute(inventory)` for crafting
- Items have `SalvageComponents` for breakdown into materials

### Combat (`Scripts.Managers.*`, `Scripts.Sequences.*`)
- Sequence-based: `g.SequenceManager.Add(new AttackSequence(...))` then `.Execute()`
- Turn order managed by `TurnManager`
- Stat formulas in `Formulas.cs`: `Health()`, `Offense()`, `Defense()`, `MagicOffense()`, etc.
- Equipment bonuses computed via `Formulas.ComputeEquipmentBonus(loadout)`

### Actor System (`Scripts.Models.Actor.*`, `Scripts.Instances.Actor.*`)
- `ActorData`: static template (stats, abilities, portrait, lore, stat growth)
- `ActorInstance`: runtime instance on the board (MonoBehaviour)
- `ActorStats`: mutable stat block (Strength, Vitality, Agility, Speed, Stamina, Intelligence, Wisdom, Luck, HP, AP)
- Level-scaled stats via `actorData.GetStats(level)`
- Character identity via `CharacterClass` enum

## UI Patterns
- Rich text in TextMeshPro: `<color=#88CC88>`, `<b>`, `<i>`
- Rarity colors: `HubItemRowFactory.RarityColor(rarity)`
- Gold display: `$"Gold: {Inventory.Gold}"`
- XP bars: text-based `[████████░░░░░░░░░░░░]` in party displays
- Layer assignment: `go.layer = LayerMask.NameToLayer("UI")`

## Unity Sorting Layers (render order)
Board → DottedLine → SupportLineBelow → ActorBelow → BoardOverlay → SupportLineAbove → ActorAbove → VFX → Coin → DamageText → PortraitPopIn → Portrait

## Reference Documentation
Detailed scene hierarchies and project settings are in the `Documentation/` folder at the project root.
