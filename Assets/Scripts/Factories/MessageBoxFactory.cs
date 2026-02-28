using Assets.Scripts.Libraries;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Factories
{
    /// <summary>
    /// MESSAGEBOXFACTORY - Creates message box (OK dialog) GameObjects.
    /// 
    /// PURPOSE:
    /// Creates modal dialog boxes with a message and OK button
    /// for informational alerts to the player.
    /// 
    /// VISUAL APPEARANCE:
    /// ```
    /// ┌────────────────────────────────┐
    /// │                                │
    /// │   Your progress has been       │
    /// │   saved successfully!          │
    /// │                                │
    /// │         [ OK ]                 │
    /// └────────────────────────────────┘
    /// ```
    /// 
    /// CREATED HIERARCHY:
    /// ```
    /// MessageBox (root)
    /// ├── MessageBoxInstance (behavior)
    /// └── Panel (centered container)
    ///     ├── Prompt (TMP - message text)
    ///     └── ButtonOk (Button)
    ///         └── Label (TMP - "OK")
    /// ```
    /// 
    /// RELATED FILES:
    /// - MessageBoxInstance.cs: Dialog behavior
    /// - ConfirmationDialogFactory.cs: Yes/No variant
    /// </summary>
    public static class MessageBoxFactory
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
            var root = new GameObject("MessageBox");
            root.layer = LayerMask.NameToLayer("UI");

            var rootRT = root.AddComponent<RectTransform>();
            rootRT.anchorMin = Vector2.zero;
            rootRT.anchorMax = Vector2.one;
            rootRT.anchoredPosition = Vector2.zero;
            rootRT.sizeDelta = Vector2.zero;
            rootRT.pivot = new Vector2(0.5f, 0.5f);

            root.AddComponent<CanvasRenderer>();
            root.AddComponent<MessageBoxInstance>();

            var panel = CreatePanel(rootRT);
            var panelRT = panel.GetComponent<RectTransform>();

            // === PROMPT ===
            CreatePrompt(panelRT);

            // === BUTTON OK ===
            CreateOkButton(panelRT);

            // Parent if specified
            if (parent != null)
            {
                rootRT.SetParent(parent, false);
            }

            return root;
        }

        private static GameObject CreatePanel(RectTransform parentRT)
        {
            var panel = new GameObject("Panel");
            panel.layer = LayerMask.NameToLayer("UI");

            var panelRT = panel.AddComponent<RectTransform>();
            panelRT.SetParent(parentRT, false);
            panelRT.anchorMin = new Vector2(0.5f, 0.5f);
            panelRT.anchorMax = new Vector2(0.5f, 0.5f);
            panelRT.anchoredPosition = Vector2.zero;
            panelRT.sizeDelta = new Vector2(1170f, 2532f);
            panelRT.pivot = new Vector2(0.5f, 0.5f);

            panel.AddComponent<CanvasRenderer>();

            // Image (semi-transparent black background)
            var panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.76862746f);
            panelImage.raycastTarget = true;
            panelImage.maskable = true;
            // sprite left null - displays as solid color
            panelImage.type = Image.Type.Sliced;
            panelImage.fillCenter = true;

            return panel;
        }

        private static GameObject CreatePrompt(RectTransform parentRT)
        {
            var prompt = new GameObject("Prompt");
            prompt.layer = LayerMask.NameToLayer("UI");

            var promptRT = prompt.AddComponent<RectTransform>();
            promptRT.SetParent(parentRT, false);
            promptRT.anchorMin = new Vector2(0.5f, 0.5f);
            promptRT.anchorMax = new Vector2(0.5f, 0.5f);
            promptRT.anchoredPosition = Vector2.zero;
            promptRT.sizeDelta = new Vector2(1053f, 105.3f);
            promptRT.pivot = new Vector2(0.5f, 0.5f);

            prompt.AddComponent<CanvasRenderer>();

            // TextMeshProUGUI
            var tmpText = prompt.AddComponent<TextMeshProUGUI>();
            tmpText.font = FontLibrary.Fonts["Attic"];
            tmpText.text = "Message";
            tmpText.fontSize = 48;
            tmpText.color = Color.white;
            tmpText.alignment = TextAlignmentOptions.Center;
            tmpText.enableWordWrapping = false;
            tmpText.overflowMode = TextOverflowModes.Overflow;
            tmpText.richText = true;
            tmpText.raycastTarget = true;

            return prompt;
        }

        private static GameObject CreateOkButton(RectTransform parentRT)
        {
            var button = new GameObject("ButtonOk");
            button.layer = LayerMask.NameToLayer("UI");

            var buttonRT = button.AddComponent<RectTransform>();
            buttonRT.SetParent(parentRT, false);
            buttonRT.anchorMin = new Vector2(0.5f, 0.5f);
            buttonRT.anchorMax = new Vector2(0.5f, 0.5f);
            buttonRT.anchoredPosition = new Vector2(0f, -50f);
            buttonRT.sizeDelta = new Vector2(200f, 100f);
            buttonRT.pivot = new Vector2(0.5f, 1f);

            button.AddComponent<CanvasRenderer>();

            // Image (blue button)
            var buttonImage = button.AddComponent<Image>();
            buttonImage.color = new Color(0f, 0.2f, 1f, 1f); // Blue
            buttonImage.raycastTarget = true;
            buttonImage.maskable = true;
            // sprite left null - displays as solid color
            buttonImage.type = Image.Type.Sliced;
            buttonImage.fillCenter = true;

            // Button
            var buttonComponent = button.AddComponent<Button>();
            buttonComponent.interactable = true;
            buttonComponent.targetGraphic = buttonImage;
            buttonComponent.transition = Selectable.Transition.ColorTint;
            buttonComponent.colors = DefaultButtonColors;
            buttonComponent.navigation = new Navigation { mode = Navigation.Mode.Automatic };

            // === LABEL (child) ===
            var label = new GameObject("Label");
            label.layer = LayerMask.NameToLayer("UI");

            var labelRT = label.AddComponent<RectTransform>();
            labelRT.SetParent(buttonRT, false);
            labelRT.anchorMin = Vector2.zero;
            labelRT.anchorMax = Vector2.one;
            labelRT.anchoredPosition = Vector2.zero;
            labelRT.sizeDelta = Vector2.zero;
            labelRT.pivot = new Vector2(0.5f, 0.5f);

            label.AddComponent<CanvasRenderer>();

            // TextMeshProUGUI
            var labelTMP = label.AddComponent<TextMeshProUGUI>();
            labelTMP.font = FontLibrary.Fonts["Attic"];
            labelTMP.text = "OK";
            labelTMP.fontSize = 48;
            labelTMP.color = Color.white;
            labelTMP.alignment = TextAlignmentOptions.Center;
            labelTMP.enableWordWrapping = true;
            labelTMP.overflowMode = TextOverflowModes.Overflow;
            labelTMP.richText = true;
            labelTMP.raycastTarget = true;

            return button;
        }
    }
}
