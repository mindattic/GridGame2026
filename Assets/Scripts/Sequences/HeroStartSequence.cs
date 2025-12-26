// --- File: Assets/Scripts/Events/Sequences/HeroStartSequence.cs ---
using System.Collections;
using g = Assets.Helpers.GameHelper;

namespace Assets.Scripts.Sequences
{
    /// <summary>
    /// Performs start-of-turn logic for the hero team.
    /// Refills the hero Animation timer and makes sure UI is in the right mode.
    /// </summary>
    public class HeroStartSequence : SequenceEvent
    {
        public override IEnumerator ProcessRoutine()
        {
            // Only run during hero turns
            if (!g.TurnManager.IsHeroTurn)
                yield break;

            // Put input back into hero mode and refill the turn timer UI
            g.InputManager.InputMode = InputMode.PlayerTurn;

            yield break;
        }
    }
}
