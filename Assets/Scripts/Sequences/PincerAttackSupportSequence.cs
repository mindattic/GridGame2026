using Assets.Helpers;
using System.Collections;

namespace Assets.Scripts.Sequences
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
