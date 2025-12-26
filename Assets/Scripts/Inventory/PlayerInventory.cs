using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// PlayerInventory stores item ownership, stack counts, and currency for hub operations.
/// Supports equipment durability and lookup by Id.
/// </summary>
public class PlayerInventory
{
    /// <summary>
    /// Single inventory entry with stack count and durability when applicable.
    /// </summary>
    public class Entry
    {
    public ItemDefinition Definition;
  public int Count;
        public int CurrentDurability; // for equipment pieces
    }

    private Dictionary<string, Entry> entries = new Dictionary<string, Entry>();

    public int Gold = 200; // starting currency placeholder

/// <summary>
    /// Adds one instance of an item (or increments stack) if within max stack limit.
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
    /// Removes a given amount of an item if present.
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
