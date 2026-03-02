using System;
using System.Collections.Generic;
using System.Linq;
using g = Scripts.Helpers.GameHelper;
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
