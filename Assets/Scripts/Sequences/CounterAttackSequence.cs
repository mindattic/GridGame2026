// --- File: Assets/Scripts/Sequences/CounterAttackSequence.cs ---
using Assets.Helpers;
using Assets.Scripts.Models;
using System.Collections;
using g = Assets.Helpers.GameHelper;

namespace Assets.Scripts.Sequences
{
    /// <summary>
    /// Executes a counter-attack from a defender who has the CounterAttack ability.
    /// Triggered automatically when the defender is attacked and survives.
    /// </summary>
    public class CounterAttackSequence : SequenceEvent
    {
        private readonly ActorInstance defender;
        private readonly ActorInstance attacker;

        public CounterAttackSequence(ActorInstance defender, ActorInstance attacker)
        {
            this.defender = defender;
            this.attacker = attacker;
        }

        public override IEnumerator ProcessRoutine()
        {
            // Guard: both actors must still be playing
            if (defender == null || !defender.IsPlaying || defender.IsDying || defender.IsDead)
                yield break;
            if (attacker == null || !attacker.IsPlaying || attacker.IsDying || attacker.IsDead)
                yield break;

            // Guard: must be adjacent to counter-attack
            if (!Geometry.IsAdjacentTo(defender.location, attacker.location))
                yield break;

            // Visual feedback
            g.CombatTextManager.Spawn("Counter!", defender.Position, "Damage");

            yield return Wait.For(Interval.TenthSecond);

            // Calculate and apply counter-attack damage
            var counterResult = Formulas.CalculateAttackResult(defender, attacker);
            if (counterResult == null || counterResult.Opponent == null)
                yield break;

            var counterAttack = AttackHelper.SingleAttackRoutine(counterResult);
            yield return defender.Animation.BumpRoutine(attacker, counterAttack);
        }
    }
}
