using System.Collections.Generic;
using System.Linq;
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
/// Supports serialization to/from InventorySaveData for persistence.
/// 
/// RELATED FILES:
/// - ItemDefinition.cs: Item data templates
/// - ShopSectionController.cs: Shop transactions
/// - HeroLoadout.cs: Equipped items
/// - Profile.cs: InventorySaveData structure
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
    /// Returns true if item exists with at least required count.
    /// </summary>
    public bool Contains(string itemId, int required = 1)
    {
        if (!entries.TryGetValue(itemId, out var entry)) return false;
        return entry.Count >= required;
    }

    /// <summary>
    /// Returns the count of an item in inventory (0 if absent).
    /// </summary>
    public int CountOf(string itemId)
    {
        return entries.TryGetValue(itemId, out var entry) ? entry.Count : 0;
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

    /// <summary>Enumerates entries filtered by item type.</summary>
    public IEnumerable<Entry> ByType(ItemType type)
    {
        return entries.Values.Where(e => e.Definition.Type == type);
    }

    /// <summary>Enumerates equipment entries filtered by slot. Relic slots return all relic-compatible items.</summary>
    public IEnumerable<Entry> BySlot(EquipmentSlot slot)
    {
        if (EquipmentSlotHelper.IsRelicSlot(slot))
            return entries.Values.Where(e => EquipmentSlotHelper.IsRelicSlot(e.Definition.Slot));
        return entries.Values.Where(e => e.Definition.Slot == slot);
    }

    // ===================== Save / Load =====================

    /// <summary>Exports inventory state to serializable save data.</summary>
    public InventorySaveData ToSaveData()
    {
        var save = new InventorySaveData { Gold = Gold, Items = new List<InventoryEntrySave>() };
        foreach (var kvp in entries)
        {
            save.Items.Add(new InventoryEntrySave(kvp.Key, kvp.Value.Count, kvp.Value.CurrentDurability));
        }
        return save;
    }

    /// <summary>Imports inventory state from save data.</summary>
    public void LoadFromSaveData(InventorySaveData save)
    {
        entries.Clear();
        if (save == null) { Gold = 200; return; }
        Gold = save.Gold;
        if (save.Items == null) return;
        foreach (var e in save.Items)
        {
            var def = ItemLibrary.Get(e.ItemId);
            if (def == null) continue;
            entries[e.ItemId] = new Entry
            {
                Definition = def,
                Count = e.Count,
                CurrentDurability = e.CurrentDurability
            };
        }
    }
}

}
