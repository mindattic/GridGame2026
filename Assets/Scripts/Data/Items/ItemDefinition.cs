using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ITEMDEFINITION - Item/equipment data template.
/// 
/// PURPOSE:
/// Describes a single item or equipment piece with stats,
/// costs, and requirements.
/// 
/// ITEM TYPES:
/// - Consumable: Single-use items (healing, buffs)
/// - Equipment: Weapons, armor, accessories
/// - CraftingMaterial: Used for crafting
/// - QuestItem: Story-related items
/// 
/// STAT MODIFIERS (equipment only):
/// Applied in order: Strength, Vitality, Agility,
/// Stamina, Intelligence, Wisdom, Luck
/// 
/// RELATED FILES:
/// - ItemLibrary.cs: Item registry
/// - PlayerInventory.cs: Item ownership
/// - ShopSectionController.cs: Shop transactions
/// </summary>
[System.Serializable]
public class ItemDefinition
{
    public string Id;
    public string DisplayName;
    public string Description;
    public ItemType Type;
    public int BaseCost;
    public int MaxStack = 99;
    public int Durability;
    public int BaseHealing;

    // Stat modifiers for equipment
    public float Strength;
    public float Vitality;
    public float Agility;
    public float Stamina;
    public float Intelligence;
    public float Wisdom;
    public float Luck;

    // Tag constraints
    public List<ActorTag> RequiredTags = new List<ActorTag>();
}

/// <summary>Item type classification.</summary>
public enum ItemType
{
    Consumable,
    Equipment,
    CraftingMaterial,
    QuestItem
}
