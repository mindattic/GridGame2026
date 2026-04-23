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
    /// ITEMDATA_THEMEDCONSUMABLES - Offensive consumables tuned against specific enemy tags.
    /// <para>PURPOSE: Tactical picks for specific biomes. Stock Holy Water before the Ruins
    /// (bonus vs Undead), Flame Oil before the Forest/Cave (bonus vs Beast / IceAffinity).</para>
    /// <para>RELATED FILES: ItemData_Consumables.cs, ItemLibrary.cs, UseItemSequence.cs</para>
    /// </summary>
    public static class ItemData_ThemedConsumables
    {
        public static readonly ItemDefinition HolyWater = new ItemDefinition
        {
            Id = "holy_water",
            DisplayName = "Holy Water",
            Description = "Blessed vial. Deals light damage to one enemy — sears the undead.",
            Type = ItemType.Consumable,
            Rarity = ItemRarity.Uncommon,
            BaseCost = 80,
            MaxStack = 10,
            BaseHealing = 0,
            BaseDamage = 40,
            BonusDamageVsTag = ActorTag.Undead,
            BonusDamageMultiplier = 2.5f,
            MaxUsesPerBattle = 3,
        };

        public static readonly ItemDefinition FlameOil = new ItemDefinition
        {
            Id = "flame_oil",
            DisplayName = "Flame Oil",
            Description = "Volatile flask. Deals fire damage — beasts and ice-kin burn well.",
            Type = ItemType.Consumable,
            Rarity = ItemRarity.Uncommon,
            BaseCost = 70,
            MaxStack = 10,
            BaseHealing = 0,
            BaseDamage = 45,
            BonusDamageVsTag = ActorTag.Beast | ActorTag.IceAffinity,
            BonusDamageMultiplier = 2f,
            MaxUsesPerBattle = 3,
        };
    }
}
