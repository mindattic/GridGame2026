using System;
using System.Collections.Generic;
using System.Linq;
using g = Assets.Helpers.GameHelper;

namespace Assets.Scripts.Models
{
    /// <summary>
    /// PINCERATTACKPAIR - Data for a pincer attack.
    /// 
    /// PURPOSE:
    /// Contains the two attackers forming a pincer, the enemies
    /// caught between them, and supporting allies.
    /// 
    /// STRUCTURE:
    /// ```
    /// [Attacker1] ← supporters1
    ///      ↓
    /// [Opponent1]
    /// [Opponent2]
    ///      ↓
    /// [Attacker2] ← supporters2
    /// ```
    /// 
    /// RELATED FILES:
    /// - PincerAttackManager.cs: Detects pincer setups
    /// - PincerAttackSequence.cs: Executes pincer attacks
    /// </summary>
    public class PincerAttackPair
    {
        public ActorInstance attacker1;
        public ActorInstance attacker2;

        /// <summary>Enemies sandwiched between attackers.</summary>
        public List<ActorInstance> opponents = new();

        /// <summary>Attack results for each attacker.</summary>
        public List<AttackResult> attackResults1 = new();
        public List<AttackResult> attackResults2 = new();

        /// <summary>Supporting allies for each attacker.</summary>
        public List<ActorInstance> supporters1 = new();
        public List<ActorInstance> supporters2 = new();
    }

    /// <summary>
    /// PINCERATTACKPARTICIPANTS - Collection of pincer pairs.
    /// 
    /// Contains all detected pincer attack pairs for a team.
    /// </summary>
    public class PincerAttackParticipants
    {
        public List<PincerAttackPair> pair = new();
        public void Clear() => pair.Clear();
    }

}
