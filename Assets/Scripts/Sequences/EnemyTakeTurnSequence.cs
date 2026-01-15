// --- File: Assets/Scripts/Events/Sequences/EnemyTakeTurnSequence.cs ---
using System.Collections;
using g = Assets.Helpers.GameHelper;

namespace Assets.Scripts.Sequences
{
    /// <summary>
    /// Executes one enemy's move/attack chain, resolves deaths, then ends the turn.
    /// </summary>
    public sealed class EnemyTakeTurnSequence : SequenceEvent
    {
        private readonly ActorInstance enemy;

        public EnemyTakeTurnSequence(ActorInstance enemy)
        {
            this.enemy = enemy;
        }

        public override IEnumerator ProcessRoutine()
        {
            UnityEngine.Debug.Log($"[EnemyTakeTurnSequence] ProcessRoutine started for {enemy?.name ?? "null"}");
            
            // If this enemy died/despawned before acting, just end turn.
            if (enemy == null || !enemy.IsPlaying)
            {
                g.SequenceManager.Add(new EndTurnSequence());
                g.SequenceManager.Execute();
                yield break;
            }

            // Small pacing
            yield return Wait.None();

            // Queue sequences: move once, attack once
            UnityEngine.Debug.Log($"[EnemyTakeTurnSequence] Adding attack sequence for {enemy.name}");
            g.SequenceManager.Add(new EnemyMoveSequence(enemy));
            g.SequenceManager.Add(new EnemyPreAttackSequence(enemy));
            g.SequenceManager.Add(new EnemyAttackSequence(enemy));
            g.SequenceManager.Add(new EnemyPostAttackSequence(enemy));
            g.SequenceManager.Add(new DeathSequence());
            g.SequenceManager.Add(new EndTurnSequence());
            g.SequenceManager.Execute();
        }
    }
}
