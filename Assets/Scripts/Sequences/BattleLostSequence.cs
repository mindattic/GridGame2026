using Assets.Scripts.Libraries;
using System.Collections;
using g = Assets.Helpers.GameHelper;
using scene = Assets.Helpers.SceneHelper;
using Assets.Scripts.Managers;

namespace Assets.Scripts.Sequences
{
    /// <summary>
    /// BATTLELOSTSEQUENCE - Handles defeat state when all heroes die.
    /// 
    /// PURPOSE:
    /// Executes when all heroes are defeated, showing defeat UI
    /// and transitioning to the post-battle screen.
    /// 
    /// SEQUENCE FLOW:
    /// 1. Lock input
    /// 2. Show defeat announcement
    /// 3. Play defeat SFX
    /// 4. Wait for SFX duration
    /// 5. Set next scene to Hub
    /// 6. Fade to PostBattleScreen
    /// 
    /// NOTE:
    /// XP is still awarded on defeat (partial XP for damage dealt).
    /// 
    /// RELATED FILES:
    /// - DefeatAnnouncement.cs: Defeat UI
    /// - PostBattleManager.cs: XP awards
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
