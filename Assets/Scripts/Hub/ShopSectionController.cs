using System.Collections.Generic;
using UnityEngine;
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
/// Manages Buy, Sell, and Craft operations for items
/// in the Hub shop screen.
/// 
/// SHOP MODES:
/// - Buy: Purchase items from catalog
/// - Sell: Sell inventory items for gold
/// - Craft: Combine materials into items
/// 
/// TRANSACTIONS:
/// Uses PlayerInventory for currency and item ownership.
/// All purchases deduct gold, sales add gold.
/// 
/// RELATED FILES:
/// - HubManager.cs: Hub scene controller
/// - PlayerInventory.cs: Item storage
/// - ItemDefinition.cs: Item data
/// - ItemLibrary.cs: Item catalog
/// </summary>
public class ShopSectionController : MonoBehaviour
{
    private HubManager hub;

    public PlayerInventory playerInventory = new PlayerInventory();

    public enum ShopMode { Buy, Sell, Craft }
    public ShopMode mode = ShopMode.Buy;

    private List<ItemDefinition> buyCatalog = new List<ItemDefinition>();

    /// <summary>Initializes with data references.</summary>
    public void Initialize(HubManager hubManager)
    {
        hub = hubManager;
        LoadCatalog();
    }

    /// <summary>Called when activated.</summary>
    public void OnActivated()
    {
        // TODO: Rebuild item list UI
    }

    /// <summary>Loads shop catalog from item libraries.</summary>
    private void LoadCatalog()
    {
        buyCatalog.Clear();
        buyCatalog.Add(ItemData_Healing.BasicHealingPotion);
        buyCatalog.Add(ItemData_Healing.ManaPotion);
        buyCatalog.Add(ItemData_Equipment.RustySword);
        buyCatalog.Add(ItemData_Equipment.LeatherArmor);
    }

    /// <summary>
    /// Attempts to buy an item, reducing currency and adding to player inventory.
    /// </summary>
    public bool Buy(ItemDefinition item)
    {
    if (item == null) return false;
    if (playerInventory.Gold < item.BaseCost) return false;
   playerInventory.Gold -= item.BaseCost;
   playerInventory.Add(item);
        return true;
    }

    /// <summary>
/// Attempts to sell an item. Grants half cost gold by default.
    /// </summary>
    public bool Sell(ItemDefinition item)
    {
   if (item == null) return false;
    if (!playerInventory.Contains(item.Id)) return false;
   playerInventory.Remove(item.Id, 1);
   playerInventory.Gold += Mathf.Max(1, item.BaseCost / 2);
  return true;
    }

    /// <summary>
    /// Crafting stub. Consumes ingredients then creates the result.
    /// </summary>
    public bool Craft(ItemDefinition result, Dictionary<string, int> ingredients, int craftCost)
    {
    // TODO: Replace with recipe system.
    if (result == null) return false;
    if (playerInventory.Gold < craftCost) return false;
    if (ingredients != null)
   {
   foreach (var kvp in ingredients)
   {
 if (!playerInventory.Contains(kvp.Key, kvp.Value)) return false;
  }
  foreach (var kvp in ingredients)
  {
 playerInventory.Remove(kvp.Key, kvp.Value);
  }
  }
        playerInventory.Gold -= craftCost;
   playerInventory.Add(result);
    return true;
    }
}

}
