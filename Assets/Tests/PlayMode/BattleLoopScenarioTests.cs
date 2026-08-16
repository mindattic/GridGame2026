// BATTLELOOPSCENARIOTESTS — PlayMode scenario tests of the core combat loop, driven
// in-process (no AltTester): load the Game scene on the deterministic Test-Harness stage
// (one wave, two slimes — StageLibrary), arrange actors via TestHooks.PlaceActor, complete
// a flank via GameHelper.TriggerPincerDropForHero, and assert real damage / win routing.
//
// Board coordinates are 1-BASED (TileMap: cols 1..6, rows 1..8). The pincer line used here
// is row 4: hero(1,4) slime(2,4) slime(3,4) hero-drop(4,4).
//
// Profile isolation: throwaway profile under temporaryCachePath; teardown restores
// production profile IO, unseeds RNG, and resets game speed.

using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Scripts.Helpers;
using Scripts.Instances.Actor;
using g = Scripts.Helpers.GameHelper;

namespace Scripts.Tests.PlayMode
{
    [TestFixture]
    public class BattleLoopScenarioTests
    {
        private string isolatedRoot;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // Breadcrumbs: the suite intermittently hard-froze right after the Game scene
            // loaded (main loop dead, background threads alive). These logs bracket every
            // phase so the wedge point is readable from the log tail.
            Debug.Log("[BattleTest] SetUp: isolating profile");
            isolatedRoot = Path.Combine(
                Application.temporaryCachePath, "TestProfiles",
                System.Guid.NewGuid().ToString("N"));
            TestHooks.CreateIsolatedProfile(isolatedRoot, "BattleTest");
            TestHooks.SeedRng(1234);

            // Point both save views at the harness stage (StageManager.Initialize reads the
            // stage from the save; LatestSave and CurrentSave may be distinct objects).
            var profile = ProfileHelper.CurrentProfile;
            profile.CurrentSave.Stage.CurrentStage = "Test-Harness";
            profile.CurrentSave.Stage.CurrentWave = 0;
            if (profile.LatestSave != null && !ReferenceEquals(profile.LatestSave, profile.CurrentSave))
            {
                profile.LatestSave.Stage.CurrentStage = "Test-Harness";
                profile.LatestSave.Stage.CurrentWave = 0;
            }

            Debug.Log("[BattleTest] SetUp: loading Game scene");
            yield return SceneManager.LoadSceneAsync("Game", LoadSceneMode.Single);
            Debug.Log("[BattleTest] SetUp: Game scene loaded; waiting for spawns");

            // Wait for the wave to spawn: 2 slimes + at least 2 heroes.
            float deadline = Time.realtimeSinceStartup + 30f;
            float nextBeat = 0f;
            while (Time.realtimeSinceStartup < deadline &&
                   (TestHooks.AliveCount("Enemy") < 2 || TestHooks.AliveCount("Hero") < 2))
            {
                if (Time.realtimeSinceStartup >= nextBeat)
                {
                    Debug.Log($"[BattleTest] waiting: heroes={TestHooks.AliveCount("Hero")} enemies={TestHooks.AliveCount("Enemy")}");
                    nextBeat = Time.realtimeSinceStartup + 3f;
                }
                yield return null;
            }
            Debug.Log($"[BattleTest] SetUp done: heroes={TestHooks.AliveCount("Hero")} enemies={TestHooks.AliveCount("Enemy")}");

            Assert.GreaterOrEqual(TestHooks.AliveCount("Enemy"), 2, "Test-Harness wave (2 slimes) must spawn.");
            Assert.GreaterOrEqual(TestHooks.AliveCount("Hero"), 2, "Party must field at least 2 heroes.");
        }

        [TearDown]
        public void TearDown()
        {
            TestHooks.SetGameSpeed(1f);
            TestHooks.UnseedRng();
            TestHooks.ClearIsolatedProfileRoot();
            if (!string.IsNullOrEmpty(isolatedRoot) && Directory.Exists(isolatedRoot))
                Directory.Delete(isolatedRoot, recursive: true);
        }

        /// <summary>Parks every hero away from row 4, then arranges the canonical pincer line
        /// and returns (flankHero, dropHero, slimeA, slimeB). All placement happens inside one
        /// frame so enemy turns can't interleave with the arrangement.</summary>
        private (ActorInstance flank, ActorInstance drop, ActorInstance slimeA, ActorInstance slimeB) ArrangePincerLine()
        {
            var heroes = g.Actors.Heroes.Where(h => h != null && h.IsPlaying).ToList();
            var enemies = g.Actors.Enemies.Where(e => e != null && e.IsPlaying).ToList();

            // Park all heroes on row 1 first so none of them accidentally sits in the line.
            for (int i = 0; i < heroes.Count; i++)
                TestHooks.PlaceActor(heroes[i].characterClass.ToString(), "Hero", 1 + i, 1);

            var slimeA = enemies[0];
            var slimeB = enemies[1];
            TestHooks.PlaceActor(slimeA.characterClass.ToString(), "Enemy", 2, 4);
            TestHooks.PlaceActor(slimeB.characterClass.ToString(), "Enemy", 3, 4);

            var flank = heroes[0];
            var drop = heroes[1];
            TestHooks.PlaceActor(flank.characterClass.ToString(), "Hero", 1, 4);

            return (flank, drop, slimeA, slimeB);
        }

        private IEnumerator DrainSequences(float timeoutSeconds)
        {
            float deadline = Time.realtimeSinceStartup + timeoutSeconds;
            // Give the sequence queue a frame to start executing before polling for drain.
            yield return null;
            while (Time.realtimeSinceStartup < deadline && GameHelper.SequenceManagerIsExecuting())
                yield return null;
        }

        [UnityTest]
        public IEnumerator Pincer_drop_damages_flanked_enemies()
        {
            var (flank, drop, slimeA, slimeB) = ArrangePincerLine();

            float hpBeforeA = slimeA.Stats.HP;
            float hpBeforeB = slimeB.Stats.HP;

            bool fired = GameHelper.TriggerPincerDropForHero(drop.characterClass.ToString(), 4, 4);
            Assert.IsTrue(fired, "Completing the flank at (4,4) must detect a pincer (GG-LAW-2).");

            // The attack resolves through queued sequences with pacing beats — the queue can be
            // momentarily idle between beats, so polling "queue drained" alone races the damage
            // application. Poll for the OUTCOME (both slimes hurt) with a hard deadline instead.
            bool aDamaged = false, bDamaged = false;
            float deadline = Time.realtimeSinceStartup + 30f;
            while (Time.realtimeSinceStartup < deadline)
            {
                aDamaged = slimeA == null || !slimeA.IsAlive || slimeA.Stats.HP < hpBeforeA;
                bDamaged = slimeB == null || !slimeB.IsAlive || slimeB.Stats.HP < hpBeforeB;
                if (aDamaged && bDamaged) break;
                yield return null;
            }

            Assert.IsTrue(aDamaged, $"Flanked slime A took no damage within 30s (HP {hpBeforeA} -> {slimeA?.Stats.HP}).");
            Assert.IsTrue(bDamaged, $"Flanked slime B took no damage within 30s (HP {hpBeforeB} -> {slimeB?.Stats.HP}).");
        }

        [UnityTest]
        public IEnumerator Killing_last_wave_routes_to_PostBattleScreen()
        {
            var (flank, drop, slimeA, slimeB) = ArrangePincerLine();

            // One pincer must finish both: drop them to 1 HP so the real damage path kills them.
            TestHooks.SetActorHp(slimeA.characterClass.ToString(), "Enemy", 1f);
            TestHooks.SetActorHp(slimeB.characterClass.ToString(), "Enemy", 1f);

            bool fired = GameHelper.TriggerPincerDropForHero(drop.characterClass.ToString(), 4, 4);
            Assert.IsTrue(fired, "Completing the flank at (4,4) must detect a pincer.");

            // Accelerate the win/pacing sequences (BattleWonSequence holds, fades, popups).
            TestHooks.SetGameSpeed(5f);

            float deadline = Time.realtimeSinceStartup + 90f;
            while (Time.realtimeSinceStartup < deadline &&
                   SceneManager.GetActiveScene().name != "PostBattleScreen")
                yield return null;

            Assert.AreEqual("PostBattleScreen", SceneManager.GetActiveScene().name,
                "Clearing the only wave of Test-Harness must route Game -> PostBattleScreen.");
        }
    }
}
