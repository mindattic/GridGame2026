using System.Collections.Generic;

/// <summary>
/// ITEMLIBRARY - Central registry for all item definitions.
/// 
/// PURPOSE:
/// Provides lookup and enumeration of item definitions
/// populated from static data classes.
/// 
/// USAGE:
/// ```csharp
/// var potion = ItemLibrary.Get("healing_potion_basic");
/// foreach (var item in ItemLibrary.All()) { ... }
/// ```
/// 
/// RELATED FILES:
/// - ItemDefinition.cs: Item data structure
/// - ItemData_Healing.cs: Consumable definitions
/// - ItemData_Equipment.cs: Equipment definitions
/// - PlayerInventory.cs: Item ownership
/// </summary>
public static class ItemLibrary
{
    private static Dictionary<string, ItemDefinition> items = new Dictionary<string, ItemDefinition>();
    private static bool initialized;

    /// <summary>Ensures library is populated once.</summary>
    private static void Ensure()
    {
        if (initialized) return;
        initialized = true;
        Register(ItemData_Healing.BasicHealingPotion);
        Register(ItemData_Healing.ManaPotion);
        Register(ItemData_Equipment.RustySword);
        Register(ItemData_Equipment.LeatherArmor);
    }

    /// <summary>Registers an item definition.</summary>
    private static void Register(ItemDefinition def)
    {
        if (def == null || string.IsNullOrEmpty(def.Id)) return;
        if (!items.ContainsKey(def.Id)) items.Add(def.Id, def);
    }

    /// <summary>Gets an item by Id or null if not found.</summary>
    public static ItemDefinition Get(string id)
    {
        Ensure();
        if (string.IsNullOrEmpty(id)) return null;
        items.TryGetValue(id, out var def);
        return def;
    }

    /// <summary>Enumerates all item definitions.</summary>
    public static IEnumerable<ItemDefinition> All()
    {
        Ensure();
        return items.Values;
    }
}
