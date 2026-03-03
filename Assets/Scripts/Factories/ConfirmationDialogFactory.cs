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
    /// CONFIRMATIONDIALOGFACTORY - Creates confirmation (Yes/No) dialogs.
    /// 
    /// PURPOSE:
    /// Creates modal dialog boxes with a message and Yes/No buttons
    /// for player confirmations (quit, restart, etc).
    /// 
    /// VISUAL APPEARANCE:
    /// ```
    /// ┌────────────────────────────────┐
    /// │                                │
    /// │   Are you sure you want to     │
    /// │   quit the game?               │
    /// │                                │
    /// │    [ Yes ]      [ No ]         │
    /// └────────────────────────────────┘
    /// ```
    /// 
    /// CREATED HIERARCHY:
    /// ```
    /// ConfirmationDialog (root)
    /// ├── ConfirmationDialogInstance (behavior)
    /// └── Panel (centered container)
    ///     ├── Prompt (TMP - question text)
    ///     ├── ButtonYes (Button)
    ///     │   └── Label (TMP - "Yes")
    ///     └── ButtonNo (Button)
    ///         └── Label (TMP - "No")
    /// ```
    /// 
    /// RELATED FILES:
    /// - ConfirmationDialogInstance.cs: Dialog behavior
    /// - MessageBoxFactory.cs: OK-only variant
    /// - PauseMenu.cs: Uses for quit confirmation
    /// </summary>
    public static class ConfirmationDialogFactory
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

        /// <summary>Creates the instance.</summary>
        public static GameObject Create(Transform parent = null)
        {
            var root = new GameObject("ConfirmationDialog");
            root.layer = LayerMask.NameToLayer("UI");

            var rootRT = root.AddComponent<RectTransform>();
            rootRT.anchorMin = Vector2.zero;
            rootRT.anchorMax = Vector2.one;
            rootRT.anchoredPosition = Vector2.zero;
            rootRT.sizeDelta = Vector2.zero;
            rootRT.pivot = new Vector2(0.5f, 0.5f);

            root.AddComponent<CanvasRenderer>();
            root.AddComponent<ConfirmationDialogInstance>();

            var panel = CreatePanel(rootRT);
            var panelRT = panel.GetComponent<RectTransform>();

            // === PROMPT ===
            CreatePrompt(panelRT);

            // === BUTTON YES ===
            CreateButton(panelRT, "ButtonYes", "Yes",
                new Color(0f, 0.2f, 1f, 1f), // Blue
                new Vector2(-10f, -50f),
                new Vector2(1f, 1f)); // Pivot right-top

            // === BUTTON NO ===
            CreateButton(panelRT, "ButtonNo", "No",
                new Color(1f, 0f, 0.2f, 1f), // Red
                new Vector2(1f, -50f),
                new Vector2(0f, 1f)); // Pivot left-top

            // Parent if specified
            if (parent != null)
            {
                rootRT.SetParent(parent, false);
            }

            return root;
        }

        /// <summary>Creates the panel.</summary>
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

        /// <summary>Creates the prompt.</summary>
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
            tmpText.text = "Are you sure?";
            tmpText.fontSize = 48;
            tmpText.color = Color.white;
            tmpText.alignment = TextAlignmentOptions.Center;
            tmpText.enableWordWrapping = false;
            tmpText.overflowMode = TextOverflowModes.Overflow;
            tmpText.richText = true;
            tmpText.raycastTarget = true;

            return prompt;
        }

        /// <summary>Creates the button.</summary>
        private static GameObject CreateButton(RectTransform parentRT, string name, string labelText, Color buttonColor, Vector2 position, Vector2 pivot)
        {
            var button = new GameObject(name);
            button.layer = LayerMask.NameToLayer("UI");

            var buttonRT = button.AddComponent<RectTransform>();
            buttonRT.SetParent(parentRT, false);
            buttonRT.anchorMin = new Vector2(0.5f, 0.5f);
            buttonRT.anchorMax = new Vector2(0.5f, 0.5f);
            buttonRT.anchoredPosition = position;
            buttonRT.sizeDelta = new Vector2(200f, 100f);
            buttonRT.pivot = pivot;

            button.AddComponent<CanvasRenderer>();

            // Image
            var buttonImage = button.AddComponent<Image>();
            buttonImage.color = buttonColor;
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
            labelTMP.text = labelText;
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
