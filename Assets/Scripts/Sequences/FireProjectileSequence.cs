using System.Collections;
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
    /// FIREPROJECTILESEQUENCE - Spawns and fires a projectile.
    /// 
    /// PURPOSE:
    /// Generic sequence wrapper for spawning projectiles via
    /// ProjectileManager. Used by abilities that launch projectiles.
    /// 
    /// USAGE:
    /// Pass ProjectileSettings to configure:
    /// - Start position, target
    /// - Motion style (straight, arc, wiggle)
    /// - VFX keys for trail and impact
    /// - Travel duration
    /// 
    /// RELATED FILES:
    /// - ProjectileManager.cs: Spawns projectiles
    /// - ProjectileSettings.cs: Configuration data
    /// - ProjectileInstance.cs: Projectile behavior
    /// </summary>
    public class FireProjectileSequence : SequenceEvent
    {
        private ProjectileSettings projectile;

        public FireProjectileSequence(ProjectileSettings projectile)
        {
            this.projectile = projectile;
        }

        /// <summary>Coroutine that executes the process sequence.</summary>
        public override IEnumerator ProcessRoutine()
        {
            yield return g.ProjectileManager.SpawnRoutine(projectile);
        }
    }
}
