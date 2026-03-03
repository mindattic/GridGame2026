// --- File: Assets/Scripts/Sequences/EnemyTakeTurnSequence.cs ---
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
    /// ENEMYTAKETURNSEQUENCE - Orchestrates a single enemy's turn.
    /// 
    /// Executes the full turn sequence for one enemy actor:
    /// 1. EnemyMoveSequence: Enemy moves toward target
    /// 2. EnemyPreAttackSequence: Attack windup
    /// 3. EnemyAttackSequence: Damage dealing
    /// 4. EnemyPostAttackSequence: Recovery
    /// 5. DeathSequence: Handle deaths
    /// 6. EndTurnSequence: Advance turn
    /// 
    /// Called by TurnManager when enemy timeline tag reaches trigger.
    /// </summary>
    public sealed class EnemyTakeTurnSequence : SequenceEvent
    {
        private readonly ActorInstance enemy;

        public EnemyTakeTurnSequence(ActorInstance enemy)
        {
            this.enemy = enemy;
        }

        /// <summary>Coroutine that executes the process sequence.</summary>
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
