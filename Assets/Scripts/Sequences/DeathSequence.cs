using Scripts.Helpers;
using System.Collections;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Factories;
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
