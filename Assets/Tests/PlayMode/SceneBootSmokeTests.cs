// SCENEBOOTSMOKETESTS — PlayMode smoke: every live scene loads by name without a single
// Error/Exception log. Unity Test Framework fails a [UnityTest] automatically on any
// unexpected LogType.Error/Exception, so the assertion is implicit — the test body just
// loads the scene and lets it settle. Scenes are free to auto-navigate (SplashScreen fades
// to TitleScreen, LoadingScreen chains onward); navigation is not a failure.
//
// Hub and Overworld are intentionally absent: both are retired from the live flow
// (docs/AMENDMENTS.md GG-A3) and scheduled for removal from the build list.
//
// Profile isolation: every test runs against a throwaway profile under temporaryCachePath —
// the player's real saves are never read or written (FolderHelper.TestProfileRootOverride).

using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Scripts.Helpers;

namespace Scripts.Tests.PlayMode
{
    [TestFixture]
    public class SceneBootSmokeTests
    {
        private static string isolatedRoot;

        // Every scene registered in the build that survives the nav consolidation.
        private static readonly string[] LiveScenes =
        {
            "SplashScreen",
            "TitleScreen",
            "ProfileCreate",
            "ProfileSelect",
            "SaveFileSelect",
            "StageSelect",
            "Game",
            "PostBattleScreen",
            "Vendor",
            "Alchemist",
            "Blacksmith",
            "Equip",
            "Party",
            "Abilities",
            "Settings",
            "Credits",
            "Bestiary",
            "LoadingScreen",
        };

        [OneTimeSetUp]
        public void FixtureSetUp()
        {
            isolatedRoot = Path.Combine(
                Application.temporaryCachePath, "TestProfiles",
                System.Guid.NewGuid().ToString("N"));
            TestHooks.CreateIsolatedProfile(isolatedRoot, "SmokeTest");
        }

        [OneTimeTearDown]
        public void FixtureTearDown()
        {
            TestHooks.ClearIsolatedProfileRoot();
            if (!string.IsNullOrEmpty(isolatedRoot) && Directory.Exists(isolatedRoot))
                Directory.Delete(isolatedRoot, recursive: true);
        }

        [UnityTest]
        public IEnumerator Scene_boots_without_errors([ValueSource(nameof(LiveScenes))] string sceneName)
        {
            yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);

            // Let Awake/Start/first-frame coroutines run; realtime so SceneLoader's
            // WaitForSecondsRealtime chains get a chance to fire too.
            yield return new WaitForSecondsRealtime(2f);

            // The Game scene is the only one whose manager singleton is load-bearing enough
            // to assert by presence; every other scene's bar is simply "no errors logged"
            // (enforced implicitly by the test framework).
            if (sceneName == "Game")
            {
                Assert.IsNotNull(Scripts.Managers.GameManager.instance,
                    "GameManager must be alive after the Game scene loads.");
            }
        }
    }
}
