using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Factories
{
    /// <summary>
    /// SETTINGTOGGLEFACTORY - Creates settings toggle (checkbox) GameObjects.
    /// 
    /// PURPOSE:
    /// Creates toggle/checkbox controls for boolean settings on the
    /// settings screen.
    /// 
    /// VISUAL APPEARANCE:
    /// ```
    /// ┌─────────────────────────────────────┐
    /// │ Enable Fullscreen                   │ ← Label
    /// │ [✓]                                 │ ← Toggle checkbox
    /// └─────────────────────────────────────┘
    /// ```
    /// 
    /// CREATED HIERARCHY:
    /// ```
    /// SettingToggle (root)
    /// ├── ContentSizeFitter
    /// ├── VerticalLayoutGroup
    /// ├── Label (TMP - setting name)
    /// └── Panel (toggle container)
    ///     └── Toggle
    ///         ├── Background (Image)
    ///         │   └── Checkmark (Image)
    ///         └── Label (legacy Text, inactive)
    /// ```
    /// 
    /// CONFIGURATION:
    /// - Default size: 512x100
    /// - Vertical layout with label above toggle
    /// - Uses Unity's Toggle component
    /// 
    /// CALLED BY:
    /// - SettingsManager.CreateToggles()
    /// 
    /// RELATED FILES:
    /// - SettingsManager.cs: Settings screen
    /// - SettingsModel.cs: Boolean settings
    /// </summary>
    public static class SettingToggleFactory
    {
        private static readonly ColorBlock DefaultToggleColors = new ColorBlock
        {
            normalColor = Color.white,
            highlightedColor = new Color(0.9607843f, 0.9607843f, 0.9607843f, 1f),
            pressedColor = new Color(0.78431374f, 0.78431374f, 0.78431374f, 1f),
            selectedColor = new Color(0.9607843f, 0.9607843f, 0.9607843f, 1f),
            disabledColor = new Color(0.78431374f, 0.78431374f, 0.78431374f, 0.5019608f),
            colorMultiplier = 1f,
            fadeDuration = 0.1f
        };

        /// <summary>Creates a new settings toggle control.</summary>
        public static GameObject Create(Transform parent = null)
        {
            // === ROOT: SettingToggle ===
            var root = new GameObject("SettingToggle");
            root.layer = 0;

            var rootRT = root.AddComponent<RectTransform>();
            rootRT.anchorMin = new Vector2(0f, 1f);
            rootRT.anchorMax = new Vector2(0f, 1f);
            rootRT.anchoredPosition = Vector2.zero;
            rootRT.sizeDelta = new Vector2(512f, 100f);
            rootRT.pivot = new Vector2(0.5f, 0.5f);

            var contentSizeFitter = root.AddComponent<ContentSizeFitter>();
            contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

            var verticalLayout = root.AddComponent<VerticalLayoutGroup>();
            verticalLayout.childAlignment = TextAnchor.UpperLeft;
            verticalLayout.spacing = 0f;
            verticalLayout.childForceExpandWidth = true;
            verticalLayout.childForceExpandHeight = true;
            verticalLayout.childControlWidth = false;
            verticalLayout.childControlHeight = false;
            verticalLayout.childScaleWidth = false;
            verticalLayout.childScaleHeight = false;

            // === CHILD: Label ===
            var label = new GameObject("Label");
            label.layer = 0;

            var labelRT = label.AddComponent<RectTransform>();
            labelRT.SetParent(rootRT, false);
            labelRT.anchorMin = Vector2.zero;
            labelRT.anchorMax = Vector2.zero;
            labelRT.anchoredPosition = Vector2.zero;
            labelRT.sizeDelta = new Vector2(512f, 36f);
            labelRT.pivot = new Vector2(0.5f, 0.5f);

            label.AddComponent<CanvasRenderer>();

            var labelTMP = label.AddComponent<TextMeshProUGUI>();
            labelTMP.text = "Label";
            labelTMP.fontSize = 24;
            labelTMP.color = Color.white;
            labelTMP.alignment = TextAlignmentOptions.Left;
            labelTMP.raycastTarget = false;

            var labelLayout = label.AddComponent<LayoutElement>();
            labelLayout.minWidth = 160f;

            // === CHILD: Panel (container for toggle) ===
            var panel = new GameObject("Panel");
            panel.layer = 0;

            var panelRT = panel.AddComponent<RectTransform>();
            panelRT.SetParent(rootRT, false);
            panelRT.anchorMin = Vector2.zero;
            panelRT.anchorMax = Vector2.zero;
            panelRT.anchoredPosition = Vector2.zero;
            panelRT.sizeDelta = new Vector2(100f, 100f);
            panelRT.pivot = new Vector2(0.5f, 0.5f);

            // === GRANDCHILD: Toggle ===
            var toggle = new GameObject("Toggle");
            toggle.layer = 0;

            var toggleRT = toggle.AddComponent<RectTransform>();
            toggleRT.SetParent(panelRT, false);
            toggleRT.anchorMin = new Vector2(0f, 1f);
            toggleRT.anchorMax = new Vector2(0f, 1f);
            toggleRT.anchoredPosition = new Vector2(16f, -32f);
            toggleRT.sizeDelta = new Vector2(32f, 32f);
            toggleRT.pivot = new Vector2(0.5f, 0.5f);

            // === GREAT-GRANDCHILD: Background ===
            var background = new GameObject("Background");
            background.layer = 0;

            var bgRT = background.AddComponent<RectTransform>();
            bgRT.SetParent(toggleRT, false);
            bgRT.anchorMin = new Vector2(0f, 1f);
            bgRT.anchorMax = new Vector2(0f, 1f);
            bgRT.anchoredPosition = new Vector2(16f, 0f);
            bgRT.sizeDelta = new Vector2(32f, 32f);
            bgRT.pivot = new Vector2(0.5f, 0.5f);

            background.AddComponent<CanvasRenderer>();

            var bgImage = background.AddComponent<Image>();
            bgImage.color = Color.white;
            bgImage.raycastTarget = true;
            bgImage.maskable = true;
            bgImage.type = Image.Type.Sliced;
            // sprite left null - uses Unity default or assigned at runtime

            // === GREAT-GREAT-GRANDCHILD: Checkmark ===
            var checkmark = new GameObject("Checkmark");
            checkmark.layer = 0;

            var checkRT = checkmark.AddComponent<RectTransform>();
            checkRT.SetParent(bgRT, false);
            checkRT.anchorMin = new Vector2(0.5f, 0.5f);
            checkRT.anchorMax = new Vector2(0.5f, 0.5f);
            checkRT.anchoredPosition = Vector2.zero;
            checkRT.sizeDelta = new Vector2(20f, 20f);
            checkRT.pivot = new Vector2(0.5f, 0.5f);

            checkmark.AddComponent<CanvasRenderer>();

            var checkImage = checkmark.AddComponent<Image>();
            checkImage.color = Color.white;
            checkImage.raycastTarget = true;
            checkImage.maskable = true;
            checkImage.type = Image.Type.Simple;
            // sprite left null - uses Unity default checkmark or assigned at runtime

            // === GREAT-GRANDCHILD: Toggle Label (inactive) ===
            var toggleLabel = new GameObject("Label");
            toggleLabel.layer = 0;
            toggleLabel.SetActive(false);

            var toggleLabelRT = toggleLabel.AddComponent<RectTransform>();
            toggleLabelRT.SetParent(toggleRT, false);
            toggleLabelRT.anchorMin = Vector2.zero;
            toggleLabelRT.anchorMax = Vector2.one;
            toggleLabelRT.anchoredPosition = new Vector2(9f, -0.5f);
            toggleLabelRT.sizeDelta = new Vector2(-28f, -3f);
            toggleLabelRT.pivot = new Vector2(0.5f, 0.5f);

            toggleLabel.AddComponent<CanvasRenderer>();

            var toggleLabelText = toggleLabel.AddComponent<Text>();
            toggleLabelText.text = "Toggle";
            toggleLabelText.fontSize = 14;
            toggleLabelText.color = new Color(0.19607843f, 0.19607843f, 0.19607843f, 1f);
            toggleLabelText.raycastTarget = true;

            // === Add Toggle component ===
            var toggleComp = toggle.AddComponent<Toggle>();
            toggleComp.interactable = true;
            toggleComp.targetGraphic = bgImage;
            toggleComp.graphic = checkImage;
            toggleComp.isOn = true;
            toggleComp.transition = Selectable.Transition.ColorTint;
            toggleComp.colors = DefaultToggleColors;
            toggleComp.navigation = new Navigation { mode = Navigation.Mode.Automatic };

            // Parent if specified
            if (parent != null)
            {
                rootRT.SetParent(parent, false);
            }

            return root;
        }
    }
}
