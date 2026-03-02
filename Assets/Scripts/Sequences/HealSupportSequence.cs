// File: Assets/Scripts/Events/HealSupportSequence.cs
using System.Collections;
using UnityEngine;
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
    /// HEALSUPPORTSEQUENCE - Support heal from ally to target.
    /// 
    /// PURPOSE:
    /// Executes a healing projectile from a support ally to
    /// the target hero during pincer support phase.
    /// 
    /// SEQUENCE FLOW:
    /// 1. Launch wiggle-motion projectile from source
    /// 2. Projectile travels to target
    /// 3. Play impact VFX
    /// 4. Execute heal routine on target
    /// 
    /// RELATED FILES:
    /// - HeroPincerSequence.cs: Queues support sequences
    /// - ProjectileManager.cs: Projectile spawning
    /// - ActorInstance.HealRoutine(): HP restoration
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

        public override IEnumerator ProcessRoutine()
        {
            if (target == null)
                yield break;

            var healSettings = new ProjectileSettings
            {
                friendlyName = "Heal",
                startPosition = source,
                target = target,

                projectileVfxKey = "GreenSparkle",
                impactVfxKey = "BuffLife",

                motionStyle = MotionStyle.Wiggle,
                travelSeconds = 0.9f,
                wiggleAmplitudeTiles = 0.35f,
                wiggleHz = 3.5f,
                arriveRadiusTiles = 0.1f,

                routine = target.HealRoutine(10)
            };

            // Launch and wait for completion
            yield return new FireProjectileSequence(healSettings).ProcessRoutine();
        }
    }
}
