using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Scripts.Canvas
{
    /// <summary>
    /// COMBATFEED - The scrolling play-by-play log of the battle (US-133 / GG-A5).
    ///
    /// <para>PURPOSE: The <see cref="AnnouncementWindow"/> banner shows ONE event at a time and
    /// fades; this feed keeps the recent history visible so the player can follow the fight:
    /// "Paladin casts Heal", "Enemy bites Rogue; Rogue is poisoned". Newest line at the bottom;
    /// old lines age out (alpha decay) and are recycled. Supports TMP rich text including
    /// &lt;sprite&gt; icon tags once the feed sprite asset is assigned.</para>
    ///
    /// <para>CHANNELS: <see cref="AnnouncementWindow.Announce"/> = banner + feed (big moments).
    /// <see cref="Post"/> = feed only (high-frequency lines like damage ticks and assists that
    /// would spam the banner). Both are no-ops outside battle.</para>
    ///
    /// <para>RELATED FILES: CombatFeedFactory.cs, AnnouncementWindow.cs, ManaPoolManager.cs
    /// (HUD boot), docs/USER_STORIES.md US-133.</para>
    /// </summary>
    public class CombatFeed : MonoBehaviour
    {
        public static CombatFeed Instance { get; private set; }

        private const int MaxLines = 7;
        private const float LineFadeDelaySeconds = 6f;   // full brightness this long...
        private const float LineFadeSeconds = 3f;        // ...then fades to MinAlpha
        private const float MinAlpha = 0.35f;            // aged lines stay faintly readable

        private readonly List<TextMeshProUGUI> lines = new List<TextMeshProUGUI>();
        private readonly List<float> postedAt = new List<float>();
        private RectTransform content;

        /// <summary>The shared icon sprite asset (Addressable "CombatFeedIcons"), loaded once —
        /// enables &lt;sprite name="Fireball"&gt; / &lt;sprite name="Poisoned"&gt; tags in feed
        /// and banner text. Null until the async load lands (tags simply don't render yet).</summary>
        public static TMP_SpriteAsset Icons { get; private set; }

        private void Awake() { Instance = this; }
        private void OnDestroy() { if (Instance == this) Instance = null; }

        /// <summary>Wires the line container (called by the factory) and kicks the icon load.</summary>
        public async void Bind(RectTransform lineContainer)
        {
            content = lineContainer;
            if (Icons == null)
                Icons = await Scripts.Helpers.AssetHelper.LoadAssetAsync<TMP_SpriteAsset>("CombatFeedIcons");
            // Retro-fit any lines created before the load landed.
            if (Icons != null)
                foreach (var line in lines)
                    if (line != null) line.spriteAsset = Icons;
        }

        /// <summary>Feed-only post (no banner). No-op when no feed exists (non-battle scenes).</summary>
        public static void Post(string richText) => Instance?.Append(richText);

        /// <summary>An inline icon tag for <paramref name="name"/> (trailing space included), or
        /// empty when the glyph isn't in the loaded sprite asset — callers can prepend this
        /// unconditionally and never leak a raw &lt;sprite&gt; tag as visible text.</summary>
        public static string Icon(string name)
        {
            if (Icons == null || string.IsNullOrEmpty(name)) return string.Empty;
            return Icons.GetSpriteIndexFromName(name) >= 0 ? $"<sprite name=\"{name}\"> " : string.Empty;
        }

        /// <summary>Appends a line, recycling the oldest once at capacity.</summary>
        public void Append(string richText)
        {
            if (string.IsNullOrEmpty(richText) || content == null) return;

            TextMeshProUGUI line;
            if (lines.Count < MaxLines)
            {
                line = CreateLine();
                lines.Add(line);
                postedAt.Add(0f);
            }
            else
            {
                // Recycle the oldest (index 0) to the bottom.
                line = lines[0];
                lines.RemoveAt(0);
                postedAt.RemoveAt(0);
                lines.Add(line);
                postedAt.Add(0f);
                line.rectTransform.SetAsLastSibling();
            }

            line.text = richText;
            postedAt[postedAt.Count - 1] = Time.unscaledTime;
            SetLineAlpha(line, 1f);
        }

        private void Update()
        {
            // Age lines: hold, then fade toward MinAlpha. Unscaled so pacing accel doesn't
            // blink the log out.
            for (int i = 0; i < lines.Count; i++)
            {
                float age = Time.unscaledTime - postedAt[i];
                if (age <= LineFadeDelaySeconds) continue;
                float t = Mathf.Clamp01((age - LineFadeDelaySeconds) / LineFadeSeconds);
                SetLineAlpha(lines[i], Mathf.Lerp(1f, MinAlpha, t));
            }
        }

        private TextMeshProUGUI CreateLine()
        {
            var go = new GameObject("FeedLine", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.layer = gameObject.layer;
            var rt = (RectTransform)go.transform;
            rt.SetParent(content, false);

            var tmp = go.GetComponent<TextMeshProUGUI>();
            if (Icons != null) tmp.spriteAsset = Icons;
            tmp.fontSize = 26f;
            tmp.enableWordWrapping = true;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.color = new Color(0.92f, 0.92f, 0.92f);
            tmp.raycastTarget = false;
            tmp.text = string.Empty;

            var le = go.AddComponent<UnityEngine.UI.LayoutElement>();
            le.minHeight = 34f;
            le.flexibleWidth = 1f;
            return tmp;
        }

        private static void SetLineAlpha(TextMeshProUGUI line, float a)
        {
            var c = line.color;
            line.color = new Color(c.r, c.g, c.b, a);
        }
    }
}
