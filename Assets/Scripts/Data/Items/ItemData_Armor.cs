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
/// ITEMDATA_ARMOR - Armor and protective equipment definitions.
/// 
/// PURPOSE:
/// Static definitions for armor, helmets, and boots.
/// Each piece provides defensive stat bonuses.
/// 
/// RELATED FILES:
/// - ItemLibrary.cs: Registers these items
/// - ItemDefinition.cs: Item data structure
/// </summary>
public static class ItemData_Armor
{
    public static readonly ItemDefinition ChainMail = new ItemDefinition
    {
        Id = "eq_armor_chain",
        DisplayName = "Chain Mail",
        Description = "Interlocking metal rings offering solid protection.",
        Type = ItemType.Equipment,
        Slot = EquipmentSlot.Armor,
        Rarity = ItemRarity.Common,
        BaseCost = 100,
        MaxStack = 1,
        Durability = 180,
        Vitality = 4,
        Stamina = 2,
    };

    public static readonly ItemDefinition PlateArmor = new ItemDefinition
    {
        Id = "eq_armor_plate",
        DisplayName = "Plate Armor",
        Description = "Heavy plate armor forged for frontline warriors.",
        Type = ItemType.Equipment,
        Slot = EquipmentSlot.Armor,
        Rarity = ItemRarity.Rare,
        BaseCost = 320,
        MaxStack = 1,
        Durability = 300,
        Vitality = 8,
        Stamina = 3,
        Agility = -2,
    };

    public static readonly ItemDefinition IronHelm = new ItemDefinition
    {
        Id = "eq_helm_iron",
        DisplayName = "Iron Helm",
        Description = "A basic iron helmet.",
        Type = ItemType.Equipment,
        Slot = EquipmentSlot.Relic1,
        Rarity = ItemRarity.Common,
        BaseCost = 60,
        MaxStack = 1,
        Durability = 100,
        Vitality = 2,
    };

    public static readonly ItemDefinition LeatherBoots = new ItemDefinition
    {
        Id = "eq_boots_leather",
        DisplayName = "Leather Boots",
        Description = "Light boots that improve footwork.",
        Type = ItemType.Equipment,
        Slot = EquipmentSlot.Relic1,
        Rarity = ItemRarity.Common,
        BaseCost = 50,
        MaxStack = 1,
        Durability = 80,
        Agility = 3,
    };

    public static readonly ItemDefinition SteelGreaves = new ItemDefinition
    {
        Id = "eq_boots_steel",
        DisplayName = "Steel Greaves",
        Description = "Heavy greaves that trade agility for durability.",
        Type = ItemType.Equipment,
        Slot = EquipmentSlot.Relic1,
        Rarity = ItemRarity.Uncommon,
        BaseCost = 140,
        MaxStack = 1,
        Durability = 200,
        Vitality = 2,
        Stamina = 2,
    };

    // === Additional Armor ===

    public static readonly ItemDefinition PaddedVest = new ItemDefinition
    {
        Id = "eq_armor_padded",
        DisplayName = "Padded Vest",
        Description = "Thick quilted cloth offering light protection.",
        Type = ItemType.Equipment,
        Slot = EquipmentSlot.Armor,
        Rarity = ItemRarity.Common,
        BaseCost = 45,
        MaxStack = 1,
        Durability = 100,
        Vitality = 2,
        Agility = 1,
    };

    public static readonly ItemDefinition MageRobes = new ItemDefinition
    {
        Id = "eq_armor_mage",
        DisplayName = "Mage Robes",
        Description = "Enchanted robes that channel magical energy.",
        Type = ItemType.Equipment,
        Slot = EquipmentSlot.Armor,
        Rarity = ItemRarity.Uncommon,
        BaseCost = 160,
        MaxStack = 1,
        Durability = 120,
        Intelligence = 4,
        Wisdom = 3,
        RequiredTags = { ActorTag.Magic },
    };

    public static readonly ItemDefinition DragonscaleArmor = new ItemDefinition
    {
        Id = "eq_armor_dragonscale",
        DisplayName = "Dragonscale Armor",
        Description = "Armor fashioned from the hide of a great wyrm.",
        Type = ItemType.Equipment,
        Slot = EquipmentSlot.Armor,
        Rarity = ItemRarity.Legendary,
        BaseCost = 1000,
        MaxStack = 1,
        Durability = 500,
        Vitality = 12,
        Stamina = 4,
        Strength = 3,
    };

    // === Additional Relics (former Helmets) ===

    public static readonly ItemDefinition SteelHelm = new ItemDefinition
    {
        Id = "eq_helm_steel",
        DisplayName = "Steel Helm",
        Description = "A reinforced steel helmet.",
        Type = ItemType.Equipment,
        Slot = EquipmentSlot.Relic1,
        Rarity = ItemRarity.Uncommon,
        BaseCost = 110,
        MaxStack = 1,
        Durability = 160,
        Vitality = 4,
    };

    public static readonly ItemDefinition WizardHat = new ItemDefinition
    {
        Id = "eq_helm_wizard",
        DisplayName = "Wizard's Hat",
        Description = "A pointed hat thrumming with magical resonance.",
        Type = ItemType.Equipment,
        Slot = EquipmentSlot.Relic1,
        Rarity = ItemRarity.Uncommon,
        BaseCost = 130,
        MaxStack = 1,
        Durability = 80,
        Intelligence = 3,
        Wisdom = 2,
        RequiredTags = { ActorTag.Magic },
    };

    public static readonly ItemDefinition HornedHelm = new ItemDefinition
    {
        Id = "eq_helm_horned",
        DisplayName = "Horned Helm",
        Description = "A fearsome helm adorned with polished horns.",
        Type = ItemType.Equipment,
        Slot = EquipmentSlot.Relic1,
        Rarity = ItemRarity.Rare,
        BaseCost = 240,
        MaxStack = 1,
        Durability = 220,
        Vitality = 5,
        Strength = 2,
    };

    // === Additional Relics (former Boots) ===

    public static readonly ItemDefinition WindRunners = new ItemDefinition
    {
        Id = "eq_boots_wind",
        DisplayName = "Wind Runners",
        Description = "Feather-light boots enchanted for speed.",
        Type = ItemType.Equipment,
        Slot = EquipmentSlot.Relic1,
        Rarity = ItemRarity.Rare,
        BaseCost = 280,
        MaxStack = 1,
        Durability = 150,
        Agility = 6,
        Luck = 2,
    };

    public static readonly ItemDefinition IronSabatons = new ItemDefinition
    {
        Id = "eq_boots_iron",
        DisplayName = "Iron Sabatons",
        Description = "Heavy iron boots that anchor the wearer.",
        Type = ItemType.Equipment,
        Slot = EquipmentSlot.Relic1,
        Rarity = ItemRarity.Common,
        BaseCost = 70,
        MaxStack = 1,
        Durability = 180,
        Vitality = 3,
        Agility = -1,
    };
}

}
