// CAMPAIGNSTAGESTESTS — EditMode tests for the linear-forward/open-backward stage-unlock
// rules (bible §22.3): stage N unlocks when HighestClearedStageIndex >= N-1; cleared stages
// stay replayable. MarkCleared mutates the live save, so these tests run against an
// isolated throwaway profile (never the player's real saves).

using System.IO;
using NUnit.Framework;
using UnityEngine;
using Scripts.Helpers;
using Scripts.Libraries;

namespace Scripts.Tests.EditMode
{
    [TestFixture]
    public class CampaignStagesTests
    {
        private string isolatedRoot;

        [SetUp]
        public void SetUp()
        {
            isolatedRoot = Path.Combine(
                Application.temporaryCachePath, "TestProfiles",
                System.Guid.NewGuid().ToString("N"));
            TestHooks.CreateIsolatedProfile(isolatedRoot, "CampaignTest");
        }

        [TearDown]
        public void TearDown()
        {
            TestHooks.ClearIsolatedProfileRoot();
            if (!string.IsNullOrEmpty(isolatedRoot) && Directory.Exists(isolatedRoot))
                Directory.Delete(isolatedRoot, recursive: true);
        }

        [Test]
        public void First_stage_is_always_unlocked()
        {
            Assert.IsTrue(CampaignStages.IsUnlocked(0, highestClearedStageIndex: -1));
        }

        [Test]
        public void Stage_unlocks_only_after_prior_stage_cleared()
        {
            Assert.IsFalse(CampaignStages.IsUnlocked(1, highestClearedStageIndex: -1), "Stage 1 locked on a fresh save.");
            Assert.IsTrue(CampaignStages.IsUnlocked(1, highestClearedStageIndex: 0), "Clearing stage 0 unlocks stage 1.");
            Assert.IsFalse(CampaignStages.IsUnlocked(5, highestClearedStageIndex: 3), "No skipping ahead.");
            Assert.IsTrue(CampaignStages.IsUnlocked(3, highestClearedStageIndex: 7), "Cleared stages stay replayable.");
        }

        [Test]
        public void MarkCleared_advances_save_and_never_regresses()
        {
            Assert.AreEqual(-1, TestHooks.HighestClearedStageIndex, "Fresh save starts with nothing cleared.");

            string firstStage = CampaignStages.Order[0];
            CampaignStages.MarkCleared(firstStage);
            Assert.AreEqual(0, TestHooks.HighestClearedStageIndex);

            string thirdStage = CampaignStages.Order[2];
            CampaignStages.MarkCleared(thirdStage);
            Assert.AreEqual(2, TestHooks.HighestClearedStageIndex);

            // Replaying an earlier stage must not regress progress.
            CampaignStages.MarkCleared(firstStage);
            Assert.AreEqual(2, TestHooks.HighestClearedStageIndex);
        }

        [Test]
        public void MarkCleared_ignores_non_campaign_stages()
        {
            CampaignStages.MarkCleared("Test-Harness");
            Assert.AreEqual(-1, TestHooks.HighestClearedStageIndex,
                "Test fixtures are not campaign stages and must not advance progression.");
        }
    }
}
