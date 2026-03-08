# Scene: Hub

Path: `Assets/Scenes/Hub.unity`
Build Index: 13
Root Object Count: 3

---

## Overview

The Hub is the town/camp scene between battles. It provides **9 dynamically-generated section screens** accessed via a top navigation bar. All UI content (item rows, vendor overlays, hero cards, stat displays) is built at runtime — the scene itself only contains empty panel containers and navigation buttons. `HubManager` orchestrates everything and owns the shared `PlayerInventory` and `PartyLoadout` that every section reads and writes.

---

## Scene Hierarchy

```
[Canvas]
  Canvas: renderMode=ScreenSpaceOverlay, sortOrder=0
  [ContentPanel]
    Image: sprite=null
    [PartyPanel]          ← PartySectionController (added at runtime)
    [ShopPanel]           ← ShopSectionController
    [MedicalPanel]        ← MedicalSectionController
    [ResidencePanel]      ← ResidenceSectionController
    [BlacksmithPanel]     ← BlacksmithSectionController
    [TrainingPanel]       ← TrainingSectionController
    [EquipPanel]          ← EquipSectionController
    [InventoryPanel]      ← InventorySectionController
    [BattlePrepPanel]     ← BattlePrepSectionController
  [NavBar]
    [PartyButton]         Button → GoToPartySection()
    [ShopButton]          Button → GoToShopSection()
    [MedicalButton]       Button → GoToMedicalSection()
    [ResidenceButton]     Button → GoToResidenceSection()
    [BlacksmithButton]    Button → GoToBlacksmithSection()
    [TrainingButton]      Button → GoToTrainingSection()
    [EquipButton]         Button → GoToEquipSection()
    [InventoryButton]     Button → GoToInventorySection()
    [BattlePrepButton]    Button → GoToBattlePrepSection()
    [OverworldButton]     Button → GoToOverworld()
    [BattleButton]        Button → GoToBattle()
  [FadeOverlay]           FadeOverlayInstance
  [CutoutOverlay]         CutoutOverlay
    [Top]
      [LeftPane]
      [CenterPane]
      [RightPane]
    [Bottom] [INACTIVE]
  [Output]                TextMeshProUGUI (debug)
[EventSystem]
[HubManager]              HubManager MonoBehaviour
```

---

## Architecture

### HubManager (`Scripts.Hub.HubManager`)

Central controller attached to the `[HubManager]` root GameObject.

**Shared State:**

| Property | Type | Description |
|----------|------|-------------|
| `SharedInventory` | `PlayerInventory` | All items, gold, durability — shared across every section |
| `SharedLoadout` | `PartyLoadout` | Per-hero equipment slots — shared across Equip/BattlePrep/Party |

**Lifecycle:**

```
Awake()
 ├── LoadFromSave()          ← Hydrate inventory + loadout from ProfileHelper
 ├── ResolveSceneObjects()   ← GameObject.Find() for all buttons + panels
 ├── AttachTiltParallax()    ← Add TiltParallax to every panel
 ├── InitializeSections()    ← EnsureController<T>() + Initialize(this)
 ├── WireButtonListeners()   ← onClick → GoTo*Section()
 └── GoToPartySection()      ← Default landing view

Activate(panel)
 ├── WriteToSave() + ProfileHelper.Save()   ← Auto-save on every switch
 ├── SetActive(false) on all other panels
 └── SetActive(true) on target panel

OnDestroy()
 └── RemoveListener() on all buttons
```

**Section Navigation:**
Only one panel is active at a time. `Activate()` auto-saves before switching.

### Section Controller Pattern

Every section follows the same pattern:

```csharp
public class *SectionController : MonoBehaviour
{
    private HubManager hub;

    public void Initialize(HubManager hubManager)
    {
        hub = hubManager;
        ResolveUI();                                    // Find child UI elements
        HubVendorFactory.Build(panel, *Theme);          // Apply themed vendor overlay
    }

    public void OnActivated()
    {
        Refresh*();                                     // Rebuild all dynamic content
    }

    private void ResolveUI()
    {
        // transform.Find("ChildName")?.GetComponent<T>()
    }
}
```

Controllers are added at runtime via `EnsureController<T>(panel)` — they are **not** pre-attached in the scene.

---

## Section Screens

### 1. Party (`PartySectionController`)

**Vendor Theme:** Camp (Paladin portrait, deep red/maroon background)

Manage party composition: add/remove heroes, view stats and equipped gear.

```
┌───────────────┬───────────────────────────────┐
│  Roster       │  Active Party (max 4)         │
│  ┌─────────┐  │  ┌──────────────────────────┐ │
│  │ Monk    │→ │  │ Paladin  Lv5  STR 12 ... │ │
│  │ Thief   │→ │  │ Archer   Lv3  STR  8 ... │ │
│  └─────────┘  │  └──────────────────────────┘ │
│               │                               │
│               │  Selected: Paladin            │
│               │  STR 12  VIT 8  AGI 5  SPD 6 │
│               │  Weapon: Iron Sword           │
│               │  Armor: Chain Mail            │
└───────────────┴───────────────────────────────┘
```

**Dynamic UI Elements:**
- `RosterList` — Available heroes not in party (from save Roster)
- `PartyList` — Active party members with XP%, weapon, armor summary
- `DetailLabel` — Selected hero stat block
- `GoldLabel` — Current gold

**Key Behaviors:**
- Tap party member to select; tap again to remove
- Tap roster hero to add to party (max 4)
- XP shown as percentage: `XP {pct}%`
- Hero portraits from `ActorLibrary.Get(cc).Portrait`

**Runtime Row Content (per party member):**

| Element | Content |
|---------|---------|
| Icon | Hero portrait sprite |
| Label | `"{Class}  Lv.{level}"` |
| SubLabel | `"XP 65%  \|  Iron Sword  \|  Chain Mail"` |

---

### 2. Shop (`ShopSectionController`)

**Vendor Theme:** General Store (Courier portrait, warm gold/amber background)

Buy consumables, materials, and basic equipment; sell owned items.

```
┌──────────────────────────────────────────┐
│  General Store              Gold: 1200   │
├──────────────────────────────────────────┤
│  [Buy Tab]  [Sell Tab]                   │
├──────────────────────────────────────────┤
│  BUY MODE:                               │
│  [△] Basic Healing Potion — 50g          │
│  [△] Mana Potion — 75g                   │
│  [◇] Iron Ore — 20g         (owned: 3)  │
│  [⚔] Rusty Sword — 100g                 │
│  [🛡] Leather Armor — 120g               │
├──────────────────────────────────────────┤
│  SELL MODE:                              │
│  [⚔] Iron Sword x2   Sell for 40g each  │
│  [◇] Leather x5      Sell for 5g each   │
└──────────────────────────────────────────┘
```

**Dynamic UI Elements:**
- `ItemList` — Buy catalog or sell inventory list
- `BuyTab` / `SellTab` — Mode toggle buttons
- `GoldLabel`, `DetailLabel`

**Modes:**

| Mode | Source | Row Content |
|------|--------|-------------|
| Buy | Static catalog (healing potions, mana potions, vendor materials, basic equipment) | `"{name} — {cost}g"` + description + owned count |
| Sell | `Inventory.All()` | `"{name} x{count}"` + `"Sell for {ComputedSellValue}g each"` |

**Catalog Sources:**
- `ItemData_Healing.BasicHealingPotion`, `ItemData_Healing.ManaPotion`
- `ItemLibrary.VendorMaterials()`
- `ItemData_Equipment.RustySword`, `ItemData_Equipment.LeatherArmor`

---

### 3. Medical (`MedicalSectionController`)

**Vendor Theme:** Medical Tent (Cleric portrait, soft green/white background)

Heal injured heroes for gold, use healing potions, or heal all at once.

```
┌────────────────────────────────────────────┐
│  Medical Tent              Gold: 1200      │
├────────────────────────────────────────────┤
│  [Icon] Paladin   HP 120/150   [Heal 15g] │
│  [Icon] Archer    HP  45/80    [Heal 18g] │
│  [Icon] Cleric    HP  80/80    (Full HP)  │
│                                            │
│  [Heal All — 33g]                          │
│  [Use Healing Potion]                      │
└────────────────────────────────────────────┘
```

**Dynamic UI Elements:**
- `HeroList` — Party members with HP bars and individual heal buttons
- `GoldLabel`, `DetailLabel`

**Heal Cost Formula:** `1 gold per 5 HP missing (minimum 1g)`

**HP Color Coding:**
- `> 50%` → `#88FF88` (green)
- `> 25%` → `#FFCC44` (yellow)
- `≤ 25%` → `#FF6666` (red)

**Special Rows:**
- **Heal All** — Appears when any hero is damaged; sums individual costs
- **Use Healing Potion** — Appears when `healing_potion_basic` count > 0 and someone is damaged

---

### 4. Residence (`ResidenceSectionController`)

**Vendor Theme:** Residence (PandaGirl portrait, warm wood/tan background)

Inn/tavern providing rest, recruitment, and hero lore browsing.

```
┌──────────────────────────────────────────────────────┐
│  Residence                           Gold: 1200      │
├────────────────────────┬─────────────────────────────┤
│  [Rest at Inn — Free]  │  Hero Lore                  │
│                        │                             │
│  Available Recruits:   │  "Paladin"                  │
│  [Icon] Monk  Lv.3     │  The stalwart defender of   │
│  [Icon] Thief Lv.1     │  the realm, wielding holy   │
│  [Icon] Sage  Lv.2     │  power against the undead.  │
│                        │                             │
│  Recruited Heroes:     │  Trivia:                    │
│  [Icon] Paladin Lv.5   │  - Trained at the Order     │
│  [Icon] Archer  Lv.3   │  - Fears spiders            │
└────────────────────────┴─────────────────────────────┘
```

**Dynamic UI Elements:**
- `ActionList` — Rest button + current party members (tap for lore)
- `RecruitList` — Roster heroes not in party (tap to add)
- `GoldLabel`, `DetailLabel` — Hero lore/backstory

**Features:**
- **Rest** — Heals all heroes to full HP (free, once per visit; tracked by `hasRestedThisVisit`)
- **Recruit** — Add roster heroes to party (disabled when party is full at 4)
- **Lore** — Tap a party member to view `ActorData.Description` in the detail panel

---

### 5. Blacksmith (`BlacksmithSectionController`)

**Vendor Theme:** Blacksmith (Engineer portrait, fiery orange/brown background)

Craft, buy, sell, repair, and salvage equipment.

```
┌───────────────────────────────────────────────────┐
│  Blacksmith                        Gold: 1200     │
├───────────────────────────────────────────────────┤
│  [Buy] [Sell] [Craft] [Repair] [Salvage]          │
├───────────────────────────────────────────────────┤
│  CRAFT MODE:                                      │
│  Iron Sword — 50g                                 │
│    Iron Ore 3/3, Leather 1/2                      │
│  Steel Sword — 120g                               │
│    Iron Ore 5/5, Steel Bar 0/2                    │
├───────────────────────────────────────────────────┤
│  REPAIR MODE:                                     │
│  Repair Iron Sword — 15g                          │
│    Durability: 150/200 (75%)                      │
├───────────────────────────────────────────────────┤
│  SALVAGE MODE:                                    │
│  Salvage Rusty Sword x1                           │
│    Yields: Iron Ore x2, Scrap Metal x1            │
└───────────────────────────────────────────────────┘
```

**Dynamic UI Elements:**
- `ItemList` — Content changes per mode
- `BuyTab`, `SellTab`, `CraftTab`, `RepairTab`, `SalvageTab`
- `GoldLabel`, `DetailLabel`

**Modes:**

| Mode | Source | Row Content |
|------|--------|-------------|
| Buy | `ItemLibrary.ByType(Equipment)` | `"{name} [{slot}] — {cost}g"` + stat summary |
| Sell | `Inventory.ByType(Equipment)` | `"{name} x{count}"` + sell value + stats |
| Craft | `RecipeLibrary.All()` | `"{recipe} — {goldCost}g"` + ingredient list with `have/need` counts |
| Repair | `Inventory.ByType(Equipment)` where damaged | `"Repair {name} — {cost}g"` + durability bar |
| Salvage | `Inventory.ByType(Equipment)` where salvageable | `"Salvage {name} x{count}"` + yield list from `SalvageComponents` |

**Craft Ingredient Colors:**
- `<color=#88CC88>` (green) — Have enough
- `<color=#CC6666>` (red) — Not enough

---

### 6. Training (`TrainingSectionController`)

**Vendor Theme:** Training Hall (Mannequin portrait, cool blue/purple background)

Learn new abilities by spending gold. Two-panel layout: hero selection → skill list.

```
┌──────────────┬────────────────────────────────┐
│  Heroes      │  Training for: Paladin         │
│  ┌─────────┐ │  ┌──────────────────────────┐  │
│  │ Paladin │ │  │ Heal — 200g              │  │
│  │ Archer  │ │  │ Shield Bash — 150g       │  │
│  │ Cleric  │ │  │ Holy Strike — Learned ✓  │  │
│  └─────────┘ │  └──────────────────────────┘  │
│              │                                │
│              │  Gold: 1200                    │
└──────────────┴────────────────────────────────┘
```

**Dynamic UI Elements:**
- `HeroList` — Party members with learned skill count
- `TrainingList` — Available trainings filtered by hero tags and level
- `VendorPortrait` — Set to Mannequin sprite
- `GoldLabel`, `DetailLabel`

**Flow:**
1. Select hero from party list (auto-selects first)
2. `TrainingLibrary.ForHero(cc, level)` populates available trainings
3. Already-learned skills shown as `"<color=#88CC88>Learned</color>"` (non-interactable)
4. Pay gold → `save.Training.GetOrCreate(cc).LearnedTrainingIds.Add(id)`

**Persistence:** Training data saved to `SaveState.Training` (`TrainingSaveData`)

---

### 7. Equip (`EquipSectionController`)

**Vendor Theme:** Armory (Knight portrait, steel blue/gray background)

Manage per-hero equipment across 6 slots with stat previews.

```
┌──────────┬───────────────────────────────────┐
│ Heroes   │  Equipment Slots                  │
│ ┌──────┐ │  ┌────────────────────────────┐   │
│ │Paladin│ │  │ Weapon: Iron Sword  [Unequip]│ │
│ │Archer │ │  │ Armor:  Chain Mail  [Unequip]│ │
│ │Cleric │ │  │ Helmet: (empty)     [Browse] │ │
│ └──────┘ │  │ Boots:  (empty)     [Browse] │ │
│          │  │ Ring:   (empty)     [Browse] │ │
│          │  │ Amulet: (empty)     [Browse] │ │
│          │  └────────────────────────────┘   │
│          │  Stats: STR 12 VIT 8 AGI 5 ...    │
│          ├───────────────────────────────────┤
│          │  Available Items for [Weapon]      │
│          │  Steel Sword (+7 STR)  [Equip]    │
│          │  War Hammer (+10 STR)  [Equip]    │
└──────────┴───────────────────────────────────┘
```

**Dynamic UI Elements:**
- `HeroList` — Party members with `"{count}/6 slots equipped"` summary
- `SlotList` — 6 equipment slots (Weapon, Armor, Helmet, Boots, Ring, Amulet)
- `ItemPicker` — Inventory items matching the browsed slot
- `StatsLabel` — Base stats + equipment bonuses + totals
- `GoldLabel`, `DetailLabel`

**Equipment Slots:** `Weapon`, `Armor`, `Helmet`, `Boots`, `Ring`, `Amulet`

**Flow:**
1. Select hero → `RefreshSlotList()` shows 6 slots
2. Tap empty slot → `BrowseSlot()` opens item picker filtered by `Inventory.BySlot(slot)`
3. Tap item → `EquipItem()`: remove from inventory, equip, return old item to inventory
4. Tap equipped slot → `UnequipSlot()`: return item to inventory

**Stat Display:** Base stats + `Formulas.ComputeEquipmentBonus(loadout)` = totals

---

### 8. Inventory (`InventorySectionController`)

**Vendor Theme:** Custom (no portrait, dark purple background, "Inventory" nameplate)

Full bag viewer with type filtering, sorting, and detail inspection.

```
┌───────────────────────┬─────────────────────────────┐
│ [All] [Equip] [Cons]  │  Item Detail                │
│ [Mats] [Quest]        │                             │
├───────────────────────┤  Iron Sword                 │
│ [⚔] Iron Sword x1    │  Type: Equipment (Weapon)   │
│ [⚔] Steel Sword x1   │  Rarity: Uncommon           │
│ [△] Heal Potion x5   │  STR +7  AGI +1             │
│ [◇] Iron Ore x12     │  Durability: 200/200        │
│ [◇] Leather x8       │  Value: 100g                │
│                       │  Salvage: Iron Ore x2, ...  │
└───────────────────────┴─────────────────────────────┘
```

**Dynamic UI Elements:**
- `ItemList` — Filtered, sorted item rows
- `FilterAll`, `FilterEquip`, `FilterCons`, `FilterMats`, `FilterQuest` — Tab buttons
- `GoldLabel`, `DetailLabel` — Full item detail (rarity, type, stats, durability, salvage)

**Filter Modes:**

| Filter | `PlayerInventory` Method |
|--------|--------------------------|
| All | `Inventory.All()` |
| Equipment | `Inventory.ByType(ItemType.Equipment)` |
| Consumable | `Inventory.ByType(ItemType.Consumable)` |
| Materials | `Inventory.ByType(ItemType.CraftingMaterial)` |
| Quest | `Inventory.ByType(ItemType.QuestItem)` |

**Sort Order:** Type → Rarity (descending) → DisplayName

**Detail Panel Content:** Name (rarity-colored), type, description, owned count, slot, stat bonuses, durability, sell value, salvage components

**Summary Row:** `"{filterName}: {stacks} stacks, {items} items"` (non-interactable)

---

### 9. Battle Prep (`BattlePrepSectionController`)

**Vendor Theme:** Custom (Captain portrait, warm brown background, "Battle Prep" nameplate)

Final review before combat. Shows full party lineup, consumable selection, and aggregate stats.

```
┌────────────────────────────────────────────────────────┐
│  Battle Preparation                    Gold: 1200      │
├────────────────────────────────────────────────────────┤
│  PARTY LINEUP                                          │
│  ┌───────────────────────────────────────────────────┐ │
│  │ [Icon] Paladin  Lv.5   HP 150                     │ │
│  │        STR 16  AGI 5  INT 3  ⚔ Iron Sword        │ │
│  │        Skills: Heal, Shield Bash                  │ │
│  ├───────────────────────────────────────────────────┤ │
│  │ [Icon] Archer   Lv.3   HP  80                     │ │
│  │        STR  5  AGI 14  INT 2  ⚔ Hunter's Bow     │ │
│  │        Skills: Power Shot                         │ │
│  └───────────────────────────────────────────────────┘ │
│                                                        │
│  BATTLE ITEMS (tap to toggle)                          │
│  [△] Healing Potion x5  [✓]                           │
│  [△] Mana Potion x3     [✓]                           │
│                                                        │
│  PARTY POWER                                           │
│  Total STR: 21  Total VIT: 20  Avg Level: 4.0         │
│                                                        │
│  [  ENTER BATTLE  ]                                    │
└────────────────────────────────────────────────────────┘
```

**Dynamic UI Elements:**
- `PartyList` — Full hero cards (portrait, level, HP, stats with gear bonuses, weapon, armor, skills, abilities)
- `ItemList` — Consumables with toggle selection (✓/✗)
- `BattleButton` — Triggers `hub.GoToBattle()`
- `GoldLabel`, `DetailLabel` — Aggregate party power summary

**Party Card Row Content:**

| Element | Content |
|---------|---------|
| Icon | Hero portrait |
| Label | `"{name}  Lv.{level}  HP {hp}"` |
| SubLabel | `"STR {n}  AGI {n}  INT {n}  \|  ⚔ {weapon}  \|  🛡 {armor}  \|  Skills: {list}"` |

**Battle Item Toggles:**
- All consumables auto-selected on activation
- Tap to toggle: `selectedBattleItems` HashSet
- Visual: `<color=#88FF88>✓</color>` or `<color=#666666>✗</color>`

---

## Dynamically Generated UI

### HubItemRowFactory

Every list row across all sections is created via `HubItemRowFactory.Create(container)`:

```
HubItemRow (root — 72px tall, full width)
├── Image (dark semi-transparent background: rgba(31,31,41,0.85))
├── Button (ColorTint transition, no navigation)
├── LayoutElement (preferredHeight=72, flexibleWidth=1)
├── Icon (56×56, left-aligned, hidden by default)
│   └── Image (preserveAspect=true)
├── Label (TMP, offset right of icon)
└── SubLabel (TMP, below label, smaller font)
```

**Static Helpers:**
- `SetLabel(go, text)` / `SetSubLabel(go, text)` — Rich text supported
- `SetIcon(go, item)` — Sets icon from `ItemDefinition`
- `SetIconSprite(go, sprite)` — Direct sprite (portraits)
- `SetIconColor(go, color)` — Colored placeholder
- `SetLabelColor(go, color)` — Tint for rarity or emphasis
- `SetSelected(go, bool)` — Highlight row background
- `RarityColor(rarity)` — Common=white, Uncommon=green, Rare=blue, Epic=purple, Legendary=orange

### HubVendorFactory

Each section panel gets a themed vendor overlay via `HubVendorFactory.Build(panel, theme)`:

```
VendorOverlay (appended to panel root, non-interactive)
├── VendorBackground (Image — gradient fill, top→bottom color)
├── VendorPortrait (Image — character portrait from ActorLibrary)
└── VendorNameplate (TMP — vendor title text)
```

**Theme Presets:**

| Section | VendorName | Portrait | Background |
|---------|-----------|----------|------------|
| Party | Camp | Paladin | Deep red/maroon |
| Shop | General Store | Courier | Warm gold/amber |
| Blacksmith | Blacksmith | Engineer | Fiery orange/brown |
| Training | Training Hall | Mannequin | Cool blue/purple |
| Medical | Medical Tent | Cleric | Soft green/white |
| Equip | Armory | Knight | Steel blue/gray |
| Residence | Residence | PandaGirl | Warm wood/tan |
| Inventory | Inventory | (none) | Dark purple |
| Battle Prep | Battle Prep | Captain | Warm brown |

### TiltParallax

Every section panel has a `TiltParallax` component added at runtime (`HubManager.AttachTiltParallax()`):
- `amplitude = 12f`
- `smoothing = 6f`
- `deadzone = 0.015f`

---

## Data Flow

### Save/Load Cycle

```
Scene Load (Awake)
 └── LoadFromSave()
      ├── SharedInventory.LoadFromSaveData(save.Inventory)
      └── SharedLoadout.LoadFromSave(save.Equipment)

Section Switch (Activate)
 └── WriteToSave() + ProfileHelper.Save(false)
      ├── save.Inventory = SharedInventory.ToSaveData()
      └── save.Equipment = SharedLoadout.ToSave()

Scene Exit (GoToOverworld / GoToBattle)
 └── WriteToSave()
      └── scene.Fade.ToOverworld() / scene.Fade.ToGame()
```

### Shared State Dependencies

```
HubManager
├── SharedInventory (PlayerInventory)
│   ├── Shop: Buy/Sell items
│   ├── Blacksmith: Buy/Sell/Craft/Repair/Salvage equipment
│   ├── Medical: Pay for healing, use potions
│   ├── Training: Pay for skill training
│   ├── Equip: Move items to/from hero loadouts
│   ├── Inventory: Browse and inspect all items
│   └── BattlePrep: Select consumables for battle
│
└── SharedLoadout (PartyLoadout)
    ├── Party: Show weapon/armor summaries
    ├── Equip: Equip/unequip per slot
    └── BattlePrep: Show gear + compute stat totals
```

---

## Scene Object Names

Defined in `GameObjectHelper.Hub`:

| Constant | Value |
|----------|-------|
| `PartyButton` | `"PartyButton"` |
| `ShopButton` | `"ShopButton"` |
| `MedicalButton` | `"MedicalButton"` |
| `ResidenceButton` | `"ResidenceButton"` |
| `BlacksmithButton` | `"BlacksmithButton"` |
| `TrainingButton` | `"TrainingButton"` |
| `EquipButton` | `"EquipButton"` |
| `InventoryButton` | `"InventoryButton"` |
| `BattlePrepButton` | `"BattlePrepButton"` |
| `OverworldButton` | `"OverworldButton"` |
| `BattleButton` | `"BattleButton"` |
| `PartyPanel` | `"PartyPanel"` |
| `ShopPanel` | `"ShopPanel"` |
| `MedicalPanel` | `"MedicalPanel"` |
| `ResidencePanel` | `"ResidencePanel"` |
| `BlacksmithPanel` | `"BlacksmithPanel"` |
| `TrainingPanel` | `"TrainingPanel"` |
| `EquipPanel` | `"EquipPanel"` |
| `InventoryPanel` | `"InventoryPanel"` |
| `BattlePrepPanel` | `"BattlePrepPanel"` |

---

## Related Files

| File | Role |
|------|------|
| `Scripts/Hub/HubManager.cs` | Central hub controller, shared state, section navigation |
| `Scripts/Hub/PartySectionController.cs` | Party management screen |
| `Scripts/Hub/ShopSectionController.cs` | Buy/sell consumables and materials |
| `Scripts/Hub/MedicalSectionController.cs` | Healing and resurrection |
| `Scripts/Hub/ResidenceSectionController.cs` | Inn, recruitment, hero lore |
| `Scripts/Hub/BlacksmithSectionController.cs` | Craft, repair, salvage equipment |
| `Scripts/Hub/TrainingSectionController.cs` | Ability training |
| `Scripts/Hub/EquipSectionController.cs` | Per-hero equipment management |
| `Scripts/Hub/InventorySectionController.cs` | Full inventory viewer |
| `Scripts/Hub/BattlePrepSectionController.cs` | Pre-battle review and item selection |
| `Scripts/Factories/HubItemRowFactory.cs` | Standardized list row creation |
| `Scripts/Factories/HubVendorFactory.cs` | Themed vendor overlays |
| `Scripts/Inventory/PlayerInventory.cs` | Item storage and gold |
| `Scripts/Inventory/HeroLoadout.cs` | Per-hero equipment slots |
| `Scripts/Helpers/GameObjectHelper.cs` | Scene object name constants |
| `Scripts/Helpers/ProfileHelper.cs` | Save/load persistence |
- **Cameras**: 
- **Canvases**: Canvas
