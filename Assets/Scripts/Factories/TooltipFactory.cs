using Scripts.Libraries;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Helpers;
using Scripts.Hub;
using Scripts.Instances;
using Scripts.Instances.Actor;
using Scripts.Instances.Board;
using Scripts.Instances.SynergyLine;
using Scripts.Inventory;
using Scripts.Managers;
using Scripts.Models;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Factories
{
    /// <summary>
    /// TOOLTIPFACTORY - Creates tooltip popup GameObjects.
    /// 
    /// PURPOSE:
    /// Creates hoverable tooltip popups that display contextual
    /// information when the player hovers over UI elements.
    /// 
    /// VISUAL APPEARANCE:
    /// ```
    /// ┌─────────────────────────┐
    /// │ Tooltip text here...    │
    /// └─────────────────────────┘
    ///            ↑
    ///     [Hovered Element]
    /// ```
    /// 
    /// CREATED HIERARCHY:
    /// ```
    /// Tooltip (root)
    /// ├── CanvasGroup (for fading)
    /// ├── TooltipInstance (behavior)
    /// └── Background (Image - tiled)
    ///     └── Label (TMP)
    /// ```
    /// 
    /// FEATURES:
    /// - Optional fade in/out
    /// - Optional typewriter effect
    /// - Optional pointer following
    /// - Auto-destroy after delay
    /// - Configurable padding
    /// 
    /// RELATED FILES:
    /// - TooltipInstance.cs: Tooltip behavior
    /// - TooltipManager.cs: Manages tooltips
    /// </summary>
    public static class TooltipFactory
    {
        public static GameObject Create(Transform parent = null)
        {
            var root = new GameObject("Tooltip");
            root.layer = LayerMask.NameToLayer("UI");

            var rootRT = root.AddComponent<RectTransform>();
            rootRT.anchorMin = new Vector2(0.5f, 0.5f);
            rootRT.anchorMax = new Vector2(0.5f, 0.5f);
            rootRT.anchoredPosition = Vector2.zero;
            rootRT.sizeDelta = Vector2.zero;
            rootRT.pivot = new Vector2(0.5f, 0.5f);

            var canvasGroup = root.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            root.AddComponent<CanvasRenderer>();

            var tooltipInstance = root.AddComponent<TooltipInstance>();
            tooltipInstance.useFade = false;
            tooltipInstance.useTypewriter = false;
            tooltipInstance.autoDestroy = false;
            tooltipInstance.followPointer = false;
            tooltipInstance.autoDestroyDelay = 2.5f;
            tooltipInstance.horizontalPadding = 12f;
            tooltipInstance.verticalPadding = 12f;

            var background = new GameObject("Background");
            background.layer = LayerMask.NameToLayer("UI");

            var bgRT = background.AddComponent<RectTransform>();
            bgRT.SetParent(rootRT, false);
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.one;
            bgRT.anchoredPosition = Vector2.zero;
            bgRT.sizeDelta = Vector2.zero;
            bgRT.pivot = new Vector2(0.5f, 0.5f);

            background.AddComponent<CanvasRenderer>();

            var bgImage = background.AddComponent<Image>();
            bgImage.color = new Color(1f, 1f, 1f, 0.9411765f);
            bgImage.raycastTarget = true;
            bgImage.maskable = true;
            bgImage.type = Image.Type.Tiled;

            // === CHILD: Label (inside Background) ===
            var label = new GameObject("Label");
            label.layer = LayerMask.NameToLayer("UI");

            var labelRT = label.AddComponent<RectTransform>();
            labelRT.SetParent(bgRT, false);
            labelRT.anchorMin = Vector2.zero;
            labelRT.anchorMax = Vector2.one;
            labelRT.anchoredPosition = Vector2.zero;
            labelRT.sizeDelta = Vector2.zero;
            labelRT.pivot = new Vector2(0.5f, 0.5f);

            label.AddComponent<CanvasRenderer>();

            var labelTMP = label.AddComponent<TextMeshProUGUI>();
            labelTMP.font = FontLibrary.Fonts["Chicago"];
            labelTMP.text = "Message";
            labelTMP.fontSize = 18;
            labelTMP.color = Color.white;
            labelTMP.alignment = TextAlignmentOptions.Center;
            labelTMP.enableWordWrapping = true;
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
