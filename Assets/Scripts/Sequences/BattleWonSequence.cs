using Assets.Scripts.Libraries;
using System.Collections;
using g = Assets.Helpers.GameHelper;
using scene = Assets.Helpers.SceneHelper;
using Assets.Scripts.Managers; // ExperienceTracker

namespace Assets.Scripts.Sequences
{
    /// <summary>
    /// Plays the victory SFX then routes to VictoryScreen.
    /// Disables player input while sequence runs.
    /// </summary>
    public class BattleWonSequence : SequenceEvent
    {
        public override IEnumerator ProcessRoutine()
        {
            // Disable input
            g.InputManager.InputMode = InputMode.None;

           

            // Show Victory announcement when the victory sound is played
            g.VictoryAnnouncement?.Show();

            // Wait until clip length (approx) using a simple delay if available
            var sfx = SoundEffectLibrary.SoundEffects.ContainsKey("Victory") ? SoundEffectLibrary.SoundEffects["Victory"] : null;
            if (sfx != null)
                yield return Wait.For(sfx.length);

            // Ensure the next scene after PostBattle is Hub
            ExperienceTracker.NextSceneAfterPostBattleScreen = scene.Hub;

            // Route to PostBattleScreen so XP is awarded on victory
            scene.Fade.ToPostBattleScreen();
        }
    }
}
