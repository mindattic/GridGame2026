// File: Assets/Scripts/Events/HealSupportSequence.cs
using System.Collections;
using UnityEngine;

namespace Assets.Scripts.Sequences
{
    /// <summary>
    /// Runs a support heal: launches a wiggle style projectile from a source point to the target,
    /// plays the impact VFX, then yields the heal routine.
    /// </summary>
    public class HealSupportSequence : SequenceEvent
    {
        private readonly Vector3 source;
        private readonly ActorInstance target;

        public HealSupportSequence(Vector3 source, ActorInstance target)
        {
            this.source = source;
            this.target = target;
        }

        /// <summary>
        /// Executes the heal using the new ProjectileEngine.
        /// Spawns a single looping trail, travels to the target, then spawns a single impact VFX.
        /// </summary>
        public override IEnumerator ProcessRoutine()
        {
            if (target == null)
                yield break;

            var healSettings = new ProjectileSettings
            {
                friendlyName = "Heal",
                startPosition = source,
                target = target,

                // Visuals
                projectileVfxKey = "GreenSparkle",
                impactVfxKey = "BuffLife",

                // Motion
                motionStyle = MotionStyle.Wiggle,
                travelSeconds = 0.9f,
                wiggleAmplitudeTiles = 0.35f,
                wiggleHz = 3.5f,
                arriveRadiusTiles = 0.1f,

                // Post impact
                routine = target.HealRoutine(10)
            };

            // Launch and wait for completion
            yield return new FireProjectileSequence(healSettings).ProcessRoutine();
        }
    }
}
