using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Scripts.Models
{
    /// <summary>
    /// ATTACKRESULT - Calculated attack outcome data.
    /// 
    /// PURPOSE:
    /// Contains the result of an attack calculation including
    /// attacker, defender, damage, and hit type.
    /// 
    /// PROPERTIES:
    /// - Attacker: Actor dealing damage
    /// - Opponent: Actor receiving damage
    /// - Damage: Calculated damage amount
    /// - HitType: Normal, Critical, or Miss
    /// 
    /// USAGE:
    /// ```csharp
    /// var result = Formulas.CalculateAttackResult(attacker, defender);
    /// defender.TakeDamage(result.Damage);
    /// ```
    /// 
    /// RELATED FILES:
    /// - Formulas.cs: Creates AttackResults
    /// - AttackHelper.cs: Applies AttackResults
    /// </summary>
    public sealed class AttackResult
    {
        public ActorInstance Attacker;
        public ActorInstance Opponent;
        public int Damage;
        public HitOutcome HitType;

        public AttackResult(ActorInstance attacker, ActorInstance opponent, int damage, HitOutcome hitType)
        {
            if (attacker == null) throw new System.ArgumentNullException(nameof(attacker));
            if (opponent == null) throw new System.ArgumentNullException(nameof(opponent));

            Attacker = attacker;
            Opponent = opponent;
            Damage = damage;
            HitType = hitType;
        }
    }


}
