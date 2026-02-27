using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Factories
{
    /// <summary>
    /// Programmatic factory for KeyButton - replaces KeyButtonPrefab.prefab
    /// Hierarchy:
    /// - KeyButton (root - Image, Button)
    ///   - Label (TMP text)
    /// </summary>
    public static class KeyButtonFactory
    {
        private static readonly ColorBlock DefaultButtonColors = new ColorBlock
        {
            normalColor = Color.white,
            highlightedColor = new Color(0.9607843f, 0.9607843f, 0.9607843f, 1f),
            pressedColor = new Color(0.78431374f, 0.78431374f, 0.78431374f, 1f),
            selectedColor = new Color(0.9607843f, 0.9607843f, 0.9607843f, 1f),
            disabledColor = new Color(0.78431374f, 0.78431374f, 0.78431374f, 0.5019608f),
            colorMultiplier = 1f,
            fadeDuration = 0.1f
        };

        public static GameObject Create(Transform parent = null)
        {
            // === ROOT: KeyButton ===
            var root = new GameObject("KeyButton");
            root.layer = 5; // UI layer

            var rootRT = root.AddComponent<RectTransform>();
            rootRT.anchorMin = new Vector2(0.5f, 0.5f);
            rootRT.anchorMax = new Vector2(0.5f, 0.5f);
            rootRT.anchoredPosition = Vector2.zero;
            rootRT.sizeDelta = new Vector2(64f, 64f);
            rootRT.pivot = new Vector2(0.5f, 0.5f);

            root.AddComponent<CanvasRenderer>();

            // Image (background)
            var image = root.AddComponent<Image>();
            image.color = Color.white;
            image.raycastTarget = true;
            image.maskable = true;
            image.type = Image.Type.Sliced;
            // sprite left null - assigned at runtime

            // Button
            var button = root.AddComponent<Button>();
            button.interactable = true;
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            button.colors = DefaultButtonColors;
            button.navigation = new Navigation { mode = Navigation.Mode.Automatic };

            // === CHILD: Label ===
            var label = new GameObject("Label");
            label.layer = 5;

            var labelRT = label.AddComponent<RectTransform>();
            labelRT.SetParent(rootRT, false);
            labelRT.anchorMin = Vector2.zero;
            labelRT.anchorMax = Vector2.one;
            labelRT.anchoredPosition = Vector2.zero;
            labelRT.sizeDelta = Vector2.zero;
            labelRT.pivot = new Vector2(0.5f, 0.5f);

            label.AddComponent<CanvasRenderer>();

            var labelTMP = label.AddComponent<TextMeshProUGUI>();
            labelTMP.text = "X";
            labelTMP.fontSize = 48;
            labelTMP.color = Color.white;
            labelTMP.alignment = TextAlignmentOptions.Center;
            labelTMP.raycastTarget = true;
            labelTMP.enableWordWrapping = true;

            // Parent if specified
            if (parent != null)
            {
                rootRT.SetParent(parent, false);
            }

            return root;
        }
    }
}
