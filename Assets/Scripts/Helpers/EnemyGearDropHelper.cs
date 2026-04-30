using System.Collections.Generic;
using UnityEngine;
using g = Scripts.Helpers.GameHelper;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Factories;
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

namespace Scripts.Helpers
{
    /// <summary>
    /// ENEMYGEARDROPHELPER - Rolls a chance for humanoid enemies to drop the equipment they
    /// were "wearing". Anything dropped is fed straight into <see cref="LootTracker"/> so the
    /// player sees it on the post-battle loot screen and it's committed to the inventory.
    /// <para>RULES: Only enemies tagged <see cref="ActorTag.Humanoid"/> can drop gear (a slime
    /// has no spaulders to leave behind). Beasts, undead, and constructs roll only the existing
    /// drop table. Drop chance and rarity ceiling scale loosely with enemy level.</para>
    /// <para>RELATED FILES: ActorInstance.cs (DieRoutine calls in), LootTracker.cs, ItemLibrary.cs.</para>
    /// </summary>
    public static class EnemyGearDropHelper
    {
        /// <summary>Per-enemy chance to drop a weapon (0..1).</summary>
        private const float WeaponDropChance = 0.25f;
        /// <summary>Per-enemy chance to drop an armor piece (0..1).</summary>
        private const float ArmorDropChance = 0.15f;

        // Pools keyed by max rarity tier (Common=0 ... Legendary=4). Drops scale with enemy level.
        private static readonly string[] WeaponPool =
        {
            "eq_sword_iron", "eq_dagger_bronze", "eq_bow_hunter", "eq_spear_serpent",
            "eq_sword_steel", "eq_axe_rune", "eq_hammer_war", "eq_mace_starfall",
            "eq_staff_mystic", "eq_wand_crystal", "eq_sword_shadow",
        };
        private static readonly string[] ArmorPool =
        {
            "eq_armor_chain", "eq_helm_iron", "eq_boots_leather", "eq_armor_plate",
        };

        /// <summary>Rolls bonus equipment drops for a humanoid enemy. No-op for non-humanoids.</summary>
        public static void RollFor(ActorInstance enemy)
        {
            if (enemy == null || !enemy.IsEnemy) return;
            var data = ActorLibrary.Get(enemy.characterClass);
            if (data == null) return;
            // Only humanoids carry equipment worth dropping.
            if ((data.Tags & ActorTag.Humanoid) == 0) return;

            int level = Mathf.Max(1, enemy.Stats != null ? Mathf.RoundToInt(enemy.Stats.Level) : 1);

            if (RNG.Float(0f, 1f) < WeaponDropChance)
            {
                var item = PickFromPool(WeaponPool, level);
                if (item != null) LootTracker.AddDrop(item.Id, 1);
            }
            if (RNG.Float(0f, 1f) < ArmorDropChance)
            {
                var item = PickFromPool(ArmorPool, level);
                if (item != null) LootTracker.AddDrop(item.Id, 1);
            }
        }

        /// <summary>Picks an item the enemy's level can plausibly drop. Common at every level,
        /// rarer pieces unlock as the enemy's level climbs (Uncommon @ 3, Rare @ 6, Epic @ 12,
        /// Legendary @ 20). Pool is narrowed before roll so low-level mobs can't gift legendary gear.</summary>
        private static ItemDefinition PickFromPool(string[] pool, int level)
        {
            int maxTier = level >= 20 ? 4
                       : level >= 12 ? 3
                       : level >= 6  ? 2
                       : level >= 3  ? 1
                       : 0;
            var candidates = new List<ItemDefinition>();
            foreach (var id in pool)
            {
                var def = ItemLibrary.Get(id);
                if (def == null) continue;
                if (RarityTier(def.Rarity) <= maxTier) candidates.Add(def);
            }
            if (candidates.Count == 0) return null;
            return candidates[RNG.Int(0, candidates.Count - 1)];
        }

        private static int RarityTier(ItemRarity r)
        {
            switch (r)
            {
                case ItemRarity.Common:    return 0;
                case ItemRarity.Uncommon:  return 1;
                case ItemRarity.Rare:      return 2;
                case ItemRarity.Epic:      return 3;
                case ItemRarity.Legendary: return 4;
                default: return 0;
            }
        }
    }
}
