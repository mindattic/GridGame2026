// File: Assets/Scripts/Events/HealAbilitySequence.cs
using System.Collections;
using UnityEngine;
using g = Assets.Helpers.GameHelper;

namespace Assets.Scripts.Sequences
{
    /// <summary>
    /// HEALABILITYSEQUENCE - Executes a healing ability on a target.
    /// 
    /// PURPOSE:
    /// Handles the visual and mechanical effects of a heal ability,
    /// including projectile animation and HP restoration.
    /// 
    /// SEQUENCE FLOW:
    /// 1. Lock input
    /// 2. Bounce caster portrait
    /// 3. Spawn healing projectile (wiggle motion)
    /// 4. Projectile travels to target
    /// 5. Play impact VFX
    /// 6. Restore target HP
    /// 
    /// RELATED FILES:
    /// - ProjectileManager.cs: Projectile spawning
    /// - ActorInstance.HealRoutine(): HP restoration
    /// - AbilityManager.cs: Ability execution
    /// </summary>
    public class HealAbilitySequence : SequenceEvent
    {
        private readonly Vector3 startPosition;
        private readonly ActorInstance target;

        public HealAbilitySequence(Vector3 startPosition, ActorInstance targetActor)
        {
            this.startPosition = startPosition;
            this.target = targetActor;
        }

        /// <summary>
        /// Executes the heal ability using the new ProjectileEngine.
        /// Spawns one looping trail that travels from start to target,
        /// then spawns a single impact VFX and yields the heal routine.
        /// </summary>
        public override IEnumerator ProcessRoutine()
        {
            g.InputManager.InputMode = InputMode.None;
            g.Card.BouncePortrait();

            var healSettings = new ProjectileSettings
            {
                friendlyName = "Heal",
                startPosition = startPosition,
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
                routine = target != null ? target.HealRoutine(10) : null
            };

            // Yield the projectile sequence which internally calls ProjectileManager.SpawnRoutine
            yield return new FireProjectileSequence(healSettings).ProcessRoutine();

            // Optional: restore input here if your flow requires it
            // g.InputManager.InputMode = InputMode.Default;
        }
    }
}
