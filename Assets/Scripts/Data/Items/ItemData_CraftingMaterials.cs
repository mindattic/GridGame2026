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
/// ITEMDATA_CRAFTINGMATERIALS - Crafting ingredient definitions.
/// 
/// PURPOSE:
/// Static definitions for materials obtained from enemy drops
/// or purchased from vendors. Used as recipe ingredients.
/// 
/// CATEGORIES:
/// - Common supplies: Bought from vendors
/// - Monster drops: Farmed from specific enemies
/// 
/// RELATED FILES:
/// - ItemLibrary.cs: Registers these items
/// - DropTableData.cs: Enemy drop assignments
/// - RecipeData.cs: Recipes that consume these
/// </summary>
public static class ItemData_CraftingMaterials
{
    // === COMMON (vendor purchasable) ===

    public static readonly ItemDefinition IronOre = new ItemDefinition
    {
        Id = "mat_iron_ore",
        DisplayName = "Iron Ore",
        Description = "A chunk of raw iron.",
        Type = ItemType.CraftingMaterial,
        Rarity = ItemRarity.Common,
        BaseCost = 10,
        MaxStack = 99,
    };

    public static readonly ItemDefinition Leather = new ItemDefinition
    {
        Id = "mat_leather",
        DisplayName = "Leather",
        Description = "Tanned animal hide.",
        Type = ItemType.CraftingMaterial,
        Rarity = ItemRarity.Common,
        BaseCost = 8,
        MaxStack = 99,
    };

    public static readonly ItemDefinition Cloth = new ItemDefinition
    {
        Id = "mat_cloth",
        DisplayName = "Cloth",
        Description = "A bolt of simple fabric.",
        Type = ItemType.CraftingMaterial,
        Rarity = ItemRarity.Common,
        BaseCost = 6,
        MaxStack = 99,
    };

    public static readonly ItemDefinition WoodPlank = new ItemDefinition
    {
        Id = "mat_wood_plank",
        DisplayName = "Wood Plank",
        Description = "A smooth piece of lumber.",
        Type = ItemType.CraftingMaterial,
        Rarity = ItemRarity.Common,
        BaseCost = 5,
        MaxStack = 99,
    };

    public static readonly ItemDefinition ArcaneDust = new ItemDefinition
    {
        Id = "mat_arcane_dust",
        DisplayName = "Arcane Dust",
        Description = "Shimmering dust imbued with magical energy.",
        Type = ItemType.CraftingMaterial,
        Rarity = ItemRarity.Uncommon,
        BaseCost = 25,
        MaxStack = 99,
    };

    // === MONSTER DROPS (farm only) ===

    public static readonly ItemDefinition SlimeGel = new ItemDefinition
    {
        Id = "mat_slime_gel",
        DisplayName = "Slime Gel",
        Description = "Viscous gel harvested from slimes.",
        Type = ItemType.CraftingMaterial,
        Rarity = ItemRarity.Common,
        BaseCost = 12,
        MaxStack = 99,
    };

    public static readonly ItemDefinition WolfPelt = new ItemDefinition
    {
        Id = "mat_wolf_pelt",
        DisplayName = "Wolf Pelt",
        Description = "A thick wolf hide, prized by leatherworkers.",
        Type = ItemType.CraftingMaterial,
        Rarity = ItemRarity.Common,
        BaseCost = 15,
        MaxStack = 99,
    };

    public static readonly ItemDefinition UndeadBone = new ItemDefinition
    {
        Id = "mat_undead_bone",
        DisplayName = "Undead Bone",
        Description = "A bone pulsing with residual dark energy.",
        Type = ItemType.CraftingMaterial,
        Rarity = ItemRarity.Uncommon,
        BaseCost = 20,
        MaxStack = 99,
    };

    public static readonly ItemDefinition TrollHide = new ItemDefinition
    {
        Id = "mat_troll_hide",
        DisplayName = "Troll Hide",
        Description = "Thick hide that regenerates slightly even after tanning.",
        Type = ItemType.CraftingMaterial,
        Rarity = ItemRarity.Rare,
        BaseCost = 40,
        MaxStack = 99,
    };

    public static readonly ItemDefinition DemonShard = new ItemDefinition
    {
        Id = "mat_demon_shard",
        DisplayName = "Demon Shard",
        Description = "A crystallized fragment of demonic essence.",
        Type = ItemType.CraftingMaterial,
        Rarity = ItemRarity.Epic,
        BaseCost = 80,
        MaxStack = 50,
    };
}

}
