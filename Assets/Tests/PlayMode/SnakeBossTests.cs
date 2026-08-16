// SNAKEBOSSTESTS — PlayMode verification of the segmented snake boss (US-140):
// the Test-Snake fixture spawns a Naga00 head that auto-grows 3 body segments; the chain is
// tail-gated (armored except the last living member) and segments never get timeline icons.

using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Scripts.Helpers;
using Scripts.Managers;
using g = Scripts.Helpers.GameHelper;

namespace Scripts.Tests.PlayMode
{
    [TestFixture]
    public class SnakeBossTests
    {
        private string isolatedRoot;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            isolatedRoot = Path.Combine(
                Application.temporaryCachePath, "TestProfiles",
                System.Guid.NewGuid().ToString("N"));
            TestHooks.CreateIsolatedProfile(isolatedRoot, "SnakeTest");
            TestHooks.SeedRng(1234);

            var profile = ProfileHelper.CurrentProfile;
            profile.CurrentSave.Stage.CurrentStage = "Test-Snake";
            profile.CurrentSave.Stage.CurrentWave = 0;
            if (profile.LatestSave != null && !ReferenceEquals(profile.LatestSave, profile.CurrentSave))
            {
                profile.LatestSave.Stage.CurrentStage = "Test-Snake";
                profile.LatestSave.Stage.CurrentWave = 0;
            }

            yield return SceneManager.LoadSceneAsync("Game", LoadSceneMode.Single);

            // Wait for the chain: 1 head + up to 3 segments (board crowding can shorten it,
            // but at least head + 1 segment must exist for the gating to be meaningful).
            float deadline = Time.realtimeSinceStartup + 30f;
            while (Time.realtimeSinceStartup < deadline && TestHooks.AliveCount("Enemy") < 2)
                yield return null;
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

        [UnityTest]
        public IEnumerator Chain_spawns_with_tail_gated_armor_and_iconless_segments()
        {
            var members = g.Actors.Enemies.Where(e => e != null && e.IsPlaying).ToList();
            Assert.GreaterOrEqual(members.Count, 2, "Head + at least one segment must spawn.");
            Assert.IsTrue(members.All(SnakeBossManager.IsChainMember), "Every Naga is chain-registered.");

            // Exactly ONE member is vulnerable (the tail); everyone else is armored.
            var vulnerable = members.Where(m => !SnakeBossManager.IsArmored(m)).ToList();
            Assert.AreEqual(1, vulnerable.Count, "Tail-first gating: exactly one vulnerable member.");

            // Segments never act: only the head may carry a timeline icon.
            var segments = members.Where(SnakeBossManager.IsSegment).ToList();
            Assert.Greater(segments.Count, 0);
            foreach (var segment in segments)
                Assert.IsNull(g.TimelineBar.GetSpellIconFor(segment), "Segments never cast either.");

            yield return null;
        }

        [UnityTest]
        public IEnumerator Armored_member_takes_zero_damage_until_the_tail_falls()
        {
            var members = g.Actors.Enemies.Where(e => e != null && e.IsPlaying).ToList();
            Assert.GreaterOrEqual(members.Count, 2);

            var armored = members.First(SnakeBossManager.IsArmored);
            var tail = members.First(m => !SnakeBossManager.IsArmored(m));

            // Direct armored strike: zero damage.
            float hpBefore = armored.Stats.HP;
            // AttackResult lives in Scripts.Models; HitOutcome in Scripts.Utilities (Formulas.cs).
            armored.Damage(new Scripts.Models.AttackResult(
                g.Actors.Heroes.First(h => h.IsPlaying), armored, 25,
                Scripts.Utilities.HitOutcome.Normal));
            float deadline = Time.realtimeSinceStartup + 2f;
            while (Time.realtimeSinceStartup < deadline) yield return null;
            Assert.AreEqual(hpBefore, armored.Stats.HP, "Armored chain members shrug all damage.");

            // Kill the tail directly (all chain members share a class — typed access, not name
            // lookup); HP=0 → IsPlaying false → the lazy armor recomputes.
            tail.Stats.HP = 0f;
            yield return null;

            var stillPlaying = members.Where(m => m != null && m.IsPlaying).ToList();
            var nowVulnerable = stillPlaying.Where(m => !SnakeBossManager.IsArmored(m)).ToList();
            Assert.AreEqual(1, nowVulnerable.Count,
                "After the tail falls, exactly one member up the chain becomes vulnerable.");
            Assert.AreNotEqual(tail, nowVulnerable[0]);
        }
    }
}
