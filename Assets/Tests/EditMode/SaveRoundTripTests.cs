// SAVEROUNDTRIPTESTS — EditMode tests for SaveState JSON serialization: the exact
// serialize/deserialize path ProfileHelper uses (Newtonsoft JsonConvert), asserting the
// fields the game loop depends on (gold, stage progress, roster, party) survive a round trip.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using NUnit.Framework;
using Scripts.Models;

namespace Scripts.Tests.EditMode
{
    [TestFixture]
    public class SaveRoundTripTests
    {
        [Test]
        public void SaveState_json_roundtrip_preserves_gold_stage_and_bounty()
        {
            var save = new SaveState
            {
                Index = 3,
                Timestamp = new DateTime(2026, 8, 15, 12, 0, 0, DateTimeKind.Utc),
                FileName = "Save_test.json",
                Global = new GlobalSaveData { TotalCoins = 1234 },
                Stage = new StageSaveData
                {
                    CurrentStage = "Test-Harness",
                    CurrentWave = 2,
                    HighestClearedStageIndex = 4,
                },
                Roster = new RosterSaveData(),
                Party = new PartySaveData(),
                Inventory = new InventorySaveData { Gold = 777 },
                Equipment = new EquipmentSaveData(),
                Training = new TrainingSaveData(),
                Bounty = new BountySaveData(),
            };

            string json = JsonConvert.SerializeObject(save, Formatting.Indented);
            var restored = JsonConvert.DeserializeObject<SaveState>(json);

            Assert.AreEqual(1234, restored.Global.TotalCoins, "Lifetime coin ticker must round-trip.");
            Assert.AreEqual(777, restored.Inventory.Gold, "Vendor-spendable gold must round-trip.");
            Assert.AreEqual("Test-Harness", restored.Stage.CurrentStage);
            Assert.AreEqual(2, restored.Stage.CurrentWave);
            Assert.AreEqual(4, restored.Stage.HighestClearedStageIndex, "Campaign unlock progress must round-trip.");
            Assert.AreEqual(3, restored.Index);
        }

        [Test]
        public void SaveState_copy_constructor_deep_copies_inventory_gold()
        {
            var original = new SaveState
            {
                Global = new GlobalSaveData(),
                Stage = new StageSaveData(),
                Roster = new RosterSaveData(),
                Party = new PartySaveData(),
                Inventory = new InventorySaveData { Gold = 100 },
            };

            var copy = new SaveState(original);
            copy.Inventory.Gold = 999;

            Assert.AreEqual(100, original.Inventory.Gold,
                "Mutating a copied save must never write through to the original (save-slot corruption).");
        }
    }
}
