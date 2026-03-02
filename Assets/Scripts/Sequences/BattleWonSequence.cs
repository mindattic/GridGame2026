using Scripts.Libraries;
using System.Collections;
using g = Scripts.Helpers.GameHelper;
using scene = Scripts.Helpers.SceneHelper;
using Scripts.Managers;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Factories;
using Scripts.Helpers;
using Scripts.Hub;
using Scripts.Instances;
using Scripts.Instances.Actor;
using Scripts.Instances.Board;
using Scripts.Instances.SynergyLine;
using Scripts.Inventory;
using Scripts.Models;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Sequences
{
    /// <summary>
    /// BATTLEWONSEQUENCE - Handles victory state after all enemies defeated.
    /// 
    /// PURPOSE:
    /// Executes when all enemies are defeated, showing victory UI
    /// and transitioning to the post-battle screen.
    /// 
    /// SEQUENCE FLOW:
    /// 1. Lock input
    /// 2. Show victory announcement
    /// 3. Play victory SFX
    /// 4. Wait for SFX duration
    /// 5. Set next scene to Hub
    /// 6. Fade to PostBattleScreen
    /// 
    /// POST-BATTLE:
    /// PostBattleScreen awards XP, shows level-ups, then
    /// transitions to the Hub.
    /// 
    /// RELATED FILES:
    /// - VictoryAnnouncement.cs: Victory UI
    /// - PostBattleManager.cs: XP awards
    /// - ExperienceTracker.cs: XP tracking
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
