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
using Scripts.Libraries;
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
    /// ENTERHUBHUTTONFACTORY - Creates the "Enter Hub" proximity button.
    ///
    /// PURPOSE:
    /// Programmatically creates the Enter Hub button that appears when the
    /// OverworldHero approaches the Caravan. Positioned bottom-center of the
    /// canvas to stay accessible on all screen sizes.
    ///
    /// VISUAL APPEARANCE:
    /// ```
    /// ┌──────────────────────┐
    /// │     Enter Hub        │
    /// └──────────────────────┘
    /// ```
    ///
    /// CREATED HIERARCHY:
    /// ```
    /// EnterHubButton (root)
    /// ├── Image (semi-transparent background)
    /// ├── Button (click handler)
    /// └── Label (TMP text: "Enter Hub")
    /// ```
    ///
    /// RELATED FILES:
    /// - OverworldManager.cs: Wires click → EnterHub()
    /// - CaravanInstance.cs: Fires OnHeroNearby proximity event
    /// - GameObjectHelper.Overworld.Canvas.EnterHubButton: Name constant
    /// </summary>
    public static class EnterHubButtonFactory
    {
        private static readonly Color BackgroundColor = new Color(0.12f, 0.12f, 0.12f, 0.85f);
        private static readonly Color LabelColor = new Color(0.93f, 0.87f, 0.53f, 1f);

        /// <summary>Creates the Enter Hub button under the given canvas parent.
        /// Anchored to bottom-center, initially inactive.</summary>
        public static Button Create(Transform canvasParent)
        {
            // === ROOT ===
            var root = new GameObject("EnterHubButton");
            root.layer = LayerMask.NameToLayer("UI");

            var rt = root.AddComponent<RectTransform>();
            rt.SetParent(canvasParent, false);

            // Anchor bottom-center
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 120f);
            rt.sizeDelta = new Vector2(320f, 80f);

            root.AddComponent<CanvasRenderer>();

            // Background image
            var image = root.AddComponent<Image>();
            image.color = BackgroundColor;
            image.raycastTarget = true;
            image.type = Image.Type.Sliced;

            // Button component
            var button = root.AddComponent<Button>();
            button.interactable = true;
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            button.colors = new ColorBlock
            {
                normalColor = Color.white,
                highlightedColor = new Color(1f, 1f, 1f, 1f),
                pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f),
                selectedColor = new Color(0.9f, 0.9f, 0.9f, 1f),
                disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f),
                colorMultiplier = 1f,
                fadeDuration = 0.1f
            };

            // === LABEL ===
            var labelGO = new GameObject("Label");
            labelGO.layer = LayerMask.NameToLayer("UI");

            var labelRT = labelGO.AddComponent<RectTransform>();
            labelRT.SetParent(rt, false);
            labelRT.anchorMin = Vector2.zero;
            labelRT.anchorMax = Vector2.one;
            labelRT.anchoredPosition = Vector2.zero;
            labelRT.sizeDelta = Vector2.zero;

            labelGO.AddComponent<CanvasRenderer>();

            var tmp = labelGO.AddComponent<TextMeshProUGUI>();
            tmp.text = "Enter Hub";
            tmp.fontSize = 36;
            tmp.color = LabelColor;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.raycastTarget = false;

            // Start hidden
            root.SetActive(false);

            return button;
        }
    }
}
