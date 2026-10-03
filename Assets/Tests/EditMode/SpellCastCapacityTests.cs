// SPELLCASTCAPACITYTESTS — EditMode tests for the cap on hero spells loading on the timeline
// at once (AbilityBar refuses a 5th cast-time spell before spending orbs).

using NUnit.Framework;
using Scripts.Services;

namespace Scripts.Tests.EditMode
{
    [TestFixture]
    public class SpellCastCapacityTests
    {
        [Test]
        public void Cap_is_four_hero_casts()
        {
            Assert.AreEqual(4, SpellCastCapacity.MaxHeroCastsInFlight);
        }

        [Test]
        public void Cast_time_spell_starts_below_cap_and_is_refused_at_cap()
        {
            for (int inFlight = 0; inFlight < SpellCastCapacity.MaxHeroCastsInFlight; inFlight++)
                Assert.IsTrue(SpellCastCapacity.CanStart(2f, inFlight), $"{inFlight} loading → may start.");
            Assert.IsFalse(SpellCastCapacity.CanStart(2f, SpellCastCapacity.MaxHeroCastsInFlight), "The 5th is refused.");
            Assert.IsFalse(SpellCastCapacity.CanStart(2f, SpellCastCapacity.MaxHeroCastsInFlight + 3));
        }

        [Test]
        public void Instant_spells_are_never_refused()
        {
            Assert.IsTrue(SpellCastCapacity.CanStart(0f, SpellCastCapacity.MaxHeroCastsInFlight + 10));
        }
    }
}
