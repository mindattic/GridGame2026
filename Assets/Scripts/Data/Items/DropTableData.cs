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
/// DROPTABLEDATA - Static drop table definitions per enemy type.
/// 
/// PURPOSE:
/// Assigns loot tables to enemy CharacterClasses so players
/// can farm specific ingredients from specific enemies.
/// 
/// RELATED FILES:
/// - DropTableLibrary.cs: Registers these tables
/// - DropTable.cs: Drop table data structure
/// - ItemData_CraftingMaterials.cs: Items that drop
/// </summary>
public static class DropTableData
{
    public static readonly DropTable Slimes = new DropTable
    {
        Enemy = CharacterClass.Slime00,
        Entries =
        {
            new DropEntry("mat_slime_gel", 60f, 1, 3),
            new DropEntry("mat_arcane_dust", 10f, 1, 1),
        }
    };

    public static readonly DropTable Slimes01 = new DropTable
    {
        Enemy = CharacterClass.Slime01,
        Entries =
        {
            new DropEntry("mat_slime_gel", 65f, 1, 4),
            new DropEntry("mat_arcane_dust", 15f, 1, 1),
        }
    };

    public static readonly DropTable Wolves = new DropTable
    {
        Enemy = CharacterClass.Wolf00,
        Entries =
        {
            new DropEntry("mat_wolf_pelt", 50f, 1, 2),
            new DropEntry("mat_leather", 30f, 1, 1),
        }
    };

    public static readonly DropTable Wolves01 = new DropTable
    {
        Enemy = CharacterClass.Wolf01,
        Entries =
        {
            new DropEntry("mat_wolf_pelt", 55f, 1, 3),
            new DropEntry("mat_leather", 35f, 1, 2),
        }
    };

    public static readonly DropTable Undead = new DropTable
    {
        Enemy = CharacterClass.Undead00,
        Entries =
        {
            new DropEntry("mat_undead_bone", 45f, 1, 2),
            new DropEntry("mat_cloth", 20f, 1, 1),
        }
    };

    public static readonly DropTable Undead01 = new DropTable
    {
        Enemy = CharacterClass.Undead01,
        Entries =
        {
            new DropEntry("mat_undead_bone", 50f, 1, 3),
            new DropEntry("mat_cloth", 25f, 1, 2),
            new DropEntry("mat_arcane_dust", 10f, 1, 1),
        }
    };

    public static readonly DropTable Trolls = new DropTable
    {
        Enemy = CharacterClass.MountainTroll,
        Entries =
        {
            new DropEntry("mat_troll_hide", 35f, 1, 1),
            new DropEntry("mat_iron_ore", 25f, 1, 2),
        }
    };

    public static readonly DropTable DemonLordTable = new DropTable
    {
        Enemy = CharacterClass.DemonLord,
        Entries =
        {
            new DropEntry("mat_demon_shard", 40f, 1, 1),
            new DropEntry("mat_arcane_dust", 60f, 2, 4),
            new DropEntry("mat_iron_ore", 50f, 1, 3),
        }
    };

    public static readonly DropTable Frogs = new DropTable
    {
        Enemy = CharacterClass.Frog00,
        Entries =
        {
            new DropEntry("mat_slime_gel", 40f, 1, 2),
            new DropEntry("mat_leather", 20f, 1, 1),
        }
    };

    public static readonly DropTable Goblins = new DropTable
    {
        Enemy = CharacterClass.GoblinThug00,
        Entries =
        {
            new DropEntry("mat_iron_ore", 35f, 1, 2),
            new DropEntry("mat_cloth", 30f, 1, 1),
            new DropEntry("mat_wood_plank", 25f, 1, 2),
        }
    };

    public static readonly DropTable TreeGolems = new DropTable
    {
        Enemy = CharacterClass.TreeGolem00,
        Entries =
        {
            new DropEntry("mat_wood_plank", 55f, 2, 4),
            new DropEntry("mat_leather", 15f, 1, 1),
        }
    };

    public static readonly DropTable Cyclops = new DropTable
    {
        Enemy = CharacterClass.Cyclops00,
        Entries =
        {
            new DropEntry("mat_iron_ore", 40f, 2, 3),
            new DropEntry("mat_troll_hide", 20f, 1, 1),
            new DropEntry("mat_demon_shard", 5f, 1, 1),
        }
    };

    public static readonly DropTable Scorpions = new DropTable
    {
        Enemy = CharacterClass.Scorpion,
        Entries =
        {
            new DropEntry("mat_leather", 45f, 1, 2),
            new DropEntry("mat_arcane_dust", 15f, 1, 1),
        }
    };

    public static readonly DropTable Nagas = new DropTable
    {
        Enemy = CharacterClass.Naga00,
        Entries =
        {
            new DropEntry("mat_arcane_dust", 40f, 1, 2),
            new DropEntry("mat_cloth", 35f, 1, 2),
            new DropEntry("mat_demon_shard", 8f, 1, 1),
        }
    };

    public static readonly DropTable Ghosts = new DropTable
    {
        Enemy = CharacterClass.Ghost,
        Entries =
        {
            new DropEntry("mat_arcane_dust", 50f, 1, 3),
            new DropEntry("mat_undead_bone", 30f, 1, 1),
        }
    };

    public static readonly DropTable Werewolves = new DropTable
    {
        Enemy = CharacterClass.Werewolf00,
        Entries =
        {
            new DropEntry("mat_wolf_pelt", 60f, 2, 3),
            new DropEntry("mat_troll_hide", 15f, 1, 1),
            new DropEntry("mat_leather", 40f, 1, 2),
        }
    };
}

}
