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
/// ITEMDATA_HEALING - Consumable item definitions.
/// 
/// PURPOSE:
/// Static definitions for healing and consumable items.
/// 
/// ITEMS:
/// - BasicHealingPotion: Restores 50 HP
/// - ManaPotion: Restores mana
/// 
/// RELATED FILES:
/// - ItemLibrary.cs: Registers these items
/// - ItemDefinition.cs: Item data structure
/// </summary>
public static class ItemData_Healing
{
    public static readonly ItemDefinition BasicHealingPotion = new ItemDefinition
    {
        Id = "healing_potion_basic",
        DisplayName = "Healing Potion",
        Description = "Restores a modest amount of health.",
        Type = ItemType.Consumable,
        BaseCost = 25,
        MaxStack = 10,
        BaseHealing = 50,
        Strength = 0,
        Vitality = 0,
        Agility = 0,
        Stamina = 0,
        Intelligence = 0,
        Wisdom = 0,
        Luck = 0
    };

    public static readonly ItemDefinition ManaPotion = new ItemDefinition
    {
        Id = "mana_potion_basic",
        DisplayName = "Mana Potion",
        Description = "Restores a small amount of mana.",
        Type = ItemType.Consumable,
        BaseCost = 30,
        MaxStack = 10,
        BaseHealing = 0,
        Strength = 0,
        Vitality = 0,
        Agility = 0,
        Stamina = 0,
        Intelligence = 0,
        Wisdom = 0,
        Luck = 0
    };
}

}
