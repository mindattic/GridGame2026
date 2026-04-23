using System.Collections.Generic;
using System.Linq;
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
    /// UPGRADELIBRARY - Registry of equipment +N upgrade chains.
    /// <para>PURPOSE: On first access, scans <see cref="ItemLibrary"/> for every weapon and
    /// generates +1/+2/+3 variants + recipes that cost gold and tier-appropriate materials.
    /// Upgraded items are inserted back into ItemLibrary so equip / inventory flows see them
    /// as first-class items.</para>
    /// <para>TIER RULES (applied to base stats, flat additions):
    /// <list type="bullet">
    /// <item>+1: +2 Strength/Intelligence, +15 base cost, 2 IronOre + 1 Leather, 30g.</item>
    /// <item>+2: +4, 30 base cost, 4 IronOre + 2 Leather + 1 ArcaneDust, 80g.</item>
    /// <item>+3: +7, 50 base cost, 6 IronOre + 3 Leather + 2 ArcaneDust, 180g.</item>
    /// </list></para>
    /// <para>RELATED FILES: UpgradeRecipe.cs, BlacksmithSection.cs, ItemLibrary.cs</para>
    /// </summary>
    public static class UpgradeLibrary
    {
        // Key is the From-item id (including the +1/+2 intermediates). Value = the recipe to go one tier higher.
        private static readonly Dictionary<string, UpgradeRecipe> recipes = new Dictionary<string, UpgradeRecipe>();
        private static bool initialized;

        public static void Ensure()
        {
            if (initialized) return;
            initialized = true;

            // Only weapons upgrade in this pass. Armor/shields follow same pattern when desired.
            var baseWeapons = ItemLibrary.All().Where(i => i.Slot == EquipmentSlot.Weapon && !i.Id.Contains("_plus")).ToList();
            foreach (var baseItem in baseWeapons)
            {
                var tier1 = BuildUpgradedItem(baseItem, 1);
                var tier2 = BuildUpgradedItem(baseItem, 2);
                var tier3 = BuildUpgradedItem(baseItem, 3);
                ItemLibrary.RegisterExternal(tier1);
                ItemLibrary.RegisterExternal(tier2);
                ItemLibrary.RegisterExternal(tier3);

                recipes[baseItem.Id] = BuildRecipe(baseItem, tier1, 1);
                recipes[tier1.Id]    = BuildRecipe(tier1, tier2, 2);
                recipes[tier2.Id]    = BuildRecipe(tier2, tier3, 3);
            }
        }

        /// <summary>Returns the recipe that upgrades <paramref name="fromId"/> one tier higher, or null if already maxed.</summary>
        public static UpgradeRecipe GetRecipe(string fromId)
        {
            Ensure();
            if (string.IsNullOrEmpty(fromId)) return null;
            recipes.TryGetValue(fromId, out var r);
            return r;
        }

        /// <summary>All recipes (useful for Blacksmith list browsing).</summary>
        public static IEnumerable<UpgradeRecipe> All()
        {
            Ensure();
            return recipes.Values;
        }

        private static ItemDefinition BuildUpgradedItem(ItemDefinition from, int tier)
        {
            float bonus = tier switch { 1 => 2f, 2 => 4f, 3 => 7f, _ => 0f };
            int costBonus = tier switch { 1 => 15, 2 => 30, 3 => 50, _ => 0 };
            return new ItemDefinition
            {
                Id = $"{from.Id}_plus{tier}",
                DisplayName = $"{from.DisplayName} +{tier}",
                Description = $"{from.Description} (upgraded +{tier})",
                Type = from.Type,
                Slot = from.Slot,
                Rarity = from.Rarity,
                BaseCost = from.BaseCost + costBonus,
                SellValue = from.SellValue >= 0 ? from.SellValue + costBonus / 2 : -1,
                MaxStack = from.MaxStack,
                Durability = from.Durability,
                BaseHealing = from.BaseHealing,
                MaxUsesPerBattle = from.MaxUsesPerBattle,
                Strength = from.Strength + bonus,
                Vitality = from.Vitality,
                Agility = from.Agility,
                Stamina = from.Stamina,
                Intelligence = from.Intelligence + bonus,
                Wisdom = from.Wisdom,
                Luck = from.Luck,
                RequiredTags = from.RequiredTags,
            };
        }

        private static UpgradeRecipe BuildRecipe(ItemDefinition from, ItemDefinition to, int tier)
        {
            var r = new UpgradeRecipe { From = from, To = to };
            switch (tier)
            {
                case 1:
                    r.GoldCost = 30;
                    r.Materials.Add(("mat_iron_ore", 2));
                    r.Materials.Add(("mat_leather", 1));
                    break;
                case 2:
                    r.GoldCost = 80;
                    r.Materials.Add(("mat_iron_ore", 4));
                    r.Materials.Add(("mat_leather", 2));
                    r.Materials.Add(("mat_arcane_dust", 1));
                    break;
                case 3:
                    r.GoldCost = 180;
                    r.Materials.Add(("mat_iron_ore", 6));
                    r.Materials.Add(("mat_leather", 3));
                    r.Materials.Add(("mat_arcane_dust", 2));
                    break;
            }
            return r;
        }
    }
}
