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
    /// ENCHANTLIBRARY - Registry of weapon enchantment recipes.
    /// <para>PURPOSE: On first access, scans <see cref="ItemLibrary"/> for every base weapon
    /// (excludes +1/+2/+3 upgrade variants and already-enchanted ones) and generates a recipe
    /// per <see cref="Element"/>. Each recipe produces a new <see cref="ItemDefinition"/>
    /// registered back into ItemLibrary as a first-class item so equip / inventory / save flows
    /// see enchanted weapons naturally.</para>
    /// <para>AFFINITY STAT BLOCS (added to base):
    /// <list type="bullet">
    /// <item>Flame  — +5 Strength</item>
    /// <item>Frost  — +5 Intelligence</item>
    /// <item>Spark  — +3 Agility, +2 Wisdom</item>
    /// <item>Shadow — +3 Luck, +2 Strength</item>
    /// </list></para>
    /// <para>RECIPE (all elements): base weapon + 1 element-essence + 2 ArcaneDust + 150g → enchanted weapon.</para>
    /// <para>RELATED FILES: EnchantRecipe.cs, EnchantSection.cs, ItemData_Essences.cs, ItemLibrary.cs</para>
    /// </summary>
    public static class EnchantLibrary
    {
        // Keyed by "fromId:element". Value = recipe.
        private static readonly Dictionary<string, EnchantRecipe> recipes = new Dictionary<string, EnchantRecipe>();
        private static bool initialized;

        private const string EnchantSuffix = "_ench_";

        public static void Ensure()
        {
            if (initialized) return;
            initialized = true;

            // Only enchant clean base weapons. Skip upgrade variants (_plus*) and already-enchanted ones.
            var baseWeapons = ItemLibrary.All()
                .Where(i => i.Slot == EquipmentSlot.Weapon
                            && !i.Id.Contains("_plus")
                            && !i.Id.Contains(EnchantSuffix))
                .ToList();

            foreach (var baseItem in baseWeapons)
            {
                foreach (Element el in System.Enum.GetValues(typeof(Element)))
                {
                    var enchanted = BuildEnchantedItem(baseItem, el);
                    ItemLibrary.RegisterExternal(enchanted);
                    recipes[Key(baseItem.Id, el)] = BuildRecipe(baseItem, enchanted, el);
                }
            }
        }

        /// <summary>Returns every recipe that can be applied to <paramref name="fromId"/>, one per element.</summary>
        public static IEnumerable<EnchantRecipe> RecipesFor(string fromId)
        {
            Ensure();
            if (string.IsNullOrEmpty(fromId)) yield break;
            foreach (Element el in System.Enum.GetValues(typeof(Element)))
            {
                if (recipes.TryGetValue(Key(fromId, el), out var r)) yield return r;
            }
        }

        /// <summary>Specific element recipe for a given base item, or null.</summary>
        public static EnchantRecipe GetRecipe(string fromId, Element el)
        {
            Ensure();
            recipes.TryGetValue(Key(fromId, el), out var r);
            return r;
        }

        /// <summary>All recipes across all weapons and elements.</summary>
        public static IEnumerable<EnchantRecipe> All()
        {
            Ensure();
            return recipes.Values;
        }

        /// <summary>True if <paramref name="itemId"/> is an enchanted weapon (i.e. the enchanter's output).</summary>
        public static bool IsEnchanted(string itemId)
        {
            return !string.IsNullOrEmpty(itemId) && itemId.Contains(EnchantSuffix);
        }

        private static string Key(string fromId, Element el) => $"{fromId}:{el}";

        private static string AffinityTag(Element el) => el switch
        {
            Element.Flame => "flame",
            Element.Frost => "frost",
            Element.Spark => "spark",
            Element.Shadow => "shadow",
            _ => "none",
        };

        private static string AffinityLabel(Element el) => el switch
        {
            Element.Flame => "Flame",
            Element.Frost => "Frost",
            Element.Spark => "Spark",
            Element.Shadow => "Shadow",
            _ => "None",
        };

        private static string EssenceId(Element el) => el switch
        {
            Element.Flame => "mat_essence_flame",
            Element.Frost => "mat_essence_frost",
            Element.Spark => "mat_essence_spark",
            Element.Shadow => "mat_essence_shadow",
            _ => null,
        };

        private static ItemDefinition BuildEnchantedItem(ItemDefinition from, Element el)
        {
            // Stat bloc additions per element
            float addStr = 0f, addInt = 0f, addAgi = 0f, addWis = 0f, addLck = 0f;
            switch (el)
            {
                case Element.Flame:  addStr = 5f; break;
                case Element.Frost:  addInt = 5f; break;
                case Element.Spark:  addAgi = 3f; addWis = 2f; break;
                case Element.Shadow: addLck = 3f; addStr = 2f; break;
            }

            // Enchanting elevates rarity one step (Common→Uncommon, Uncommon→Rare, etc.).
            var rarity = BumpRarity(from.Rarity);

            return new ItemDefinition
            {
                Id = $"{from.Id}{EnchantSuffix}{AffinityTag(el)}",
                DisplayName = $"{from.DisplayName} ({AffinityLabel(el)})",
                Description = $"{from.Description}\nImbued with the essence of {AffinityLabel(el)}.",
                Type = from.Type,
                Slot = from.Slot,
                Rarity = rarity,
                BaseCost = from.BaseCost + 120,
                SellValue = from.SellValue >= 0 ? from.SellValue + 60 : -1,
                MaxStack = from.MaxStack,
                Durability = from.Durability,
                BaseHealing = from.BaseHealing,
                MaxUsesPerBattle = from.MaxUsesPerBattle,
                Strength     = from.Strength + addStr,
                Vitality     = from.Vitality,
                Agility      = from.Agility + addAgi,
                Stamina      = from.Stamina,
                Intelligence = from.Intelligence + addInt,
                Wisdom       = from.Wisdom + addWis,
                Luck         = from.Luck + addLck,
                RequiredTags = from.RequiredTags,
            };
        }

        private static EnchantRecipe BuildRecipe(ItemDefinition from, ItemDefinition to, Element el)
        {
            var r = new EnchantRecipe { From = from, To = to, Affinity = el };
            r.GoldCost = 150;
            r.Materials.Add((EssenceId(el), 1));
            r.Materials.Add(("mat_arcane_dust", 2));
            return r;
        }

        private static ItemRarity BumpRarity(ItemRarity r) => r switch
        {
            ItemRarity.Common    => ItemRarity.Uncommon,
            ItemRarity.Uncommon  => ItemRarity.Rare,
            ItemRarity.Rare      => ItemRarity.Epic,
            ItemRarity.Epic      => ItemRarity.Legendary,
            ItemRarity.Legendary => ItemRarity.Legendary,
            _ => r,
        };
    }
}
