using Scripts.Helpers;
using Scripts.Helpers;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
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
using Scripts.Managers;
using Scripts.Models;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Helpers
{
    /// <summary>
    /// SCENEHELPER - Centralized scene navigation with transitions.
    /// 
    /// PURPOSE:
    /// Provides unified scene change functionality with fade transitions,
    /// async loading, and scene history tracking.
    /// 
    /// USAGE PATTERNS:
    /// ```csharp
    /// using scene = Scripts.Helpers.SceneHelper;
    /// 
    /// // Fade out → load → fade in
    /// scene.Fade.ToSettings();
    /// scene.Fade.ToGame();
    /// 
    /// // Instant switch (no fade)
    /// scene.Switch.ToTitleScreen();
    /// 
    /// // Manual fade in after scene loads
    /// scene.FadeIn();
    /// 
    /// // Go back to previous scene
    /// scene.Fade.ToPreviousScene();
    /// ```
    /// 
    /// SCENE NAMES:
    /// All scene name constants defined here to avoid typos.
    /// 
    /// TRANSITION FLOW:
    /// 1. Fade out overlay
    /// 2. Load new scene (async)
    /// 3. Fade in overlay
    /// 
    /// RELATED FILES:
    /// - FadeOverlay.cs: Fade animation component
    /// - FadeOverlayHelper.cs: Overlay access
    /// - LoadingScreen scene: Async loading display
    /// </summary>
    public static class SceneHelper
    {
        #region Scene Name Constants

        public const string Credits = "Credits";
        public const string Game = "Game";
        public const string LoadingScreen = "LoadingScreen";
        public const string Overworld = "Overworld";
        public const string PartyManager = "PartyManager";
        public const string ProfileCreate = "ProfileCreate";
        public const string ProfileSelect = "ProfileSelect";
        public const string SaveFileSelect = "SaveFileSelect";
        public const string SplashScreen = "SplashScreen";
        public const string Settings = "Settings";
        public const string StageSelect = "StageSelect";
        public const string TitleScreen = "TitleScreen";
        public const string PostBattleScreen = "PostBattleScreen";
        public const string Hub = "Hub";

        #endregion

        #region Scene Queries

        /// <summary>Returns true if the active scene matches the provided name.</summary>
        public static bool IsCurrentScene(string sceneName) =>
            SceneManager.GetActiveScene().name == sceneName;

        /// <summary>True if the current scene is the main game scene.</summary>
        public static bool IsGameScene => IsCurrentScene(Game);

        #endregion

        #region Fade Transitions

        /// <summary>Calls FadeIn on the active FadeOverlay if it exists.</summary>
        public static void FadeIn(IEnumerator routine = null)
        {

            var overlay = FadeOverlayHelper.Overlay;
            if (overlay != null)
            {
                overlay.FadeIn(routine);
            }
            else
            {
                Debug.LogWarning("SceneHelper.FadeIn called but no FadeOverlay found in scene.");
            }
        }

        /// <summary>
        /// Calls FadeOut with a provided IEnumerator or Animation.
        /// </summary>
        public static void FadeOut(IEnumerator routine)
        {
            var overlay = FadeOverlayHelper.Overlay;
            if (overlay != null)
            {
                overlay.FadeOut(routine);
            }
            else
            {
                Debug.LogWarning("SceneHelper.FadeOut called but no FadeOverlay found in scene.");
            }
        }

        public static void SetAlpha(float alpha)
        {
            var overlay = FadeOverlayHelper.Overlay;
            var image = overlay.GetComponent<UnityEngine.UI.Image>();
            var color = image.color;
            color.a = Mathf.Clamp01(alpha);
            image.color = color;
        }

        /// <summary>
        /// Fluent scene change API that encapsulates fade and loading flow.
        /// </summary>
        public static class Fade
        {
            public static void To(string sceneName)
            {
                if (string.IsNullOrWhiteSpace(sceneName))
                {
                    Debug.LogError("SceneHelper.Change.To received an empty scene name.");
                    return;
                }

                IEnumerator afterFade()
                {
                    //Always save before changing scenes
                    ProfileHelper.Save(overwrite: false);
                    ProfileHelper.Save(overwrite: true);
                    SceneLoader.Load(sceneName);
                    yield break;
                }

                FadeOut(afterFade());
            }

            public static void ToPreviousScene(string defaultScene = Game)
            {
                IEnumerator afterFade()
                {
                    //Always save before changing scenes
                    ProfileHelper.Save(overwrite: false);
                    ProfileHelper.Save(overwrite: true);
                    SceneLoader.LoadPreviousScene(defaultScene);
                    yield break;
                }

                FadeOut(afterFade());
            }

            // Strongly typed helpers
            public static void ToCredits() => To(Credits);
            public static void ToGame() => To(Game);
            public static void ToOverworld() => To(Overworld);
            public static void ToPartyManager() => To(PartyManager);
            public static void ToProfileCreate() => To(ProfileCreate);
            public static void ToProfileSelect() => To(ProfileSelect);
            public static void ToSaveFileSelect() => To(SaveFileSelect);
            public static void ToSplashScreen() => To(SplashScreen);
            public static void ToSettings() => To(Settings);
            public static void ToStageSelect() => To(StageSelect);
            public static void ToTitleScreen() => To(TitleScreen);
            public static void ToPostBattleScreen() => To(PostBattleScreen);
            public static void ToHub() => To(Hub); // new helper
        }

        /// <summary>
        /// Fluent scene change API that encapsulates fade and loading flow.
        /// </summary>
        public static class Switch
        {
            public static void To(string sceneName)
            {
                if (string.IsNullOrWhiteSpace(sceneName))
                {
                    Debug.LogError("SceneHelper.Change.To received an empty scene name.");
                    return;
                }

                SetAlpha(0f);

                //Always save before changing scenes
                ProfileHelper.Save(overwrite: false);
                ProfileHelper.Save(overwrite: true);
                SceneLoader.Load(sceneName);
            }

            public static void ToPreviousScene(string defaultScene = Game)
            {
                SetAlpha(0f);
                ProfileHelper.Save(overwrite: false);
                ProfileHelper.Save(overwrite: true);
                SceneLoader.LoadPreviousScene(defaultScene);
            }

            // Strongly typed helpers
            public static void ToCredits() => To(Credits);
            public static void ToGame() => To(Game);
            public static void ToOverworld() => To(Overworld);
            public static void ToPartyManager() => To(PartyManager);
            public static void ToProfileCreate() => To(ProfileCreate);
            public static void ToProfileSelect() => To(ProfileSelect);
            public static void ToSaveFileSelect() => To(SaveFileSelect);
            public static void ToSplashScreen() => To(SplashScreen);
            public static void ToSettings() => To(Settings);
            public static void ToStageSelect() => To(StageSelect);
            public static void ToTitleScreen() => To(TitleScreen);
            public static void ToPostBattleScreen() => To(PostBattleScreen);
            public static void ToHub() => To(Hub); 
        }

        #endregion
    }
}
