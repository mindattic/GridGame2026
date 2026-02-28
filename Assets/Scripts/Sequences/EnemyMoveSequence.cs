// --- File: Assets/Scripts/Events/Sequences/EnemyMoveSequence.cs ---
using System.Collections;
using g = Assets.Helpers.GameHelper;

namespace Assets.Scripts.Sequences
{
    /// <summary>
    /// ENEMYMOVESEQUENCE - Handles enemy movement toward targets.
    /// 
    /// PURPOSE:
    /// Executes the movement phase of an enemy's turn,
    /// moving them toward their chosen target.
    /// 
    /// SEQUENCE FLOW:
    /// 1. Validate enemy still in play
    /// 2. Check root status (skip if rooted)
    /// 3. Wait for movement intermission
    /// 4. Calculate attack strategy/path
    /// 5. Move toward destination
    /// 
    /// ROOT MECHANIC:
    /// If rooted, decrements root counter and skips movement.
    /// Despawns root VFX when effect expires.
    /// 
    /// RELATED FILES:
    /// - EnemyTakeTurnSequence.cs: Orchestrates turn
    /// - ActorMovement.cs: Movement logic
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
