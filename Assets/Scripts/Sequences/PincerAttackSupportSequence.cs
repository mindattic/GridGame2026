using Scripts.Helpers;
using System.Collections;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Factories;
using Scripts.Hub;
using Scripts.Instances;
using Scripts.Instances.Actor;
using Scripts.Instances.Board;
using Scripts.Instances.SynergyLine;
using Scripts.Inventory;
using Scripts.Libraries;
using Scripts.Managers;
using Scripts.Models;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Sequences
{
    /// <summary>
    /// PINCERATTACKSUPPORTSEQUENCE - Support action during pincer.
    /// 
    /// PURPOSE:
    /// Executes support abilities from allies positioned to
    /// assist during a pincer attack.
    /// 
    /// SUPPORT TYPES:
    /// - Cleric: Heals the attacker
    /// - (Future: Other support classes)
    /// 
    /// SEQUENCE FLOW:
    /// 1. Check supporter's class
    /// 2. Execute appropriate support action
    /// 3. (Cleric) Launch heal projectile to attacker
    /// 
    /// RELATED FILES:
    /// - HeroPincerSequence.cs: Queues support sequences
    /// - HealSupportSequence.cs: Heal execution
    /// - SynergyLineManager.cs: Visual connection lines
    /// </summary>
    public class PincerAttackSupportSequence : SequenceEvent
    {
        private ActorInstance attacker;
        private ActorInstance supporter;

        public PincerAttackSupportSequence(ActorInstance attacker, ActorInstance supporter)
        {
            this.attacker = attacker;
            this.supporter = supporter;
        }

        public override IEnumerator ProcessRoutine()
        {
            // If supporter is a Cleric, heal the attacker
            if (supporter.characterClass == CharacterClass.Cleric)
                yield return new HealSupportSequence(supporter.Position, attacker).ProcessRoutine();
        }
    }
}
