// BOUNTYFLOWTESTS — EditMode tests for the single-slot bounty contract:
// accept → RecordKill progress (only matching classes count) → complete → ClaimReward
// credits gold + reward item and frees the slot → abandon resets cleanly.
// Pure save-state logic (BountyHelper/BountyLibrary), isolated throwaway profile.

using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Scripts.Helpers;
using Scripts.Inventory;
using Scripts.Libraries;

namespace Scripts.Tests.EditMode
{
    [TestFixture]
    public class BountyFlowTests
    {
        private string isolatedRoot;

        [SetUp]
        public void SetUp()
        {
            isolatedRoot = Path.Combine(
                Application.temporaryCachePath, "TestProfiles",
                System.Guid.NewGuid().ToString("N"));
            TestHooks.CreateIsolatedProfile(isolatedRoot, "BountyTest");
        }

        [TearDown]
        public void TearDown()
        {
            TestHooks.ClearIsolatedProfileRoot();
            if (!string.IsNullOrEmpty(isolatedRoot) && Directory.Exists(isolatedRoot))
                Directory.Delete(isolatedRoot, recursive: true);
        }

        [Test]
        public void Accept_track_complete_claim_credits_gold()
        {
            var bounty = BountyLibrary.All().First();
            Assert.IsFalse(BountyHelper.HasActive(), "Fresh save has no active bounty.");

            Assert.IsTrue(BountyHelper.Accept(bounty.Id));
            Assert.AreEqual(bounty.Id, BountyHelper.ActiveBounty().Id);

            // Non-matching kills must not advance the contract.
            BountyHelper.RecordKill(CharacterClass.Paladin);
            Assert.AreEqual(0, BountyHelper.ActiveProgress());

            for (int i = 0; i < bounty.RequiredCount; i++)
                BountyHelper.RecordKill(bounty.TargetClass);

            Assert.IsTrue(BountyHelper.IsComplete());

            // Extra kills past the requirement don't overflow.
            BountyHelper.RecordKill(bounty.TargetClass);
            Assert.AreEqual(bounty.RequiredCount, BountyHelper.ActiveProgress());

            var save = ProfileHelper.CurrentProfile.CurrentSave;
            var inventory = new PlayerInventory();
            inventory.LoadFromSaveData(save.Inventory);
            int goldBefore = inventory.Gold;

            Assert.IsTrue(BountyHelper.ClaimReward(inventory));
            Assert.AreEqual(goldBefore + bounty.RewardGold, inventory.Gold, "Claim must credit the posted gold.");
            Assert.IsFalse(BountyHelper.HasActive(), "Claiming frees the contract slot.");
        }

        [Test]
        public void Claim_before_complete_is_refused()
        {
            var bounty = BountyLibrary.All().First();
            BountyHelper.Accept(bounty.Id);

            var inventory = new PlayerInventory();
            inventory.LoadFromSaveData(ProfileHelper.CurrentProfile.CurrentSave.Inventory);
            int goldBefore = inventory.Gold;

            Assert.IsFalse(BountyHelper.ClaimReward(inventory), "Incomplete contract must not pay out.");
            Assert.AreEqual(goldBefore, inventory.Gold);
            Assert.IsTrue(BountyHelper.HasActive(), "Refused claim keeps the contract active.");
        }

        [Test]
        public void Abandon_resets_slot_and_progress()
        {
            var bounty = BountyLibrary.All().First();
            BountyHelper.Accept(bounty.Id);
            BountyHelper.RecordKill(bounty.TargetClass);

            BountyHelper.Abandon();

            Assert.IsFalse(BountyHelper.HasActive());
            Assert.AreEqual(0, BountyHelper.ActiveProgress());
        }
    }
}
