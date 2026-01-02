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
    /// Flow:
    /// 1) Wait pre-attack intermission.
    /// 2) Find adjacent defenders.
    /// 3) Determine attack count based on passive abilities (DoubleAttack, TripleAttack).
    /// 4) For each defender, run bump animation with an impact routine.
    /// 5) Check for CounterAttack reactive abilities on defenders.
    /// 6) Reset the attacker's action bar.
    /// </summary>
    public class EnemyAttackSequence : SequenceEvent
    {
        private readonly ActorInstance attacker;

        public EnemyAttackSequence(ActorInstance enemy)
        {
            attacker = enemy;
        }

        /// <summary>
        /// Gets the number of attacks this actor should perform based on passive abilities.
        /// </summary>
        private int GetAttackCount()
        {
            int baseAttacks = 1;
            int extraAttacks = 0;
            
            foreach (var ability in attacker.Abilities)
            {
                if (ability.IsPassive && ability.ExtraAttacks > 0)
                {
                    extraAttacks += ability.ExtraAttacks;
                }
            }
            
            return baseAttacks + extraAttacks;
        }

        /// <summary>
        /// Checks if an actor has the CounterAttack reactive ability.
        /// </summary>
        private bool HasCounterAttack(ActorInstance actor)
        {
            foreach (var ability in actor.Abilities)
            {
                if (ability.IsReactive && ability.Effect == AbilityEffect.CounterAttack)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Orchestrates enemy attack against adjacent heroes.
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

            int attackCount = GetAttackCount();
            
            // Track heroes that were attacked and survived for counter-attacks
            var heroesToCounter = new List<ActorInstance>();

            // Perform attack sequence for each attack (based on passive abilities)
            for (int attackIndex = 0; attackIndex < attackCount; attackIndex++)
            {
                foreach (var opponent in defendingHeroes)
                {
                    // Skip if opponent died during this attack sequence
                    if (!opponent.IsPlaying || opponent.IsDying || opponent.IsDead)
                        continue;

                    var attackResult = Formulas.CalculateAttackResult(attacker, opponent);

                    if (attackResult == null || attackResult.Opponent == null || 
                        attackResult.Opponent.IsDying || attackResult.Opponent.IsDead)
                        continue;

                    var singleAttack = AttackHelper.SingleAttackRoutine(attackResult);
                    yield return attacker.Animation.BumpRoutine(opponent, singleAttack);

                    // Track for counter-attack if they survived and have the ability
                    if (opponent.IsPlaying && !opponent.IsDying && !opponent.IsDead && HasCounterAttack(opponent))
                    {
                        if (!heroesToCounter.Contains(opponent))
                            heroesToCounter.Add(opponent);
                    }
                }

                // Small delay between multi-attacks for visual clarity
                if (attackIndex < attackCount - 1)
                    yield return Wait.For(Interval.TenthSecond);
            }

            // Process counter-attacks from heroes with CounterAttack ability
            foreach (var hero in heroesToCounter)
            {
                if (hero.IsPlaying && !hero.IsDying && !hero.IsDead &&
                    attacker.IsPlaying && !attacker.IsDying && !attacker.IsDead)
                {
                    yield return Wait.For(Interval.TenthSecond);
                    
                    var counterSequence = new CounterAttackSequence(hero, attacker);
                    yield return counterSequence.ProcessRoutine();
                }
            }

            attacker.ActionBar.Reset();
        }
    }
}
