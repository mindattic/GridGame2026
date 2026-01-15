// --- File: Assets/Scripts/Events/Sequences/EnemyAttackSequence.cs ---
using Assets.Helpers;
using Assets.Scripts.Models;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using g = Assets.Helpers.GameHelper;

namespace Assets.Scripts.Sequences
{
    /// <summary>
    /// Executes a single enemy attack turn for one attacker.
    /// </summary>
    public class EnemyAttackSequence : SequenceEvent
    {
        private readonly ActorInstance attacker;

        public EnemyAttackSequence(ActorInstance enemy)
        {
            attacker = enemy;
        }

        /// <summary>
        /// Orchestrates enemy attack against adjacent heroes.
        /// Attacks each adjacent hero exactly once.
        /// </summary>
        public override IEnumerator ProcessRoutine()
        {
            UnityEngine.Debug.Log($"[EnemyAttackSequence] ProcessRoutine started for {attacker?.name ?? "null"}");
            
            if (attacker == null || !attacker.IsPlaying)
                yield break;

            yield return Wait.For(Intermission.Before.Enemy.Attack);

            var defendingHeroes = g.Actors.Heroes
                .Where(x => x.IsPlaying && Geometry.IsAdjacentTo(x.location, attacker.location))
                .ToList();

            if (defendingHeroes.Count == 0)
                yield break;

            // Attack only the first adjacent hero, then end attack phase
            var opponent = defendingHeroes.First();
            
            if (opponent.IsPlaying && !opponent.IsDying && !opponent.IsDead)
            {
                UnityEngine.Debug.Log($"[EnemyAttackSequence] {attacker.name} attacking {opponent.name} NOW");
                var attackResult = Formulas.CalculateAttackResult(attacker, opponent);

                if (attackResult != null && attackResult.Opponent != null && 
                    !attackResult.Opponent.IsDying && !attackResult.Opponent.IsDead)
                {
                    var singleAttack = AttackHelper.SingleAttackRoutine(attackResult);
                    yield return attacker.Animation.BumpRoutine(opponent, singleAttack);
                }
            }

            attacker.ActionBar.Reset();
        }
    }
}
