using Assets.Scripts.Managers;

namespace Assets.Helpers
{
    public static class GameModeHelper
    {
        public static GameMode CurrentMode = GameMode.Campaign;
        public static bool IsEndless => CurrentMode == GameMode.Endless;

        // Tag mask for enemy selection (flags). Default to Enemy only.
        public static ActorTag Tags = ActorTag.Enemy;

        public static void SetTags(ActorTag tags) => Tags = tags;
        public static void AddTags(ActorTag tags) => Tags |= tags;
        public static void ClearTags() => Tags = ActorTag.None;

        public static void ToCampaignMode()
        {
            CurrentMode = GameMode.Campaign;
            ClearTags();
            Tags = ActorTag.Enemy; // default back to Enemy
        }

        // Activate Endless without scene switch
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
