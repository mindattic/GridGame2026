// ABILITYBARSLOTCOUNTTESTS — EditMode tests that the combat bar has exactly as many slots as
// campaign progress can unlock (§4.7: hard max 5), so no slot is permanently locked, and that
// every class preset fits — including the Alchemist's Sleep Dart.

using System;
using NUnit.Framework;
using Scripts.Data;
using Scripts.Data.Items;
using Scripts.Factories;
using Scripts.Helpers;
using Scripts.Services;
using Scripts.Vendor.Abilities;

namespace Scripts.Tests.EditMode
{
    [TestFixture]
    public class AbilityBarSlotCountTests
    {
        private static int FullyUnlocked => AbilitySlotProgression.UnlockedSlots(int.MaxValue);

        [Test]
        public void Combat_bar_has_one_button_per_unlockable_slot()
        {
            Assert.AreEqual(AbilitySlotProgression.MaxSlots, AbilityBarFactory.Slots,
                "The combat bar must not render a slot that can never unlock.");
            Assert.AreEqual(FullyUnlocked, AbilityBarFactory.Slots,
                "A fully progressed save must be able to use every combat-bar slot.");
        }

        [Test]
        public void Combat_bar_and_abilities_scene_agree_on_slot_count()
        {
            Assert.AreEqual(AbilitiesManager.SlotCount, AbilityBarFactory.Slots);
            Assert.AreEqual(AbilitiesManager.SlotCount, CombatLoadouts.SlotCount);
        }

        [Test]
        public void Every_class_preset_fits_the_bar()
        {
            foreach (CharacterClass cls in Enum.GetValues(typeof(CharacterClass)))
            {
                var loadout = HeroLoadouts.For(cls);
                Assert.LessOrEqual(loadout.Count, AbilityBarFactory.Slots,
                    $"{cls}'s preset has entries past the last slot; they could never be used.");
            }
            Assert.LessOrEqual(ManaAbilities.Slots.Count, AbilityBarFactory.Slots);
        }

        [Test]
        public void Alchemist_sleep_dart_sits_in_an_unlockable_slot()
        {
            var loadout = HeroLoadouts.For(CharacterClass.Alchemist);
            int index = -1;
            for (int i = 0; i < loadout.Count; i++)
                if (loadout[i] != null && loadout[i].SourceItemId == ItemData_Consumables.SleepDart.Id) { index = i; break; }

            Assert.GreaterOrEqual(index, 0, "The Alchemist preset must carry the Sleep Dart.");
            Assert.Less(index, FullyUnlocked, "The Sleep Dart slot must be unlockable.");
        }
    }
}
