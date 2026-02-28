using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Factories
{
    /// <summary>
    /// SETTINGDROPDOWNFACTORY - Creates settings dropdown GameObjects.
    /// 
    /// PURPOSE:
    /// Creates dropdown/combobox controls for multi-choice settings
    /// on the settings screen (resolution, language, etc).
    /// 
    /// VISUAL APPEARANCE:
    /// ```
    /// ┌─────────────────────────────────────┐
    /// │ Resolution                          │ ← Label
    /// │ [1920x1080                     ▼]  │ ← Dropdown
    /// └─────────────────────────────────────┘
    ///           ↓ (when expanded)
    /// ┌─────────────────────────────────────┐
    /// │ 1920x1080                           │
    /// │ 1280x720                            │
    /// │ 1600x900                            │
    /// └─────────────────────────────────────┘
    /// ```
    /// 
    /// CREATED HIERARCHY:
    /// ```
    /// SettingDropdown (root)
    /// ├── ContentSizeFitter
    /// ├── VerticalLayoutGroup
    /// ├── Label (TMP - setting name)
    /// └── Panel (dropdown container)
    ///     └── TMP_Dropdown
    ///         ├── Label (selected value)
    ///         ├── Arrow (dropdown indicator)
    ///         └── Template (popup list)
    /// ```
    /// 
    /// CALLED BY:
    /// - SettingsManager.CreateDropdowns()
    /// 
    /// RELATED FILES:
    /// - SettingsManager.cs: Settings screen
    /// - SettingsModel.cs: Choice settings
    /// </summary>
    public static class SettingDropdownFactory
    {
        private static readonly ColorBlock DefaultDropdownColors = new ColorBlock
        {
            normalColor = Color.white,
            highlightedColor = new Color(0.9607843f, 0.9607843f, 0.9607843f, 1f),
            pressedColor = new Color(0.78431374f, 0.78431374f, 0.78431374f, 1f),
            selectedColor = new Color(0.9607843f, 0.9607843f, 0.9607843f, 1f),
            disabledColor = new Color(0.78431374f, 0.78431374f, 0.78431374f, 0.5019608f),
            colorMultiplier = 1f,
            fadeDuration = 0.1f
        };

        /// <summary>Creates a new settings dropdown control.</summary>
        public static GameObject Create(Transform parent = null)
        {
            // === ROOT: SettingDropdown ===
            var root = new GameObject("SettingDropdown");
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

            // === GRANDCHILD: Dropdown ===
            var dropdown = new GameObject("Dropdown");
            dropdown.layer = 0;

            var dropdownRT = dropdown.AddComponent<RectTransform>();
            dropdownRT.SetParent(panelRT, false);
            dropdownRT.anchorMin = new Vector2(0f, 1f);
            dropdownRT.anchorMax = new Vector2(0f, 1f);
            dropdownRT.anchoredPosition = new Vector2(150f, -24f);
            dropdownRT.sizeDelta = new Vector2(300f, 48f);
            dropdownRT.pivot = new Vector2(0.5f, 0.5f);

            dropdown.AddComponent<CanvasRenderer>();

            var dropdownImage = dropdown.AddComponent<Image>();
            dropdownImage.color = Color.white;
            dropdownImage.raycastTarget = true;
            dropdownImage.type = Image.Type.Sliced;

            // === GREAT-GRANDCHILD: Arrow ===
            var arrow = new GameObject("Arrow");
            arrow.layer = 0;

            var arrowRT = arrow.AddComponent<RectTransform>();
            arrowRT.SetParent(dropdownRT, false);
            arrowRT.anchorMin = new Vector2(1f, 0.5f);
            arrowRT.anchorMax = new Vector2(1f, 0.5f);
            arrowRT.anchoredPosition = new Vector2(-15f, 0f);
            arrowRT.sizeDelta = new Vector2(20f, 20f);
            arrowRT.pivot = new Vector2(0.5f, 0.5f);

            arrow.AddComponent<CanvasRenderer>();

            var arrowImage = arrow.AddComponent<Image>();
            arrowImage.color = Color.white;
            arrowImage.raycastTarget = true;
            arrowImage.type = Image.Type.Simple;

            // === GREAT-GRANDCHILD: Caption ===
            var caption = new GameObject("Label");
            caption.layer = 0;

            var captionRT = caption.AddComponent<RectTransform>();
            captionRT.SetParent(dropdownRT, false);
            captionRT.anchorMin = Vector2.zero;
            captionRT.anchorMax = Vector2.one;
            captionRT.anchoredPosition = new Vector2(-7.5f, -0.5f);
            captionRT.sizeDelta = new Vector2(-35f, -13f);
            captionRT.pivot = new Vector2(0.5f, 0.5f);

            caption.AddComponent<CanvasRenderer>();

            var captionTMP = caption.AddComponent<TextMeshProUGUI>();
            captionTMP.text = "Option A";
            captionTMP.fontSize = 14;
            captionTMP.color = new Color(0.19607843f, 0.19607843f, 0.19607843f, 1f);
            captionTMP.alignment = TextAlignmentOptions.Left;
            captionTMP.raycastTarget = true;

            // === GREAT-GRANDCHILD: Template (disabled by default) ===
            var template = new GameObject("Template");
            template.layer = 0;
            template.SetActive(false);

            var templateRT = template.AddComponent<RectTransform>();
            templateRT.SetParent(dropdownRT, false);
            templateRT.anchorMin = new Vector2(0f, 0f);
            templateRT.anchorMax = new Vector2(1f, 0f);
            templateRT.anchoredPosition = new Vector2(0f, 2f);
            templateRT.sizeDelta = new Vector2(0f, 150f);
            templateRT.pivot = new Vector2(0.5f, 1f);

            template.AddComponent<CanvasRenderer>();

            var templateImage = template.AddComponent<Image>();
            templateImage.color = Color.white;
            templateImage.type = Image.Type.Sliced;

            var scrollRect = template.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.inertia = true;
            scrollRect.decelerationRate = 0.135f;
            scrollRect.scrollSensitivity = 1f;

            // === Template > Viewport ===
            var viewport = new GameObject("Viewport");
            viewport.layer = 0;

            var viewportRT = viewport.AddComponent<RectTransform>();
            viewportRT.SetParent(templateRT, false);
            viewportRT.anchorMin = Vector2.zero;
            viewportRT.anchorMax = Vector2.one;
            viewportRT.anchoredPosition = Vector2.zero;
            viewportRT.sizeDelta = new Vector2(-18f, 0f);
            viewportRT.pivot = new Vector2(0f, 1f);

            var viewportMask = viewport.AddComponent<Mask>();
            viewportMask.showMaskGraphic = false;

            viewport.AddComponent<CanvasRenderer>();

            var viewportImage = viewport.AddComponent<Image>();
            viewportImage.color = Color.white;
            viewportImage.type = Image.Type.Sliced;

            // === Viewport > Content ===
            var content = new GameObject("Content");
            content.layer = 0;

            var contentRT = content.AddComponent<RectTransform>();
            contentRT.SetParent(viewportRT, false);
            contentRT.anchorMin = new Vector2(0f, 1f);
            contentRT.anchorMax = new Vector2(1f, 1f);
            contentRT.anchoredPosition = Vector2.zero;
            contentRT.sizeDelta = new Vector2(0f, 28f);
            contentRT.pivot = new Vector2(0.5f, 1f);

            // === Content > Item ===
            var item = new GameObject("Item");
            item.layer = 0;

            var itemRT = item.AddComponent<RectTransform>();
            itemRT.SetParent(contentRT, false);
            itemRT.anchorMin = new Vector2(0f, 0.5f);
            itemRT.anchorMax = new Vector2(1f, 0.5f);
            itemRT.anchoredPosition = Vector2.zero;
            itemRT.sizeDelta = new Vector2(0f, 20f);
            itemRT.pivot = new Vector2(0.5f, 0.5f);

            var itemToggle = item.AddComponent<Toggle>();
            itemToggle.isOn = true;

            // === Item > Item Background ===
            var itemBg = new GameObject("Item Background");
            itemBg.layer = 0;

            var itemBgRT = itemBg.AddComponent<RectTransform>();
            itemBgRT.SetParent(itemRT, false);
            itemBgRT.anchorMin = Vector2.zero;
            itemBgRT.anchorMax = Vector2.one;
            itemBgRT.anchoredPosition = Vector2.zero;
            itemBgRT.sizeDelta = Vector2.zero;
            itemBgRT.pivot = new Vector2(0.5f, 0.5f);

            itemBg.AddComponent<CanvasRenderer>();

            var itemBgImage = itemBg.AddComponent<Image>();
            itemBgImage.color = new Color(0.9607843f, 0.9607843f, 0.9607843f, 1f);

            // === Item > Item Checkmark ===
            var itemCheckmark = new GameObject("Item Checkmark");
            itemCheckmark.layer = 0;

            var itemCheckRT = itemCheckmark.AddComponent<RectTransform>();
            itemCheckRT.SetParent(itemRT, false);
            itemCheckRT.anchorMin = new Vector2(0f, 0.5f);
            itemCheckRT.anchorMax = new Vector2(0f, 0.5f);
            itemCheckRT.anchoredPosition = new Vector2(10f, 0f);
            itemCheckRT.sizeDelta = new Vector2(20f, 20f);
            itemCheckRT.pivot = new Vector2(0.5f, 0.5f);

            itemCheckmark.AddComponent<CanvasRenderer>();

            var itemCheckImage = itemCheckmark.AddComponent<Image>();
            itemCheckImage.color = Color.white;

            // === Item > Item Label ===
            var itemLabel = new GameObject("Item Label");
            itemLabel.layer = 0;

            var itemLabelRT = itemLabel.AddComponent<RectTransform>();
            itemLabelRT.SetParent(itemRT, false);
            itemLabelRT.anchorMin = Vector2.zero;
            itemLabelRT.anchorMax = Vector2.one;
            itemLabelRT.anchoredPosition = new Vector2(5f, -0.5f);
            itemLabelRT.sizeDelta = new Vector2(-30f, -3f);
            itemLabelRT.pivot = new Vector2(0.5f, 0.5f);

            itemLabel.AddComponent<CanvasRenderer>();

            var itemLabelTMP = itemLabel.AddComponent<TextMeshProUGUI>();
            itemLabelTMP.text = "Option A";
            itemLabelTMP.fontSize = 14;
            itemLabelTMP.color = new Color(0.19607843f, 0.19607843f, 0.19607843f, 1f);
            itemLabelTMP.alignment = TextAlignmentOptions.Left;

            // Wire up toggle
            itemToggle.targetGraphic = itemBgImage;
            itemToggle.graphic = itemCheckImage;

            // Wire up scrollRect
            scrollRect.viewport = viewportRT;
            scrollRect.content = contentRT;

            // === Add TMP_Dropdown component ===
            var dropdownComp = dropdown.AddComponent<TMP_Dropdown>();
            dropdownComp.interactable = true;
            dropdownComp.targetGraphic = dropdownImage;
            dropdownComp.template = templateRT;
            dropdownComp.captionText = captionTMP;
            dropdownComp.itemText = itemLabelTMP;
            dropdownComp.transition = Selectable.Transition.ColorTint;
            dropdownComp.colors = DefaultDropdownColors;
            dropdownComp.navigation = new Navigation { mode = Navigation.Mode.Automatic };

            // Parent if specified
            if (parent != null)
            {
                rootRT.SetParent(parent, false);
            }

            return root;
        }
    }
}
