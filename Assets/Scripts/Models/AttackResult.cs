using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Models
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
