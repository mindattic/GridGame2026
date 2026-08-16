using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Scripts.Canvas
{
    /// <summary>
    /// ANNOUNCEMENTWINDOW - The dedicated event-callout banner ("X casts Ice", "Boss is ENRAGED",
    /// "Slime A is poisoned", …).
    ///
    /// <para>CADENCE ([[feedback_effect_cadence]]): announcements are NEVER an instant text swap.
    /// Each is QUEUED and played with deliberate timing — a rapid <b>flash a few times</b>, a readable
    /// <b>hold</b>, then a <b>fade out</b> — one at a time, with a chiptune "Announce" sting. A flood of
    /// events reads as a paced sequence, not a flicker.</para>
    ///
    /// <para>Spawned by <see cref="Scripts.Factories.AnnouncementWindowFactory"/> (auto-created in the
    /// battle HUD). Call the static <see cref="Announce"/> from anywhere — it's a no-op if the window
    /// isn't present (e.g. non-battle scenes).</para>
    /// </summary>
    public class AnnouncementWindow : MonoBehaviour
    {
        public static AnnouncementWindow Instance { get; private set; }

        private CanvasGroup group;
        private TextMeshProUGUI label;
        private readonly Queue<string> queue = new Queue<string>();
        private bool running;

        // Cadence knobs.
        private const int FlashCount = 3;
        private const float FlashOnSeconds = 0.07f;
        private const float FlashOffSeconds = 0.06f;
        private const float HoldSeconds = 0.9f;
        private const float FadeSeconds = 0.25f;
        private const float GapSeconds = 0.08f;

        private void Awake() { Instance = this; }
        private void OnDestroy() { if (Instance == this) Instance = null; }

        /// <summary>Wires the visual parts (called by the factory). Starts hidden.</summary>
        public async void Bind(CanvasGroup canvasGroup, TextMeshProUGUI text)
        {
            group = canvasGroup;
            label = text;
            if (group != null) group.alpha = 0f;

            // Share the combat-feed icon asset so banner lines can carry <sprite> tags too.
            if (label != null && label.spriteAsset == null)
            {
                var icons = CombatFeed.Icons
                    ?? await Scripts.Helpers.AssetHelper.LoadAssetAsync<TMPro.TMP_SpriteAsset>("CombatFeedIcons");
                if (label != null && icons != null) label.spriteAsset = icons;
            }
        }

        /// <summary>Queue an announcement (deduped against the one currently showing to avoid spam).</summary>
        public void Enqueue(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            // Every banner moment also lands in the persistent play-by-play log (US-133);
            // the banner is the spotlight, the feed is the history.
            CombatFeed.Post(text);
            queue.Enqueue(text);
            if (!running) StartCoroutine(Run());
        }

        /// <summary>Announce a game event in the dedicated window. No-op if the window isn't present.</summary>
        public static void Announce(string text) => Instance?.Enqueue(text);

        private IEnumerator Run()
        {
            running = true;
            while (queue.Count > 0)
            {
                var text = queue.Dequeue();
                if (label != null) label.text = text;
                Scripts.Helpers.GameHelper.AudioManager?.Play("Announce");

                // Flash a few times in rapid succession, then settle to fully visible.
                for (int i = 0; i < FlashCount; i++)
                {
                    SetAlpha(1f);
                    yield return new WaitForSeconds(FlashOnSeconds);
                    SetAlpha(0.25f);
                    yield return new WaitForSeconds(FlashOffSeconds);
                }
                SetAlpha(1f);

                // Hold so it reads.
                yield return new WaitForSeconds(HoldSeconds);

                // Fade out.
                float t = 0f;
                while (t < FadeSeconds)
                {
                    t += Time.deltaTime;
                    SetAlpha(Mathf.Lerp(1f, 0f, t / FadeSeconds));
                    yield return null;
                }
                SetAlpha(0f);

                yield return new WaitForSeconds(GapSeconds);
            }
            running = false;
        }

        private void SetAlpha(float a)
        {
            if (group != null) group.alpha = Mathf.Clamp01(a);
        }
    }
}
