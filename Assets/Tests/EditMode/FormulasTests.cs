// FORMULASTESTS — EditMode pure-logic tests for Scripts.Utilities.Formulas.
// No scene, no GameManager: ActorStats is a plain class and the formulas under test
// are deterministic (or seeded via TestHooks.SeedRng).

using NUnit.Framework;
using Scripts.Helpers;
using Scripts.Models.Actor;
using Scripts.Utilities;

namespace Scripts.Tests.EditMode
{
    [TestFixture]
    public class FormulasTests
    {
        [TearDown]
        public void TearDown() => TestHooks.UnseedRng();

        [Test]
        public void Health_increases_with_vitality()
        {
            var low = new ActorStats { Level = 1, Vitality = 2 };
            var high = new ActorStats { Level = 1, Vitality = 10 };

            Assert.Greater(Formulas.Health(high), Formulas.Health(low),
                "Higher Vitality must yield higher max HP.");
        }

        [Test]
        public void CastTime_is_floored_and_scales_down_with_wis_int()
        {
            const float baseSeconds = 4f;

            float slow = Formulas.CastTime(baseSeconds, wisdom: 0f, intelligence: 0f);
            float fast = Formulas.CastTime(baseSeconds, wisdom: 50f, intelligence: 50f);

            Assert.AreEqual(baseSeconds, slow, 0.001f, "Zero skill = uncompressed cast time.");
            Assert.Less(fast, slow, "Wisdom+Intelligence must speed up casting.");
            Assert.GreaterOrEqual(fast, baseSeconds * 0.25f, "Cast time never drops below the 25% floor.");
            Assert.AreEqual(0f, Formulas.CastTime(0f, 100f, 100f), "Instant abilities stay instant.");
        }
    }
}
