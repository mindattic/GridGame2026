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
/// RARITY TIERS (WoW-style palette — drives loot-drop tint):
/// - Junk:      filler drops, auto-vendor candidates (gray)
/// - Common:    vendor supplies + common farm mats (white)
/// - Uncommon:  magical components, mid-tier mats (green)
/// - Rare:      elite-mob drops (blue)
/// - Epic:      boss / rare-spawn drops (purple)
/// - Legendary: one-per-run capstone mats (orange)
///
/// RELATED FILES:
/// - ItemLibrary.cs: Registers these items
/// - DropTableData.cs: Enemy drop assignments
/// - RecipeData.cs: Recipes that consume these
/// </summary>
public static class ItemData_CraftingMaterials
{
    // ============== JUNK (filler drops — vendor trash) ==============

    public static readonly ItemDefinition BrokenBlade = new ItemDefinition
    {
        Id = "mat_broken_blade",
        DisplayName = "Broken Blade",
        Description = "A shattered weapon fragment. Worthless alone, but a blacksmith can melt it down.",
        Type = ItemType.CraftingMaterial,
        Rarity = ItemRarity.Junk,
        BaseCost = 3,
        MaxStack = 99,
    };

    public static readonly ItemDefinition CrackedFang = new ItemDefinition
    {
        Id = "mat_cracked_fang",
        DisplayName = "Cracked Fang",
        Description = "A broken tooth or claw. Alchemists grind it into low-grade powders.",
        Type = ItemType.CraftingMaterial,
        Rarity = ItemRarity.Junk,
        BaseCost = 2,
        MaxStack = 99,
    };

    public static readonly ItemDefinition TatteredCloth = new ItemDefinition
    {
        Id = "mat_tattered_cloth",
        DisplayName = "Tattered Cloth",
        Description = "A filthy scrap of fabric. Can be bleached into usable cloth in bulk.",
        Type = ItemType.CraftingMaterial,
        Rarity = ItemRarity.Junk,
        BaseCost = 2,
        MaxStack = 99,
    };

    // ============== COMMON (vendor purchasable) ==============

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

    public static readonly ItemDefinition GoblinEar = new ItemDefinition
    {
        Id = "mat_goblin_ear",
        DisplayName = "Goblin Ear",
        Description = "Proof of a goblin slain. Turned in for bounties, or mashed into poultice.",
        Type = ItemType.CraftingMaterial,
        Rarity = ItemRarity.Common,
        BaseCost = 14,
        MaxStack = 99,
    };

    // ============== UNCOMMON ==============

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

    public static readonly ItemDefinition EnchantedFeather = new ItemDefinition
    {
        Id = "mat_enchanted_feather",
        DisplayName = "Enchanted Feather",
        Description = "A feather that refuses to fall. Used to lighten garments and enchant amulets.",
        Type = ItemType.CraftingMaterial,
        Rarity = ItemRarity.Uncommon,
        BaseCost = 35,
        MaxStack = 99,
    };

    // ============== RARE ==============

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

    public static readonly ItemDefinition NagaScale = new ItemDefinition
    {
        Id = "mat_naga_scale",
        DisplayName = "Naga Scale",
        Description = "An iridescent scale that sheds water. Crafts into water-resistant armor.",
        Type = ItemType.CraftingMaterial,
        Rarity = ItemRarity.Rare,
        BaseCost = 55,
        MaxStack = 99,
    };

    public static readonly ItemDefinition WerewolfFang = new ItemDefinition
    {
        Id = "mat_werewolf_fang",
        DisplayName = "Werewolf Fang",
        Description = "A pristine fang pulled from an alpha. Sought for predator relics.",
        Type = ItemType.CraftingMaterial,
        Rarity = ItemRarity.Rare,
        BaseCost = 60,
        MaxStack = 50,
    };

    // ============== EPIC ==============

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

    public static readonly ItemDefinition GhostlyEctoplasm = new ItemDefinition
    {
        Id = "mat_ghost_ectoplasm",
        DisplayName = "Ghostly Ectoplasm",
        Description = "A shimmering residue of an unrested spirit. Anchors revival magic.",
        Type = ItemType.CraftingMaterial,
        Rarity = ItemRarity.Epic,
        BaseCost = 100,
        MaxStack = 50,
    };

    // ============== LEGENDARY ==============

    public static readonly ItemDefinition DragonScale = new ItemDefinition
    {
        Id = "mat_dragon_scale",
        DisplayName = "Dragon Scale",
        Description = "A scale from an ancient wyrm. The stuff of legendary armor.",
        Type = ItemType.CraftingMaterial,
        Rarity = ItemRarity.Legendary,
        BaseCost = 250,
        MaxStack = 20,
    };
}

}
