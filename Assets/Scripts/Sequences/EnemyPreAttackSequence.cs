// --- File: Assets/Scripts/Events/Sequences/EnemyPreAttackSequence.cs ---
using System.Collections;

namespace Assets.Scripts.Sequences
{
    /// <summary>
    /// EnemyPreAttackSequence
    /// Purpose:
    ///   Runs immediately before a single attacker's attack.
    ///   Designed as a hook for buffs, debuffs, or pre-attack effects.
    ///
    /// Behavior:
    ///   1) Performs optional pacing or anticipation visuals.
    ///   2) Applies any status effects that routine before attacking.
    ///   3) Does not schedule other enemies or end the turn.
    ///
    /// Safety:
    ///   Skips quietly if the actor reference is null or no longer playing.
    /// </summary>
    public class EnemyPreAttackSequence : SequenceEvent
    {
        private readonly ActorInstance enemy; // Enemy preparing to attack.

        /// <summary>
        /// Creates a new pre-attack sequence for a specific attacker.
        /// </summary>
        public EnemyPreAttackSequence(ActorInstance enemy)
        {
            this.enemy = enemy;
        }

        /// <summary>
        /// Executes any pre-attack effects for the given attacker.
        /// </summary>
        public override IEnumerator ProcessRoutine()
        {
            // Safety check: null or inactive attacker should skip this step.
            if (enemy == null || !enemy.IsPlaying)
                yield break;

            // Optional: short pacing or anticipation before the attack.
            yield return Wait.None();

            // Placeholder for future: apply buffs, debuffs here.

            yield break;
        }
    }
}
