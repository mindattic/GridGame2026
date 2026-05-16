using System.Collections;
using UnityEngine;
using g = Scripts.Helpers.GameHelper;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Items;
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
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Sequences
{
    /// <summary>
    /// CHANGEEQUIPPEDWEAPONSEQUENCE - Swaps a weapon currently on the wielder's ability bar
    /// into their equipped slot, and pushes the previously equipped weapon back onto the bar.
    /// <para>FLOW:
    /// <list type="number">
    /// <item>Resolve current loadout from save data.</item>
    /// <item>Find the bar slot whose WeaponId matches the swap target.</item>
    /// <item>Atomically: bar slot's WeaponId ↔ HeroEquipmentSave.WeaponId.</item>
    /// <item>Reset incoming weapon's durability to its factory max and clear its RepairCount —
    /// per-instance state isn't preserved across swaps in v1 (see KNOWN LIMITATIONS).</item>
    /// <item>Show "Equipping {Name}" on the top-center ActionTitle banner.</item>
    /// <item>Persist save.</item>
    /// </list></para>
    /// <para>KNOWN LIMITATIONS (v1):</para>
    /// <para>Bar slots store only a weapon ID, not its per-instance durability or repair count.
    /// When the player swaps, both weapons reset to factory-max durability with 0 repairs. To
    /// preserve durability/repair state across swaps, add Durability + RepairCount fields to
    /// AbilityBarSlotSave and update this sequence to copy them through.</para>
    /// <para>RELATED FILES: AbilityBarSlotSave (Profile.cs), AbilityLibrary.FromWeapon,
    /// HeroLoadout.LoadFromSave / ToSave, AbilityManager (Cast switch), ActionTitle.cs</para>
    /// </summary>
    public class ChangeEquippedWeaponSequence : SequenceEvent
    {
        private readonly ActorInstance user;
        private readonly ItemDefinition barWeapon;

        public ChangeEquippedWeaponSequence(ActorInstance user, ItemDefinition barWeapon)
        {
            this.user = user;
            this.barWeapon = barWeapon;
        }

        public override IEnumerator ProcessRoutine()
        {
            if (user == null || !user.IsPlaying || barWeapon == null) yield break;
            var save = ProfileHelper.CurrentProfile?.CurrentSave;
            if (save?.Equipment == null) yield break;

            var heroSave = save.Equipment.GetOrCreate(user.characterClass);

            // Capture the currently equipped weapon ID — it will move into the bar slot.
            string previousWeaponId = heroSave.WeaponId;

            // Find the matching bar slot. AbilityBar slots are persisted on HeroEquipmentSave.
            int slotIndex = -1;
            if (heroSave.AbilityBarSlots != null)
            {
                for (int i = 0; i < heroSave.AbilityBarSlots.Count; i++)
                {
                    var slot = heroSave.AbilityBarSlots[i];
                    if (slot != null && slot.IsWeapon && slot.WeaponId == barWeapon.Id)
                    {
                        slotIndex = i;
                        break;
                    }
                }
            }
            if (slotIndex < 0)
            {
                Debug.LogWarning($"[ChangeEquippedWeapon] No bar slot found for '{barWeapon.Id}' on {user.characterClass}.");
                yield break;
            }

            // Atomic swap: bar slot gets the old equipped weapon, equipped slot gets the bar weapon.
            heroSave.AbilityBarSlots[slotIndex] = string.IsNullOrEmpty(previousWeaponId)
                ? new AbilityBarSlotSave()                   // bar slot becomes empty if nothing was equipped
                : AbilityBarSlotSave.ForWeapon(previousWeaponId);

            heroSave.WeaponId = barWeapon.Id;
            heroSave.WeaponDurability = barWeapon.Durability;  // v1: incoming weapon is at factory max
            heroSave.WeaponRepairCount = 0;                    // v1: repair history resets across swaps

            // Announce on the top banner.
            g.ActionTitle?.Equip(barWeapon);

            // Persist immediately so the change survives if the player leaves the scene mid-turn.
            ProfileHelper.Save(overwrite: true);

            // Brief pause so the banner reads before the next turn fires.
            yield return Wait.For(0.5f);
        }
    }
}
