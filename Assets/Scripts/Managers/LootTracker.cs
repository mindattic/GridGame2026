using System.Collections.Generic;
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
using Scripts.Inventory;
using Scripts.Libraries;
using Scripts.Models;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Managers
{
/// <summary>
/// LOOTTRACKER - Tracks loot drops collected during a battle.
/// 
/// PURPOSE:
/// Static class that accumulates item drops from defeated enemies
/// during combat. Persists across scenes until consumed by
/// PostBattleManager's loot display phase.
/// 
/// LIFECYCLE:
/// 1. StartSession() clears previous drops
/// 2. AddDrop() called when enemies die (from DieRoutine)
/// 3. PostBattleManager reads and displays collected loot
/// 4. CommitToInventory() writes drops to player inventory
/// 5. Clear() resets for next battle
/// 
/// RELATED FILES:
/// - ActorInstance.cs: Calls AddDrop on enemy death
/// - DropTableLibrary.cs: Rolls drop tables
/// - PostBattleManager.cs: Displays loot screen
/// - PlayerInventory.cs: Receives committed loot
/// </summary>
public static class LootTracker
{
    /// <summary>Single collected loot entry.</summary>
    public class LootEntry
    {
        public string ItemId;
        public string DisplayName;
        public int Count;

        public LootEntry() { }
        public LootEntry(string itemId, string displayName, int count)
        {
            ItemId = itemId;
            DisplayName = displayName;
            Count = count;
        }
    }

    private static readonly Dictionary<string, LootEntry> collected = new Dictionary<string, LootEntry>();

    /// <summary>Clears all tracked loot for a new battle session.</summary>
    public static void StartSession()
    {
        collected.Clear();
    }

    /// <summary>Adds a drop result to the tracker, merging counts for duplicate items.</summary>
    public static void AddDrop(string itemId, int count)
    {
        if (string.IsNullOrEmpty(itemId) || count <= 0) return;
        if (collected.TryGetValue(itemId, out var existing))
        {
            existing.Count += count;
        }
        else
        {
            var def = ItemLibrary.Get(itemId);
            string name = def?.DisplayName ?? itemId;
            collected[itemId] = new LootEntry(itemId, name, count);
        }
    }

    /// <summary>Adds all results from a drop table roll.</summary>
    public static void AddDropResults(List<DropResult> results)
    {
        if (results == null) return;
        foreach (var r in results)
        {
            AddDrop(r.ItemId, r.Count);
        }
    }

    /// <summary>True if any loot was collected this session.</summary>
    public static bool HasLoot => collected.Count > 0;

    /// <summary>All collected loot entries (read-only snapshot).</summary>
    public static IReadOnlyCollection<LootEntry> AllLoot => collected.Values;

    /// <summary>
    /// Writes all collected loot to the player's inventory save data.
    /// Call this when the player confirms loot collection.
    /// </summary>
    public static void CommitToInventory()
    {
        var save = ProfileHelper.CurrentProfile?.CurrentSave;
        if (save == null) return;

        // Build a temporary PlayerInventory from save, add loot, write back
        var inv = new PlayerInventory();
        if (save.Inventory != null)
            inv.LoadFromSaveData(save.Inventory);

        foreach (var entry in collected.Values)
        {
            var def = ItemLibrary.Get(entry.ItemId);
            if (def != null)
                inv.Add(def, entry.Count);
        }

        save.Inventory = inv.ToSaveData();
    }

    /// <summary>Clears all tracked loot.</summary>
    public static void Clear()
    {
        collected.Clear();
    }
}

}
