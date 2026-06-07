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
        /// <summary>Coroutine that executes the process sequence.</summary>
        public override IEnumerator ProcessRoutine()
        {
            // Disable input
            g.InputManager.InputMode = InputMode.None;

           

            // Show Victory announcement when the victory sound is played
            g.VictoryAnnouncement?.Show();
            g.AudioManager?.Play("Victory"); // resilient: real clip or chiptune fanfare

            // Swap the battle bed for the victory bed now and hand it to PostBattleScreen so it
            // carries seamlessly across the fade (Jukebox no-ops if the same track is playing).
            Jukebox.PlayMusic("Victory");
            MusicDirector.PendingPostBattleTrack = "Victory";

            // Hold a beat on the victory banner.
            yield return Wait.For(1.2f);

            // Honor whatever the battle-launcher set (StageSelect → StageSelect,
            // OverworldManager → Overworld). Default in ExperienceTracker is StageSelect.

            // US-053: persist each party hero's current HP so wounds carry into the next battle.
            // Survivors keep their wound; a hero who fell during this WON battle revives at 1 HP and
            // must be healed for gold at the Alchemist (§29.3 #12, model A). Cleared by a full-heal
            // or a defeat. Dead heroes remain in g.Actors.All, so iterate it (not the living list).
            var save = ProfileHelper.CurrentProfile?.CurrentSave;
            if (save?.Party?.Members != null && g.Actors.All != null)
            {
                foreach (var a in g.Actors.All)
                {
                    if (a == null || !a.IsHero || a.Stats == null) continue;
                    foreach (var m in save.Party.Members)
                    {
                        if (m != null && m.CharacterClass == a.characterClass)
                        {
                            m.HpCurrent = a.Stats.HP < 1f ? 1f : a.Stats.HP;
                            break;
                        }
                    }
                }
                ProfileHelper.Save(true);
            }

            // Route to PostBattleScreen so XP is awarded on victory
            scene.Fade.ToPostBattleScreen();
        }
    }
}
