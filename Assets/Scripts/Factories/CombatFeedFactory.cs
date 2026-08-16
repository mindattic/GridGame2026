using UnityEngine;
using UnityEngine.UI;
using Scripts.Canvas;

namespace Scripts.Factories
{
    /// <summary>
    /// COMBATFEEDFACTORY - Builds the scrolling battle play-by-play panel from code (no prefab).
    /// A low-contrast column pinned to the LEFT edge under the announcement banner: up to 7
    /// aging lines in a VerticalLayoutGroup, raycast-transparent (never eats input).
    /// <see cref="CombatFeed"/> drives content and aging.
    /// </summary>
    public static class CombatFeedFactory
    {
        public const float Width = 640f;
        public const float Height = 300f;

        public static CombatFeed Create(Transform parent)
        {
            var go = new GameObject(
                "CombatFeed",
                typeof(RectTransform),
                typeof(CombatFeed));
            go.layer = LayerMask.NameToLayer("UI");

            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot     = new Vector2(0f, 1f);
            // Left edge, below the AnnouncementWindow pill (-360) with breathing room.
            rt.anchoredPosition = new Vector2(20f, -480f);
            rt.sizeDelta = new Vector2(Width, Height);

            // Line container: newest lines land at the BOTTOM (lower-left anchored stack).
            var contentGO = new GameObject("Lines", typeof(RectTransform));
            contentGO.layer = go.layer;
            var contentRT = (RectTransform)contentGO.transform;
            contentRT.SetParent(rt, false);
            contentRT.anchorMin = Vector2.zero;
            contentRT.anchorMax = Vector2.one;
            contentRT.offsetMin = Vector2.zero;
            contentRT.offsetMax = Vector2.zero;

            var vlg = contentGO.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.LowerLeft;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.spacing = 2f;

            var feed = go.GetComponent<CombatFeed>();
            feed.Bind(contentRT);
            return feed;
        }
    }
}
