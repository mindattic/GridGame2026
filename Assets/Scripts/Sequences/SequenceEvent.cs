using System.Collections;

namespace Assets.Scripts.Sequences
{
    public abstract class SequenceEvent
    {
        // ProcessRoutine returns an IEnumerator so that it can yield for asynchronous operations.
        public abstract IEnumerator ProcessRoutine();
    }
}
