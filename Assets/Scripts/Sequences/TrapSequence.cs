using Assets.Scripts.Libraries;
using System.Collections;
using UnityEngine;
using g = Assets.Helpers.GameHelper;

namespace Assets.Scripts.Sequences
{
    /// <summary>
    /// Plays a trap effect: fires a straight projectile to the target, spawns an explosion,
    /// and applies a looping status effect VFX on the target (rooted until after next turn).
    /// </summary>
    public class TrapSequence : SequenceEvent
    {
        private readonly Vector3 startPosition;
        private readonly ActorInstance target;

        public TrapSequence(Vector3 startPosition, ActorInstance target)
        {
            this.startPosition = startPosition;
            this.target = target;
        }

        public override IEnumerator ProcessRoutine()
        {
            if (target == null || !target.IsPlaying)
                yield break;

            // 1) Fire projectile (straight)
            var projectile = new ProjectileSettings
            {
                friendlyName = "Trap",
                startPosition = startPosition,
                target = target,
                projectileVfxKey = "Fireball", // choose a visible looping trail
                impactVfxKey = "PuffyExplosion", // impact
                motionStyle = MotionStyle.Straight,
                travelSeconds = 0.5f,
                arriveRadiusTiles = 0.1f
            };

            yield return new FireProjectileSequence(projectile).ProcessRoutine();

            // 2) Apply status effect VFX (loop) on the target
            var status = VisualEffectLibrary.Get("BlueGlow"); // looping
            var inst = g.VisualEffectManager.SpawnInstance(status, target.Position, target.transform);
            if (inst != null)
            {
                target.Flags.RootedVfxInstanceName = inst.name;
            }

            // 3) Root until after next turn (simple flag on ActorFlags or Movement)
            target.Flags.RootedTurnsRemaining = 1; // You must add this field on ActorFlags (int) if missing

            yield return null;
        }
    }
}
