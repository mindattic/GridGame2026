using Scripts.Libraries;
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
using Scripts.Managers;
using Scripts.Models;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Sequences
{
    /// <summary>
    /// SMITESEQUENCE - Executes instant holy damage ability.
    /// 
    /// PURPOSE:
    /// Plays a holy explosion VFX directly on target (no projectile)
    /// and applies damage. Instant effect ability.
    /// 
    /// SEQUENCE FLOW:
    /// 1. Validate target alive
    /// 2. Play LightningExplosion VFX at target
    /// 3. Wait for VFX completion
    /// 4. Show "Smite" combat text
    /// 5. Apply damage (placeholder)
    /// 
    /// RELATED FILES:
    /// - VisualEffectManager.cs: VFX playback
    /// - AbilityManager.cs: Ability execution
    /// </summary>
    public class SmiteSequence : SequenceEvent
    {
        private readonly ActorInstance target;

        public SmiteSequence(ActorInstance target)
        {
            this.target = target;
        }

        /// <summary>Coroutine that executes the process sequence.</summary>
        public override IEnumerator ProcessRoutine()
        {
            if (target == null || !target.IsPlaying)
                yield break;

            // Choose a bright explosion-like effect from VfxLibrary. "LightningExplosion" is suitable.
            var vfx = VisualEffectLibrary.Get("LightningExplosion");
            if (vfx != null)
            {
                // Play and wait for completion before continuing
                yield return g.VisualEffectManager.PlayRoutine(vfx, target.Position);
            }

            // Optional: show holy themed combat text
            g.CombatTextManager.Spawn("Smite", target.Position, "Damage");

            // Placeholder: if you want real damage, wire AttackResult and call target.Damage()
            yield return null;
        }
    }
}
