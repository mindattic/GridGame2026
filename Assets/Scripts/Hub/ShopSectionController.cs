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
/// NAVIGATION:
/// ```
/// ┌─────────────────────────────┐
/// │  General Store              │
/// │  Gold: 500                  │
/// ├─────────────────────────────┤
/// │  ► Buy                      │  ← Menu (default landing)
/// │  ► Sell                     │
/// └─────────────────────────────┘
///
/// Click "Buy" →
/// ┌─────────────────────────────┐
/// │  Buy Items                  │
/// │  Gold: 500                  │
/// ├─────────────────────────────┤
/// │  ← Back                    │
/// │  Health Potion — 50g       │
/// │  Mana Potion — 30g         │
/// │  Rusty Sword — 100g        │
/// └─────────────────────────────┘
/// ```
///
/// SHOP MODES:
/// - Menu: Initial landing with Buy/Sell navigation rows
/// - Buy: Purchase items from catalog (consumables + materials + equipment)
/// - Sell: Sell inventory items for gold (always 50% of buy price)
///
/// RELATED FILES:
/// - HubManager.cs: Hub scene controller, owns SharedInventory
/// - PlayerInventory.cs: Item storage
/// - ItemDefinition.cs: Item data (ComputedSellValue = BaseCost/2)
/// - ItemLibrary.cs: Item catalog
/// - HubItemRowFactory.cs: Row UI creation
/// </summary>
public class ShopSectionController : MonoBehaviour
{
    private HubManager hub;

    private enum ShopMode { Menu, Buy, Sell }
    private ShopMode mode = ShopMode.Menu;

    private List<ItemDefinition> buyCatalog = new List<ItemDefinition>();

    // Runtime UI references
    private RectTransform itemListContainer;
    private TextMeshProUGUI goldLabel;
    private TextMeshProUGUI detailLabel;

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

    /// <summary>Called when activated — always resets to the main menu.</summary>
    public void OnActivated()
    {
        mode = ShopMode.Menu;
        RefreshList();
        RefreshGoldDisplay();
    }

    /// <summary>Resolves UI references from scene hierarchy, creating missing children at runtime.</summary>
    private void ResolveUI()
    {
        var rt = GetComponent<RectTransform>();
        if (rt == null) return;

        // Center — scrollable list used for menu, buy list, and sell list
        itemListContainer = EnsureContainer(rt, GameObjectHelper.Hub.ItemList);
        // Top-right — current gold display
        goldLabel = EnsureLabel(rt, GameObjectHelper.Hub.GoldLabel);
        // Top-center — heading text (changes per mode)
        detailLabel = EnsureLabel(rt, GameObjectHelper.Hub.DetailLabel);
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

    /// <summary>Changes mode and refreshes the list.</summary>
    private void SetMode(ShopMode newMode)
    {
        mode = newMode;
        RefreshList();
        RefreshGoldDisplay();
    }

    // ===================== List Rendering =====================

    /// <summary>Refreshes the item list UI for current mode.</summary>
    private void RefreshList()
    {
        if (itemListContainer == null) return;
        ClearChildren(itemListContainer);

        switch (mode)
        {
            case ShopMode.Menu: ShowMenu(); break;
            case ShopMode.Buy:  ShowBuyList(); break;
            case ShopMode.Sell: ShowSellList(); break;
        }
    }

    /// <summary>Shows the main shop menu (Buy / Sell navigation).</summary>
    private void ShowMenu()
    {
        if (detailLabel != null) detailLabel.text = "General Store";

        // Buy row
        var buyRow = HubItemRowFactory.Create(itemListContainer);
        HubItemRowFactory.SetLabel(buyRow, "► Buy");
        HubItemRowFactory.SetSubLabel(buyRow, "Browse items for sale");
        var buyBtn = buyRow.GetComponent<Button>();
        if (buyBtn != null) buyBtn.onClick.AddListener(() => SetMode(ShopMode.Buy));

        // Sell row
        var sellRow = HubItemRowFactory.Create(itemListContainer);
        HubItemRowFactory.SetLabel(sellRow, "► Sell");
        HubItemRowFactory.SetSubLabel(sellRow, "Sell items from your inventory");
        var sellBtn = sellRow.GetComponent<Button>();
        if (sellBtn != null) sellBtn.onClick.AddListener(() => SetMode(ShopMode.Sell));
    }

    /// <summary>Shows the buy catalog list.</summary>
    private void ShowBuyList()
    {
        if (detailLabel != null) detailLabel.text = "Buy Items";

        // Back row
        var backRow = HubItemRowFactory.Create(itemListContainer);
        HubItemRowFactory.SetLabel(backRow, "← Back");
        var backBtn = backRow.GetComponent<Button>();
        if (backBtn != null) backBtn.onClick.AddListener(() => SetMode(ShopMode.Menu));

        // Catalog items
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

    /// <summary>Shows the sell list from player inventory.</summary>
    private void ShowSellList()
    {
        if (detailLabel != null) detailLabel.text = "Sell Items";

        // Back row
        var backRow = HubItemRowFactory.Create(itemListContainer);
        HubItemRowFactory.SetLabel(backRow, "← Back");
        var backBtn = backRow.GetComponent<Button>();
        if (backBtn != null) backBtn.onClick.AddListener(() => SetMode(ShopMode.Menu));

        // Inventory items
        if (Inventory == null) return;
        var items = Inventory.All();
        if (!items.Any())
        {
            var emptyRow = HubItemRowFactory.Create(itemListContainer);
            HubItemRowFactory.SetLabel(emptyRow, "No items to sell");
            var emptyBtn = emptyRow.GetComponent<Button>();
            if (emptyBtn != null) emptyBtn.interactable = false;
            return;
        }

        foreach (var entry in items)
        {
            var item = entry.Definition;
            int sellPrice = item.ComputedSellValue;

            var go = HubItemRowFactory.Create(itemListContainer);
            HubItemRowFactory.SetIcon(go, item);
            HubItemRowFactory.SetLabel(go, $"{item.DisplayName} x{entry.Count}");
            HubItemRowFactory.SetLabelColor(go, HubItemRowFactory.RarityColor(item.Rarity));
            HubItemRowFactory.SetSubLabel(go, $"Sell for {sellPrice}g each");

            var btn = go.GetComponent<Button>();
            if (btn != null)
            {
                var captured = item;
                btn.onClick.AddListener(() => { Sell(captured); RefreshList(); RefreshGoldDisplay(); });
            }
        }
    }

    // ===================== Transactions =====================

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

    /// <summary>Attempts to sell an item (sell price is always 50% of buy price).</summary>
    public bool Sell(ItemDefinition item)
    {
        if (item == null || Inventory == null) return false;
        if (!Inventory.Contains(item.Id)) return false;
        Inventory.Remove(item.Id, 1);
        Inventory.Gold += item.ComputedSellValue;
        return true;
    }

    // ===================== Helpers =====================

    /// <summary>Clears all child GameObjects of a container.</summary>
    private void ClearChildren(RectTransform container)
    {
        for (int i = container.childCount - 1; i >= 0; i--)
            Destroy(container.GetChild(i).gameObject);
    }

    // ── Runtime UI scaffolding ──

    /// <summary>Ensures a named scrollable container child exists on the parent.</summary>
    private static RectTransform EnsureContainer(RectTransform parent, string childName)
    {
        var found = parent.Find(childName);
        if (found != null) return found.GetComponent<RectTransform>();

        var go = new GameObject(childName);
        go.layer = LayerMask.NameToLayer("UI");
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var vlg = go.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.spacing = 4f;

        var csf = go.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        return rt;
    }

    /// <summary>Ensures a named TMP label child exists on the parent.</summary>
    private static TextMeshProUGUI EnsureLabel(RectTransform parent, string childName)
    {
        var found = parent.Find(childName);
        if (found != null) return found.GetComponent<TextMeshProUGUI>();

        var go = new GameObject(childName);
        go.layer = LayerMask.NameToLayer("UI");
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        go.AddComponent<CanvasRenderer>();
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = "";
        tmp.fontSize = 24;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.TopLeft;
        tmp.enableWordWrapping = true;
        tmp.raycastTarget = false;
        return tmp;
    }
}

}
