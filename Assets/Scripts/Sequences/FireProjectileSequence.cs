using System.Collections;
using g = Assets.Helpers.GameHelper;

namespace Assets.Scripts.Sequences
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

        public override IEnumerator ProcessRoutine()
        {
            yield return g.ProjectileManager.SpawnRoutine(projectile);
        }
    }
}
