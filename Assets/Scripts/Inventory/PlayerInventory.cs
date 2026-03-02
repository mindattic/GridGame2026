using System.Collections.Generic;
using UnityEngine;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Factories;
using Scripts.Helpers;
using Scripts.Hub;
using Scripts.Instances;
using Scripts.Instances.Actor;
using Scripts.Instances.Board;
using Scripts.Instances.SynergyLine;
using Scripts.Libraries;
using Scripts.Managers;
using Scripts.Models;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Inventory
{
/// <summary>
/// PLAYERINVENTORY - Player item and currency storage.
/// 
/// PURPOSE:
/// Stores item ownership, stack counts, durability, and currency
/// for hub shop operations and equipment management.
/// 
/// FEATURES:
/// - Add/Remove items with stack tracking
/// - Equipment durability tracking
/// - Currency (Gold) management
/// - Lookup by item ID
/// 
/// ENTRY STRUCTURE:
/// - Definition: Item template
/// - Count: Stack quantity
/// - CurrentDurability: Remaining durability
/// 
/// RELATED FILES:
/// - ItemDefinition.cs: Item data templates
/// - ShopSectionController.cs: Shop transactions
/// - HeroLoadout.cs: Equipped items
/// </summary>
public class PlayerInventory
{
    /// <summary>Single inventory entry with stack count and durability.</summary>
    public class Entry
    {
        public ItemDefinition Definition;
        public int Count;
        public int CurrentDurability;
    }

    private Dictionary<string, Entry> entries = new Dictionary<string, Entry>();

    public int Gold = 200;

    /// <summary>
    /// Adds item(s) to inventory if within max stack limit.
    /// </summary>
    public bool Add(ItemDefinition item, int amount = 1)
    {
        if (item == null || amount <= 0) return false;
        if (!entries.TryGetValue(item.Id, out var entry))
        {
            entry = new Entry { Definition = item, Count = 0, CurrentDurability = item.Durability };
            entries[item.Id] = entry;
        }
        if (entry.Count + amount > item.MaxStack) return false;
        entry.Count += amount;
        return true;
    }

    /// <summary>
    /// Removes item(s) from inventory if present.
    /// </summary>
    public bool Remove(string itemId, int amount = 1)
    {
        if (!entries.TryGetValue(itemId, out var entry)) return false;
        if (amount <= 0 || entry.Count < amount) return false;
        entry.Count -= amount;
        if (entry.Count <= 0) entries.Remove(itemId);
        return true;
    }

    /// <summary>
    /// Returns true if item exists with at least one stack.
    /// </summary>
    public bool Contains(string itemId, int required = 1)
    {
   if (!entries.TryGetValue(itemId, out var entry)) return false;
   return entry.Count >= required;
    }

    /// <summary>
    /// Retrieves an inventory entry or null if missing.
    /// </summary>
    public Entry GetEntry(string itemId)
    {
   entries.TryGetValue(itemId, out var entry);
        return entry;
    }

    /// <summary>
    /// Enumerates all inventory entries.
    /// </summary>
    public IEnumerable<Entry> All()
    {
   return entries.Values;
    }
}

}
