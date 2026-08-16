// ABILITYSLOTPROGRESSIONTESTS — EditMode tests for the ability-bar slot unlock gates
// (US-143 / GG-A6): 2 slots on a fresh save, +1 at each campaign gate, hard max 5.

using NUnit.Framework;
using Scripts.Services;

namespace Scripts.Tests.EditMode
{
    [TestFixture]
    public class AbilitySlotProgressionTests
    {
        [Test]
        public void Fresh_save_has_two_slots()
        {
            Assert.AreEqual(2, AbilitySlotProgression.UnlockedSlots(-1));
        }

        [Test]
        public void Gates_open_one_slot_each()
        {
            Assert.AreEqual(3, AbilitySlotProgression.UnlockedSlots(0), "Clearing stage 1 opens slot 3.");
            Assert.AreEqual(3, AbilitySlotProgression.UnlockedSlots(1), "No gate between 0 and 2.");
            Assert.AreEqual(4, AbilitySlotProgression.UnlockedSlots(2), "First theme done opens slot 4.");
            Assert.AreEqual(4, AbilitySlotProgression.UnlockedSlots(4));
            Assert.AreEqual(5, AbilitySlotProgression.UnlockedSlots(5), "Second theme done opens slot 5.");
        }

        [Test]
        public void Never_exceeds_max()
        {
            Assert.AreEqual(5, AbilitySlotProgression.UnlockedSlots(999));
        }

        [Test]
        public void GateForSlot_names_the_right_stage()
        {
            Assert.AreEqual(-1, AbilitySlotProgression.GateForSlot(0), "Starting slots have no gate.");
            Assert.AreEqual(-1, AbilitySlotProgression.GateForSlot(1));
            Assert.AreEqual(0, AbilitySlotProgression.GateForSlot(2));
            Assert.AreEqual(2, AbilitySlotProgression.GateForSlot(3));
            Assert.AreEqual(5, AbilitySlotProgression.GateForSlot(4));
            Assert.AreEqual(-1, AbilitySlotProgression.GateForSlot(5), "Beyond MaxSlots.");
        }
    }
}
