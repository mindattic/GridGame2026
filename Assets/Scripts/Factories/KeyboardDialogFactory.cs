using Scripts.Factories;
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
    /// KEYBOARDDIALOGFACTORY - Creates virtual keyboard dialog GameObjects.
    /// 
    /// PURPOSE:
    /// Creates a full-screen virtual keyboard for text input on devices
    /// without physical keyboards (mobile, console, etc).
    /// 
    /// VISUAL LAYOUT:
    /// ```
    /// ┌─────────────────────────────────────┐
    /// │          Enter your name:           │
    /// │  ┌─────────────────────────────┐    │
    /// │  │ Player1                     │    │ ← Input field
    /// │  └─────────────────────────────┘    │
    /// │  [1][2][3][4][5][6][7][8][9][0]     │ ← Row 1: Numbers
    /// │  [Q][W][E][R][T][Y][U][I][O][P]     │ ← Row 2: QWERTY
    /// │   [A][S][D][F][G][H][J][K][L]       │ ← Row 3
    /// │  [⇧][Z][X][C][V][B][N][M][⌫]       │ ← Row 4
    /// │       [        SPACE        ]       │ ← Row 5
    /// │        [ OK ]    [ Cancel ]         │ ← Row 6
    /// └─────────────────────────────────────┘
    /// ```
    /// 
    /// CREATED HIERARCHY:
    /// ```
    /// KeyboardDialog (root)
    /// ├── KeyboardDialogInstance (behavior)
    /// └── Panel (dark overlay)
    ///     ├── Prompt (TMP)
    ///     ├── InputBackdrop
    ///     │   └── InputLabel (TMP)
    ///     ├── KeysContainer
    ///     │   ├── Row1 → Number keys
    ///     │   ├── Row2 → QWERTY row
    ///     │   ├── Row3 → ASDF row
    ///     │   ├── Row4 → Shift + ZXCV + Backspace
    ///     │   └── Row5 → Space bar
    ///     └── ButtonRow (OK/Cancel)
    /// ```
    /// 
    /// CALLED BY:
    /// - ProfileCreateManager.ShowKeyboard()
    /// 
    /// RELATED FILES:
    /// - KeyboardDialogInstance.cs: Keyboard behavior
    /// - KeyButtonFactory.cs: Creates individual keys
    /// - ProfileSelectManager.cs: Profile naming
    /// </summary>
    public static class KeyboardDialogFactory
    {
        private const float KeySize = 64f;
        private const float KeySpacing = 8f;
        private const float RowSpacing = 8f;

        /// <summary>Creates a new virtual keyboard dialog.</summary>
        public static GameObject Create(Transform parent = null)
        {
            // === ROOT: KeyboardDialog ===
            var root = new GameObject("KeyboardDialog");
            root.layer = 5; // UI layer

            var rootRT = root.AddComponent<RectTransform>();
            rootRT.anchorMin = Vector2.zero;
            rootRT.anchorMax = Vector2.one;
            rootRT.anchoredPosition = Vector2.zero;
            rootRT.sizeDelta = Vector2.zero;
            rootRT.pivot = new Vector2(0.5f, 0.5f);

            root.AddComponent<KeyboardDialogInstance>();

            // === CHILD: Panel (dark overlay) ===
            var panel = new GameObject("Panel");
            panel.layer = 5;

            var panelRT = panel.AddComponent<RectTransform>();
            panelRT.SetParent(rootRT, false);
            panelRT.anchorMin = Vector2.zero;
            panelRT.anchorMax = Vector2.one;
            panelRT.anchoredPosition = Vector2.zero;
            panelRT.sizeDelta = Vector2.zero;
            panelRT.pivot = new Vector2(0.5f, 0.5f);

            panel.AddComponent<CanvasRenderer>();

            var panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.9f);
            panelImage.raycastTarget = true;

            // === GRANDCHILD: Prompt ===
            var prompt = CreateTextObject("Prompt", panelRT, new Vector2(0f, 300f), new Vector2(600f, 50f), "Enter Name", 32);

            // === GRANDCHILD: InputBackdrop ===
            var inputBackdrop = new GameObject("InputBackdrop");
            inputBackdrop.layer = 5;

            var inputBackdropRT = inputBackdrop.AddComponent<RectTransform>();
            inputBackdropRT.SetParent(panelRT, false);
            inputBackdropRT.anchorMin = new Vector2(0.5f, 0.5f);
            inputBackdropRT.anchorMax = new Vector2(0.5f, 0.5f);
            inputBackdropRT.anchoredPosition = new Vector2(0f, 220f);
            inputBackdropRT.sizeDelta = new Vector2(500f, 60f);
            inputBackdropRT.pivot = new Vector2(0.5f, 0.5f);

            inputBackdrop.AddComponent<CanvasRenderer>();

            var inputBgImage = inputBackdrop.AddComponent<Image>();
            inputBgImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            inputBgImage.raycastTarget = false;

            // === InputBackdrop > InputLabel ===
            var inputLabel = CreateTextObject("InputLabel", inputBackdropRT, Vector2.zero, new Vector2(480f, 50f), "", 28);

            // === GRANDCHILD: KeysContainer ===
            var keysContainer = new GameObject("KeysContainer");
            keysContainer.layer = 5;

            var keysContainerRT = keysContainer.AddComponent<RectTransform>();
            keysContainerRT.SetParent(panelRT, false);
            keysContainerRT.anchorMin = new Vector2(0.5f, 0.5f);
            keysContainerRT.anchorMax = new Vector2(0.5f, 0.5f);
            keysContainerRT.anchoredPosition = new Vector2(0f, -20f);
            keysContainerRT.sizeDelta = new Vector2(800f, 400f);
            keysContainerRT.pivot = new Vector2(0.5f, 0.5f);

            // Create keyboard rows
            float rowY = 150f;

            // Row 1: 1-0 (10 keys)
            var row1 = CreateRow("Row1", keysContainerRT, rowY);
            var keys1 = new[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0" };
            CreateKeyRow(row1, keys1, "Key");

            // Row 2: Q-P (10 keys)
            rowY -= KeySize + RowSpacing;
            var row2 = CreateRow("Row2", keysContainerRT, rowY);
            var keys2 = new[] { "Q", "W", "E", "R", "T", "Y", "U", "I", "O", "P" };
            CreateKeyRow(row2, keys2, "Key");

            // Row 3: A-L (9 keys)
            rowY -= KeySize + RowSpacing;
            var row3 = CreateRow("Row3", keysContainerRT, rowY);
            var keys3 = new[] { "A", "S", "D", "F", "G", "H", "J", "K", "L" };
            CreateKeyRow(row3, keys3, "Key");

            // Row 4: Z-M (7 keys)
            rowY -= KeySize + RowSpacing;
            var row4 = CreateRow("Row4", keysContainerRT, rowY);
            var keys4 = new[] { "Z", "X", "C", "V", "B", "N", "M" };
            CreateKeyRow(row4, keys4, "Key");

            // Row 5: CapsLock, Space, Enter, Backspace
            rowY -= KeySize + RowSpacing;
            var row5 = CreateRow("Row5", keysContainerRT, rowY);
            CreateSpecialKey(row5, "CapsLock", "CAPS", -280f, KeySize);
            CreateSpecialKey(row5, "Space", "SPACE", 0f, KeySize * 3);
            CreateSpecialKey(row5, "Enter", "ENTER", 200f, KeySize * 1.5f);
            CreateSpecialKey(row5, "Backspace", "←", 320f, KeySize);

            // === GRANDCHILD: ConfirmationContainer ===
            var confirmContainer = new GameObject("ConfirmationContainer");
            confirmContainer.layer = 5;
            confirmContainer.SetActive(false);

            var confirmContainerRT = confirmContainer.AddComponent<RectTransform>();
            confirmContainerRT.SetParent(panelRT, false);
            confirmContainerRT.anchorMin = new Vector2(0.5f, 0.5f);
            confirmContainerRT.anchorMax = new Vector2(0.5f, 0.5f);
            confirmContainerRT.anchoredPosition = Vector2.zero;
            confirmContainerRT.sizeDelta = new Vector2(400f, 200f);
            confirmContainerRT.pivot = new Vector2(0.5f, 0.5f);

            // Confirmation text
            var confirmation = CreateTextObject("Confirmation", confirmContainerRT, new Vector2(0f, 50f), new Vector2(380f, 60f), "Are you sure?", 24);

            // Yes/No buttons
            CreateConfirmButton("ButtonYes", "YES", confirmContainerRT, new Vector2(-80f, -40f));
            CreateConfirmButton("ButtonNo", "NO", confirmContainerRT, new Vector2(80f, -40f));

            // Parent if specified
            if (parent != null)
            {
                rootRT.SetParent(parent, false);
            }

            return root;
        }

        private static GameObject CreateRow(string name, RectTransform parent, float yPos)
        {
            var row = new GameObject(name);
            row.layer = 5;

            var rowRT = row.AddComponent<RectTransform>();
            rowRT.SetParent(parent, false);
            rowRT.anchorMin = new Vector2(0.5f, 0.5f);
            rowRT.anchorMax = new Vector2(0.5f, 0.5f);
            rowRT.anchoredPosition = new Vector2(0f, yPos);
            rowRT.sizeDelta = new Vector2(800f, KeySize);
            rowRT.pivot = new Vector2(0.5f, 0.5f);

            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = KeySpacing;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            return row;
        }

        private static void CreateKeyRow(GameObject row, string[] keys, string prefix)
        {
            var rowRT = row.GetComponent<RectTransform>();
            foreach (var key in keys)
            {
                var keyGO = CreateKey($"{prefix}{key}", key, rowRT);
            }
        }

        private static GameObject CreateKey(string name, string label, RectTransform parent)
        {
            var key = new GameObject(name);
            key.layer = 5;

            var keyRT = key.AddComponent<RectTransform>();
            keyRT.SetParent(parent, false);
            keyRT.sizeDelta = new Vector2(KeySize, KeySize);
            keyRT.pivot = new Vector2(0.5f, 0.5f);

            key.AddComponent<CanvasRenderer>();

            var keyImage = key.AddComponent<Image>();
            keyImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);
            keyImage.raycastTarget = true;

            var keyButton = key.AddComponent<Button>();
            keyButton.targetGraphic = keyImage;
            keyButton.transition = Selectable.Transition.ColorTint;
            keyButton.colors = new ColorBlock
            {
                normalColor = new Color(0.3f, 0.3f, 0.3f, 1f),
                highlightedColor = new Color(0.4f, 0.4f, 0.4f, 1f),
                pressedColor = new Color(0.5f, 0.5f, 0.5f, 1f),
                selectedColor = new Color(0.4f, 0.4f, 0.4f, 1f),
                disabledColor = new Color(0.2f, 0.2f, 0.2f, 0.5f),
                colorMultiplier = 1f,
                fadeDuration = 0.1f
            };

            // Label
            var labelGO = new GameObject("Label");
            labelGO.layer = 5;

            var labelRT = labelGO.AddComponent<RectTransform>();
            labelRT.SetParent(keyRT, false);
            labelRT.anchorMin = Vector2.zero;
            labelRT.anchorMax = Vector2.one;
            labelRT.anchoredPosition = Vector2.zero;
            labelRT.sizeDelta = Vector2.zero;
            labelRT.pivot = new Vector2(0.5f, 0.5f);

            labelGO.AddComponent<CanvasRenderer>();

            var labelTMP = labelGO.AddComponent<TextMeshProUGUI>();
            labelTMP.text = label;
            labelTMP.fontSize = 32;
            labelTMP.color = Color.white;
            labelTMP.alignment = TextAlignmentOptions.Center;
            labelTMP.raycastTarget = false;

            return key;
        }

        private static GameObject CreateSpecialKey(GameObject row, string name, string label, float xOffset, float width)
        {
            var key = new GameObject($"Key{name}");
            key.layer = 5;

            var rowRT = row.GetComponent<RectTransform>();
            var keyRT = key.AddComponent<RectTransform>();
            keyRT.SetParent(rowRT, false);
            keyRT.anchorMin = new Vector2(0.5f, 0.5f);
            keyRT.anchorMax = new Vector2(0.5f, 0.5f);
            keyRT.anchoredPosition = new Vector2(xOffset, 0f);
            keyRT.sizeDelta = new Vector2(width, KeySize);
            keyRT.pivot = new Vector2(0.5f, 0.5f);

            key.AddComponent<CanvasRenderer>();

            var keyImage = key.AddComponent<Image>();
            keyImage.color = new Color(0.25f, 0.25f, 0.35f, 1f);
            keyImage.raycastTarget = true;

            var keyButton = key.AddComponent<Button>();
            keyButton.targetGraphic = keyImage;
            keyButton.transition = Selectable.Transition.ColorTint;
            keyButton.colors = new ColorBlock
            {
                normalColor = new Color(0.25f, 0.25f, 0.35f, 1f),
                highlightedColor = new Color(0.35f, 0.35f, 0.45f, 1f),
                pressedColor = new Color(0.45f, 0.45f, 0.55f, 1f),
                selectedColor = new Color(0.35f, 0.35f, 0.45f, 1f),
                disabledColor = new Color(0.15f, 0.15f, 0.25f, 0.5f),
                colorMultiplier = 1f,
                fadeDuration = 0.1f
            };

            // Label
            var labelGO = new GameObject("Label");
            labelGO.layer = 5;

            var labelRT = labelGO.AddComponent<RectTransform>();
            labelRT.SetParent(keyRT, false);
            labelRT.anchorMin = Vector2.zero;
            labelRT.anchorMax = Vector2.one;
            labelRT.anchoredPosition = Vector2.zero;
            labelRT.sizeDelta = Vector2.zero;
            labelRT.pivot = new Vector2(0.5f, 0.5f);

            labelGO.AddComponent<CanvasRenderer>();

            var labelTMP = labelGO.AddComponent<TextMeshProUGUI>();
            labelTMP.text = label;
            labelTMP.fontSize = name == "Backspace" ? 40 : 18;
            labelTMP.color = Color.white;
            labelTMP.alignment = TextAlignmentOptions.Center;
            labelTMP.raycastTarget = false;

            return key;
        }

        private static GameObject CreateTextObject(string name, RectTransform parent, Vector2 position, Vector2 size, string text, float fontSize)
        {
            var obj = new GameObject(name);
            obj.layer = 5;

            var objRT = obj.AddComponent<RectTransform>();
            objRT.SetParent(parent, false);
            objRT.anchorMin = new Vector2(0.5f, 0.5f);
            objRT.anchorMax = new Vector2(0.5f, 0.5f);
            objRT.anchoredPosition = position;
            objRT.sizeDelta = size;
            objRT.pivot = new Vector2(0.5f, 0.5f);

            obj.AddComponent<CanvasRenderer>();

            var tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;

            return obj;
        }

        private static GameObject CreateConfirmButton(string name, string label, RectTransform parent, Vector2 position)
        {
            var button = new GameObject(name);
            button.layer = 5;

            var buttonRT = button.AddComponent<RectTransform>();
            buttonRT.SetParent(parent, false);
            buttonRT.anchorMin = new Vector2(0.5f, 0.5f);
            buttonRT.anchorMax = new Vector2(0.5f, 0.5f);
            buttonRT.anchoredPosition = position;
            buttonRT.sizeDelta = new Vector2(120f, 50f);
            buttonRT.pivot = new Vector2(0.5f, 0.5f);

            button.AddComponent<CanvasRenderer>();

            var buttonImage = button.AddComponent<Image>();
            buttonImage.color = new Color(0.2f, 0.4f, 0.2f, 1f);
            buttonImage.raycastTarget = true;

            var buttonComp = button.AddComponent<Button>();
            buttonComp.targetGraphic = buttonImage;
            buttonComp.transition = Selectable.Transition.ColorTint;

            // Label
            var labelGO = new GameObject("Label");
            labelGO.layer = 5;

            var labelRT = labelGO.AddComponent<RectTransform>();
            labelRT.SetParent(buttonRT, false);
            labelRT.anchorMin = Vector2.zero;
            labelRT.anchorMax = Vector2.one;
            labelRT.anchoredPosition = Vector2.zero;
            labelRT.sizeDelta = Vector2.zero;
            labelRT.pivot = new Vector2(0.5f, 0.5f);

            labelGO.AddComponent<CanvasRenderer>();

            var labelTMP = labelGO.AddComponent<TextMeshProUGUI>();
            labelTMP.text = label;
            labelTMP.fontSize = 24;
            labelTMP.color = Color.white;
            labelTMP.alignment = TextAlignmentOptions.Center;
            labelTMP.raycastTarget = false;

            return button;
        }
    }
}
