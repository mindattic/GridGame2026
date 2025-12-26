// --- File: Assets/Scripts/Events/EnemyStartSequence.cs ---
using System.Collections;
using g = Assets.Helpers.GameHelper;

namespace Assets.Scripts.Sequences
{
    /// <summary>
    /// Starts the enemy turn using the Timeline as source of truth.
    /// Centers on the acting enemy from the current timeline block,
    /// runs its move/attack chain, then queues turn end.
    /// </summary>
    public class EnemyStartSequence : SequenceEvent
    {
        public override IEnumerator ProcessRoutine()
        {
            if (!g.TurnManager.IsEnemyTurn)
                yield break;

            g.InputManager.InputMode = InputMode.None;

            var actingEnemy = g.TurnManager.ActiveActor;
            if (actingEnemy == null || !actingEnemy.IsPlaying)
            {
                g.SequenceManager.Add(new EndTurnSequence());
                g.SequenceManager.Execute();
                yield break;
            }

            g.SequenceManager.Add(new EnemyMoveSequence(actingEnemy));
            g.SequenceManager.Add(new EnemyPreAttackSequence(actingEnemy));
            g.SequenceManager.Add(new EnemyAttackSequence(actingEnemy));
            g.SequenceManager.Add(new EnemyPostAttackSequence(actingEnemy));
            g.SequenceManager.Add(new DeathSequence());
            g.SequenceManager.Add(new EndTurnSequence());
            g.SequenceManager.Execute();
        }
    }
}
