using UnityEngine;
using UnityEngine.UI;
using Scripts.Canvas;

namespace Scripts.Factories
{
    /// <summary>
    /// SHIELDBUTTONFACTORY - Builds the HUD shield button from code (no prefab). Replaces the old
    /// blinking-dot Bank button position (bottom-right of the timeline bar).
    ///
    /// <para>Until a real shield sprite is wired in, the button is drawn as a steel-blue rectangle
    /// with a slightly taller aspect (shield silhouette hint). A small darker inset Image gives it
    /// a "boss-shield" feel.</para>
    /// </summary>
    public static class ShieldButtonFactory
    {
        public const float Width = 52f;
        public const float Height = 64f;

        public static ShieldButton Create(Transform parent)
        {
            // Root: button + body image
            var go = new GameObject(
                "ShieldButton",
                typeof(RectTransform),
                typeof(Image),
                typeof(Button),
                typeof(ShieldButton));
            go.layer = LayerMask.NameToLayer("UI");

            // Anchored bottom-right of the timeline (Row 2 area). Y from HudLayout — single source
            // of truth shared with GameBuilder.
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot     = new Vector2(1f, 0.5f);
            rt.anchoredPosition = new Vector2(-16f, Scripts.Utilities.HudLayout.Row2Y_FromTop);
            rt.sizeDelta = new Vector2(Width, Height);

            var img = go.GetComponent<Image>();
            img.color = new Color(0.25f, 0.40f, 0.65f); // steel blue — shield body

            // Inset "boss" — a smaller darker square centered, giving the rect a shield feel.
            var bossGO = new GameObject("Boss", typeof(RectTransform), typeof(Image));
            bossGO.layer = go.layer;
            var brt = (RectTransform)bossGO.transform;
            brt.SetParent(go.transform, false);
            brt.anchorMin = new Vector2(0.5f, 0.5f);
            brt.anchorMax = new Vector2(0.5f, 0.5f);
            brt.pivot     = new Vector2(0.5f, 0.5f);
            brt.anchoredPosition = Vector2.zero;
            brt.sizeDelta = new Vector2(Width * 0.45f, Height * 0.45f);
            bossGO.GetComponent<Image>().color = new Color(0.12f, 0.22f, 0.42f);

            // Wire click → ShieldButton.Click (applies Protection + auto-skips).
            var btn = go.GetComponent<Button>();
            var shield = go.GetComponent<ShieldButton>();
            btn.onClick.AddListener(shield.Click);

            return shield;
        }
    }
}
