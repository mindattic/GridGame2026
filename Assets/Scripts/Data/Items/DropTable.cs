using System.Collections.Generic;
using Scripts.Canvas;
using Scripts.Data.Actor;
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
using Scripts.Managers;
using Scripts.Models;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Data.Items
{
/// <summary>
/// DROPTABLE - Defines loot drops for an enemy type.
/// 
/// PURPOSE:
/// Maps an enemy CharacterClass to a weighted list of
/// items that can drop on defeat. Used for farming
/// specific crafting ingredients.
/// 
/// USAGE:
/// ```csharp
/// var table = DropTableLibrary.Get(CharacterClass.Slime00);
/// var loot = table.Roll();
/// ```
/// 
/// RELATED FILES:
/// - DropTableLibrary.cs: Registry
/// - DropTableData.cs: Static definitions
/// - PlayerInventory.cs: Receives drops
/// </summary>
[System.Serializable]
public class DropTable
{
    public CharacterClass Enemy;
    public List<DropEntry> Entries = new List<DropEntry>();

    /// <summary>Rolls all entries and returns items that dropped.</summary>
    public List<DropResult> Roll()
    {
        var results = new List<DropResult>();
        foreach (var entry in Entries)
        {
            float roll = UnityEngine.Random.Range(0f, 100f);
            if (roll <= entry.Weight)
            {
                results.Add(new DropResult
                {
                    ItemId = entry.ItemId,
                    Count = UnityEngine.Random.Range(entry.MinCount, entry.MaxCount + 1)
                });
            }
        }
        return results;
    }
}

/// <summary>Single loot entry with weight and count range.</summary>
[System.Serializable]
public class DropEntry
{
    public string ItemId;
    public float Weight; // 0-100 percent chance
    public int MinCount = 1;
    public int MaxCount = 1;

    public DropEntry() { }
    public DropEntry(string itemId, float weight, int min = 1, int max = 1)
    {
        ItemId = itemId;
        Weight = weight;
        MinCount = min;
        MaxCount = max;
    }
}

/// <summary>Result of a drop roll.</summary>
public class DropResult
{
    public string ItemId;
    public int Count;
}

}
