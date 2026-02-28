using Assets.Helpers;
using System.Collections;

namespace Assets.Scripts.Sequences
{
    /// <summary>
    /// DEATHSEQUENCE - Processes actor deaths after combat.
    /// 
    /// PURPOSE:
    /// Finds all actors with HP <= 0 and processes their death animations,
    /// removes them from the battlefield, and awards XP/coins.
    /// 
    /// TIMING:
    /// Queued AFTER attack sequences to handle deaths that occurred
    /// during combat resolution. Multiple attacks may have reduced
    /// multiple actors to 0 HP before this runs.
    /// 
    /// DEATH PROCESSING (via DeathHelper.ProcessRoutine):
    /// 1. Find all IsDying actors (IsActive && HP <= 0)
    /// 2. Play death animation for each
    /// 3. Spawn death VFX
    /// 4. Award XP to party (if enemy)
    /// 5. Spawn coin drops (if enemy)
    /// 6. Deactivate/destroy actor
    /// 7. Update timeline (remove enemy tags)
    /// 
    /// SEQUENCE CHAIN:
    /// Typically appears in sequence chains like:
    /// - PincerAttackSequence
    /// - DeathSequence ← Here
    /// - WaveCheckSequence
    /// - EndTurnSequence
    /// 
    /// RELATED FILES:
    /// - DeathHelper.cs: Contains actual death processing logic
    /// - PincerAttackSequence.cs: Deals damage before this
    /// - EnemyAttackSequence.cs: Enemy attacks before this
    /// - ExperienceTracker.cs: XP award system
    /// - CoinManager.cs: Coin spawning
    /// </summary>
    public class DeathSequence : SequenceEvent
    {
        /// <summary>
        /// Processes all dying actors via DeathHelper.
        /// </summary>
        public override IEnumerator ProcessRoutine()
        {
            yield return DeathHelper.ProcessRoutine();
        }
    }
}
