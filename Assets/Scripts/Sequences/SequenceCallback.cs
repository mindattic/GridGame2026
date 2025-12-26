using System;
using System.Collections;

namespace Assets.Scripts.Sequences
{
    /// <summary>
    /// Lightweight sequence that invokes a callback when processed.
    /// Useful to restore state after a sequence batch completes.
    /// </summary>
    public class SequenceCallback : SequenceEvent
    {
        private readonly Action action;

        public SequenceCallback(Action action)
        {
            this.action = action;
        }

        public override IEnumerator ProcessRoutine()
        {
            action?.Invoke();
            yield return null;
        }
    }
}
