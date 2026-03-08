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
/// ITEMDATA_RELICS - Ring and amulet definitions.
/// 
/// PURPOSE:
/// Static definitions for accessory equipment (rings, amulets)
/// that provide unique stat combinations.
/// 
/// RELATED FILES:
/// - ItemLibrary.cs: Registers these items
/// - ItemDefinition.cs: Item data structure
/// </summary>
public static class ItemData_Relics
{
    public static readonly ItemDefinition CopperRing = new ItemDefinition
    {
        Id = "eq_ring_copper",
        DisplayName = "Copper Ring",
        Description = "A simple ring that bolsters luck.",
        Type = ItemType.Equipment,
        Slot = EquipmentSlot.Ring,
        Rarity = ItemRarity.Common,
        BaseCost = 40,
        MaxStack = 1,
        Luck = 3,
    };

    public static readonly ItemDefinition SilverRing = new ItemDefinition
    {
        Id = "eq_ring_silver",
        DisplayName = "Silver Ring",
        Description = "A polished silver ring with faint enchantment.",
        Type = ItemType.Equipment,
        Slot = EquipmentSlot.Ring,
        Rarity = ItemRarity.Uncommon,
        BaseCost = 120,
        MaxStack = 1,
        Luck = 4,
        Wisdom = 2,
    };

    public static readonly ItemDefinition BoneAmulet = new ItemDefinition
    {
        Id = "eq_amulet_bone",
        DisplayName = "Bone Amulet",
        Description = "A carved bone amulet radiating protective energy.",
        Type = ItemType.Equipment,
        Slot = EquipmentSlot.Amulet,
        Rarity = ItemRarity.Common,
        BaseCost = 55,
        MaxStack = 1,
        Vitality = 2,
        Wisdom = 1,
    };

    public static readonly ItemDefinition JadeAmulet = new ItemDefinition
    {
        Id = "eq_amulet_jade",
        DisplayName = "Jade Amulet",
        Description = "A translucent jade stone channeling nature's fortitude.",
        Type = ItemType.Equipment,
        Slot = EquipmentSlot.Amulet,
        Rarity = ItemRarity.Rare,
        BaseCost = 260,
        MaxStack = 1,
        Vitality = 4,
        Intelligence = 3,
        Wisdom = 3,
    };

    // === Additional Rings ===

    public static readonly ItemDefinition GoldRing = new ItemDefinition
    {
        Id = "eq_ring_gold",
        DisplayName = "Gold Ring",
        Description = "A gleaming gold ring that sharpens the wearer's focus.",
        Type = ItemType.Equipment,
        Slot = EquipmentSlot.Ring,
        Rarity = ItemRarity.Rare,
        BaseCost = 220,
        MaxStack = 1,
        Luck = 5,
        Agility = 2,
        Intelligence = 1,
    };

    public static readonly ItemDefinition BloodstoneRing = new ItemDefinition
    {
        Id = "eq_ring_bloodstone",
        DisplayName = "Bloodstone Ring",
        Description = "A crimson gem pulses with vitality.",
        Type = ItemType.Equipment,
        Slot = EquipmentSlot.Ring,
        Rarity = ItemRarity.Epic,
        BaseCost = 480,
        MaxStack = 1,
        Vitality = 5,
        Strength = 3,
        Luck = 2,
    };

    public static readonly ItemDefinition PhantomBand = new ItemDefinition
    {
        Id = "eq_ring_phantom",
        DisplayName = "Phantom Band",
        Description = "A spectral ring that phases its wearer between strikes.",
        Type = ItemType.Equipment,
        Slot = EquipmentSlot.Ring,
        Rarity = ItemRarity.Legendary,
        BaseCost = 900,
        MaxStack = 1,
        Agility = 8,
        Luck = 6,
        Wisdom = 2,
    };

    // === Additional Amulets ===

    public static readonly ItemDefinition IronTalisman = new ItemDefinition
    {
        Id = "eq_amulet_iron",
        DisplayName = "Iron Talisman",
        Description = "A crude talisman hammered from iron scraps.",
        Type = ItemType.Equipment,
        Slot = EquipmentSlot.Amulet,
        Rarity = ItemRarity.Common,
        BaseCost = 35,
        MaxStack = 1,
        Vitality = 1,
        Strength = 1,
    };

    public static readonly ItemDefinition SunfireAmulet = new ItemDefinition
    {
        Id = "eq_amulet_sunfire",
        DisplayName = "Sunfire Amulet",
        Description = "An amulet blazing with solar energy.",
        Type = ItemType.Equipment,
        Slot = EquipmentSlot.Amulet,
        Rarity = ItemRarity.Epic,
        BaseCost = 520,
        MaxStack = 1,
        Intelligence = 5,
        Strength = 3,
        Stamina = 2,
    };

    public static readonly ItemDefinition CrownOfStars = new ItemDefinition
    {
        Id = "eq_amulet_stars",
        DisplayName = "Crown of Stars",
        Description = "An ancient relic said to contain the light of a constellation.",
        Type = ItemType.Equipment,
        Slot = EquipmentSlot.Amulet,
        Rarity = ItemRarity.Legendary,
        BaseCost = 1100,
        MaxStack = 1,
        Intelligence = 7,
        Wisdom = 6,
        Luck = 3,
    };
}

}
