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
/// ITEMDATA_CONSUMABLES - Extended consumable item catalog.
///
/// PURPOSE:
/// FF4/5/6-style consumable item definitions covering healing,
/// mana restoration, revival, status cures, and utility.
///
/// ITEMS:
/// Healing:  Hi-Potion, X-Potion
/// Mana:     Ether, Hi-Ether
/// Revival:  Phoenix Down
/// Status:   Antidote, Eye Drops, Remedy
/// Utility:  Smoke Bomb, Tent
///
/// RELATED FILES:
/// - ItemData_Healing.cs: Basic Healing Potion and Mana Potion
/// - ItemLibrary.cs: Registers these items
/// - ShopSectionController.cs: Shop catalog
/// - AbilityLibrary.FromConsumable(): Creates battle abilities
/// </summary>
public static class ItemData_Consumables
{
    // ============== HEALING ==============

    public static readonly ItemDefinition HiPotion = new ItemDefinition
    {
        Id = "hi_potion",
        DisplayName = "Hi-Potion",
        Description = "Restores a large amount of HP to one ally.",
        Type = ItemType.Consumable,
        Rarity = ItemRarity.Uncommon,
        BaseCost = 75,
        MaxStack = 10,
        BaseHealing = 150,
    };

    public static readonly ItemDefinition XPotion = new ItemDefinition
    {
        Id = "x_potion",
        DisplayName = "X-Potion",
        Description = "Fully restores HP to one ally.",
        Type = ItemType.Consumable,
        Rarity = ItemRarity.Rare,
        BaseCost = 250,
        MaxStack = 5,
        BaseHealing = 500,
    };

    // ============== MANA ==============

    public static readonly ItemDefinition Ether = new ItemDefinition
    {
        Id = "ether",
        DisplayName = "Ether",
        Description = "Restores a small amount of MP to one ally.",
        Type = ItemType.Consumable,
        BaseCost = 50,
        MaxStack = 10,
        BaseHealing = 0, // MP restore handled by effect logic
    };

    public static readonly ItemDefinition HiEther = new ItemDefinition
    {
        Id = "hi_ether",
        DisplayName = "Hi-Ether",
        Description = "Restores a large amount of MP to one ally.",
        Type = ItemType.Consumable,
        Rarity = ItemRarity.Uncommon,
        BaseCost = 150,
        MaxStack = 5,
        BaseHealing = 0,
    };

    // ============== REVIVAL ==============

    public static readonly ItemDefinition PhoenixDown = new ItemDefinition
    {
        Id = "phoenix_down",
        DisplayName = "Phoenix Down",
        Description = "Revives a fallen ally with partial HP.",
        Type = ItemType.Consumable,
        Rarity = ItemRarity.Rare,
        BaseCost = 300,
        MaxStack = 5,
        BaseHealing = 50, // revival amount
    };

    // ============== STATUS CURES ==============

    public static readonly ItemDefinition Antidote = new ItemDefinition
    {
        Id = "antidote",
        DisplayName = "Antidote",
        Description = "Cures poison from one ally.",
        Type = ItemType.Consumable,
        BaseCost = 20,
        MaxStack = 15,
        BaseHealing = 0,
    };

    public static readonly ItemDefinition EyeDrops = new ItemDefinition
    {
        Id = "eye_drops",
        DisplayName = "Eye Drops",
        Description = "Cures blindness from one ally.",
        Type = ItemType.Consumable,
        BaseCost = 20,
        MaxStack = 15,
        BaseHealing = 0,
    };

    public static readonly ItemDefinition Remedy = new ItemDefinition
    {
        Id = "remedy",
        DisplayName = "Remedy",
        Description = "Cures all status ailments from one ally.",
        Type = ItemType.Consumable,
        Rarity = ItemRarity.Uncommon,
        BaseCost = 150,
        MaxStack = 5,
        BaseHealing = 0,
    };

    // ============== UTILITY ==============

    public static readonly ItemDefinition SmokeBomb = new ItemDefinition
    {
        Id = "smoke_bomb",
        DisplayName = "Smoke Bomb",
        Description = "Allows the party to escape from most battles.",
        Type = ItemType.Consumable,
        BaseCost = 50,
        MaxStack = 10,
        BaseHealing = 0,
        MaxUsesPerBattle = 1,
    };

    public static readonly ItemDefinition Tent = new ItemDefinition
    {
        Id = "tent",
        DisplayName = "Tent",
        Description = "Fully restores HP and MP for the entire party. Can only be used outside of battle.",
        Type = ItemType.Consumable,
        Rarity = ItemRarity.Uncommon,
        BaseCost = 200,
        MaxStack = 3,
        BaseHealing = 999, // full restore
        MaxUsesPerBattle = 0, // not usable in battle
    };
}

}
