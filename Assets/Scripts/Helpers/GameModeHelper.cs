using Scripts.Managers;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Factories;
using Scripts.Hub;
using Scripts.Instances;
using Scripts.Instances.Actor;
using Scripts.Instances.Board;
using Scripts.Instances.SynergyLine;
using Scripts.Inventory;
using Scripts.Libraries;
using Scripts.Models;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Helpers
{
    public static class GameModeHelper
    {
        public static GameMode CurrentMode = GameMode.Campaign;
        public static bool IsEndless => CurrentMode == GameMode.Endless;

        // Tag mask for enemy selection (flags). Default to Enemy only.
        public static ActorTag Tags = ActorTag.Enemy;

        /// <summary>Sets the tags.</summary>
        public static void SetTags(ActorTag tags) => Tags = tags;
        /// <summary>Add tags.</summary>
        public static void AddTags(ActorTag tags) => Tags |= tags;
        /// <summary> clear tags..Groups[0].Value.ToUpper() lear tags.</summary>
        public static void ClearTags() => Tags = ActorTag.None;

        /// <summary>To campaign mode.</summary>
        public static void ToCampaignMode()
        {
            CurrentMode = GameMode.Campaign;
            ClearTags();
            Tags = ActorTag.Enemy; // default back to Enemy
        }

        // Activate Endless without scene switch
        /// <summary>To endless mode.</summary>
        public static void ToEndlessMode()
        {
            // Auto-select latest save into CurrentSave so Game can start immediately
            if (ProfileHelper.HasCurrentProfile && ProfileHelper.CurrentProfile.LatestSave != null)
            {
                ProfileHelper.CurrentProfile.CurrentSave = ProfileHelper.CurrentProfile.LatestSave;
            }
            CurrentMode = GameMode.Endless;

            ExperienceTracker.NextSceneAfterPostBattleScreen = SceneHelper.TitleScreen;
            SceneHelper.Fade.ToGame();
        }
    }
}
