using Assets.Scripts.Canvas;
using Assets.Scripts.Libraries;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Factories
{
    /// <summary>
    /// Programmatic factory for TimelineTag - replaces TimelineTagPrefab.prefab
    /// Hierarchy:
    /// - TimelineTag (root)
    ///   - Tag (Image - main background)
    ///   - Icon (Image - actor icon)
    ///   - Label (TextMeshProUGUI - time label)
    /// </summary>
    public static class TimelineTagFactory
    {
        public static GameObject Create(Transform parent = null)
        {
            // === ROOT: TimelineTag ===
            var root = new GameObject("TimelineTag");
            root.layer = LayerMask.NameToLayer("Default");

            var rootRT = root.AddComponent<RectTransform>();
            rootRT.anchorMin = new Vector2(0.5f, 0.5f);
            rootRT.anchorMax = new Vector2(0.5f, 0.5f);
            rootRT.anchoredPosition = Vector2.zero;
            rootRT.sizeDelta = Vector2.zero;
            rootRT.pivot = new Vector2(0.5f, 1f);

            root.AddComponent<CanvasRenderer>();

            // CanvasGroup (for fade-out)
            var canvasGroup = root.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            // TimelineTag (custom component)
            root.AddComponent<TimelineTag>();

            // === CHILD: Tag (background image) ===
            var tag = new GameObject("Tag");
            tag.layer = LayerMask.NameToLayer("Default");

            var tagRT = tag.AddComponent<RectTransform>();
            tagRT.SetParent(rootRT, false);
            tagRT.anchorMin = new Vector2(0.5f, 0.5f);
            tagRT.anchorMax = new Vector2(0.5f, 0.5f);
            tagRT.anchoredPosition = new Vector2(0f, -16f);
            tagRT.sizeDelta = new Vector2(64f, 64f);
            tagRT.pivot = new Vector2(0.5f, 0f);

            tag.AddComponent<CanvasRenderer>();

                        var tagImage = tag.AddComponent<Image>();
                        // sprite left null - assigned at runtime or via TimelineTag component
                        tagImage.color = Color.white;
                        tagImage.raycastTarget = true;
                        tagImage.maskable = true;
                        tagImage.type = Image.Type.Simple;

            // === CHILD: Icon (actor portrait) ===
            var icon = new GameObject("Icon");
            icon.layer = LayerMask.NameToLayer("Default");

            var iconRT = icon.AddComponent<RectTransform>();
            iconRT.SetParent(rootRT, false);
            iconRT.anchorMin = new Vector2(0.5f, 0.5f);
            iconRT.anchorMax = new Vector2(0.5f, 0.5f);
            iconRT.anchoredPosition = new Vector2(0f, 26f);
            iconRT.sizeDelta = new Vector2(32f, 32f);
            iconRT.pivot = new Vector2(0.5f, 0.5f);

            icon.AddComponent<CanvasRenderer>();

            var iconImage = icon.AddComponent<Image>();
            iconImage.color = Color.white;
            iconImage.raycastTarget = true;
            iconImage.maskable = true;
            iconImage.type = Image.Type.Simple;

            // === CHILD: Label (time text) ===
            var label = new GameObject("Label");
            label.layer = LayerMask.NameToLayer("Default");

            var labelRT = label.AddComponent<RectTransform>();
            labelRT.SetParent(rootRT, false);
            labelRT.anchorMin = new Vector2(0.5f, 0.5f);
            labelRT.anchorMax = new Vector2(0.5f, 0.5f);
            labelRT.anchoredPosition = new Vector2(0f, -18f);
            labelRT.sizeDelta = Vector2.zero;
            labelRT.pivot = new Vector2(0.5f, 0f);

            label.AddComponent<CanvasRenderer>();

            var labelTMP = label.AddComponent<TextMeshProUGUI>();
            labelTMP.font = FontLibrary.Fonts["Avenir"];
            labelTMP.text = "0.0";
            labelTMP.fontSize = 24;
            labelTMP.color = Color.white;
            labelTMP.alignment = TextAlignmentOptions.Center;
            labelTMP.enableWordWrapping = false;
            labelTMP.overflowMode = TextOverflowModes.Overflow;
            labelTMP.richText = true;
            labelTMP.raycastTarget = true;

            // Parent if specified
            if (parent != null)
            {
                rootRT.SetParent(parent, false);
            }

            return root;
        }
    }
}
