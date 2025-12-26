// --- File: Assets/Scripts/Events/Sequences/EnemyPostAttackSequence.cs ---
using System.Collections;

namespace Assets.Scripts.Sequences
{
    /// <summary>
    /// EnemyPostAttackSequence
    /// Purpose:
    ///   Runs immediately after a single attacker finishes its attack.
    ///   Designed as a hook for post-attack effects or cleanup.
    ///
    /// Behavior:
    ///   1) Performs optional pacing to let visuals settle.
    ///   2) Applies any status effects that routine after attacking.
    ///   3) Does not schedule other enemies or end the turn.
    ///
    /// Safety:
    ///   Skips quietly if the actor reference is null or no longer playing.
    /// </summary>
    public class EnemyPostAttackSequence : SequenceEvent
    {
        private readonly ActorInstance enemy; // Enemy that just attacked.

        /// <summary>
        /// Creates a new post-attack sequence for a specific attacker.
        /// </summary>
        public EnemyPostAttackSequence(ActorInstance enemy)
        {
            this.enemy = enemy;
        }

        /// <summary>
        /// Executes any post-attack effects for the given attacker.
        /// </summary>
        public override IEnumerator ProcessRoutine()
        {
            // Safety check: null or inactive attacker should skip this step.
            if (enemy == null || !enemy.IsPlaying)
                yield break;

            // Optional: short pacing after the attack animation.
            yield return Wait.None();

            // Placeholder for future: apply poison, lifesteal, debuffs, or cleanup here.

            yield break;
        }
    }
}
