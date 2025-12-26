// --- File: Assets/Scripts/Sequences/EndTurnSequence.cs ---
using System.Collections;
using g = Assets.Helpers.GameHelper;

namespace Assets.Scripts.Sequences
{
    /// <summary>
    /// Ends the current turn and flips to the next side.
    /// Timeline advancement is handled centrally by TurnManager.NextTurn().
    /// </summary>
    public class EndTurnSequence : SequenceEvent
    {
        /// <summary>
        /// Yield a frame for pacing, then hand control to TurnManager.
        /// Do not touch Timeline here to avoid double-advancing the belt.
        /// </summary>
        public override IEnumerator ProcessRoutine()
        {
            yield return Wait.None();

            g.TurnManager.NextTurn();
        }
    }
}
