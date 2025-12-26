using Assets.Helpers;
using System.Collections;

namespace Assets.Scripts.Sequences
{
    public class PincerAttackSupportSequence : SequenceEvent
    {
        private ActorInstance attacker;
        private ActorInstance supporter;

        // Single-constructor approach
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
