// PROFILEPERSISTENCETESTS — EditMode tests of the vendor persistence contract every shop
// scene relies on: hydrate PlayerInventory from the save, mutate (buy/sell = gold + items),
// write back via ToSaveData, persist with ProfileHelper.Save(overwrite: true), then Reload
// from disk and assert the mutation survived. Runs against an isolated throwaway profile.

using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Scripts.Helpers;
using Scripts.Inventory;

namespace Scripts.Tests.EditMode
{
    [TestFixture]
    public class ProfilePersistenceTests
    {
        private string isolatedRoot;

        [SetUp]
        public void SetUp()
        {
            isolatedRoot = Path.Combine(
                Application.temporaryCachePath, "TestProfiles",
                System.Guid.NewGuid().ToString("N"));
            TestHooks.CreateIsolatedProfile(isolatedRoot, "PersistTest");
        }

        [TearDown]
        public void TearDown()
        {
            TestHooks.ClearIsolatedProfileRoot();
            if (!string.IsNullOrEmpty(isolatedRoot) && Directory.Exists(isolatedRoot))
                Directory.Delete(isolatedRoot, recursive: true);
        }

        [Test]
        public void Gold_mutation_survives_save_and_reload()
        {
            var save = ProfileHelper.CurrentProfile.CurrentSave;

            var inventory = new PlayerInventory();
            inventory.LoadFromSaveData(save.Inventory);
            int before = inventory.Gold;

            inventory.Gold = before + 123;
            save.Inventory = inventory.ToSaveData();
            Assert.IsTrue(ProfileHelper.Save(overwrite: true), "Persisting the save must succeed.");

            ProfileHelper.Reload();

            var reloaded = ProfileHelper.CurrentProfile.CurrentSave;
            Assert.AreEqual(before + 123, reloaded.Inventory.Gold,
                "Gold spent/earned at a vendor must survive a full disk round trip.");
        }

        [Test]
        public void Fresh_profile_has_a_current_save_with_starter_inventory()
        {
            var save = ProfileHelper.CurrentProfile.CurrentSave;

            Assert.IsNotNull(save, "CreateProfile must leave a usable CurrentSave.");
            Assert.IsNotNull(save.Inventory, "Starter inventory must exist.");
            Assert.Greater(save.Inventory.Gold, 0, "Starter gold must be non-zero so the first vendor visit works.");
            Assert.IsNotNull(save.Party, "Starter party must exist.");
        }

        [Test]
        public void Profile_isolation_never_touches_real_profile_folder()
        {
            StringAssert.StartsWith(isolatedRoot, FolderHelper.Folder.Profiles,
                "With the override set, all profile IO must resolve under the isolated root.");
            Assert.IsTrue(ProfileHelper.CurrentProfile.Folder.StartsWith(isolatedRoot),
                "The created profile must live inside the isolated root.");
        }
    }
}
