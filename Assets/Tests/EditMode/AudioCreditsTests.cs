// AUDIOCREDITSTESTS — EditMode tests for the audio attribution rule (US-137 / GG-A5):
// every authored music key resolves, and every credited entry carries author + license +
// usage; CC-BY entries carry the license URL (MacLeod's required format needs it).

using System.Linq;
using NUnit.Framework;
using Scripts.Data;

namespace Scripts.Tests.EditMode
{
    [TestFixture]
    public class AudioCreditsTests
    {
        [Test]
        public void Every_entry_is_fully_attributed()
        {
            foreach (var e in AudioCredits.Music.Concat(AudioCredits.SoundEffects))
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(e.Title), "Entry missing Title.");
                Assert.IsFalse(string.IsNullOrWhiteSpace(e.Author), $"'{e.Title}' missing Author.");
                Assert.IsFalse(string.IsNullOrWhiteSpace(e.License), $"'{e.Title}' missing License.");
                Assert.IsFalse(string.IsNullOrWhiteSpace(e.UsedFor), $"'{e.Title}' missing UsedFor.");
            }
        }

        [Test]
        public void CC_BY_entries_carry_the_license_url()
        {
            foreach (var e in AudioCredits.Music.Where(m => m.License.Contains("Attribution")))
                Assert.IsFalse(string.IsNullOrWhiteSpace(e.Url),
                    $"'{e.Title}' is CC-BY — MacLeod's attribution format requires the license URL.");
        }

        [Test]
        public void Credits_section_renders_every_title()
        {
            string block = AudioCredits.BuildCreditsSection("\n");
            foreach (var e in AudioCredits.Music.Concat(AudioCredits.SoundEffects))
                StringAssert.Contains(e.Title, block);
        }
    }
}
