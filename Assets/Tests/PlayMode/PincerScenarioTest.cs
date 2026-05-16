// PINCERSCENARIOTEST — AltTester-driven PlayMode test of the pincer attack flow.
//
// Compiles in two modes:
//   - With AltTester SDK installed (com.alttester.alttester-sdk): full external-driver test
//     runs against the live Game scene over the AltDriver WebSocket bridge.
//   - Without AltTester: only the in-process smoke test runs (verifies the scene loaded
//     and the core managers are alive). External-driver code is excluded by #if ALTTESTER.
//
// HOW TO RUN
//   1. Open Window -> General -> Test Runner.
//   2. Switch to the PlayMode tab.
//   3. Select "Tests.PlayMode" and click "Run All" (or right-click PincerScenarioTest -> Run).
//
// For the full AltDriver pincer scenario you ALSO need:
//   - AltTester SDK installed (see Documentation/AltTester-Setup.md).
//   - The Game scene open with an AltRunner component in the hierarchy (the SDK ships one
//     as AltRunnerPrefab; alternatively call AltRunner.StartInstrumentation in a test fixture).
//   - The test runner started in a Development Build OR with the ALTTESTER define on.

using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

#if ALTTESTER
using AltTester.AltTesterSDK.Driver;
using AltTester.AltTesterSDK.Driver.Commands;
using AltTester.AltTesterSDK.Driver.Logging;
#endif

namespace Scripts.Tests.PlayMode
{
    [TestFixture]
    public class PincerScenarioTest
    {
        private const string GameSceneName = "Game";

#if ALTTESTER
        private const string AltDriverHost = "127.0.0.1";
        private const int    AltDriverPort = 13000;
        private AltDriver driver;
#endif

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // Load the Game scene so the actor/board systems are alive.
            if (SceneManager.GetActiveScene().name != GameSceneName)
            {
                yield return SceneManager.LoadSceneAsync(GameSceneName, LoadSceneMode.Single);
            }
            // Give one frame for GameManager.Awake/Start to wire everything.
            yield return null;

#if ALTTESTER
            try
            {
                driver = new AltDriver(host: AltDriverHost, port: AltDriverPort, enableLogging: false);
            }
            catch
            {
                Assert.Inconclusive(
                    $"AltDriver could not connect to {AltDriverHost}:{AltDriverPort}. " +
                    "Is AltRunner running in the scene? See Documentation/AltTester-Setup.md.");
            }
#endif
        }

        [TearDown]
        public void TearDown()
        {
#if ALTTESTER
            driver?.Stop();
            driver = null;
#endif
        }

        // ---------- Smoke test (always runs) ----------

        /// <summary>Verifies the Game scene actually loads and the core managers exist.
        /// If this fails, no other PlayMode test can succeed.</summary>
        [UnityTest]
        public IEnumerator Game_scene_boots_with_core_managers()
        {
            var gameManagerType = System.AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(SafeGetTypes)
                .FirstOrDefault(t => t.Name == "GameManager");
            Assert.IsNotNull(gameManagerType, "GameManager type not found in any loaded assembly.");

            var gm = Object.FindObjectOfType(gameManagerType);
            Assert.IsNotNull(gm, "GameManager instance not found in the loaded Game scene. " +
                                  "Did the scene load? Is GameManager attached?");
            yield return null;
        }

        private static System.Type[] SafeGetTypes(System.Reflection.Assembly a)
        {
            try { return a.GetTypes(); }
            catch (System.Reflection.ReflectionTypeLoadException e)
            {
                return e.Types.Where(t => t != null).ToArray();
            }
        }

        // ---------- Full pincer scenario (requires AltTester) ----------

#if ALTTESTER
        /// <summary>
        /// End-to-end pincer test driven through the AltDriver bridge.
        ///
        /// SCENARIO:
        ///   1. Find every ActorInstance in the scene; partition into heroes vs enemies.
        ///   2. Find an enemy adjacent to exactly one hero — the "candidate" enemy that could
        ///      be pincered if a second hero slid into the opposite cell.
        ///   3. Find the closest free hero who can reach that opposite cell.
        ///   4. Call SelectionManager.Drop(hero, tile) via AltDriver.CallStaticMethod to move
        ///      the hero into the pincer position. This triggers PincerAttackManager.Check.
        ///   5. Wait for the sequence queue to drain (poll SequenceManager.IsExecuting).
        ///   6. Assert: candidate enemy's HP is strictly lower than its pre-move HP.
        ///
        /// NOTE: This test currently uses naming conventions that match the GridGame2026 codebase
        /// (Scripts.Managers.SelectionManager, Scripts.Helpers.GameHelper, Scripts.Instances.Actor.ActorInstance).
        /// If those classes are renamed, the AltDriver lookups need to be updated.
        /// </summary>
        [UnityTest]
        public IEnumerator Pincer_drop_damages_flanked_enemy()
        {
            const string ActorComponent = "Scripts.Instances.Actor.ActorInstance";
            const string ActorAssembly  = "Assembly-CSharp";

            // 1. Snapshot every actor in the scene via AltDriver scene-introspection.
            var actors = driver.FindObjectsWhichContain(
                By.COMPONENT, ActorComponent, /*cameraBy*/ By.NAME, /*cameraValue*/ "");

            Assert.IsNotEmpty(actors, "No ActorInstance components found in the live scene.");

            // Partition heroes vs enemies via the public `team` field on ActorInstance.
            var heroes  = new System.Collections.Generic.List<AltObject>();
            var enemies = new System.Collections.Generic.List<AltObject>();
            foreach (var a in actors)
            {
                var team = a.GetComponentProperty<string>(ActorComponent, "team", ActorAssembly);
                if (team == "Hero")  heroes.Add(a);
                if (team == "Enemy") enemies.Add(a);
            }

            Assert.GreaterOrEqual(heroes.Count, 2, "Pincer requires at least 2 heroes on the board.");
            Assert.GreaterOrEqual(enemies.Count, 1, "Pincer requires at least 1 enemy on the board.");

            // 2-3. Compute a pincer setup. Production test logic lives in this helper so the
            // test reads top-down — for the v1 scaffold we keep this stubbed and skip if the
            // current scene doesn't have an obvious flank candidate.
            if (!TryFindPincerSetup(heroes, enemies,
                out var moverHero, out var targetEnemy, out var pincerTile))
            {
                Assert.Inconclusive(
                    "No pincer-able configuration in the current Game scene. Set up a deterministic " +
                    "test scene (or add a TestHooks helper that places actors at fixed positions).");
                yield break;
            }

            int enemyHpBefore = targetEnemy.GetComponentProperty<int>(
                ActorComponent, "Stats.HP", ActorAssembly);

            // Resolve the mover hero's CharacterClass name (the only token AltDriver can pass
            // across the bridge to the static drop shim — enums and ActorInstance refs don't
            // serialize cleanly).
            string moverClassName = moverHero.GetComponentProperty<string>(
                ActorComponent, "characterClass", ActorAssembly);

            // 4. Place the hero on the pincer cell and trigger the pincer scan in one call.
            bool pincerFired = driver.CallStaticMethod<bool>(
                typeName: "Scripts.Helpers.GameHelper",
                methodName: "TriggerPincerDropForHero",
                assemblyName: ActorAssembly,
                parameters: new object[] { moverClassName, pincerTile.x, pincerTile.y });

            Assert.IsTrue(pincerFired,
                $"PincerAttackManager.Check returned false after dropping {moverClassName} on " +
                $"({pincerTile.x},{pincerTile.y}). The flank should have been detected.");

            // 5. Wait for the sequence queue to drain.
            const float timeoutSeconds = 8f;
            float t0 = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - t0 < timeoutSeconds)
            {
                bool executing = driver.CallStaticMethod<bool>(
                    typeName: "Scripts.Helpers.GameHelper",
                    methodName: "SequenceManagerIsExecuting",
                    assemblyName: ActorAssembly,
                    parameters: new object[0]);
                if (!executing) break;
                yield return new WaitForSeconds(0.1f);
            }

            // 6. Assert the pincer landed.
            int enemyHpAfter = targetEnemy.GetComponentProperty<int>(
                ActorComponent, "Stats.HP", ActorAssembly);

            Assert.Less(enemyHpAfter, enemyHpBefore,
                $"Pincer did not damage the flanked enemy (HP {enemyHpBefore} → {enemyHpAfter}).");
        }

        /// <summary>Scans the live board for the simplest possible pincer setup: an enemy with
        /// exactly one hero adjacent in a single axis, plus at least one *other* free hero who
        /// can be teleported into the opposite cell. The test then completes the flank by
        /// dropping that free hero onto <paramref name="pincerTile"/> via the
        /// <c>TriggerPincerDropForHero</c> static shim.</summary>
        /// <remarks>Returns false (test marks Inconclusive) if no candidate exists — e.g. when
        /// the Game scene's default actor layout has no adjacencies yet. For deterministic CI,
        /// pair this with a fixed-state PincerTest.unity scene.</remarks>
        private bool TryFindPincerSetup(
            System.Collections.Generic.List<AltObject> heroes,
            System.Collections.Generic.List<AltObject> enemies,
            out AltObject moverHero, out AltObject targetEnemy, out Vector2Int pincerTile)
        {
            moverHero = null;
            targetEnemy = null;
            pincerTile = Vector2Int.zero;

            const string ActorComponent = "Scripts.Instances.Actor.ActorInstance";
            const string ActorAssembly  = "Assembly-CSharp";

            // Snapshot every hero's tile coordinate for adjacency lookup.
            var heroPos = new System.Collections.Generic.Dictionary<AltObject, Vector2Int>();
            foreach (var h in heroes)
            {
                int hx = h.GetComponentProperty<int>(ActorComponent, "location.x", ActorAssembly);
                int hy = h.GetComponentProperty<int>(ActorComponent, "location.y", ActorAssembly);
                heroPos[h] = new Vector2Int(hx, hy);
            }

            AltObject HeroAt(Vector2Int loc)
            {
                foreach (var kv in heroPos)
                    if (kv.Value == loc) return kv.Key;
                return null;
            }

            AltObject FreeMoverExcluding(AltObject exclude)
            {
                foreach (var kv in heroPos)
                    if (kv.Key != exclude) return kv.Key;
                return null;
            }

            foreach (var enemy in enemies)
            {
                int ex = enemy.GetComponentProperty<int>(ActorComponent, "location.x", ActorAssembly);
                int ey = enemy.GetComponentProperty<int>(ActorComponent, "location.y", ActorAssembly);

                // Horizontal axis: a single hero on one side, opposite side open + a free mover.
                var hL = HeroAt(new Vector2Int(ex - 1, ey));
                var hR = HeroAt(new Vector2Int(ex + 1, ey));
                if (hL != null && hR == null)
                {
                    var mover = FreeMoverExcluding(hL);
                    if (mover != null) { moverHero = mover; targetEnemy = enemy; pincerTile = new Vector2Int(ex + 1, ey); return true; }
                }
                if (hR != null && hL == null)
                {
                    var mover = FreeMoverExcluding(hR);
                    if (mover != null) { moverHero = mover; targetEnemy = enemy; pincerTile = new Vector2Int(ex - 1, ey); return true; }
                }

                // Vertical axis: same check rotated 90°.
                var hD = HeroAt(new Vector2Int(ex, ey - 1));
                var hU = HeroAt(new Vector2Int(ex, ey + 1));
                if (hD != null && hU == null)
                {
                    var mover = FreeMoverExcluding(hD);
                    if (mover != null) { moverHero = mover; targetEnemy = enemy; pincerTile = new Vector2Int(ex, ey + 1); return true; }
                }
                if (hU != null && hD == null)
                {
                    var mover = FreeMoverExcluding(hU);
                    if (mover != null) { moverHero = mover; targetEnemy = enemy; pincerTile = new Vector2Int(ex, ey - 1); return true; }
                }
            }

            return false;
        }
#endif
    }
}
