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
/// RECIPELIBRARY - Central registry for crafting recipes.
/// 
/// PURPOSE:
/// Provides lookup and enumeration of crafting recipes
/// for the Blacksmith section.
/// 
/// USAGE:
/// ```csharp
/// var recipe = RecipeLibrary.Get("recipe_sword_iron");
/// foreach (var r in RecipeLibrary.All()) { ... }
/// ```
/// 
/// RELATED FILES:
/// - CraftingRecipe.cs: Recipe data structure
/// - RecipeData.cs: Recipe static definitions
/// - BlacksmithSectionController.cs: Crafting execution
/// </summary>
public static class RecipeLibrary
{
    private static Dictionary<string, CraftingRecipe> recipes = new Dictionary<string, CraftingRecipe>();
    private static bool initialized;

    /// <summary>Ensures library is populated once.</summary>
    private static void Ensure()
    {
        if (initialized) return;
        initialized = true;
        Register(RecipeData.IronSwordRecipe);
        Register(RecipeData.SteelSwordRecipe);
        Register(RecipeData.ChainMailRecipe);
        Register(RecipeData.LeatherBootsRecipe);
        Register(RecipeData.BoneAmuletRecipe);
        Register(RecipeData.MysticStaffRecipe);
        Register(RecipeData.HealingPotionRecipe);
        Register(RecipeData.ManaPotionRecipe);
        Register(RecipeData.IronHelmRecipe);
        Register(RecipeData.CopperRingRecipe);
        Register(RecipeData.PlateArmorRecipe);
        Register(RecipeData.HunterBowRecipe);
        Register(RecipeData.JadeAmuletRecipe);
    }

    /// <summary>Registers a recipe.</summary>
    private static void Register(CraftingRecipe recipe)
    {
        if (recipe == null || string.IsNullOrEmpty(recipe.Id)) return;
        if (!recipes.ContainsKey(recipe.Id)) recipes.Add(recipe.Id, recipe);
    }

    /// <summary>Gets a recipe by Id or null if missing.</summary>
    public static CraftingRecipe Get(string id)
    {
        Ensure();
        if (string.IsNullOrEmpty(id)) return null;
        recipes.TryGetValue(id, out var r);
        return r;
    }

    /// <summary>Enumerates all recipes.</summary>
    public static IEnumerable<CraftingRecipe> All()
    {
        Ensure();
        return recipes.Values;
    }
}

}
