// ASPECTGUARDTESTS — EditMode multi-aspect verification (US-136 / GG-LAW-8): the letterbox
// math must produce a centered, uncropped, valid-aspect viewport for every common device
// ratio — phones, tablets, and folds. The law: portrait-lock, letterbox/pillarbox, NEVER
// stretch. (True pixel-rendering verification stays on the human play script — the editor
// game view can't be resolution-driven from tests.)

using NUnit.Framework;
using UnityEngine;
using Scripts.Utilities;

namespace Scripts.Tests.EditMode
{
    [TestFixture]
    public class AspectGuardTests
    {
        // (width, height, label) — the matrix the owner asked to support: any phone/tablet.
        private static readonly (int w, int h, string label)[] Devices =
        {
            (1170, 2532, "iPhone 14 (9:19.5 - the reference)"),
            (1080, 2400, "20:9 Android flagship"),
            (1080, 2340, "19.5:9 Android"),
            (750,  1334, "iPhone SE (9:16)"),
            (1536, 2048, "iPad (3:4)"),
            (1200, 1920, "16:10 tablet"),
            (1080, 2160, "1:2 phone"),
            (1080, 2520, "9:21 ultra-tall"),
            (1812, 2176, "Fold inner display (~5:6)"),
        };

        [Test]
        public void Every_device_gets_a_valid_centered_uncropped_viewport()
        {
            foreach (var (w, h, label) in Devices)
            {
                var r = AspectGuard.LetterboxRect(w, h);

                // Within the screen, never inverted.
                Assert.GreaterOrEqual(r.x, 0f, label);
                Assert.GreaterOrEqual(r.y, 0f, label);
                Assert.LessOrEqual(r.xMax, 1f + 1e-4f, label);
                Assert.LessOrEqual(r.yMax, 1f + 1e-4f, label);
                Assert.Greater(r.width, 0f, label);
                Assert.Greater(r.height, 0f, label);

                // Bars on at most ONE axis (fit, never cropped, never both-axis shrink).
                bool fullWidth = Mathf.Approximately(r.width, 1f);
                bool fullHeight = Mathf.Approximately(r.height, 1f);
                Assert.IsTrue(fullWidth || fullHeight, $"{label}: content must span one full axis.");

                // Centered bars.
                Assert.AreEqual(r.x, 1f - r.xMax, 1e-4f, $"{label}: pillarbox must center.");
                Assert.AreEqual(r.y, 1f - r.yMax, 1e-4f, $"{label}: letterbox must center.");

                // The visible viewport renders at EXACTLY a valid aspect — never stretched.
                float device = (float)w / h;
                float rendered = (r.width * w) / (r.height * h);
                float target = AspectGuard.ClosestValidAspect(device);
                Assert.AreEqual(target, rendered, 1e-3f,
                    $"{label}: rendered aspect {rendered:0.###} must equal the snapped target {target:0.###} (no stretch).");
            }
        }

        [Test]
        public void Exact_and_near_valid_aspects_get_negligible_bars()
        {
            // 1536x2048 is EXACTLY 3:4 — true fullscreen.
            var ipad = AspectGuard.LetterboxRect(1536, 2048);
            Assert.AreEqual(1f, ipad.width, 1e-5f, "3:4 exact — no pillarbox.");
            Assert.AreEqual(1f, ipad.height, 1e-5f, "3:4 exact — no letterbox.");

            // 1170x2532 (the reference device) is 9:19.477 — a hair off the 9:19.5 snap, so it
            // gets sub-1.5% bars. That's the fit-never-crop contract, not a defect.
            var reference = AspectGuard.LetterboxRect(1170, 2532);
            Assert.Greater(reference.width, 0.985f, "Reference device bars must be negligible.");
            Assert.Greater(reference.height, 0.985f, "Reference device bars must be negligible.");
        }

        [Test]
        public void Landscape_devices_pillarbox_hard_but_stay_valid()
        {
            // The game is portrait-locked; a landscape surface gets heavy side bars, never a crash
            // or a stretch.
            var r = AspectGuard.LetterboxRect(2532, 1170);
            Assert.Greater(r.x, 0.2f, "Landscape must pillarbox substantially.");
            float rendered = (r.width * 2532f) / (r.height * 1170f);
            Assert.AreEqual(AspectGuard.ClosestValidAspect(2532f / 1170f), rendered, 1e-3f);
        }

        [Test]
        public void Safe_area_anchors_normalize_correctly()
        {
            // iPhone-style notch: safe area inset 47px top, 34px bottom on 1170x2532.
            var safe = new Rect(0, 34, 1170, 2532 - 34 - 47);
            var (min, max) = AspectGuard.SafeAreaAnchors(safe, 1170, 2532);

            Assert.AreEqual(0f, min.x, 1e-4f);
            Assert.AreEqual(34f / 2532f, min.y, 1e-4f);
            Assert.AreEqual(1f, max.x, 1e-4f);
            Assert.AreEqual((2532f - 47f) / 2532f, max.y, 1e-4f);
        }
    }
}
