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
/// DROPTABLELIBRARY - Central registry for enemy drop tables.
/// 
/// PURPOSE:
/// Maps enemy CharacterClass to drop tables so loot can be
/// rolled when enemies are defeated in combat.
/// 
/// USAGE:
/// ```csharp
/// var table = DropTableLibrary.Get(CharacterClass.Slime00);
/// if (table != null) var loot = table.Roll();
/// ```
/// 
/// RELATED FILES:
/// - DropTable.cs: Drop table data structure
/// - DropTableData.cs: Static definitions
/// - PlayerInventory.cs: Receives drops
/// </summary>
public static class DropTableLibrary
{
    private static Dictionary<CharacterClass, DropTable> tables = new Dictionary<CharacterClass, DropTable>();
    private static bool initialized;

    /// <summary>Ensures library is populated once.</summary>
    private static void Ensure()
    {
        if (initialized) return;
        initialized = true;
        Register(DropTableData.Slimes);
        Register(DropTableData.Slimes01);
        Register(DropTableData.Wolves);
        Register(DropTableData.Wolves01);
        Register(DropTableData.Undead);
        Register(DropTableData.Undead01);
        Register(DropTableData.Trolls);
        Register(DropTableData.DemonLordTable);
        Register(DropTableData.Frogs);
        Register(DropTableData.Goblins);
        Register(DropTableData.TreeGolems);
        Register(DropTableData.Cyclops);
        Register(DropTableData.Scorpions);
        Register(DropTableData.Nagas);
        Register(DropTableData.Ghosts);
        Register(DropTableData.Werewolves);
    }

    /// <summary>Registers a drop table.</summary>
    private static void Register(DropTable table)
    {
        if (table == null) return;
        if (!tables.ContainsKey(table.Enemy)) tables.Add(table.Enemy, table);
    }

    /// <summary>Gets a drop table by enemy class or null.</summary>
    public static DropTable Get(CharacterClass enemy)
    {
        Ensure();
        tables.TryGetValue(enemy, out var t);
        return t;
    }

    /// <summary>Rolls drops for an enemy and returns results (empty list if no table).</summary>
    public static List<DropResult> RollDrops(CharacterClass enemy)
    {
        Ensure();
        var table = Get(enemy);
        return table != null ? table.Roll() : new List<DropResult>();
    }
}

}
