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
    /// SETTINGSLIDERFACTORY - Creates settings slider GameObjects.
    /// 
    /// PURPOSE:
    /// Creates slider controls for numeric range settings on the
    /// settings screen (volume, speed, sensitivity, etc).
    /// 
    /// VISUAL APPEARANCE:
    /// ```
    /// ┌─────────────────────────────────────┐
    /// │ Game Speed                          │ ← Label
    /// │ [========■==========] 1.5x          │ ← Slider + Value
    /// └─────────────────────────────────────┘
    /// ```
    /// 
    /// CREATED HIERARCHY:
    /// ```
    /// SettingSlider (root)
    /// ├── ContentSizeFitter
    /// ├── VerticalLayoutGroup
    /// ├── Label (TMP - setting name)
    /// └── Panel (horizontal container)
    ///     ├── Slider
    ///     │   ├── Background (Image)
    ///     │   ├── Fill Area → Fill
    ///     │   └── Handle Slide Area → Handle
    ///     └── Value (TMP - current value)
    /// ```
    /// 
    /// CONFIGURATION:
    /// - Default size: 512x100
    /// - Vertical layout with label above slider
    /// - Value text shows current numeric value
    /// 
    /// CALLED BY:
    /// - SettingsManager.CreateSliders()
    /// 
    /// RELATED FILES:
    /// - SettingsManager.cs: Settings screen
    /// - SettingsModel.cs: Numeric settings
    /// </summary>
    public static class SettingSliderFactory
    {
        private static readonly ColorBlock DefaultSliderColors = new ColorBlock
        {
            normalColor = Color.white,
            highlightedColor = new Color(0.9607843f, 0.9607843f, 0.9607843f, 1f),
            pressedColor = new Color(0.78431374f, 0.78431374f, 0.78431374f, 1f),
            selectedColor = new Color(0.9607843f, 0.9607843f, 0.9607843f, 1f),
            disabledColor = new Color(0.78431374f, 0.78431374f, 0.78431374f, 0.5019608f),
            colorMultiplier = 1f,
            fadeDuration = 0.1f
        };

        /// <summary>Creates a new settings slider control.</summary>
        public static GameObject Create(Transform parent = null)
        {
            // === ROOT: SettingSlider ===
            var root = new GameObject("SettingSlider");
            root.layer = 0;

            var rootRT = root.AddComponent<RectTransform>();
            rootRT.anchorMin = new Vector2(0.5f, 0.5f);
            rootRT.anchorMax = new Vector2(0.5f, 0.5f);
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
            labelRT.sizeDelta = new Vector2(512f, 32f);
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

            // === CHILD: Panel ===
            var panel = new GameObject("Panel");
            panel.layer = 0;

            var panelRT = panel.AddComponent<RectTransform>();
            panelRT.SetParent(rootRT, false);
            panelRT.anchorMin = Vector2.zero;
            panelRT.anchorMax = Vector2.zero;
            panelRT.anchoredPosition = Vector2.zero;
            panelRT.sizeDelta = new Vector2(100f, 100f);
            panelRT.pivot = new Vector2(0.5f, 0.5f);

            // === GRANDCHILD: Slider ===
            var slider = new GameObject("Slider");
            slider.layer = 0;

            var sliderRT = slider.AddComponent<RectTransform>();
            sliderRT.SetParent(panelRT, false);
            sliderRT.anchorMin = new Vector2(0f, 1f);
            sliderRT.anchorMax = new Vector2(0f, 1f);
            sliderRT.anchoredPosition = new Vector2(150f, -24f);
            sliderRT.sizeDelta = new Vector2(300f, 48f);
            sliderRT.pivot = new Vector2(0.5f, 0.5f);

            // === GREAT-GRANDCHILD: Background ===
            var background = new GameObject("Background");
            background.layer = 0;

            var bgRT = background.AddComponent<RectTransform>();
            bgRT.SetParent(sliderRT, false);
            bgRT.anchorMin = new Vector2(0f, 0.25f);
            bgRT.anchorMax = new Vector2(1f, 0.75f);
            bgRT.anchoredPosition = Vector2.zero;
            bgRT.sizeDelta = Vector2.zero;
            bgRT.pivot = new Vector2(0.5f, 0.5f);

            background.AddComponent<CanvasRenderer>();

            var bgImage = background.AddComponent<Image>();
            bgImage.color = Color.white;
            bgImage.raycastTarget = true;
            bgImage.type = Image.Type.Sliced;

            // === GREAT-GRANDCHILD: Fill Area ===
            var fillArea = new GameObject("Fill Area");
            fillArea.layer = 0;

            var fillAreaRT = fillArea.AddComponent<RectTransform>();
            fillAreaRT.SetParent(sliderRT, false);
            fillAreaRT.anchorMin = new Vector2(0f, 0.25f);
            fillAreaRT.anchorMax = new Vector2(1f, 0.75f);
            fillAreaRT.anchoredPosition = new Vector2(-5f, 0f);
            fillAreaRT.sizeDelta = new Vector2(-20f, 0f);
            fillAreaRT.pivot = new Vector2(0.5f, 0.5f);

            // === GREAT-GREAT-GRANDCHILD: Fill ===
            var fill = new GameObject("Fill");
            fill.layer = 0;

            var fillRT = fill.AddComponent<RectTransform>();
            fillRT.SetParent(fillAreaRT, false);
            fillRT.anchorMin = Vector2.zero;
            fillRT.anchorMax = Vector2.zero;
            fillRT.anchoredPosition = Vector2.zero;
            fillRT.sizeDelta = new Vector2(10f, 0f);
            fillRT.pivot = new Vector2(0.5f, 0.5f);

            fill.AddComponent<CanvasRenderer>();

            var fillImage = fill.AddComponent<Image>();
            fillImage.color = Color.white;
            fillImage.raycastTarget = true;
            fillImage.type = Image.Type.Sliced;

            // === GREAT-GRANDCHILD: Handle Slide Area ===
            var handleSlideArea = new GameObject("Handle Slide Area");
            handleSlideArea.layer = 0;

            var handleSlideAreaRT = handleSlideArea.AddComponent<RectTransform>();
            handleSlideAreaRT.SetParent(sliderRT, false);
            handleSlideAreaRT.anchorMin = Vector2.zero;
            handleSlideAreaRT.anchorMax = Vector2.one;
            handleSlideAreaRT.anchoredPosition = Vector2.zero;
            handleSlideAreaRT.sizeDelta = new Vector2(-20f, 0f);
            handleSlideAreaRT.pivot = new Vector2(0.5f, 0.5f);

            // === GREAT-GREAT-GRANDCHILD: Handle ===
            var handle = new GameObject("Handle");
            handle.layer = 0;

            var handleRT = handle.AddComponent<RectTransform>();
            handleRT.SetParent(handleSlideAreaRT, false);
            handleRT.anchorMin = Vector2.zero;
            handleRT.anchorMax = Vector2.zero;
            handleRT.anchoredPosition = Vector2.zero;
            handleRT.sizeDelta = new Vector2(20f, 0f);
            handleRT.pivot = new Vector2(0.5f, 0.5f);

            handle.AddComponent<CanvasRenderer>();

            var handleImage = handle.AddComponent<Image>();
            handleImage.color = Color.white;
            handleImage.raycastTarget = true;
            handleImage.type = Image.Type.Simple;

            // === Add Slider component ===
            var sliderComp = slider.AddComponent<Slider>();
            sliderComp.interactable = true;
            sliderComp.targetGraphic = handleImage;
            sliderComp.fillRect = fillRT;
            sliderComp.handleRect = handleRT;
            sliderComp.direction = Slider.Direction.LeftToRight;
            sliderComp.minValue = 0f;
            sliderComp.maxValue = 1f;
            sliderComp.wholeNumbers = false;
            sliderComp.value = 0f;
            sliderComp.transition = Selectable.Transition.ColorTint;
            sliderComp.colors = DefaultSliderColors;
            sliderComp.navigation = new Navigation { mode = Navigation.Mode.Automatic };

            // === GRANDCHILD: Value ===
            var value = new GameObject("Value");
            value.layer = 0;

            var valueRT = value.AddComponent<RectTransform>();
            valueRT.SetParent(panelRT, false);
            valueRT.anchorMin = new Vector2(0f, 1f);
            valueRT.anchorMax = new Vector2(0f, 1f);
            valueRT.anchoredPosition = new Vector2(565f, -24f);
            valueRT.sizeDelta = new Vector2(512f, 32f);
            valueRT.pivot = new Vector2(0.5f, 0.5f);

            value.AddComponent<CanvasRenderer>();

            var valueTMP = value.AddComponent<TextMeshProUGUI>();
            valueTMP.text = "0.00";
            valueTMP.fontSize = 24;
            valueTMP.color = Color.white;
            valueTMP.alignment = TextAlignmentOptions.Left;
            valueTMP.raycastTarget = false;
            valueTMP.enableWordWrapping = false;

            var valueLayout = value.AddComponent<LayoutElement>();
            valueLayout.minWidth = 60f;

            // Parent if specified
            if (parent != null)
            {
                rootRT.SetParent(parent, false);
            }

            return root;
        }
    }
}
