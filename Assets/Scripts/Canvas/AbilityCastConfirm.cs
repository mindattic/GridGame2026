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

        /// <summary>Builds the modal on a centered world-space panel (UI sorting layer) instead of
        /// the overlay node — so it shares the board's coordinate system and VFX can sort over it.</summary>
        private void BuildModal()
        {
            int uiLayer = LayerMask.NameToLayer("UI");

            // Clear the scene-baked overlay node; the modal now lives on the world-space panel.
            var selfRt = transform as RectTransform;
            if (selfRt != null)
                for (int i = selfRt.childCount - 1; i >= 0; i--)
                    Destroy(selfRt.GetChild(i).gameObject);
            var selfImg = GetComponent<Image>();
            if (selfImg != null) selfImg.enabled = false;

            var vr = UnitConversionHelper.World.VisibleRect();
            var panel = WorldSpaceUiPanel.Create("AbilityCastConfirmWS", vr.width * 0.7f, vr.height * 0.4f, sortingOrder: 100);
            panel.PlaceAt(vr.center);

            var rootRT = panel.Content;
            canvasGroup = rootRT.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = rootRT.gameObject.AddComponent<CanvasGroup>();

            // Card fills the panel.
            var card = MakeRect("Card", rootRT, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
            card.offsetMin = Vector2.zero;
            card.offsetMax = Vector2.zero;
            var cardImg = card.gameObject.AddComponent<Image>();
            cardImg.color = new Color(0.10f, 0.10f, 0.13f, 0.96f);
            cardImg.raycastTarget = true;

            // Title near the top of the card (authored in the panel's reference-pixel space).
            var titleRT = MakeRect("Title", card, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
            titleRT.anchoredPosition = new Vector2(0f, -70f);
            titleRT.sizeDelta = new Vector2(920f, 130f);
            label = MakeText(titleRT, string.Empty, 64, FontStyles.Bold);

            // Description in the middle, word-wrapped.
            var descRT = MakeRect("Description", card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            descRT.anchoredPosition = new Vector2(0f, 30f);
            descRT.sizeDelta = new Vector2(880f, 320f);
            description = MakeText(descRT, string.Empty, 44, FontStyles.Normal);
            description.enableWordWrapping = true;
            description.color = new Color(0.85f, 0.85f, 0.88f, 1f);

            // Button row at the bottom.
            cancelBtn = MakeButton(card, "CancelButton", "Cancel", new Color(0.7f, 0.15f, 0.2f, 1f),
                new Vector2(-230f, 95f), new Vector2(0.5f, 0f));
            castBtn = MakeButton(card, "CastButton", "OK", new Color(0.15f, 0.45f, 0.95f, 1f),
                new Vector2(230f, 95f), new Vector2(0.5f, 0f));
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
