using System.Collections;
using g = Assets.Helpers.GameHelper;

namespace Assets.Scripts.Sequences
{
    public class HideTargetIndicatorSequence : SequenceEvent
    {
        public override IEnumerator ProcessRoutine()
        {
            g.Actors.TargetActor.Render.SetTargetIndicatorEnabled(false);
            g.Actors.TargetActor = null;
            g.InputManager.InputMode = InputMode.PlayerTurn;
            yield return Wait.None();
        }
    }


}
