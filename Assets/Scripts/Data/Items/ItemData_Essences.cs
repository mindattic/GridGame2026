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
    /// ITEMDATA_ESSENCES - Elemental crafting materials consumed by the Enchanter.
    /// <para>PURPOSE: Each essence imbues a weapon with an elemental affinity that alters
    /// its stat bloc. Essences are rarer than bulk materials (Uncommon / Rare) and drop
    /// from themed enemy encounters or are bought from the Enchanter at a premium.</para>
    /// <para>AFFINITY → STAT MAP (see <see cref="EnchantLibrary"/>):
    /// <list type="bullet">
    /// <item>Flame — +5 Strength (physical amplification)</item>
    /// <item>Frost — +5 Intelligence (magical amplification)</item>
    /// <item>Spark — +3 Agility, +2 Wisdom (speed &amp; mana recovery)</item>
    /// <item>Shadow — +3 Luck, +2 Strength (critical affinity)</item>
    /// </list></para>
    /// <para>RELATED FILES: ItemLibrary.cs, EnchantLibrary.cs, EnchantSection.cs</para>
    /// </summary>
    public static class ItemData_Essences
    {
        public static readonly ItemDefinition FlameEssence = new ItemDefinition
        {
            Id = "mat_essence_flame",
            DisplayName = "Flame Essence",
            Description = "A shard of captured fire. Imbues weapons with burning fury (+STR).",
            Type = ItemType.CraftingMaterial,
            Rarity = ItemRarity.Uncommon,
            BaseCost = 60,
            MaxStack = 50,
        };

        public static readonly ItemDefinition FrostEssence = new ItemDefinition
        {
            Id = "mat_essence_frost",
            DisplayName = "Frost Essence",
            Description = "Crystallized winter. Imbues weapons with piercing intellect (+INT).",
            Type = ItemType.CraftingMaterial,
            Rarity = ItemRarity.Uncommon,
            BaseCost = 60,
            MaxStack = 50,
        };

        public static readonly ItemDefinition SparkEssence = new ItemDefinition
        {
            Id = "mat_essence_spark",
            DisplayName = "Spark Essence",
            Description = "Bottled lightning. Imbues weapons with quickened reflexes (+AGI, +WIS).",
            Type = ItemType.CraftingMaterial,
            Rarity = ItemRarity.Rare,
            BaseCost = 90,
            MaxStack = 50,
        };

        public static readonly ItemDefinition ShadowEssence = new ItemDefinition
        {
            Id = "mat_essence_shadow",
            DisplayName = "Shadow Essence",
            Description = "A sliver of living darkness. Imbues weapons with lethal fortune (+LCK, +STR).",
            Type = ItemType.CraftingMaterial,
            Rarity = ItemRarity.Rare,
            BaseCost = 90,
            MaxStack = 50,
        };
    }
}
