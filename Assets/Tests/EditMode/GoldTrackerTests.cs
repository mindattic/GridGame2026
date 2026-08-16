// GOLDTRACKERTESTS — EditMode tests for the coins→gold battle bridge (GoldTracker):
// the session snapshot of GameHelper.TotalCoins, the Collected delta, the commit into
// save.Inventory.Gold, and the no-double-count guarantee across consecutive battles.
// Runs against an isolated throwaway profile (TotalCoins lives on the live save).

using System.IO;
using NUnit.Framework;
using UnityEngine;
using Scripts.Helpers;
using Scripts.Managers;
using g = Scripts.Helpers.GameHelper;

namespace Scripts.Tests.EditMode
{
    [TestFixture]
    public class GoldTrackerTests
    {
        private string isolatedRoot;

        [SetUp]
        public void SetUp()
        {
            isolatedRoot = Path.Combine(
                Application.temporaryCachePath, "TestProfiles",
                System.Guid.NewGuid().ToString("N"));
            TestHooks.CreateIsolatedProfile(isolatedRoot, "GoldTest");
        }

        [TearDown]
        public void TearDown()
        {
            GoldTracker.Clear();
            TestHooks.ClearIsolatedProfileRoot();
            if (!string.IsNullOrEmpty(isolatedRoot) && Directory.Exists(isolatedRoot))
                Directory.Delete(isolatedRoot, recursive: true);
        }

        [Test]
        public void Collected_is_the_coins_earned_since_session_start()
        {
            g.TotalCoins = 100;
            GoldTracker.StartSession();
            Assert.AreEqual(0, GoldTracker.Collected, "Nothing collected at session start.");

            g.TotalCoins += 25; // simulate 25 coin pickups (CoinInstance.Despawn path)
            Assert.AreEqual(25, GoldTracker.Collected);
        }

        [Test]
        public void Commit_adds_collected_coins_to_spendable_gold()
        {
            int goldBefore = TestHooks.InventoryGold;

            GoldTracker.StartSession();
            g.TotalCoins += 40;
            GoldTracker.CommitToInventory();

            Assert.AreEqual(goldBefore + 40, TestHooks.InventoryGold,
                "Coins collected in battle must become vendor-spendable gold.");
        }

        [Test]
        public void Consecutive_battles_never_double_count()
        {
            int goldBefore = TestHooks.InventoryGold;

            GoldTracker.StartSession();
            g.TotalCoins += 30;
            GoldTracker.CommitToInventory();
            GoldTracker.Clear();

            // Second battle: no pickups. Committing again must add nothing.
            GoldTracker.StartSession();
            GoldTracker.CommitToInventory();
            GoldTracker.Clear();

            Assert.AreEqual(goldBefore + 30, TestHooks.InventoryGold);
        }

        [Test]
        public void Collected_is_zero_outside_a_session()
        {
            GoldTracker.Clear();
            g.TotalCoins += 500;
            Assert.AreEqual(0, GoldTracker.Collected,
                "Coins earned outside a battle session must not leak into the next commit.");
        }

        [Test]
        public void Lifetime_ticker_is_untouched_by_commit()
        {
            GoldTracker.StartSession();
            g.TotalCoins = 200;
            int tickerBefore = g.TotalCoins;

            GoldTracker.CommitToInventory();

            Assert.AreEqual(tickerBefore, g.TotalCoins,
                "The lifetime coin ticker (Global.TotalCoins) is a stat, not a wallet — commits must not drain it.");
        }
    }
}
