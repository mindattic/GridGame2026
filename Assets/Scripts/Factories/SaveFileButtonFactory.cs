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
    /// SAVEFILEBUTTONFACTORY - Creates save slot button GameObjects.
    /// 
    /// PURPOSE:
    /// Creates buttons for the save file selection screen. Each button
    /// displays save slot number and timestamp.
    /// 
    /// VISUAL APPEARANCE:
    /// ```
    /// ???????????????????????????????????????????
    /// ? Save 1                    2024-01-15   ?
    /// ???????????????????????????????????????????
    ///    ?                              ?
    ///  SaveNumber              Timestamp
    /// ```
    /// 
    /// CREATED HIERARCHY:
    /// ```
    /// SaveFileButton (root)
    /// ??? Image (button background)
    /// ??? Button (click handler)
    /// ??? LayoutElement (scroll view sizing)
    /// ??? SaveNumber (TMP - left aligned)
    /// ??? Timestamp (TMP - right aligned)
    /// ```
    /// 
    /// LAYOUT:
    /// - Default size: 1024x128
    /// - Uses LayoutElement for flexible scroll view sizing
    /// - SaveNumber left-aligned, Timestamp right-aligned
    /// 
    /// CALLED BY:
    /// - SaveFileSelectManager.AddSaveSlotButton()
    /// 
    /// RELATED FILES:
    /// - SaveFileSelectManager.cs: Save selection screen
    /// - ProfileHelper.cs: Save data access
    /// </summary>
    public static class SaveFileButtonFactory
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

        /// <summary>Creates a new save file button.</summary>
        public static GameObject Create(Transform parent = null)
        {
            // === ROOT: SaveFileButton ===
            var root = new GameObject("SaveFileButton");
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

            // LayoutElement
            var layoutElement = root.AddComponent<LayoutElement>();
            layoutElement.ignoreLayout = false;
            layoutElement.minWidth = -1;
            layoutElement.minHeight = -1;
            layoutElement.preferredWidth = -1;
            layoutElement.preferredHeight = 64f;
            layoutElement.flexibleWidth = 1f;
            layoutElement.flexibleHeight = -1;
            layoutElement.layoutPriority = 1;

            // === CHILD: SaveNumber (left-aligned label) ===
            var saveNumber = new GameObject("SaveNumber");
            saveNumber.layer = LayerMask.NameToLayer("UI");

            var saveNumberRT = saveNumber.AddComponent<RectTransform>();
            saveNumberRT.SetParent(rootRT, false);
            saveNumberRT.anchorMin = new Vector2(0f, 0.5f);
            saveNumberRT.anchorMax = new Vector2(0f, 0.5f);
            saveNumberRT.anchoredPosition = new Vector2(64f, 0f);
            saveNumberRT.sizeDelta = Vector2.zero;
            saveNumberRT.pivot = new Vector2(0.5f, 0.5f);

            saveNumber.AddComponent<CanvasRenderer>();

            var saveNumberTMP = saveNumber.AddComponent<TextMeshProUGUI>();
            saveNumberTMP.text = "Save 1";
            saveNumberTMP.fontSize = 48;
            saveNumberTMP.color = Color.white;
            saveNumberTMP.alignment = TextAlignmentOptions.Left;
            saveNumberTMP.enableWordWrapping = false;
            saveNumberTMP.overflowMode = TextOverflowModes.Overflow;
            saveNumberTMP.richText = true;
            saveNumberTMP.raycastTarget = true;

            // === CHILD: Timestamp (right-aligned label) ===
            var timestamp = new GameObject("Timestamp");
            timestamp.layer = LayerMask.NameToLayer("UI");

            var timestampRT = timestamp.AddComponent<RectTransform>();
            timestampRT.SetParent(rootRT, false);
            timestampRT.anchorMin = new Vector2(1f, 0.5f);
            timestampRT.anchorMax = new Vector2(1f, 0.5f);
            timestampRT.anchoredPosition = new Vector2(-64f, 0f);
            timestampRT.sizeDelta = Vector2.zero;
            timestampRT.pivot = new Vector2(0.5f, 0.5f);

            timestamp.AddComponent<CanvasRenderer>();

            var timestampTMP = timestamp.AddComponent<TextMeshProUGUI>();
            timestampTMP.text = "";
            timestampTMP.fontSize = 48;
            timestampTMP.color = Color.white;
            timestampTMP.alignment = TextAlignmentOptions.Right;
            timestampTMP.enableWordWrapping = false;
            timestampTMP.overflowMode = TextOverflowModes.Overflow;
            timestampTMP.richText = true;
            timestampTMP.raycastTarget = true;

            // Parent if specified
            if (parent != null)
            {
                rootRT.SetParent(parent, false);
            }

            return root;
        }
    }
}
