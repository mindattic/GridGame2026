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
    /// TRAPSEQUENCE - Executes root/snare ability on target.
    /// 
    /// PURPOSE:
    /// Fires a projectile that roots the target, preventing
    /// movement for a number of turns.
    /// 
    /// SEQUENCE FLOW:
    /// 1. Fire projectile to target (straight motion)
    /// 2. Play impact explosion VFX
    /// 3. Spawn looping status VFX on target
    /// 4. Set RootedTurnsRemaining on target
    /// 5. Store VFX instance name for cleanup
    /// 
    /// ROOT MECHANIC:
    /// Rooted enemies skip their movement phase but can
    /// still attack if adjacent to heroes.
    /// 
    /// RELATED FILES:
    /// - FireProjectileSequence.cs: Projectile launch
    /// - ActorFlags.cs: Root status tracking
    /// - EnemyMoveSequence.cs: Checks root status
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
