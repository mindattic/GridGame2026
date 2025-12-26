using System;
using System.Collections.Generic;
using System.Linq;
using g = Assets.Helpers.GameHelper;

namespace Assets.Scripts.Models
{
    /// <summary>
    /// Each pair of attackers (e.g. A and B), plus the opponents they sandwich
    /// and any supporters for each attacker.
    /// </summary>
    public class PincerAttackPair
    {
        public ActorInstance attacker1;
        public ActorInstance attacker2;

        // Enemies (opponents) in between attacker1 and attacker2
        public List<ActorInstance> opponents = new();

        // Attack attackResult actors stored here, so PincerAttackAction can see it
        public List<AttackResult> attackResults1 = new();
        public List<AttackResult> attackResults2 = new();

        // Potential same-team supporters who have Hide line of sight to each attacker
        public List<ActorInstance> supporters1 = new();
        public List<ActorInstance> supporters2 = new();
    }

    /// <summary>
    /// A container with all of the "bookend pairs" found for s certain team.
    /// </summary>
    public class PincerAttackParticipants
    {
        public List<PincerAttackPair> pair = new();
        public void Clear() => pair.Clear();
    }

}
