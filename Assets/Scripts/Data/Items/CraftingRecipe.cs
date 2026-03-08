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
/// CRAFTINGRECIPE - Recipe definition for crafting an item.
/// 
/// PURPOSE:
/// Defines the ingredients and gold cost required to craft
/// a specific item at the Blacksmith or Shop.
/// 
/// RELATED FILES:
/// - RecipeLibrary.cs: Recipe registry
/// - RecipeData.cs: Recipe static definitions
/// - BlacksmithSectionController.cs: Crafting execution
/// - PlayerInventory.cs: Ingredient ownership
/// </summary>
[System.Serializable]
public class CraftingRecipe
{
    /// <summary>Unique recipe identifier.</summary>
    public string Id;

    /// <summary>Display name shown in crafting UI.</summary>
    public string DisplayName;

    /// <summary>The item produced by this recipe.</summary>
    public string ResultItemId;

    /// <summary>Number of result items produced.</summary>
    public int ResultCount = 1;

    /// <summary>Gold cost to craft.</summary>
    public int GoldCost;

    /// <summary>Required ingredients: item ID to quantity needed.</summary>
    public List<RecipeIngredient> Ingredients = new List<RecipeIngredient>();

    /// <summary>Checks whether the player can afford this recipe.</summary>
    public bool CanCraft(PlayerInventory inventory)
    {
        if (inventory == null) return false;
        if (inventory.Gold < GoldCost) return false;
        foreach (var ing in Ingredients)
        {
            if (!inventory.Contains(ing.ItemId, ing.Count)) return false;
        }
        return true;
    }

    /// <summary>Consumes ingredients and gold, adds result to inventory.</summary>
    public bool Execute(PlayerInventory inventory)
    {
        if (!CanCraft(inventory)) return false;

        foreach (var ing in Ingredients)
        {
            inventory.Remove(ing.ItemId, ing.Count);
        }
        inventory.Gold -= GoldCost;

        var resultDef = ItemLibrary.Get(ResultItemId);
        if (resultDef != null)
        {
            inventory.Add(resultDef, ResultCount);
        }
        return true;
    }
}

/// <summary>Single ingredient entry in a crafting recipe.</summary>
[System.Serializable]
public class RecipeIngredient
{
    public string ItemId;
    public int Count;

    public RecipeIngredient() { }
    public RecipeIngredient(string itemId, int count)
    {
        ItemId = itemId;
        Count = count;
    }
}

}
