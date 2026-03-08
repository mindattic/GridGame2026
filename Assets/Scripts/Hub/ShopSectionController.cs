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
/// SHOPSECTIONCONTROLLER - Hub shop/store section.
/// 
/// PURPOSE:
/// Manages Buy and Sell operations for consumables, crafting
/// materials, and vendor equipment in the Hub shop screen.
/// Uses HubManager.SharedInventory for all transactions.
/// 
/// SHOP MODES:
/// - Buy: Purchase items from catalog (consumables + materials)
/// - Sell: Sell inventory items for gold (uses ComputedSellValue)
/// 
/// RELATED FILES:
/// - HubManager.cs: Hub scene controller, owns SharedInventory
/// - PlayerInventory.cs: Item storage
/// - ItemDefinition.cs: Item data
/// - ItemLibrary.cs: Item catalog
/// </summary>
public class ShopSectionController : MonoBehaviour
{
    private HubManager hub;

    public enum ShopMode { Buy, Sell }
    public ShopMode mode = ShopMode.Buy;

    private List<ItemDefinition> buyCatalog = new List<ItemDefinition>();

    // Runtime UI references
    private RectTransform itemListContainer;
    private TextMeshProUGUI goldLabel;
    private TextMeshProUGUI detailLabel;
    private Button buyTabButton;
    private Button sellTabButton;

    /// <summary>Gets the shared inventory from HubManager.</summary>
    private PlayerInventory Inventory => hub?.SharedInventory;

    /// <summary>Initializes with data references.</summary>
    public void Initialize(HubManager hubManager)
    {
        hub = hubManager;
        LoadCatalog();
        ResolveUI();
        HubVendorFactory.Build(GetComponent<RectTransform>(), HubVendorFactory.ShopTheme);
    }

    /// <summary>Called when activated.</summary>
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

        itemListContainer = rt.Find("ItemList")?.GetComponent<RectTransform>();
        goldLabel = rt.Find("GoldLabel")?.GetComponent<TextMeshProUGUI>();
        detailLabel = rt.Find("DetailLabel")?.GetComponent<TextMeshProUGUI>();
        buyTabButton = rt.Find("BuyTab")?.GetComponent<Button>();
        sellTabButton = rt.Find("SellTab")?.GetComponent<Button>();

        if (buyTabButton != null) buyTabButton.onClick.AddListener(() => SetMode(ShopMode.Buy));
        if (sellTabButton != null) sellTabButton.onClick.AddListener(() => SetMode(ShopMode.Sell));
    }

    /// <summary>Loads shop catalog from item libraries.</summary>
    private void LoadCatalog()
    {
        buyCatalog.Clear();

        // Consumables
        buyCatalog.Add(ItemData_Healing.BasicHealingPotion);
        buyCatalog.Add(ItemData_Healing.ManaPotion);

        // Vendor-purchasable materials
        foreach (var mat in ItemLibrary.VendorMaterials())
        {
            if (!buyCatalog.Any(x => x.Id == mat.Id))
                buyCatalog.Add(mat);
        }

        // Basic equipment
        buyCatalog.Add(ItemData_Equipment.RustySword);
        buyCatalog.Add(ItemData_Equipment.LeatherArmor);
    }

    /// <summary>Sets the shop mode and refreshes the list.</summary>
    public void SetMode(ShopMode newMode)
    {
        mode = newMode;
        RefreshList();
    }

    /// <summary>Refreshes the item list UI for current mode.</summary>
    private void RefreshList()
    {
        if (itemListContainer == null) return;
        ClearChildren(itemListContainer);

        if (mode == ShopMode.Buy)
        {
            foreach (var item in buyCatalog)
            {
                bool canAfford = Inventory != null && Inventory.Gold >= item.BaseCost;
                int owned = Inventory?.CountOf(item.Id) ?? 0;

                var go = HubItemRowFactory.Create(itemListContainer);
                HubItemRowFactory.SetIcon(go, item);
                HubItemRowFactory.SetLabel(go, $"{item.DisplayName} — {item.BaseCost}g");
                HubItemRowFactory.SetLabelColor(go, HubItemRowFactory.RarityColor(item.Rarity));

                string sub = item.Description ?? "";
                if (owned > 0) sub += $"  (owned: {owned})";
                HubItemRowFactory.SetSubLabel(go, sub);

                var btn = go.GetComponent<Button>();
                if (btn != null)
                {
                    btn.interactable = canAfford;
                    var captured = item;
                    btn.onClick.AddListener(() => { Buy(captured); RefreshList(); RefreshGoldDisplay(); });
                }
            }
        }
        else
        {
            if (Inventory == null) return;
            foreach (var entry in Inventory.All())
            {
                var item = entry.Definition;
                var go = HubItemRowFactory.Create(itemListContainer);

                HubItemRowFactory.SetIcon(go, item);
                HubItemRowFactory.SetLabel(go, $"{item.DisplayName} x{entry.Count}");
                HubItemRowFactory.SetLabelColor(go, HubItemRowFactory.RarityColor(item.Rarity));
                HubItemRowFactory.SetSubLabel(go, $"Sell for {item.ComputedSellValue}g each — {item.Description ?? ""}");

                var btn = go.GetComponent<Button>();
                var captured = item;
                if (btn != null) btn.onClick.AddListener(() => { Sell(captured); RefreshList(); RefreshGoldDisplay(); });
            }
        }

        if (detailLabel != null)
            detailLabel.text = mode == ShopMode.Buy ? "Buy Items" : "Sell Items";
    }

    /// <summary>Refreshes gold display.</summary>
    private void RefreshGoldDisplay()
    {
        if (goldLabel != null && Inventory != null)
            goldLabel.text = $"Gold: {Inventory.Gold}";
    }

    /// <summary>Attempts to buy an item.</summary>
    public bool Buy(ItemDefinition item)
    {
        if (item == null || Inventory == null) return false;
        if (Inventory.Gold < item.BaseCost) return false;
        Inventory.Gold -= item.BaseCost;
        Inventory.Add(item);
        return true;
    }

    /// <summary>Attempts to sell an item using ComputedSellValue.</summary>
    public bool Sell(ItemDefinition item)
    {
        if (item == null || Inventory == null) return false;
        if (!Inventory.Contains(item.Id)) return false;
        Inventory.Remove(item.Id, 1);
        Inventory.Gold += item.ComputedSellValue;
        return true;
    }

    /// <summary>Clears all child GameObjects of a container.</summary>
    private void ClearChildren(RectTransform container)
    {
        for (int i = container.childCount - 1; i >= 0; i--)
            Destroy(container.GetChild(i).gameObject);
    }
}

}
