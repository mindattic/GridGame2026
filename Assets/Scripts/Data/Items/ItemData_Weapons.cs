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
/// ITEMDATA_WEAPONS - Weapon item definitions.
/// 
/// PURPOSE:
/// Static definitions for weapon equipment. Each weapon
/// provides stat bonuses and occupies the Weapon slot.
/// 
/// RELATED FILES:
/// - ItemLibrary.cs: Registers these items
/// - ItemDefinition.cs: Item data structure
/// </summary>
public static class ItemData_Weapons
{
    public static readonly ItemDefinition IronSword = new ItemDefinition
    {
        Id = "eq_sword_iron",
        DisplayName = "Iron Sword",
        Description = "A sturdy blade forged from iron.",
        Type = ItemType.Equipment,
        Slot = EquipmentSlot.Weapon,
        Rarity = ItemRarity.Common,
        BaseCost = 80,
        MaxStack = 1,
        Durability = 150,
        Strength = 4,
    };

    public static readonly ItemDefinition SteelSword = new ItemDefinition
    {
        Id = "eq_sword_steel",
        DisplayName = "Steel Sword",
        Description = "A well-tempered steel blade.",
        Type = ItemType.Equipment,
        Slot = EquipmentSlot.Weapon,
        Rarity = ItemRarity.Uncommon,
        BaseCost = 200,
        MaxStack = 1,
        Durability = 200,
        Strength = 7,
        Agility = 1,
    };

    public static readonly ItemDefinition MysticStaff = new ItemDefinition
    {
        Id = "eq_staff_mystic",
        DisplayName = "Mystic Staff",
        Description = "A staff humming with arcane energy.",
        Type = ItemType.Equipment,
        Slot = EquipmentSlot.Weapon,
        Rarity = ItemRarity.Uncommon,
        BaseCost = 180,
        MaxStack = 1,
        Durability = 120,
        Intelligence = 6,
        Wisdom = 3,
        RequiredTags = { ActorTag.Magic },
    };

    public static readonly ItemDefinition HunterBow = new ItemDefinition
    {
        Id = "eq_bow_hunter",
        DisplayName = "Hunter's Bow",
        Description = "A lightweight bow favored by scouts.",
        Type = ItemType.Equipment,
        Slot = EquipmentSlot.Weapon,
        Rarity = ItemRarity.Common,
        BaseCost = 90,
        MaxStack = 1,
        Durability = 100,
        Agility = 5,
        Luck = 1,
    };

    public static readonly ItemDefinition WarHammer = new ItemDefinition
    {
        Id = "eq_hammer_war",
        DisplayName = "War Hammer",
        Description = "A heavy hammer that crushes armor.",
        Type = ItemType.Equipment,
        Slot = EquipmentSlot.Weapon,
        Rarity = ItemRarity.Rare,
        BaseCost = 350,
        MaxStack = 1,
        Durability = 250,
        Strength = 10,
        Vitality = 2,
    };

    public static readonly ItemDefinition BronzeDagger = new ItemDefinition
    {
        Id = "eq_dagger_bronze",
        DisplayName = "Bronze Dagger",
        Description = "A quick blade that strikes before the enemy reacts.",
        Type = ItemType.Equipment,
        Slot = EquipmentSlot.Weapon,
        Rarity = ItemRarity.Common,
        BaseCost = 55,
        MaxStack = 1,
        Durability = 90,
        Strength = 2,
        Agility = 3,
        Luck = 1,
    };

    public static readonly ItemDefinition FlameTongue = new ItemDefinition
    {
        Id = "eq_sword_flame",
        DisplayName = "Flame Tongue",
        Description = "A blade wreathed in perpetual fire.",
        Type = ItemType.Equipment,
        Slot = EquipmentSlot.Weapon,
        Rarity = ItemRarity.Epic,
        BaseCost = 600,
        MaxStack = 1,
        Durability = 180,
        Strength = 12,
        Intelligence = 4,
    };

    public static readonly ItemDefinition CrystalWand = new ItemDefinition
    {
        Id = "eq_wand_crystal",
        DisplayName = "Crystal Wand",
        Description = "A slender wand amplifying magical focus.",
        Type = ItemType.Equipment,
        Slot = EquipmentSlot.Weapon,
        Rarity = ItemRarity.Rare,
        BaseCost = 280,
        MaxStack = 1,
        Durability = 80,
        Intelligence = 8,
        Wisdom = 4,
        RequiredTags = { ActorTag.Magic },
    };

    public static readonly ItemDefinition SerpentSpear = new ItemDefinition
    {
        Id = "eq_spear_serpent",
        DisplayName = "Serpent Spear",
        Description = "A long polearm with a venomous tip.",
        Type = ItemType.Equipment,
        Slot = EquipmentSlot.Weapon,
        Rarity = ItemRarity.Uncommon,
        BaseCost = 160,
        MaxStack = 1,
        Durability = 170,
        Strength = 6,
        Agility = 2,
    };

    public static readonly ItemDefinition RuneAxe = new ItemDefinition
    {
        Id = "eq_axe_rune",
        DisplayName = "Rune Axe",
        Description = "An axe etched with ancient glyphs that hum with power.",
        Type = ItemType.Equipment,
        Slot = EquipmentSlot.Weapon,
        Rarity = ItemRarity.Rare,
        BaseCost = 400,
        MaxStack = 1,
        Durability = 220,
        Strength = 9,
        Intelligence = 3,
    };

    public static readonly ItemDefinition ShadowBlade = new ItemDefinition
    {
        Id = "eq_sword_shadow",
        DisplayName = "Shadow Blade",
        Description = "A dark blade that seems to vanish mid-swing.",
        Type = ItemType.Equipment,
        Slot = EquipmentSlot.Weapon,
        Rarity = ItemRarity.Epic,
        BaseCost = 700,
        MaxStack = 1,
        Durability = 150,
        Strength = 8,
        Agility = 6,
        Luck = 4,
    };

    public static readonly ItemDefinition StarfallMace = new ItemDefinition
    {
        Id = "eq_mace_starfall",
        DisplayName = "Starfall Mace",
        Description = "A celestial weapon forged from a fallen star.",
        Type = ItemType.Equipment,
        Slot = EquipmentSlot.Weapon,
        Rarity = ItemRarity.Legendary,
        BaseCost = 1200,
        MaxStack = 1,
        Durability = 400,
        Strength = 14,
        Vitality = 4,
        Wisdom = 3,
    };
}

}
