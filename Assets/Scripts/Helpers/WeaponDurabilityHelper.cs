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
    /// WEAPONDURABILITYHELPER - FireEmblem-style weapon attrition + Blacksmith repair pricing.
    /// <para>PURPOSE: A single point that tracks a wear-and-repair lifecycle:
    /// <list type="bullet">
    /// <item>Each successful hero attack ticks the equipped weapon down by one durability point.</item>
    /// <item>At 0 the weapon shatters and the slot empties — exactly like FE.</item>
    /// <item>The Blacksmith can restore durability for gold; each repair raises the next repair's cost
    /// until repairing exceeds the price of buying a fresh copy from the store / looting one.</item>
    /// </list></para>
    /// <para>RELATED FILES: HeroLoadout.cs, BlacksmithSection.cs, HeroPincerSequence.cs.</para>
    /// </summary>
    public static class WeaponDurabilityHelper
    {
        /// <summary>Logs a single hero swing against the equipped weapon's durability. Looks up the
        /// hero's saved loadout, decrements weapon durability by 1, and writes the change back to
        /// the save data so it survives the battle. Broken weapons are removed from the slot.</summary>
        public static void OnHeroAttacked(ActorInstance hero)
        {
            if (hero == null || hero.team != Team.Hero) return;
            var save = ProfileHelper.CurrentProfile?.CurrentSave;
            if (save?.Equipment?.Heroes == null) return;
            var heroSave = save.Equipment.GetOrCreate(hero.characterClass);
            if (string.IsNullOrEmpty(heroSave.WeaponId)) return;

            var weapon = ItemLibrary.Get(heroSave.WeaponId);
            if (weapon == null || weapon.Durability <= 0) return;

            int current = heroSave.WeaponDurability > 0 ? heroSave.WeaponDurability : weapon.Durability;
            current = Mathf.Max(0, current - 1);
            heroSave.WeaponDurability = current;

            if (current == 0)
            {
                // Snap the broken weapon out of the slot. Repair history dies with it.
                heroSave.WeaponId = null;
                heroSave.WeaponDurability = 0;
                heroSave.WeaponRepairCount = 0;
                BattleEventTracker.Record($"{hero.characterClass}'s {weapon.DisplayName} broke in battle!");
            }
        }

        // ---- Blacksmith pricing ----

        /// <summary>Gold cost to repair a piece from <paramref name="currentDurability"/> back to
        /// <paramref name="maxDurability"/>, given how many times it has already been repaired.
        /// <para>Shape: per-point repair cost starts at 30 % of the item's per-point manufacturing
        /// cost and multiplies by 1.6× per prior repair. After ~3 repairs the per-point cost crosses
        /// the per-point new-buy cost and a fourth repair is more expensive than a brand new copy.</para>
        /// </summary>
        public static int RepairCost(ItemDefinition item, int currentDurability, int repairCount)
        {
            if (item == null || item.Durability <= 0) return 0;
            int missing = Mathf.Max(0, item.Durability - currentDurability);
            if (missing == 0) return 0;
            float perPointBaseCost = (float)item.BaseCost / item.Durability;
            float multiplier = 0.30f * Mathf.Pow(1.6f, Mathf.Max(0, repairCount));
            float total = missing * perPointBaseCost * multiplier;
            return Mathf.Max(1, Mathf.CeilToInt(total));
        }

        /// <summary>True when repairing this item would cost more than buying a fresh copy.
        /// The UI uses this to nudge the player toward replacement.</summary>
        public static bool IsUneconomical(ItemDefinition item, int currentDurability, int repairCount)
            => RepairCost(item, currentDurability, repairCount) >= item.BaseCost;
    }
}
