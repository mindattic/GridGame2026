using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// BlacksmithSectionController manages equipment buy, sell, craft, and repair.
/// Relies on PlayerInventory and item definitions with stat modifiers.
/// </summary>
public class BlacksmithSectionController : MonoBehaviour
{
    private HubManager hub;

    public PlayerInventory playerInventory = new PlayerInventory();

    public enum BlacksmithMode { Buy, Sell, Craft, Repair }
    public BlacksmithMode mode = BlacksmithMode.Buy;

    private List<ItemDefinition> equipmentCatalog = new List<ItemDefinition>();

    /// <summary>
/// Initializes and loads equipment catalog.
    /// </summary>
    public void Initialize(HubManager hubManager)
    {
  hub = hubManager;
        LoadCatalog();
    }

    /// <summary>
    /// Called when section is shown.
    /// </summary>
    public void OnActivated()
    {
   // TODO: Refresh equipment list UI based on mode.
    }

    /// <summary>
    /// Loads baseline equipment offerings.
    /// </summary>
    private void LoadCatalog()
    {
    equipmentCatalog.Clear();
        equipmentCatalog.Add(ItemData_Equipment.RustySword);
  equipmentCatalog.Add(ItemData_Equipment.LeatherArmor);
    }

    /// <summary>
    /// Buys equipment and adds to inventory.
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
    /// Sells equipment for half cost.
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
    /// Crafts equipment via stub recipe.
    /// </summary>
    public bool Craft(ItemDefinition result, Dictionary<string, int> ingredients, int craftCost)
    {
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

    /// <summary>
    /// Repairs durability of equipment at flat cost. Durability fields are stored per inventory entry.
    /// </summary>
public bool Repair(string itemId, int cost)
    {
    if (playerInventory.Gold < cost) return false;
  var entry = playerInventory.GetEntry(itemId);
   if (entry == null) return false;
    if (entry.Definition.Durability <= 0) return false; // not repairable or no durability tracking
    entry.CurrentDurability = entry.Definition.Durability; // restore to max
        playerInventory.Gold -= cost;
    return true;
    }
}
