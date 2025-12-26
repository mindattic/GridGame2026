using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Scripts.Models
{
    public sealed class AttackResult
    {
        public ActorInstance Attacker;
        public ActorInstance Opponent;
        public int Damage;
        public HitOutcome HitType;

        // Require all fields up front
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
