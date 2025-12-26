using Assets.Scripts.Libraries;
using System.Collections;
using UnityEngine;
using g = Assets.Helpers.GameHelper;

namespace Assets.Scripts.Sequences
{
    /// <summary>
    /// Instantly plays a holy explosion VFX on the target (no projectile), then applies damage or feedback.
    /// Waits for the VFX impact duration before completing.
    /// </summary>
    public class SmiteSequence : SequenceEvent
    {
        private readonly ActorInstance target;

        public SmiteSequence(ActorInstance target)
        {
            this.target = target;
        }

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
