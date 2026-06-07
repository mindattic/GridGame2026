using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Scripts.Canvas;

namespace Scripts.Factories
{
    /// <summary>
    /// ANNOUNCEMENTWINDOWFACTORY - Builds the dedicated event-callout banner from code (no prefab).
    /// A translucent dark pill near the top-center of the battle HUD with a single bold TMP line;
    /// the <see cref="AnnouncementWindow"/> drives its queued flash/hold/fade cadence.
    /// </summary>
    public static class AnnouncementWindowFactory
    {
        public const float Width = 780f;
        public const float Height = 96f;

        public static AnnouncementWindow Create(Transform parent)
        {
            var go = new GameObject(
                "AnnouncementWindow",
                typeof(RectTransform),
                typeof(CanvasGroup),
                typeof(Image),
                typeof(AnnouncementWindow));
            go.layer = LayerMask.NameToLayer("UI");

            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot     = new Vector2(0.5f, 1f);
            // Top-center, below the action-title row so the two don't overlap.
            rt.anchoredPosition = new Vector2(0f, -360f);
            rt.sizeDelta = new Vector2(Width, Height);

            var bg = go.GetComponent<Image>();
            bg.color = new Color(0.05f, 0.05f, 0.08f, 0.78f); // translucent dark pill
            bg.raycastTarget = false; // purely informational — never eats input

            var group = go.GetComponent<CanvasGroup>();
            group.interactable = false;
            group.blocksRaycasts = false;

            // Child TMP line.
            var textGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGO.layer = go.layer;
            var trt = (RectTransform)textGO.transform;
            trt.SetParent(go.transform, false);
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(16f, 8f);
            trt.offsetMax = new Vector2(-16f, -8f);

            var label = textGO.GetComponent<TextMeshProUGUI>();
            label.alignment = TextAlignmentOptions.Center;
            label.enableAutoSizing = true;
            label.fontSizeMin = 18f;
            label.fontSizeMax = 44f;
            label.fontStyle = FontStyles.Bold;
            label.color = new Color(1f, 0.96f, 0.7f); // warm gold
            label.raycastTarget = false;
            label.text = string.Empty;

            var window = go.GetComponent<AnnouncementWindow>();
            window.Bind(group, label);
            return window;
        }
    }
}
