using Assets.Scripts.Libraries;
using System.Collections;
using g = Assets.Helpers.GameHelper;
using scene = Assets.Helpers.SceneHelper;
using Assets.Scripts.Managers; // ExperienceTracker

namespace Assets.Scripts.Sequences
{
    /// <summary>
    /// Plays the defeat SFX then routes to VictoryScreen to award XP.
    /// Disables player input while sequence runs.
    /// </summary>
    public class BattleLostSequence : SequenceEvent
    {
        public override IEnumerator ProcessRoutine()
        {
            // Disable input
            g.InputManager.InputMode = InputMode.None;

            // Show Defeat announcement when the defeat sound is played
            g.DefeatAnnouncement?.Show();

            var sfx = SoundEffectLibrary.SoundEffects.ContainsKey("Defeat") ? SoundEffectLibrary.SoundEffects["Defeat"] : null;
            if (sfx != null)
                yield return Wait.For(sfx.length);
            // Ensure the next scene after PostBattle is Hub
            ExperienceTracker.NextSceneAfterPostBattleScreen = scene.Hub;
            // Route to PostBattleScreen so XP is awarded on defeat
            scene.Fade.ToPostBattleScreen();
        }
    }
}
