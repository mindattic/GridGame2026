// --- File: Assets/Scripts/Events/Sequences/EnemyMoveSequence.cs ---
using System.Collections;
using g = Assets.Helpers.GameHelper;

namespace Assets.Scripts.Sequences
{
    /// <summary>
    /// Moves one attacker toward its target.
    /// Does not end the turn or schedule other enemies.
    /// </summary>
    public class EnemyMoveSequence : SequenceEvent
    {
        private readonly ActorInstance enemy;

        public EnemyMoveSequence(ActorInstance enemy)
        {
            this.enemy = enemy;
        }

        public override IEnumerator ProcessRoutine()
        {
            // Safety: null or not in play should quietly skip.
            if (enemy == null || !enemy.IsPlaying)
                yield break;

            // Root gating: on enemy turn, consume one root turn and skip movement if rooted
            if (enemy.Flags.RootedTurnsRemaining > 0)
            {
                enemy.Flags.RootedTurnsRemaining = System.Math.Max(0, enemy.Flags.RootedTurnsRemaining - 1);

                // If root just ended, despawn the looping VFX if present
                if (enemy.Flags.RootedTurnsRemaining == 0 && !string.IsNullOrEmpty(enemy.Flags.RootedVfxInstanceName))
                {
                    g.VisualEffectManager?.Despawn(enemy.Flags.RootedVfxInstanceName);
                    enemy.Flags.RootedVfxInstanceName = null;
                }

                // Skip movement this turn while rooted
                yield break;
            }

            // Optional pacing before movement.
            yield return Wait.For(Intermission.Before.Enemy.Move);

            // Decide path and Move toward destination.
            enemy.CalculateAttackStrategy();
            yield return enemy.Move.TowardDestinationRoutine();

            // No chaining here. EnemyStartSequence enqueued the follow-up attack explicitly.
        }
    }
}
