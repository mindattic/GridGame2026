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
            if (attacker == null || !attacker.IsPlaying)
                yield break;

            yield return Wait.For(Intermission.Before.Enemy.Attack);

            var defendingHeroes = g.Actors.Heroes
                .Where(x => x.IsPlaying && Geometry.IsAdjacentTo(x.location, attacker.location))
                .ToList();

            if (defendingHeroes.Count == 0)
                yield break;

            // Attack each adjacent hero once
            foreach (var opponent in defendingHeroes)
            {
                if (!opponent.IsPlaying || opponent.IsDying || opponent.IsDead)
                    continue;

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
