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
/// RECIPEDATA - Static crafting recipe definitions.
/// 
/// PURPOSE:
/// Defines all crafting recipes linking ingredients to
/// finished items. Used at the Blacksmith and Shop.
/// 
/// RELATED FILES:
/// - RecipeLibrary.cs: Registers these recipes
/// - CraftingRecipe.cs: Recipe data structure
/// - BlacksmithSectionController.cs: Crafting execution
/// </summary>
public static class RecipeData
{
    public static readonly CraftingRecipe IronSwordRecipe = new CraftingRecipe
    {
        Id = "recipe_sword_iron",
        DisplayName = "Forge Iron Sword",
        ResultItemId = "eq_sword_iron",
        ResultCount = 1,
        GoldCost = 30,
        Ingredients =
        {
            new RecipeIngredient("mat_iron_ore", 3),
            new RecipeIngredient("mat_wood_plank", 1),
        }
    };

    public static readonly CraftingRecipe SteelSwordRecipe = new CraftingRecipe
    {
        Id = "recipe_sword_steel",
        DisplayName = "Forge Steel Sword",
        ResultItemId = "eq_sword_steel",
        ResultCount = 1,
        GoldCost = 80,
        Ingredients =
        {
            new RecipeIngredient("mat_iron_ore", 6),
            new RecipeIngredient("mat_arcane_dust", 1),
        }
    };

    public static readonly CraftingRecipe ChainMailRecipe = new CraftingRecipe
    {
        Id = "recipe_armor_chain",
        DisplayName = "Forge Chain Mail",
        ResultItemId = "eq_armor_chain",
        ResultCount = 1,
        GoldCost = 40,
        Ingredients =
        {
            new RecipeIngredient("mat_iron_ore", 4),
            new RecipeIngredient("mat_leather", 2),
        }
    };

    public static readonly CraftingRecipe LeatherBootsRecipe = new CraftingRecipe
    {
        Id = "recipe_boots_leather",
        DisplayName = "Craft Leather Boots",
        ResultItemId = "eq_boots_leather",
        ResultCount = 1,
        GoldCost = 15,
        Ingredients =
        {
            new RecipeIngredient("mat_leather", 3),
        }
    };

    public static readonly CraftingRecipe BoneAmuletRecipe = new CraftingRecipe
    {
        Id = "recipe_amulet_bone",
        DisplayName = "Carve Bone Amulet",
        ResultItemId = "eq_amulet_bone",
        ResultCount = 1,
        GoldCost = 20,
        Ingredients =
        {
            new RecipeIngredient("mat_undead_bone", 3),
        }
    };

    public static readonly CraftingRecipe MysticStaffRecipe = new CraftingRecipe
    {
        Id = "recipe_staff_mystic",
        DisplayName = "Enchant Mystic Staff",
        ResultItemId = "eq_staff_mystic",
        ResultCount = 1,
        GoldCost = 60,
        Ingredients =
        {
            new RecipeIngredient("mat_wood_plank", 2),
            new RecipeIngredient("mat_arcane_dust", 3),
        }
    };

    public static readonly CraftingRecipe HealingPotionRecipe = new CraftingRecipe
    {
        Id = "recipe_potion_healing",
        DisplayName = "Brew Healing Potion",
        ResultItemId = "healing_potion_basic",
        ResultCount = 2,
        GoldCost = 10,
        Ingredients =
        {
            new RecipeIngredient("mat_slime_gel", 2),
        }
    };

    public static readonly CraftingRecipe ManaPotionRecipe = new CraftingRecipe
    {
        Id = "recipe_potion_mana",
        DisplayName = "Brew Mana Potion",
        ResultItemId = "mana_potion_basic",
        ResultCount = 2,
        GoldCost = 15,
        Ingredients =
        {
            new RecipeIngredient("mat_arcane_dust", 1),
            new RecipeIngredient("mat_slime_gel", 1),
        }
    };

    public static readonly CraftingRecipe IronHelmRecipe = new CraftingRecipe
    {
        Id = "recipe_helm_iron",
        DisplayName = "Forge Iron Helm",
        ResultItemId = "eq_helm_iron",
        ResultCount = 1,
        GoldCost = 25,
        Ingredients =
        {
            new RecipeIngredient("mat_iron_ore", 3),
        }
    };

    public static readonly CraftingRecipe CopperRingRecipe = new CraftingRecipe
    {
        Id = "recipe_ring_copper",
        DisplayName = "Craft Copper Ring",
        ResultItemId = "eq_ring_copper",
        ResultCount = 1,
        GoldCost = 15,
        Ingredients =
        {
            new RecipeIngredient("mat_iron_ore", 2),
            new RecipeIngredient("mat_arcane_dust", 1),
        }
    };

    public static readonly CraftingRecipe PlateArmorRecipe = new CraftingRecipe
    {
        Id = "recipe_armor_plate",
        DisplayName = "Forge Plate Armor",
        ResultItemId = "eq_armor_plate",
        ResultCount = 1,
        GoldCost = 120,
        Ingredients =
        {
            new RecipeIngredient("mat_iron_ore", 8),
            new RecipeIngredient("mat_leather", 3),
            new RecipeIngredient("mat_cloth", 2),
        }
    };

    public static readonly CraftingRecipe HunterBowRecipe = new CraftingRecipe
    {
        Id = "recipe_bow_hunter",
        DisplayName = "Craft Hunter's Bow",
        ResultItemId = "eq_bow_hunter",
        ResultCount = 1,
        GoldCost = 35,
        Ingredients =
        {
            new RecipeIngredient("mat_wood_plank", 3),
            new RecipeIngredient("mat_leather", 1),
        }
    };

    public static readonly CraftingRecipe JadeAmuletRecipe = new CraftingRecipe
    {
        Id = "recipe_amulet_jade",
        DisplayName = "Enchant Jade Amulet",
        ResultItemId = "eq_amulet_jade",
        ResultCount = 1,
        GoldCost = 50,
        Ingredients =
        {
            new RecipeIngredient("mat_arcane_dust", 3),
            new RecipeIngredient("mat_undead_bone", 2),
        }
    };
}

}
