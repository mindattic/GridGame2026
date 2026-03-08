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
    /// USEITEMSEQUENCE - Executes a consumable item usage in battle.
    ///
    /// PURPOSE:
    /// Handles the visual and mechanical effects of using a consumable
    /// item during combat. The item is consumed from inventory after use.
    /// After the sequence completes, automatically advances to the next
    /// enemy turn (costs the hero's turn).
    ///
    /// SEQUENCE FLOW:
    /// 1. Lock input
    /// 2. Show ability bar announcement ("{Hero} uses {Item}")
    /// 3. Bounce user portrait
    /// 4. Spawn item projectile (wiggle motion)
    /// 5. Projectile travels to target
    /// 6. Play impact VFX
    /// 7. Apply item effect (heal, buff, etc.)
    /// 8. Show combat text
    /// 9. Consume item from inventory
    /// 10. Advance to next turn
    ///
    /// RELATED FILES:
    /// - HealAbilitySequence.cs: Similar pattern for heal spell
    /// - FireProjectileSequence.cs: Projectile spawning
    /// - AbilityBar.cs: Announcement display
    /// - PlayerInventory.cs: Item consumption
    /// </summary>
    public class UseItemSequence : SequenceEvent
    {
        private readonly ActorInstance user;
        private readonly ActorInstance target;
        private readonly ItemDefinition item;

        public UseItemSequence(ActorInstance user, ActorInstance target, ItemDefinition item)
        {
            this.user = user;
            this.target = target;
            this.item = item;
        }

        /// <summary>
        /// Executes the item usage sequence with VFX, combat text, and item consumption.
        /// </summary>
        public override IEnumerator ProcessRoutine()
        {
            if (user == null || !user.IsPlaying || item == null)
                yield break;

            // Lock input during sequence
            g.InputManager.InputMode = InputMode.None;

            // Announce the item usage on the ability bar
            g.AbilityBar?.Show($"{user.characterClass} uses {item.DisplayName}");

            // Bounce the user portrait for visual feedback
            g.Card?.BouncePortrait();

            // Determine the effective target (self if no target specified)
            var effectTarget = target != null && target.IsPlaying ? target : user;

            // Apply item effect based on type
            if (item.BaseHealing > 0)
            {
                // Healing item: projectile + heal
                var healSettings = new ProjectileSettings
                {
                    friendlyName = item.DisplayName,
                    startPosition = user.Position,
                    target = effectTarget,
                    projectileVfxKey = "GreenSparkle",
                    impactVfxKey = "BuffLife",
                    motionStyle = MotionStyle.Wiggle,
                    travelSeconds = 0.7f,
                    wiggleAmplitudeTiles = 0.3f,
                    wiggleHz = 3f,
                    arriveRadiusTiles = 0.1f,
                    routine = effectTarget.HealRoutine(item.BaseHealing)
                };

                yield return new FireProjectileSequence(healSettings).ProcessRoutine();
            }
            else
            {
                // Generic item: just show combat text
                if (g.CombatTextManager != null)
                    g.CombatTextManager.Spawn(item.DisplayName, effectTarget.Position, "Heal");
                yield return new WaitForSeconds(0.5f);
            }

            // Consume the item from inventory
            var save = ProfileHelper.CurrentProfile?.CurrentSave;
            if (save?.Inventory != null)
            {
                var inventory = new PlayerInventory();
                inventory.LoadFromSaveData(save.Inventory);
                inventory.Remove(item.Id, 1);
                save.Inventory = inventory.ToSaveData();
            }

            // Small delay before turn advances
            yield return new WaitForSeconds(0.3f);

            // Restore input and advance turn (item usage costs the hero's turn)
            g.InputManager.InputMode = InputMode.PlayerTurn;
        }
    }
}
