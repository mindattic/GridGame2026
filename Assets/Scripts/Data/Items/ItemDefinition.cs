using System.Collections.Generic;
using UnityEngine;
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
/// EQUIPMENT SLOTS:
/// - Weapon, Armor, Helmet, Boots, Ring, Amulet
/// 
/// RELATED FILES:
/// - ItemLibrary.cs: Item registry
/// - PlayerInventory.cs: Item ownership
/// - ShopSectionController.cs: Shop transactions
/// - RecipeLibrary.cs: Crafting recipes
/// </summary>
[System.Serializable]
public class ItemDefinition
{
    public string Id;
    public string DisplayName;
    public string Description;
    public ItemType Type;
    public ItemRarity Rarity = ItemRarity.Common;
    public EquipmentSlot Slot = EquipmentSlot.None;
    public int BaseCost;
    public int SellValue = -1; // -1 means auto-compute as BaseCost / 2
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

    // Salvage: what materials this item breaks down into
    public List<SalvageComponent> SalvageComponents = new List<SalvageComponent>();

    /// <summary>Computed sell value (half of cost if not explicitly set).</summary>
    public int ComputedSellValue => SellValue >= 0 ? SellValue : UnityEngine.Mathf.Max(1, BaseCost / 2);

    /// <summary>True if this item is equippable gear.</summary>
    public bool IsEquipment => Type == ItemType.Equipment && Slot != EquipmentSlot.None;

    /// <summary>True if this item is a crafting material.</summary>
    public bool IsCraftingMaterial => Type == ItemType.CraftingMaterial;

    /// <summary>True if this item can be salvaged into materials.</summary>
    public bool CanSalvage => SalvageComponents != null && SalvageComponents.Count > 0;
}

/// <summary>A single material returned when salvaging an item.</summary>
[System.Serializable]
public class SalvageComponent
{
    public string MaterialId;
    public int Count;

    public SalvageComponent() { }
    public SalvageComponent(string materialId, int count)
    {
        MaterialId = materialId;
        Count = count;
    }
}

/// <summary>Item type classification.</summary>
public enum ItemType
{
    Consumable,
    Equipment,
    CraftingMaterial,
    QuestItem
}

}
