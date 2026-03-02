using Scripts.Helpers;
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
    /// ABILITYCASTCONFIRM - Ability cast confirmation dialog.
    /// 
    /// PURPOSE:
    /// Displays a confirmation UI when the player is about to cast
    /// an ability, showing the ability name and Cast/Cancel buttons.
    /// 
    /// VISUAL APPEARANCE:
    /// ```
    /// ┌─────────────────────────┐
    /// │      "Cast Heal?"       │ ← Label
    /// │                         │
    /// │  [Cancel]    [Cast]     │ ← Buttons
    /// └─────────────────────────┘
    /// ```
    /// 
    /// VISIBILITY:
    /// - Uses CanvasGroup alpha for fade in/out
    /// - interactable and blocksRaycasts controlled together
    /// - Hidden by default (alpha = 0)
    /// 
    /// BUTTON WIRING:
    /// - Cancel → AbilityManager.OnCancelButtonClickedEvent()
    /// - Cast → AbilityManager.OnCastButtonClicked()
    /// 
    /// RELATED FILES:
    /// - AbilityManager.cs: Handles cast/cancel events
    /// - AbilityButtonManager.cs: Triggers ability selection
    /// - TargetLineManager.cs: Target selection UI
    /// 
    /// ACCESS: AbilityCastConfirm.instance
    /// </summary>
    public class AbilityCastConfirm : MonoBehaviour
    {
        public static AbilityCastConfirm instance;
        public TextMeshProUGUI label;
        private CanvasGroup canvasGroup;
        private Button cancelBtn;
        private Button castBtn;

        public CanvasGroup CanvasGroup => canvasGroup;

        private void Awake()
        {
            instance = this;

            // Resolve references directly from scene (hardcoded path)
            var root = GameObject.Find("Canvas/AbilityCastConfirm");
            canvasGroup = root.GetComponent<CanvasGroup>();
            label = root.transform.Find("Label").GetComponent<TextMeshProUGUI>();
            cancelBtn = root.transform.Find("CancelButton").GetComponent<Button>();
            castBtn = root.transform.Find("CastButton").GetComponent<Button>();

            // Ensure initial hidden state
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            label.text = string.Empty;

            cancelBtn.gameObject.SetActive(false);
            castBtn.gameObject.SetActive(false);

            // Wire UI to AbilityManager
            cancelBtn.onClick.RemoveAllListeners();
            cancelBtn.onClick.AddListener(() => GameHelper.AbilityManager.OnCancelButtonClickedEvent());
            castBtn.onClick.RemoveAllListeners();
            castBtn.onClick.AddListener(() => GameHelper.AbilityManager.OnCastButtonClicked());
        }

        public void SetTitle(string text)
        {
            label.text = text ?? string.Empty;
        }

        public void ClearTitle()
        {
            label.text = string.Empty;
        }

        public void Toggle(bool isActive = true)
        {
            // make interactable immediately so buttons respond once visible
            canvasGroup.interactable = isActive;
            canvasGroup.blocksRaycasts = isActive;
            
            if (isActive)
                FadeIn();
            else
                FadeOut();
        }


        public void FadeIn()
        {
            cancelBtn.gameObject.SetActive(true);
            castBtn.gameObject.SetActive(true);
            StopAllCoroutines();
            StartCoroutine(FadeGroupTo(1f, 0.12f));
        }

        public void FadeOut()
        {
            cancelBtn.gameObject.SetActive(false);
            castBtn.gameObject.SetActive(false);
            StopAllCoroutines();
            StartCoroutine(FadeGroupTo(0f, 0.12f));         
        }

        // Expose explicit show/hide for buttons without changing canvas visibility
        public void ShowButtons()
        {
            cancelBtn.gameObject.SetActive(true);
            castBtn.gameObject.SetActive(true);
        }

        public void HideButtons()
        {
            cancelBtn.gameObject.SetActive(false);
            castBtn.gameObject.SetActive(false);
        }

        private IEnumerator FadeGroupTo(float targetAlpha, float duration)
        {
            // assume canvasGroup always present
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
