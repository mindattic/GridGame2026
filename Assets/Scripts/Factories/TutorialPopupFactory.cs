using Assets.Scripts.GUI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Factories
{
    /// <summary>
    /// Programmatic factory for TutorialPopup - replaces TutorialPopup.prefab
    /// Hierarchy:
    /// - TutorialPopup (root - TutorialPopup component)
    ///   - Panel (Image - background)
    ///     - Title (TMP)
    ///     - Image (Image - tutorial image)
    ///     - Content (TMP)
    ///     - PreviousButton (Button with TMP child)
    ///     - NextButton (Button with TMP child)
    ///     - CloseButton (Button with TMP child)
    /// </summary>
    public static class TutorialPopupFactory
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
            // === ROOT: TutorialPopup ===
            var root = new GameObject("TutorialPopup");
            root.layer = 5; // UI layer

            var rootRT = root.AddComponent<RectTransform>();
            rootRT.anchorMin = Vector2.zero;
            rootRT.anchorMax = Vector2.one;
            rootRT.anchoredPosition = Vector2.zero;
            rootRT.sizeDelta = Vector2.zero;
            rootRT.pivot = new Vector2(0.5f, 0.5f);

            // Add TutorialPopup component (will be initialized later)
            root.AddComponent<TutorialPopup>();

            // === CHILD: Panel (dark overlay + content container) ===
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
            panelImage.color = new Color(0f, 0f, 0f, 0.85f);
            panelImage.raycastTarget = true;

            // === GRANDCHILD: Title ===
            var titleObj = new GameObject("Title");
            titleObj.layer = 5;

            var titleRT = titleObj.AddComponent<RectTransform>();
            titleRT.SetParent(panelRT, false);
            titleRT.anchorMin = new Vector2(0.5f, 0.5f);
            titleRT.anchorMax = new Vector2(0.5f, 0.5f);
            titleRT.anchoredPosition = new Vector2(0f, -90f);
            titleRT.sizeDelta = new Vector2(400f, 300f);
            titleRT.pivot = new Vector2(0.5f, 0.5f);

            titleObj.AddComponent<CanvasRenderer>();

            var titleTMP = titleObj.AddComponent<TextMeshProUGUI>();
            titleTMP.text = "Title";
            titleTMP.fontSize = 36;
            titleTMP.color = Color.white;
            titleTMP.alignment = TextAlignmentOptions.Top;
            titleTMP.enableWordWrapping = true;
            titleTMP.raycastTarget = true;

            // === GRANDCHILD: Image (tutorial image) ===
            var imageObj = new GameObject("Image");
            imageObj.layer = 5;

            var imageRT = imageObj.AddComponent<RectTransform>();
            imageRT.SetParent(panelRT, false);
            imageRT.anchorMin = new Vector2(0.5f, 0.5f);
            imageRT.anchorMax = new Vector2(0.5f, 0.5f);
            imageRT.anchoredPosition = new Vector2(0f, 50f);
            imageRT.sizeDelta = new Vector2(300f, 200f);
            imageRT.pivot = new Vector2(0.5f, 0.5f);

            imageObj.AddComponent<CanvasRenderer>();

            var tutorialImage = imageObj.AddComponent<Image>();
            tutorialImage.color = Color.white;
            tutorialImage.raycastTarget = false;
            tutorialImage.preserveAspect = true;

            // === GRANDCHILD: Content ===
            var contentObj = new GameObject("Content");
            contentObj.layer = 5;

            var contentRT = contentObj.AddComponent<RectTransform>();
            contentRT.SetParent(panelRT, false);
            contentRT.anchorMin = new Vector2(0.5f, 0.5f);
            contentRT.anchorMax = new Vector2(0.5f, 0.5f);
            contentRT.anchoredPosition = new Vector2(0f, -200f);
            contentRT.sizeDelta = new Vector2(400f, 150f);
            contentRT.pivot = new Vector2(0.5f, 0.5f);

            contentObj.AddComponent<CanvasRenderer>();

            var contentTMP = contentObj.AddComponent<TextMeshProUGUI>();
            contentTMP.text = "Content";
            contentTMP.fontSize = 24;
            contentTMP.color = Color.white;
            contentTMP.alignment = TextAlignmentOptions.Top;
            contentTMP.enableWordWrapping = true;
            contentTMP.raycastTarget = true;

            // === GRANDCHILD: PreviousButton ===
            var prevButton = CreateButton("PreviousButton", "<", panelRT, new Vector2(-170f, -340f));
            
            // === GRANDCHILD: NextButton ===
            var nextButton = CreateButton("NextButton", ">", panelRT, new Vector2(170f, -340f));

            // === GRANDCHILD: CloseButton ===
            var closeButton = CreateButton("CloseButton", "X", panelRT, new Vector2(0f, -340f));

            // Parent if specified
            if (parent != null)
            {
                rootRT.SetParent(parent, false);
            }

            return root;
        }

        private static GameObject CreateButton(string name, string labelText, RectTransform parent, Vector2 position)
        {
            var button = new GameObject(name);
            button.layer = 5;

            var buttonRT = button.AddComponent<RectTransform>();
            buttonRT.SetParent(parent, false);
            buttonRT.anchorMin = new Vector2(0.5f, 0.5f);
            buttonRT.anchorMax = new Vector2(0.5f, 0.5f);
            buttonRT.anchoredPosition = position;
            buttonRT.sizeDelta = new Vector2(64f, 64f);
            buttonRT.pivot = new Vector2(0.5f, 0.5f);

            button.AddComponent<CanvasRenderer>();

            var buttonImage = button.AddComponent<Image>();
            buttonImage.color = new Color(1f, 1f, 1f, 0f); // Transparent
            buttonImage.raycastTarget = true;
            buttonImage.type = Image.Type.Sliced;

            var buttonComp = button.AddComponent<Button>();
            buttonComp.interactable = true;
            buttonComp.targetGraphic = buttonImage;
            buttonComp.transition = Selectable.Transition.ColorTint;
            buttonComp.colors = DefaultButtonColors;
            buttonComp.navigation = new Navigation { mode = Navigation.Mode.Automatic };

            // === Button > Label ===
            var label = new GameObject("Label");
            label.layer = 5;

            var labelRT = label.AddComponent<RectTransform>();
            labelRT.SetParent(buttonRT, false);
            labelRT.anchorMin = Vector2.zero;
            labelRT.anchorMax = Vector2.one;
            labelRT.anchoredPosition = Vector2.zero;
            labelRT.sizeDelta = Vector2.zero;
            labelRT.pivot = new Vector2(0.5f, 0.5f);

            label.AddComponent<CanvasRenderer>();

            var labelTMP = label.AddComponent<TextMeshProUGUI>();
            labelTMP.text = labelText;
            labelTMP.fontSize = 32;
            labelTMP.color = Color.white;
            labelTMP.alignment = TextAlignmentOptions.Center;
            labelTMP.raycastTarget = false;

            return button;
        }
    }
}
