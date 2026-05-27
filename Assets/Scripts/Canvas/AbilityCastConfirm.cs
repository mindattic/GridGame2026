using Scripts.Helpers;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Factories;
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

namespace Scripts.Canvas
{
    /// <summary>
    /// ABILITYCASTCONFIRM - Cast confirmation modal, built entirely in code.
    ///
    /// <para>VISUAL APPEARANCE: a normal centered modal — faint full-screen dim, a dark card
    /// with a Title at the top, a Description below it, and a Cancel / OK button row at the
    /// bottom.</para>
    /// <code>
    /// ┌──────────────────────────────┐
    /// │          Cast Heal?          │  ← Title
    /// │                              │
    /// │  Launches a healing spark…   │  ← Description
    /// │                              │
    /// │   [ Cancel ]      [  OK  ]   │  ← Buttons
    /// └──────────────────────────────┘
    /// </code>
    ///
    /// <para>The modal is constructed in Awake (mirroring ConfirmationDialogFactory) onto this
    /// component's GameObject, so its look is defined in code — not baked into Game.unity.
    /// Visibility is driven by a CanvasGroup fade.</para>
    ///
    /// <para>BUTTON WIRING: Cancel → AbilityManager.OnCancelButtonClickedEvent(),
    /// OK → AbilityManager.OnCastButtonClicked().</para>
    ///
    /// ACCESS: AbilityCastConfirm.instance
    /// </summary>
    public class AbilityCastConfirm : MonoBehaviour
    {
        public static AbilityCastConfirm instance;
        public TextMeshProUGUI label;          // title
        private TextMeshProUGUI description;
        private CanvasGroup canvasGroup;
        private Button cancelBtn;
        private Button castBtn;

        public CanvasGroup CanvasGroup => canvasGroup;

        private static readonly ColorBlock ButtonColors = new ColorBlock
        {
            normalColor = Color.white,
            highlightedColor = new Color(0.96f, 0.96f, 0.96f, 1f),
            pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f),
            selectedColor = new Color(0.96f, 0.96f, 0.96f, 1f),
            disabledColor = new Color(0.78f, 0.78f, 0.78f, 0.5f),
            colorMultiplier = 1f,
            fadeDuration = 0.1f
        };

        /// <summary>Caches the singleton, builds the modal in code, and starts hidden.</summary>
        private void Awake()
        {
            instance = this;
            BuildModal();

            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            label.text = string.Empty;
            description.text = string.Empty;
            cancelBtn.gameObject.SetActive(false);
            castBtn.gameObject.SetActive(false);

            cancelBtn.onClick.RemoveAllListeners();
            cancelBtn.onClick.AddListener(() => GameHelper.AbilityManager.OnCancelButtonClickedEvent());
            castBtn.onClick.RemoveAllListeners();
            castBtn.onClick.AddListener(() => GameHelper.AbilityManager.OnCastButtonClicked());
        }

        /// <summary>Constructs the modal hierarchy on this GameObject (dim + card + texts + buttons).</summary>
        private void BuildModal()
        {
            int uiLayer = LayerMask.NameToLayer("UI");
            gameObject.layer = uiLayer;

            // Root stretches to fill the canvas so the card lands dead-center on any aspect ratio.
            var rootRT = GetComponent<RectTransform>();
            if (rootRT == null) rootRT = gameObject.AddComponent<RectTransform>();
            rootRT.anchorMin = Vector2.zero;
            rootRT.anchorMax = Vector2.one;
            rootRT.offsetMin = Vector2.zero;
            rootRT.offsetMax = Vector2.zero;
            rootRT.pivot = new Vector2(0.5f, 0.5f);

            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

            // Remove any pre-existing scene children so the code-built look is authoritative.
            for (int i = rootRT.childCount - 1; i >= 0; i--)
                Destroy(rootRT.GetChild(i).gameObject);

            // Faint full-screen dim that also blocks taps outside the card.
            var dim = gameObject.GetComponent<Image>();
            if (dim == null) dim = gameObject.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.45f);
            dim.raycastTarget = true;

            // Centered card.
            var card = MakeRect("Card", rootRT, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            card.anchoredPosition = Vector2.zero;
            card.sizeDelta = new Vector2(900f, 520f);
            var cardImg = card.gameObject.AddComponent<Image>();
            cardImg.color = new Color(0.10f, 0.10f, 0.13f, 0.96f);
            cardImg.raycastTarget = true;

            // Title near the top of the card.
            var titleRT = MakeRect("Title", card, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
            titleRT.anchoredPosition = new Vector2(0f, -40f);
            titleRT.sizeDelta = new Vector2(820f, 90f);
            label = MakeText(titleRT, string.Empty, 48, FontStyles.Bold);

            // Description in the middle, word-wrapped.
            var descRT = MakeRect("Description", card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            descRT.anchoredPosition = new Vector2(0f, 20f);
            descRT.sizeDelta = new Vector2(800f, 220f);
            description = MakeText(descRT, string.Empty, 34, FontStyles.Normal);
            description.enableWordWrapping = true;
            description.color = new Color(0.85f, 0.85f, 0.88f, 1f);

            // Button row at the bottom.
            cancelBtn = MakeButton(card, "CancelButton", "Cancel", new Color(0.7f, 0.15f, 0.2f, 1f),
                new Vector2(-150f, 70f), new Vector2(0.5f, 0f));
            castBtn = MakeButton(card, "CastButton", "OK", new Color(0.15f, 0.45f, 0.95f, 1f),
                new Vector2(150f, 70f), new Vector2(0.5f, 0f));
        }

        private static RectTransform MakeRect(string name, RectTransform parent, Vector2 aMin, Vector2 aMax, Vector2 pivot)
        {
            var go = new GameObject(name);
            go.layer = LayerMask.NameToLayer("UI");
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.pivot = pivot;
            return rt;
        }

        private static TextMeshProUGUI MakeText(RectTransform parent, string text, float fontSize, FontStyles style)
        {
            var tmp = parent.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.richText = true;
            return tmp;
        }

        private Button MakeButton(RectTransform parent, string name, string text, Color color, Vector2 pos, Vector2 anchor)
        {
            var rt = MakeRect(name, parent, anchor, anchor, new Vector2(0.5f, 0.5f));
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(240f, 100f);

            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = true;

            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.transition = Selectable.Transition.ColorTint;
            btn.colors = ButtonColors;

            var labelRT = MakeRect("Label", rt, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
            labelRT.offsetMin = Vector2.zero;
            labelRT.offsetMax = Vector2.zero;
            MakeText(labelRT, text, 40, FontStyles.Bold);

            return btn;
        }

        /// <summary>Sets the modal title text.</summary>
        public void SetTitle(string text) => label.text = text ?? string.Empty;

        /// <summary>Sets the modal description text.</summary>
        public void SetDescription(string text) => description.text = text ?? string.Empty;

        /// <summary>Builds a verb-dispatched title + description prompt for the given ability.</summary>
        public void SetTitleFor(Scripts.Instances.Ability ability)
        {
            if (ability == null) { ClearTitle(); return; }

            if (ability.IsItemAbility && ability.SourceItem != null)
            {
                SetTitle($"Use {ability.SourceItem.DisplayName}?");
                SetDescription(ability.SourceItem.Description);
                return;
            }

            if (ability.IsWeaponAbility && ability.SourceWeapon != null)
            {
                int max = ability.SourceWeapon.Durability;
                SetTitle(max > 0
                    ? $"Equip {ability.SourceWeapon.DisplayName} ({max}/{max})?"
                    : $"Equip {ability.SourceWeapon.DisplayName}?");
                SetDescription(ability.Description);
                return;
            }

            SetTitle($"Cast {ability.name}?");
            SetDescription(ability.Description);
        }

        /// <summary>Clears the title and description.</summary>
        public void ClearTitle()
        {
            label.text = string.Empty;
            description.text = string.Empty;
        }

        /// <summary>Toggles the modal visibility/interactivity with a fade.</summary>
        public void Toggle(bool isActive = true)
        {
            canvasGroup.interactable = isActive;
            canvasGroup.blocksRaycasts = isActive;
            if (isActive) FadeIn();
            else FadeOut();
        }

        /// <summary>Shows buttons and fades the modal in.</summary>
        public void FadeIn()
        {
            cancelBtn.gameObject.SetActive(true);
            castBtn.gameObject.SetActive(true);
            StopAllCoroutines();
            StartCoroutine(FadeGroupTo(1f, 0.12f));
        }

        /// <summary>Hides buttons and fades the modal out.</summary>
        public void FadeOut()
        {
            cancelBtn.gameObject.SetActive(false);
            castBtn.gameObject.SetActive(false);
            StopAllCoroutines();
            StartCoroutine(FadeGroupTo(0f, 0.12f));
        }

        /// <summary>Activates the buttons without changing alpha.</summary>
        public void ShowButtons()
        {
            cancelBtn.gameObject.SetActive(true);
            castBtn.gameObject.SetActive(true);
        }

        /// <summary>Deactivates the buttons without changing alpha.</summary>
        public void HideButtons()
        {
            cancelBtn.gameObject.SetActive(false);
            castBtn.gameObject.SetActive(false);
        }

        /// <summary>Lerps the canvas group alpha to the target over the given duration.</summary>
        private IEnumerator FadeGroupTo(float targetAlpha, float duration)
        {
            float start = canvasGroup.alpha;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(start, targetAlpha, Mathf.Clamp01(t / duration));
                yield return null;
            }
            canvasGroup.alpha = targetAlpha;
            if (Mathf.Approximately(targetAlpha, 0f))
            {
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
        }
    }
}
