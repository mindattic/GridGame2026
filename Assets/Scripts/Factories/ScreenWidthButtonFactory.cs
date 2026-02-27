using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Factories
{
    /// <summary>
    /// Programmatic factory for ScreenWidthButton - replaces ScreenWidthButtonPrefab.prefab
    /// Hierarchy:
    /// - ScreenWidthButton (root with Image, Button, LayoutElement)
    ///   - Label (TextMeshProUGUI)
    /// </summary>
    public static class ScreenWidthButtonFactory
    {
        private static readonly ColorBlock DefaultButtonColors = new ColorBlock
        {
            normalColor = Color.white,
            highlightedColor = new Color(0.9607843f, 0.9607843f, 0.9607843f, 1f),
            pressedColor = new Color(0.78431374f, 0.78431374f, 0.78431374f, 1f),
            selectedColor = new Color(0.9607843f, 0.9607843f, 0.9607843f, 1f),
            disabledColor = new Color(0.78431374f, 0.78431374f, 0.78431374f, 1f),
            colorMultiplier = 1f,
            fadeDuration = 0.1f
        };

        public static GameObject Create(Transform parent = null)
        {
            // === ROOT: ScreenWidthButton ===
            var root = new GameObject("ScreenWidthButton");
            root.layer = LayerMask.NameToLayer("UI");

            var rootRT = root.AddComponent<RectTransform>();
            rootRT.anchorMin = new Vector2(0.5f, 0.5f);
            rootRT.anchorMax = new Vector2(0.5f, 0.5f);
            rootRT.anchoredPosition = Vector2.zero;
            rootRT.sizeDelta = new Vector2(1024f, 128f);
            rootRT.pivot = new Vector2(0.5f, 0.5f);

            root.AddComponent<CanvasRenderer>();

            // Image (button background)
            var image = root.AddComponent<Image>();
            image.color = Color.white;
            image.raycastTarget = true;
            image.maskable = true;
            image.type = Image.Type.Simple;

            // Button
            var button = root.AddComponent<Button>();
            button.interactable = true;
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            button.colors = DefaultButtonColors;
            button.navigation = new Navigation { mode = Navigation.Mode.Automatic };

            // LayoutElement (for flexible layout in scroll views)
            var layoutElement = root.AddComponent<LayoutElement>();
            layoutElement.ignoreLayout = false;
            layoutElement.minWidth = -1;
            layoutElement.minHeight = -1;
            layoutElement.preferredWidth = -1;
            layoutElement.preferredHeight = 64f;
            layoutElement.flexibleWidth = 1f;
            layoutElement.flexibleHeight = -1;
            layoutElement.layoutPriority = 1;

            // === CHILD: Label ===
            var label = new GameObject("Label");
            label.layer = LayerMask.NameToLayer("UI");

            var labelRT = label.AddComponent<RectTransform>();
            labelRT.SetParent(rootRT, false);
            labelRT.anchorMin = Vector2.zero;
            labelRT.anchorMax = Vector2.one;
            labelRT.anchoredPosition = Vector2.zero;
            labelRT.sizeDelta = Vector2.zero;
            labelRT.pivot = new Vector2(0.5f, 0.5f);

            label.AddComponent<CanvasRenderer>();

            var labelTMP = label.AddComponent<TextMeshProUGUI>();
            labelTMP.text = "Button";
            labelTMP.fontSize = 48;
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
