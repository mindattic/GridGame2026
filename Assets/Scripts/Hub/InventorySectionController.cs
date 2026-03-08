using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Factories;
using Scripts.Helpers;
using Scripts.Instances;
using Scripts.Instances.Actor;
using Scripts.Instances.Board;
using Scripts.Instances.SynergyLine;
using Scripts.Inventory;
using Scripts.Libraries;
using Scripts.Managers;
using Scripts.Models;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Hub
{
/// <summary>
/// INVENTORYSECTIONCONTROLLER - Full inventory/bag viewer.
/// 
/// PURPOSE:
/// Shows the player's entire inventory with filtering by type,
/// sorting, item detail inspection, and quick actions (use, drop).
/// This is the "Bag" screen found in most RPGs.
/// 
/// LAYOUT:
/// ```
/// ┌─────────────────────────────────────────────────────┐
/// │  Inventory                          Gold: 1200      │
/// ├───────────────────────┬─────────────────────────────┤
/// │ [All] [Equip] [Cons]  │  Item Detail                │
/// │ [Mats] [Quest]        │                             │
/// ├───────────────────────┤  Iron Sword                 │
/// │ [⚔] Iron Sword x1    │  Type: Equipment (Weapon)   │
/// │ [⚔] Steel Sword x1   │  Rarity: Uncommon           │
/// │ [△] Heal Potion x5   │  STR +7  AGI +1             │
/// │ [◇] Iron Ore x12     │  Durability: 200/200        │
/// │ [◇] Leather x8       │  Value: 100g                │
/// │                       │  Salvage: Iron Ore x2, ...  │
/// └───────────────────────┴─────────────────────────────┘
/// ```
/// 
/// FILTER TABS:
/// - All: Every item
/// - Equipment: Weapons, armor, accessories
/// - Consumable: Potions, scrolls
/// - Materials: Crafting ingredients
/// - Quest: Quest items
/// 
/// RELATED FILES:
/// - HubManager.cs: Owns SharedInventory
/// - PlayerInventory.cs: Item storage
/// - ItemDefinition.cs: Item data
/// - PlaceholderIconFactory.cs: Item icons
/// </summary>
public class InventorySectionController : MonoBehaviour
{
    private HubManager hub;

    private enum FilterMode { All, Equipment, Consumable, Materials, Quest }
    private FilterMode filter = FilterMode.All;
    private string selectedItemId;

    // Runtime UI references
    private RectTransform itemListContainer;
    private TextMeshProUGUI goldLabel;
    private TextMeshProUGUI detailLabel;
    private Button filterAllButton;
    private Button filterEquipButton;
    private Button filterConsButton;
    private Button filterMatsButton;
    private Button filterQuestButton;

    private PlayerInventory Inventory => hub?.SharedInventory;

    /// <summary>Initializes the controller.</summary>
    public void Initialize(HubManager hubManager)
    {
        hub = hubManager;
        ResolveUI();
        HubVendorFactory.Build(GetComponent<RectTransform>(), new HubVendorFactory.VendorTheme
        {
            VendorName = "Inventory",
            PortraitClass = CharacterClass.None,
            BackgroundTop = new Color(0.15f, 0.15f, 0.20f, 0.90f),
            BackgroundBottom = new Color(0.08f, 0.08f, 0.12f, 0.95f),
            NameplateColor = new Color(0.85f, 0.85f, 0.95f),
        });
    }

    /// <summary>Called when section becomes active.</summary>
    public void OnActivated()
    {
        selectedItemId = null;
        RefreshItemList();
        RefreshGoldDisplay();
        RefreshDetail();
    }

    /// <summary>Resolves UI references from scene hierarchy.</summary>
    private void ResolveUI()
    {
        var rt = GetComponent<RectTransform>();
        if (rt == null) return;

        itemListContainer = rt.Find("ItemList")?.GetComponent<RectTransform>();
        goldLabel = rt.Find("GoldLabel")?.GetComponent<TextMeshProUGUI>();
        detailLabel = rt.Find("DetailLabel")?.GetComponent<TextMeshProUGUI>();
        filterAllButton = rt.Find("FilterAll")?.GetComponent<Button>();
        filterEquipButton = rt.Find("FilterEquip")?.GetComponent<Button>();
        filterConsButton = rt.Find("FilterCons")?.GetComponent<Button>();
        filterMatsButton = rt.Find("FilterMats")?.GetComponent<Button>();
        filterQuestButton = rt.Find("FilterQuest")?.GetComponent<Button>();

        if (filterAllButton != null) filterAllButton.onClick.AddListener(() => SetFilter(FilterMode.All));
        if (filterEquipButton != null) filterEquipButton.onClick.AddListener(() => SetFilter(FilterMode.Equipment));
        if (filterConsButton != null) filterConsButton.onClick.AddListener(() => SetFilter(FilterMode.Consumable));
        if (filterMatsButton != null) filterMatsButton.onClick.AddListener(() => SetFilter(FilterMode.Materials));
        if (filterQuestButton != null) filterQuestButton.onClick.AddListener(() => SetFilter(FilterMode.Quest));
    }

    /// <summary>Sets the filter mode and refreshes.</summary>
    private void SetFilter(FilterMode mode)
    {
        filter = mode;
        RefreshItemList();
    }

    // ===================== Item List =====================

    /// <summary>Refreshes the filtered and sorted item list.</summary>
    private void RefreshItemList()
    {
        ClearContainer(itemListContainer);
        if (itemListContainer == null || Inventory == null) return;

        var entries = GetFilteredEntries()
            .OrderBy(e => e.Definition.Type)
            .ThenByDescending(e => e.Definition.Rarity)
            .ThenBy(e => e.Definition.DisplayName);

        int totalItems = 0;
        int totalStacks = 0;

        foreach (var entry in entries)
        {
            var item = entry.Definition;
            totalStacks++;
            totalItems += entry.Count;

            var go = HubItemRowFactory.Create(itemListContainer);
            HubItemRowFactory.SetIcon(go, item);

            string countText = entry.Count > 1 ? $" x{entry.Count}" : "";
            HubItemRowFactory.SetLabel(go, $"{item.DisplayName}{countText}");
            HubItemRowFactory.SetLabelColor(go, HubItemRowFactory.RarityColor(item.Rarity));
            HubItemRowFactory.SetSelected(go, item.Id == selectedItemId);

            // Sub-label: type + key stat
            string typeLabel = FormatTypeLabel(item);
            HubItemRowFactory.SetSubLabel(go, typeLabel);

            var capturedId = item.Id;
            var btn = go.GetComponent<Button>();
            if (btn != null) btn.onClick.AddListener(() =>
            {
                selectedItemId = capturedId;
                RefreshItemList();
                RefreshDetail();
            });
        }

        // Summary row
        var summaryGo = HubItemRowFactory.Create(itemListContainer);
        string filterName = filter == FilterMode.All ? "All Items" : filter.ToString();
        HubItemRowFactory.SetLabel(summaryGo, $"{filterName}: {totalStacks} stacks, {totalItems} items");
        HubItemRowFactory.SetSubLabel(summaryGo, "");
        HubItemRowFactory.SetLabelColor(summaryGo, new Color(0.6f, 0.6f, 0.7f));
        var summaryBtn = summaryGo.GetComponent<Button>();
        if (summaryBtn != null) summaryBtn.interactable = false;
    }

    /// <summary>Returns inventory entries matching the current filter.</summary>
    private IEnumerable<PlayerInventory.Entry> GetFilteredEntries()
    {
        if (Inventory == null) return Enumerable.Empty<PlayerInventory.Entry>();

        switch (filter)
        {
            case FilterMode.Equipment: return Inventory.ByType(ItemType.Equipment);
            case FilterMode.Consumable: return Inventory.ByType(ItemType.Consumable);
            case FilterMode.Materials: return Inventory.ByType(ItemType.CraftingMaterial);
            case FilterMode.Quest: return Inventory.ByType(ItemType.QuestItem);
            default: return Inventory.All();
        }
    }

    // ===================== Detail Panel =====================

    /// <summary>Refreshes the detail panel for the selected item.</summary>
    private void RefreshDetail()
    {
        if (detailLabel == null) return;

        if (string.IsNullOrEmpty(selectedItemId))
        {
            detailLabel.text = "Select an item to view details";
            return;
        }

        var entry = Inventory?.GetEntry(selectedItemId);
        if (entry == null)
        {
            detailLabel.text = "Item not found";
            selectedItemId = null;
            return;
        }

        var item = entry.Definition;
        var lines = new List<string>();

        // Name + rarity
        string rarityHex = ColorUtility.ToHtmlStringRGB(HubItemRowFactory.RarityColor(item.Rarity));
        lines.Add($"<b><color=#{rarityHex}>{item.DisplayName}</color></b>");
        lines.Add($"{item.Rarity} {item.Type}");

        // Description
        if (!string.IsNullOrEmpty(item.Description))
            lines.Add($"<i>{item.Description}</i>");

        lines.Add("");

        // Quantity
        lines.Add($"Owned: {entry.Count} / {item.MaxStack}");

        // Equipment-specific
        if (item.IsEquipment)
        {
            lines.Add($"Slot: {item.Slot}");

            // Stats
            var statParts = new List<string>();
            if (item.Strength != 0) statParts.Add($"STR {item.Strength:+0;-0}");
            if (item.Vitality != 0) statParts.Add($"VIT {item.Vitality:+0;-0}");
            if (item.Agility != 0) statParts.Add($"AGI {item.Agility:+0;-0}");
            if (item.Stamina != 0) statParts.Add($"STA {item.Stamina:+0;-0}");
            if (item.Intelligence != 0) statParts.Add($"INT {item.Intelligence:+0;-0}");
            if (item.Wisdom != 0) statParts.Add($"WIS {item.Wisdom:+0;-0}");
            if (item.Luck != 0) statParts.Add($"LCK {item.Luck:+0;-0}");
            if (statParts.Count > 0)
                lines.Add($"<color=#88CC88>{string.Join("  ", statParts)}</color>");

            // Durability
            if (item.Durability > 0)
                lines.Add($"Durability: {entry.CurrentDurability}/{item.Durability}");

            // Salvage preview
            if (item.CanSalvage)
            {
                string salvageStr = string.Join(", ", item.SalvageComponents.Select(sc =>
                {
                    var matDef = ItemLibrary.Get(sc.MaterialId);
                    return $"{matDef?.DisplayName ?? sc.MaterialId} x{sc.Count}";
                }));
                lines.Add($"<color=#CCAA66>Salvage: {salvageStr}</color>");
            }
        }

        // Consumable-specific
        if (item.Type == ItemType.Consumable && item.BaseHealing > 0)
            lines.Add($"<color=#88FF88>Heals: {item.BaseHealing} HP</color>");

        // Value
        lines.Add("");
        lines.Add($"Buy: {item.BaseCost}g  |  Sell: {item.ComputedSellValue}g");

        // Who has it equipped?
        if (item.IsEquipment && hub?.SharedLoadout != null)
        {
            var equippedBy = new List<string>();
            foreach (var kvp in hub.SharedLoadout.HeroLoadouts)
            {
                foreach (var slotKvp in kvp.Value.EquippedSlots)
                {
                    if (slotKvp.Value?.Id == item.Id)
                        equippedBy.Add($"{kvp.Key} ({slotKvp.Key})");
                }
            }
            if (equippedBy.Count > 0)
                lines.Add($"<color=#FFAA66>Equipped by: {string.Join(", ", equippedBy)}</color>");
        }

        detailLabel.text = string.Join("\n", lines);
    }

    // ===================== Helpers =====================

    /// <summary>Formats a compact type label for the sub-line.</summary>
    private string FormatTypeLabel(ItemDefinition item)
    {
        if (item == null) return "";
        switch (item.Type)
        {
            case ItemType.Equipment:
                var parts = new List<string>();
                parts.Add($"[{item.Slot}]");
                if (item.Strength != 0) parts.Add($"STR {item.Strength:+0;-0}");
                if (item.Vitality != 0) parts.Add($"VIT {item.Vitality:+0;-0}");
                if (item.Agility != 0) parts.Add($"AGI {item.Agility:+0;-0}");
                if (item.Intelligence != 0) parts.Add($"INT {item.Intelligence:+0;-0}");
                return string.Join("  ", parts);
            case ItemType.Consumable:
                return item.BaseHealing > 0 ? $"Heals {item.BaseHealing} HP" : "Consumable";
            case ItemType.CraftingMaterial:
                return $"Material — {item.BaseCost}g";
            case ItemType.QuestItem:
                return "Quest Item";
            default:
                return "";
        }
    }

    /// <summary>Refreshes gold display.</summary>
    private void RefreshGoldDisplay()
    {
        if (goldLabel != null && Inventory != null)
            goldLabel.text = $"Gold: {Inventory.Gold}";
    }

    /// <summary>Clears all children of a container.</summary>
    private void ClearContainer(RectTransform container)
    {
        if (container == null) return;
        for (int i = container.childCount - 1; i >= 0; i--)
            Destroy(container.GetChild(i).gameObject);
    }
}

}
