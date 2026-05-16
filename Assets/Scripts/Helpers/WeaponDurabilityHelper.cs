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
    /// <summary>Result of a single swing's durability decrement. When <see cref="Shattered"/> is
    /// true the swing was the one that brought durability to 0; callers (HeroPincerSequence)
    /// apply <see cref="TargetBonusMultiplier"/> to the swing's damage and deal
    /// <see cref="WielderSelfDamage"/> to the wielder as the weapon shatters.</summary>
    public struct ShatterResult
    {
        public bool Shattered;
        /// <summary>Multiplier on the swing's target damage (1.0 = no bonus, 1.5 = +50% on shatter).</summary>
        public float TargetBonusMultiplier;
        /// <summary>HP damage dealt to the wielder by the shatter itself (separate from any return strike).</summary>
        public int WielderSelfDamage;
        /// <summary>The weapon that just shattered (null if no shatter).</summary>
        public ItemDefinition ShatteredWeapon;

        public static ShatterResult None => new ShatterResult { TargetBonusMultiplier = 1f };
    }

    public static class WeaponDurabilityHelper
    {
        /// <summary>Target damage multiplier on the shatter swing — TBD per tuning, currently +50%.</summary>
        public const float ShatterTargetBonusMultiplier = 1.5f;

        /// <summary>Wielder self-damage as fraction of MaxHP on shatter — TBD per tuning, currently 15%.</summary>
        public const float ShatterWielderSelfDamageFraction = 0.15f;

        /// <summary>Logs a single hero swing against the equipped weapon's durability. Looks up the
        /// hero's saved loadout, decrements weapon durability by 1, and writes the change back to
        /// the save data so it survives the battle. When the swing brings durability to 0 the
        /// weapon shatters: returned <see cref="ShatterResult"/> tells callers to apply a target
        /// bonus + wielder self-damage on this same swing.</summary>
        public static ShatterResult OnHeroAttacked(ActorInstance hero)
        {
            if (hero == null || hero.team != Team.Hero) return ShatterResult.None;
            var save = ProfileHelper.CurrentProfile?.CurrentSave;
            if (save?.Equipment?.Heroes == null) return ShatterResult.None;
            var heroSave = save.Equipment.GetOrCreate(hero.characterClass);
            if (string.IsNullOrEmpty(heroSave.WeaponId)) return ShatterResult.None;

            var weapon = ItemLibrary.Get(heroSave.WeaponId);
            if (weapon == null || weapon.Durability <= 0) return ShatterResult.None;

            int current = heroSave.WeaponDurability > 0 ? heroSave.WeaponDurability : weapon.Durability;
            current = Mathf.Max(0, current - 1);
            heroSave.WeaponDurability = current;

            if (current == 0)
            {
                // Compute wielder self-damage BEFORE we lose the reference to the weapon name.
                int wielderSelfDamage = Mathf.Max(1, Mathf.RoundToInt(hero.Stats.MaxHP * ShatterWielderSelfDamageFraction));

                // Snap the broken weapon out of the slot. Repair history dies with it.
                heroSave.WeaponId = null;
                heroSave.WeaponDurability = 0;
                heroSave.WeaponRepairCount = 0;
                BattleEventTracker.Record($"{hero.characterClass}'s {weapon.DisplayName} shattered in battle!");

                return new ShatterResult
                {
                    Shattered = true,
                    TargetBonusMultiplier = ShatterTargetBonusMultiplier,
                    WielderSelfDamage = wielderSelfDamage,
                    ShatteredWeapon = weapon,
                };
            }

            return ShatterResult.None;
        }

        /// <summary>Effective max durability for a weapon, given how many times it has been
        /// repaired. Per the design rule, each repair shaves 1 off the ceiling so weapons
        /// eventually retire instead of being repaired forever.</summary>
        public static int EffectiveMaxDurability(ItemDefinition item, int repairCount)
        {
            if (item == null || item.Durability <= 0) return 0;
            return Mathf.Max(1, item.Durability - Mathf.Max(0, repairCount));
        }

        // ---- Blacksmith pricing ----

        /// <summary>Gold cost to repair a piece from <paramref name="currentDurability"/> back up to
        /// its effective max (factory max minus prior repairs — see <see cref="EffectiveMaxDurability"/>).
        /// <para>Shape: per-point repair cost starts at 30 % of the item's per-point manufacturing
        /// cost and multiplies by 1.6× per prior repair. After ~3 repairs the per-point cost crosses
        /// the per-point new-buy cost and a fourth repair is more expensive than a brand new copy.
        /// Combined with the shrinking ceiling, the weapon naturally retires.</para>
        /// </summary>
        public static int RepairCost(ItemDefinition item, int currentDurability, int repairCount)
        {
            if (item == null || item.Durability <= 0) return 0;
            int effectiveMax = EffectiveMaxDurability(item, repairCount);
            int missing = Mathf.Max(0, effectiveMax - currentDurability);
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
