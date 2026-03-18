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
/// BLACKSMITHSECTIONCONTROLLER - Hub equipment/blacksmith section.
/// 
/// PURPOSE:
/// Manages crafting, buy, sell, and repair operations for
/// equipment items. Uses RecipeLibrary for real crafting
/// and HubManager.SharedInventory for all transactions.
/// 
/// MODES:
/// - Buy: Purchase equipment from catalog
/// - Sell: Sell equipment for gold (uses ComputedSellValue)
/// - Craft: Combine materials via RecipeLibrary recipes
/// - Repair: Restore equipment durability
/// 
/// RELATED FILES:
/// - HubManager.cs: Hub scene controller, owns SharedInventory
/// - RecipeLibrary.cs: Crafting recipe registry
/// - CraftingRecipe.cs: Recipe data structure
/// - PlayerInventory.cs: Item storage
/// - ItemDefinition.cs: Equipment stats
/// </summary>
public class BlacksmithSectionController : MonoBehaviour
{
    private HubManager hub;

    public enum BlacksmithMode { Buy, Sell, Craft, Repair, Salvage }
    public BlacksmithMode mode = BlacksmithMode.Craft;

    private List<ItemDefinition> equipmentCatalog = new List<ItemDefinition>();

    // Runtime UI references
    private RectTransform listContainer;
    private TextMeshProUGUI goldLabel;
    private TextMeshProUGUI detailLabel;
    private Button buyTabButton;
    private Button sellTabButton;
    private Button craftTabButton;
    private Button repairTabButton;
    private Button salvageTabButton;

    /// <summary>Gets the shared inventory from HubManager.</summary>
    private PlayerInventory Inventory => hub?.SharedInventory;

    /// <summary>Initializes and loads equipment catalog.</summary>
    public void Initialize(HubManager hubManager)
    {
        hub = hubManager;
        LoadCatalog();
        ResolveUI();
        HubVendorFactory.Build(GetComponent<RectTransform>(), HubVendorFactory.BlacksmithTheme);
    }

    /// <summary>Called when section is shown.</summary>
    public void OnActivated()
    {
        RefreshList();
        RefreshGoldDisplay();
    }

    /// <summary>Resolves UI references from scene hierarchy.</summary>
    private void ResolveUI()
    {
        var rt = GetComponent<RectTransform>();
        if (rt == null) return;

        // Center — scrollable list (inside ItemScrollView/Viewport); content changes per mode
        listContainer = rt.Find("ItemScrollView/Viewport/" + GameObjectHelper.Hub.ItemList)?.GetComponent<RectTransform>();
        // Top-right — current gold display
        goldLabel = rt.Find(GameObjectHelper.Hub.GoldLabel)?.GetComponent<TextMeshProUGUI>();
        // Top-center — shows current mode name
        detailLabel = rt.Find(GameObjectHelper.Hub.DetailLabel)?.GetComponent<TextMeshProUGUI>();
        // Top tab bar — 5 mode toggle buttons
        buyTabButton = rt.Find(GameObjectHelper.Hub.BuyTab)?.GetComponent<Button>();
        sellTabButton = rt.Find(GameObjectHelper.Hub.SellTab)?.GetComponent<Button>();
        craftTabButton = rt.Find(GameObjectHelper.Hub.CraftTab)?.GetComponent<Button>();
        repairTabButton = rt.Find(GameObjectHelper.Hub.RepairTab)?.GetComponent<Button>();
        salvageTabButton = rt.Find(GameObjectHelper.Hub.SalvageTab)?.GetComponent<Button>();

        if (buyTabButton != null) buyTabButton.onClick.AddListener(() => SetMode(BlacksmithMode.Buy));
        if (sellTabButton != null) sellTabButton.onClick.AddListener(() => SetMode(BlacksmithMode.Sell));
        if (craftTabButton != null) craftTabButton.onClick.AddListener(() => SetMode(BlacksmithMode.Craft));
        if (repairTabButton != null) repairTabButton.onClick.AddListener(() => SetMode(BlacksmithMode.Repair));
        if (salvageTabButton != null) salvageTabButton.onClick.AddListener(() => SetMode(BlacksmithMode.Salvage));
    }

    /// <summary>Loads baseline equipment offerings.</summary>
    private void LoadCatalog()
    {
        equipmentCatalog.Clear();

        // All equipment from the various data files
        foreach (var item in ItemLibrary.ByType(ItemType.Equipment))
        {
            equipmentCatalog.Add(item);
        }
    }

    /// <summary>Sets the mode and refreshes the list.</summary>
    public void SetMode(BlacksmithMode newMode)
    {
        mode = newMode;
        RefreshList();
    }

    /// <summary>Refreshes the list UI for current mode.</summary>
    private void RefreshList()
    {
        if (listContainer == null) return;
        ClearChildren(listContainer);

        switch (mode)
        {
            case BlacksmithMode.Buy:
                foreach (var item in equipmentCatalog)
                {
                    bool canAfford = Inventory != null && Inventory.Gold >= item.BaseCost;
                    var go = HubItemRowFactory.Create(listContainer);

                    HubItemRowFactory.SetIcon(go, item);
                    HubItemRowFactory.SetLabel(go, $"{item.DisplayName} [{item.Slot}] — {item.BaseCost}g");
                    HubItemRowFactory.SetLabelColor(go, HubItemRowFactory.RarityColor(item.Rarity));
                    HubItemRowFactory.SetSubLabel(go, FormatItemStats(item));

                    var btn = go.GetComponent<Button>();
                    if (btn != null)
                    {
                        btn.interactable = canAfford;
                        var captured = item;
                        btn.onClick.AddListener(() => { Buy(captured); RefreshList(); RefreshGoldDisplay(); });
                    }
                }
                break;

            case BlacksmithMode.Sell:
                if (Inventory == null) break;
                foreach (var entry in Inventory.ByType(ItemType.Equipment))
                {
                    var item = entry.Definition;
                    var go = HubItemRowFactory.Create(listContainer);

                    HubItemRowFactory.SetIcon(go, item);
                    HubItemRowFactory.SetLabel(go, $"{item.DisplayName} x{entry.Count}");
                    HubItemRowFactory.SetLabelColor(go, HubItemRowFactory.RarityColor(item.Rarity));
                    HubItemRowFactory.SetSubLabel(go, $"Sell for {item.ComputedSellValue}g — {FormatItemStats(item)}");

                    var btn = go.GetComponent<Button>();
                    var captured = item;
                    if (btn != null) btn.onClick.AddListener(() => { Sell(captured); RefreshList(); RefreshGoldDisplay(); });
                }
                break;

            case BlacksmithMode.Craft:
                foreach (var recipe in RecipeLibrary.All())
                {
                    bool canCraft = Inventory != null && recipe.CanCraft(Inventory);
                    var go = HubItemRowFactory.Create(listContainer);

                    var resultDef = ItemLibrary.Get(recipe.ResultItemId);
                    var rarity = resultDef?.Rarity ?? ItemRarity.Common;
                    HubItemRowFactory.SetIcon(go, resultDef);
                    HubItemRowFactory.SetLabel(go, $"{recipe.DisplayName} — {recipe.GoldCost}g");
                    HubItemRowFactory.SetLabelColor(go, HubItemRowFactory.RarityColor(rarity));

                    string ingredients = string.Join(", ", recipe.Ingredients.Select(i =>
                    {
                        var def = ItemLibrary.Get(i.ItemId);
                        string name = def?.DisplayName ?? i.ItemId;
                        int have = Inventory?.CountOf(i.ItemId) ?? 0;
                        bool enough = have >= i.Count;
                        string color = enough ? "#88CC88" : "#CC6666";
                        return $"<color={color}>{name} {have}/{i.Count}</color>";
                    }));
                    HubItemRowFactory.SetSubLabel(go, ingredients);

                    var btn = go.GetComponent<Button>();
                    if (btn != null)
                    {
                        btn.interactable = canCraft;
                        var capturedRecipe = recipe;
                        btn.onClick.AddListener(() => { Craft(capturedRecipe); RefreshList(); RefreshGoldDisplay(); });
                    }
                }
                break;

            case BlacksmithMode.Repair:
                if (Inventory == null) break;
                foreach (var entry in Inventory.ByType(ItemType.Equipment))
                {
                    if (entry.Definition.Durability <= 0) continue;
                    if (entry.CurrentDurability >= entry.Definition.Durability) continue;
                    int cost = ComputeRepairCost(entry);
                    bool canAffordRepair = Inventory.Gold >= cost;
                    float pct = (float)entry.CurrentDurability / entry.Definition.Durability * 100f;

                    var go = HubItemRowFactory.Create(listContainer);
                    HubItemRowFactory.SetIcon(go, entry.Definition);
                    HubItemRowFactory.SetLabel(go, $"Repair {entry.Definition.DisplayName} — {cost}g");
                    HubItemRowFactory.SetLabelColor(go, HubItemRowFactory.RarityColor(entry.Definition.Rarity));
                    HubItemRowFactory.SetSubLabel(go, $"Durability: {entry.CurrentDurability}/{entry.Definition.Durability} ({pct:F0}%)");

                    var btn = go.GetComponent<Button>();
                    if (btn != null)
                    {
                        btn.interactable = canAffordRepair;
                        var capturedId = entry.Definition.Id;
                        var capturedCost = cost;
                        btn.onClick.AddListener(() => { Repair(capturedId, capturedCost); RefreshList(); RefreshGoldDisplay(); });
                    }
                }
                break;

            case BlacksmithMode.Salvage:
                if (Inventory == null) break;
                foreach (var entry in Inventory.ByType(ItemType.Equipment))
                {
                    var item = entry.Definition;
                    if (!item.CanSalvage) continue;

                    var go = HubItemRowFactory.Create(listContainer);
                    HubItemRowFactory.SetIcon(go, item);
                    HubItemRowFactory.SetLabel(go, $"Salvage {item.DisplayName} x{entry.Count}");
                    HubItemRowFactory.SetLabelColor(go, HubItemRowFactory.RarityColor(item.Rarity));

                    string yields = string.Join(", ", item.SalvageComponents.Select(sc =>
                    {
                        var matDef = ItemLibrary.Get(sc.MaterialId);
                        string matName = matDef?.DisplayName ?? sc.MaterialId;
                        return $"{matName} x{sc.Count}";
                    }));
                    HubItemRowFactory.SetSubLabel(go, $"Yields: {yields}");

                    var btn = go.GetComponent<Button>();
                    var capturedId = item.Id;
                    if (btn != null) btn.onClick.AddListener(() => { Salvage(capturedId); RefreshList(); RefreshGoldDisplay(); });
                }
                break;
        }

        if (detailLabel != null)
            detailLabel.text = mode.ToString();
    }

    /// <summary>Refreshes gold display.</summary>
    private void RefreshGoldDisplay()
    {
        if (goldLabel != null && Inventory != null)
            goldLabel.text = $"Gold: {Inventory.Gold}";
    }

    /// <summary>Buys equipment and adds to shared inventory.</summary>
    public bool Buy(ItemDefinition item)
    {
        if (item == null || Inventory == null) return false;
        if (Inventory.Gold < item.BaseCost) return false;
        Inventory.Gold -= item.BaseCost;
        Inventory.Add(item);
        return true;
    }

    /// <summary>Sells equipment using ComputedSellValue.</summary>
    public bool Sell(ItemDefinition item)
    {
        if (item == null || Inventory == null) return false;
        if (!Inventory.Contains(item.Id)) return false;
        Inventory.Remove(item.Id, 1);
        Inventory.Gold += item.ComputedSellValue;
        return true;
    }

    /// <summary>Crafts an item via a CraftingRecipe.</summary>
    public bool Craft(CraftingRecipe recipe)
    {
        if (recipe == null || Inventory == null) return false;
        return recipe.Execute(Inventory);
    }

    /// <summary>Repairs durability of equipment at computed cost.</summary>
    public bool Repair(string itemId, int cost)
    {
        if (Inventory == null) return false;
        if (Inventory.Gold < cost) return false;
        var entry = Inventory.GetEntry(itemId);
        if (entry == null) return false;
        if (entry.Definition.Durability <= 0) return false;
        entry.CurrentDurability = entry.Definition.Durability;
        Inventory.Gold -= cost;
        return true;
    }

    /// <summary>Salvages an equipment item into its component materials.</summary>
    public bool Salvage(string itemId)
    {
        if (Inventory == null) return false;
        if (!Inventory.Contains(itemId)) return false;

        var def = ItemLibrary.Get(itemId);
        if (def == null || !def.CanSalvage) return false;

        Inventory.Remove(itemId, 1);

        foreach (var comp in def.SalvageComponents)
        {
            var matDef = ItemLibrary.Get(comp.MaterialId);
            if (matDef != null)
                Inventory.Add(matDef, comp.Count);
        }
        return true;
    }

    /// <summary>Computes repair cost based on damage taken.</summary>
    private int ComputeRepairCost(PlayerInventory.Entry entry)
    {
        int missing = entry.Definition.Durability - entry.CurrentDurability;
        return Mathf.Max(1, missing / 2);
    }

    /// <summary>Formats equipment stat modifiers as a compact string.</summary>
    private string FormatItemStats(ItemDefinition item)
    {
        if (item == null) return "";
        var parts = new List<string>();
        if (item.Strength != 0) parts.Add($"STR {item.Strength:+0;-0}");
        if (item.Vitality != 0) parts.Add($"VIT {item.Vitality:+0;-0}");
        if (item.Agility != 0) parts.Add($"AGI {item.Agility:+0;-0}");
        if (item.Stamina != 0) parts.Add($"STA {item.Stamina:+0;-0}");
        if (item.Intelligence != 0) parts.Add($"INT {item.Intelligence:+0;-0}");
        if (item.Wisdom != 0) parts.Add($"WIS {item.Wisdom:+0;-0}");
        if (item.Luck != 0) parts.Add($"LCK {item.Luck:+0;-0}");
        return string.Join("  ", parts);
    }

    /// <summary>Clears all child GameObjects of a container.</summary>
    private void ClearChildren(RectTransform container)
    {
        for (int i = container.childCount - 1; i >= 0; i--)
            Destroy(container.GetChild(i).gameObject);
    }
}

}
