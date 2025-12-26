using System.Collections;
using g = Assets.Helpers.GameHelper;

namespace Assets.Scripts.Sequences
{
    public class PortraitPopOutSequence : SequenceEvent
    {
        private ActorInstance actor;
        public PortraitPopOutSequence(ActorInstance actor)
        {
            this.actor = actor;
        }

        public override IEnumerator ProcessRoutine()
        {
            yield return g.PortraitManager.PopOutRoutine(actor);
        }
    }


}
