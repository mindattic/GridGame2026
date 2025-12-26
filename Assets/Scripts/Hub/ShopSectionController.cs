using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ShopSectionController manages Buy, Sell, and Craft operations for general items.
/// Uses PlayerInventory for currency and item ownership. Crafting uses simple recipe stubs.
/// </summary>
public class ShopSectionController : MonoBehaviour
{
    private HubManager hub;

    public PlayerInventory playerInventory = new PlayerInventory();

    public enum ShopMode { Buy, Sell, Craft }
    public ShopMode mode = ShopMode.Buy;

    // Cached catalog (could be filtered by rarity or unlocked status later).
    private List<ItemDefinition> buyCatalog = new List<ItemDefinition>();

    /// <summary>
    /// Initializes with data references and loads basic catalog.
    /// </summary>
    public void Initialize(HubManager hubManager)
    {
    hub = hubManager;
  LoadCatalog();
    }

    /// <summary>
/// Called when activated. Refresh UI based on current mode.
    /// </summary>
    public void OnActivated()
    {
   // TODO: Rebuild item list UI for current mode.
    }

    /// <summary>
    /// Loads baseline shop inventory from static item data libraries.
    /// </summary>
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
