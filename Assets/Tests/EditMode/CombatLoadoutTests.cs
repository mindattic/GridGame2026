// COMBATLOADOUTTESTS — EditMode tests that the combat AbilityBar shows the per-hero bar the
// player saved in the Abilities scene (HeroEquipmentSave.AbilityBarSlots), falling back to the
// class preset (HeroLoadouts) only when that saved bar is empty.

using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using Scripts.Data;
using Scripts.Data.Items;
using Scripts.Helpers;
using Scripts.Models;
using Scripts.Services;

namespace Scripts.Tests.EditMode
{
    [TestFixture]
    public class CombatLoadoutTests
    {
        private static readonly string DartId = ItemData_Consumables.SleepDart.Id;

        private static HeroEquipmentSave SaveWith(params AbilityBarSlotSave[] slots) =>
            new HeroEquipmentSave
            {
                CharacterClass = CharacterClass.Cleric,
                AbilityBarSlots = new List<AbilityBarSlotSave>(slots),
            };

        private static InventorySaveData Owning(string itemId, int count) =>
            new InventorySaveData { Items = new List<InventoryEntrySave> { new InventoryEntrySave { ItemId = itemId, Count = count } } };

        [Test]
        public void Saved_bar_wins_over_class_preset_and_keeps_positions()
        {
            var save = SaveWith(
                new AbilityBarSlotSave(),
                new AbilityBarSlotSave(null, DartId),
                new AbilityBarSlotSave("Fireball", null),
                new AbilityBarSlotSave(),
                new AbilityBarSlotSave());

            var bar = CombatLoadouts.Resolve(CharacterClass.Cleric, save, Owning(DartId, 3));

            Assert.AreEqual(CombatLoadouts.SlotCount, bar.Count);
            Assert.IsNull(bar[0]);
            Assert.AreEqual(AbilityKind.Item, bar[1].Kind);
            Assert.AreEqual(DartId, bar[1].SourceItemId, "Item slots stay linked to their item (Sleep Dart → Sleep).");
            Assert.AreEqual(3, bar[1].Charges, "Charges = owned count.");
            Assert.AreSame(ManaAbilities.Fireball, bar[2], "Named slots resolve to the combat-bar catalog entry.");
            Assert.IsNull(bar[3]);
            Assert.IsNull(bar[4]);
        }

        [Test]
        public void Item_charges_are_capped_at_max_stack()
        {
            var bar = CombatLoadouts.Resolve(CharacterClass.Cleric, SaveWith(new AbilityBarSlotSave(null, DartId)), Owning(DartId, 99));
            Assert.AreEqual(ItemData_Consumables.SleepDart.MaxStack, bar[0].Charges);
        }

        [Test]
        public void Empty_saved_bar_falls_back_to_class_preset()
        {
            var empty = SaveWith(new AbilityBarSlotSave(), new AbilityBarSlotSave(), new AbilityBarSlotSave());
            Assert.AreSame(HeroLoadouts.For(CharacterClass.Cleric), CombatLoadouts.Resolve(CharacterClass.Cleric, empty, null));
            Assert.AreSame(HeroLoadouts.For(CharacterClass.Cleric), CombatLoadouts.Resolve(CharacterClass.Cleric, null, null));
        }

        [Test]
        public void Unknown_entries_render_as_empty_slots()
        {
            var save = SaveWith(
                new AbilityBarSlotSave("No Such Ability", null),
                new AbilityBarSlotSave(null, "no_such_item"),
                AbilityBarSlotSave.ForWeapon("eq_sword_iron"));
            var bar = CombatLoadouts.Resolve(CharacterClass.Cleric, save, null);
            Assert.IsNull(bar[0]);
            Assert.IsNull(bar[1]);
            Assert.IsNull(bar[2]);
        }

        // ── Live save path (what AbilityBar actually calls) ──

        private string isolatedRoot;

        [SetUp]
        public void SetUp()
        {
            isolatedRoot = Path.Combine(Application.temporaryCachePath, "TestProfiles", System.Guid.NewGuid().ToString("N"));
            TestHooks.CreateIsolatedProfile(isolatedRoot, "CombatLoadoutTest");
            CombatLoadouts.ResetForBattle();
        }

        [TearDown]
        public void TearDown()
        {
            CombatLoadouts.ResetForBattle();
            TestHooks.ClearIsolatedProfileRoot();
            if (!string.IsNullOrEmpty(isolatedRoot) && Directory.Exists(isolatedRoot))
                Directory.Delete(isolatedRoot, recursive: true);
        }

        [Test]
        public void Live_save_choice_reaches_the_combat_bar()
        {
            var save = ProfileHelper.CurrentProfile.CurrentSave;
            if (save.Equipment == null) save.Equipment = new EquipmentSaveData();
            save.Equipment.GetOrCreate(CharacterClass.Paladin).AbilityBarSlots = new List<AbilityBarSlotSave>
            {
                new AbilityBarSlotSave("Frost", null),
            };

            var bar = CombatLoadouts.For(CharacterClass.Paladin);
            Assert.AreSame(ManaAbilities.Frost, bar[0], "The Abilities-scene choice must be what combat binds.");
            Assert.AreSame(bar, CombatLoadouts.For(CharacterClass.Paladin), "Cached per battle so item charges persist.");

            Assert.AreSame(HeroLoadouts.For(CharacterClass.Barbarian), CombatLoadouts.For(CharacterClass.Barbarian),
                "A hero with no saved bar uses the class preset.");
        }
    }
}
