using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Factories
{
    /// <summary>
    /// Programmatic factory for AbilityButton - replaces AbilityButtonPrefab.prefab
    /// </summary>
    public static class AbilityButtonFactory
    {
        public static GameObject Create(Transform parent = null)
        {
            // Root GameObject
            var root = new GameObject("AbilityButton");
            root.layer = LayerMask.NameToLayer("UI");

            // RectTransform (root)
            var rootRT = root.AddComponent<RectTransform>();
            rootRT.anchorMin = Vector2.zero;
            rootRT.anchorMax = Vector2.zero;
            rootRT.anchoredPosition = Vector2.zero;
            rootRT.sizeDelta = new Vector2(128, 128);
            rootRT.pivot = Vector2.zero;

            // CanvasRenderer (required for UI rendering)
            root.AddComponent<CanvasRenderer>();

            // LayoutElement
            var layoutElement = root.AddComponent<LayoutElement>();
            layoutElement.ignoreLayout = false;
            layoutElement.minWidth = -1;
            layoutElement.minHeight = -1;
            layoutElement.preferredWidth = -1;
            layoutElement.preferredHeight = -1;
            layoutElement.flexibleWidth = -1;
            layoutElement.flexibleHeight = -1;
            layoutElement.layoutPriority = 1;

            // Image (background)
            var image = root.AddComponent<Image>();
            image.color = Color.white;
            image.raycastTarget = true;
            image.maskable = true;
            // sprite left null - assign via AbilityLibrary or at runtime
            image.type = Image.Type.Sliced;
            image.preserveAspect = false;
            image.fillCenter = true;

            // Button
            var button = root.AddComponent<Button>();
            button.interactable = true;
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            button.colors = new ColorBlock
            {
                normalColor = Color.white,
                highlightedColor = new Color(0.9607843f, 0.9607843f, 0.9607843f, 1f),
                pressedColor = new Color(0.78431374f, 0.78431374f, 0.78431374f, 1f),
                selectedColor = new Color(0.9607843f, 0.9607843f, 0.9607843f, 1f),
                disabledColor = new Color(0.78431374f, 0.78431374f, 0.78431374f, 0.5019608f),
                colorMultiplier = 1f,
                fadeDuration = 0.1f
            };
            button.navigation = new Navigation { mode = Navigation.Mode.Automatic };

            // AbilityButton (custom component)
            var abilityButton = root.AddComponent<AbilityButton>();

            // === Child: Label ===
            var labelGO = new GameObject("Label");
            labelGO.layer = LayerMask.NameToLayer("UI");

            // RectTransform (label) - stretched to fill parent with offset
            var labelRT = labelGO.AddComponent<RectTransform>();
            labelRT.SetParent(rootRT, false);
            labelRT.anchorMin = Vector2.zero;
            labelRT.anchorMax = Vector2.one;
            labelRT.anchoredPosition = new Vector2(0, -64);
            labelRT.sizeDelta = new Vector2(0, -128);
            labelRT.pivot = new Vector2(0.5f, 0.5f);

            // CanvasRenderer (label)
            labelGO.AddComponent<CanvasRenderer>();

            // TextMeshProUGUI
            var tmpText = labelGO.AddComponent<TextMeshProUGUI>();
            tmpText.enabled = false; // Was disabled in prefab
            tmpText.text = "Button";
            tmpText.fontSize = 12;
            tmpText.color = Color.white;
            tmpText.alignment = TextAlignmentOptions.Center;
            tmpText.raycastTarget = true;
            tmpText.maskable = true;
            tmpText.richText = true;
            tmpText.enableWordWrapping = true;
            tmpText.overflowMode = TextOverflowModes.Overflow;

            // Wire up AbilityButton references
            abilityButton.button = button;
            abilityButton.label = tmpText;

            // Parent if specified
            if (parent != null)
            {
                rootRT.SetParent(parent, false);
            }

            return root;
        }
    }
}
