// --- File: Assets/Scripts/Sequences/CounterAttackSequence.cs ---
using Assets.Helpers;
using Assets.Scripts.Models;
using System.Collections;
using g = Assets.Helpers.GameHelper;

namespace Assets.Scripts.Sequences
{
    /// <summary>
    /// COUNTERATTACKSEQUENCE - Executes reactive counter-attack.
    /// 
    /// PURPOSE:
    /// When an actor with CounterAttack ability is hit and survives,
    /// this sequence executes an immediate retaliatory strike.
    /// 
    /// TRIGGER CONDITIONS:
    /// - Defender has CounterAttack ability
    /// - Defender survives the attack
    /// - Attacker is adjacent to defender
    /// - Both actors still active
    /// 
    /// SEQUENCE FLOW:
    /// 1. Validate both actors alive
    /// 2. Check adjacency
    /// 3. Show "Counter!" text
    /// 4. Calculate counter damage
    /// 5. Bump animation toward attacker
    /// 6. Apply damage to attacker
    /// 
    /// RELATED FILES:
    /// - AttackHelper.cs: Damage application
    /// - Formulas.cs: Damage calculation
    /// - AbilityLibrary.cs: CounterAttack ability
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
