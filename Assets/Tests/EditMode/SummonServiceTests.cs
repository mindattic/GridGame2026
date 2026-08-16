// SUMMONSERVICETESTS — EditMode tests for the Summon Circle recruit rules (US-132):
// pool membership, rising cost, gold gating, roster append, refuse-duplicates, and
// persistence through the save round trip. Isolated throwaway profile.

using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Scripts.Helpers;
using Scripts.Inventory;
using Scripts.Services;

namespace Scripts.Tests.EditMode
{
    [TestFixture]
    public class SummonServiceTests
    {
        private string isolatedRoot;

        [SetUp]
        public void SetUp()
        {
            isolatedRoot = Path.Combine(
                Application.temporaryCachePath, "TestProfiles",
                System.Guid.NewGuid().ToString("N"));
            TestHooks.CreateIsolatedProfile(isolatedRoot, "SummonTest");
        }

        [TearDown]
        public void TearDown()
        {
            TestHooks.ClearIsolatedProfileRoot();
            if (!string.IsNullOrEmpty(isolatedRoot) && Directory.Exists(isolatedRoot))
                Directory.Delete(isolatedRoot, recursive: true);
        }

        [Test]
        public void Fresh_save_starts_with_trio_and_full_pool_unowned()
        {
            var save = ProfileHelper.CurrentProfile.CurrentSave;
            Assert.AreEqual(3, save.Roster.Members.Count, "Fresh roster = the starting trio (GG-A5).");
            foreach (var characterClass in SummonService.Pool)
                Assert.IsFalse(SummonService.IsOwned(save, characterClass), $"{characterClass} must start unowned.");
        }

        [Test]
        public void Recruit_deducts_gold_appends_roster_and_cost_rises()
        {
            var save = ProfileHelper.CurrentProfile.CurrentSave;
            var inventory = new PlayerInventory();
            inventory.LoadFromSaveData(save.Inventory);
            inventory.Gold = 10_000;

            int firstCost = SummonService.NextCost(save);
            Assert.AreEqual(SummonService.BaseCost, firstCost, "First recruit costs the base price.");

            var recruit = SummonService.Pool.First();
            Assert.IsTrue(SummonService.TryRecruit(save, inventory, recruit));
            Assert.AreEqual(10_000 - firstCost, inventory.Gold);
            Assert.IsTrue(SummonService.IsOwned(save, recruit));
            Assert.AreEqual(4, save.Roster.Members.Count);

            Assert.AreEqual(SummonService.BaseCost + SummonService.CostPerRecruit,
                SummonService.NextCost(save), "Second recruit costs more.");
        }

        [Test]
        public void Recruit_refuses_duplicates_and_short_gold()
        {
            var save = ProfileHelper.CurrentProfile.CurrentSave;
            var inventory = new PlayerInventory();
            inventory.LoadFromSaveData(save.Inventory);

            var recruit = SummonService.Pool.First();
            inventory.Gold = SummonService.NextCost(save) - 1;
            Assert.IsFalse(SummonService.TryRecruit(save, inventory, recruit), "Short gold must refuse.");
            Assert.AreEqual(3, save.Roster.Members.Count, "Refused recruit must not mutate the roster.");

            inventory.Gold = 10_000;
            Assert.IsTrue(SummonService.TryRecruit(save, inventory, recruit));
            int goldAfterFirst = inventory.Gold;
            Assert.IsFalse(SummonService.TryRecruit(save, inventory, recruit), "Duplicates must refuse.");
            Assert.AreEqual(goldAfterFirst, inventory.Gold, "Refused duplicate must not charge.");
        }

        [Test]
        public void Recruited_hero_survives_save_reload()
        {
            var save = ProfileHelper.CurrentProfile.CurrentSave;
            var inventory = new PlayerInventory();
            inventory.LoadFromSaveData(save.Inventory);
            inventory.Gold = 10_000;

            var recruit = SummonService.Pool.First();
            Assert.IsTrue(SummonService.TryRecruit(save, inventory, recruit));
            save.Inventory = inventory.ToSaveData();
            Assert.IsTrue(ProfileHelper.Save(overwrite: true));

            ProfileHelper.Reload();
            var reloaded = ProfileHelper.CurrentProfile.CurrentSave;
            Assert.IsTrue(SummonService.IsOwned(reloaded, recruit),
                "A recruited hero must survive the disk round trip.");
        }
    }
}
