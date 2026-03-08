using System.Collections;
using TMPro;
using UnityEngine;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Factories;
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

namespace Scripts.Canvas
{
    /// <summary>
    /// ABILITYBAR - Displays ability names when executed.
    /// 
    /// PURPOSE:
    /// Shows a text notification when an ability is used, then automatically
    /// fades out after a duration. Provides visual feedback for ability activation.
    /// 
    /// VISUAL APPEARANCE:
    /// ```
    /// ┌─────────────────────────────┐
    /// │  "Paladin uses Heal"        │  ← Text fades in
    /// └─────────────────────────────┘
    ///           ↓ (after displayDuration)
    ///       fades out
    /// ```
    /// 
    /// SETTINGS:
    /// - displayDuration: Time to show before fade (default 2s)
    /// - fadeDuration: Time to fade out (default 0.5s)
    /// 
    /// USAGE:
    /// ```csharp
    /// g.AbilityBar.Show("Heal");
    /// g.AbilityBar.Show(hero, healAbility);
    /// g.AbilityBar.Hide();
    /// ```
    /// 
    /// LIFECYCLE:
    /// 1. Show() displays text with alpha = 1
    /// 2. AutoHideRoutine() waits displayDuration
    /// 3. Fades alpha to 0 over fadeDuration
    /// 4. Deactivates GameObject
    /// 
    /// RELATED FILES:
    /// - AbilityManager.cs: Calls Show() when ability cast
    /// - Ability.cs: Ability data definition
    /// 
    /// ACCESS: g.AbilityBar
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasGroup))]
    public class AbilityBar : MonoBehaviour
    {
        #region Inspector Fields

        [Header("References")]
        [SerializeField] private TextMeshProUGUI label;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Settings")]
        [SerializeField] private float displayDuration = 2f;
        [SerializeField] private float fadeDuration = 0.5f;

        #endregion

        #region Runtime State

        private Coroutine hideCoroutine;

        #endregion

        #region Initialization

        /// <summary>Initializes component references and state.</summary>
        private void Awake()
        {
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
            if (label == null)
                label = GetComponentInChildren<TextMeshProUGUI>();

            Hide();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Shows the ability bar with the specified text. Auto-hides after displayDuration.
        /// </summary>
        public void Show(string text)
        {
            if (string.IsNullOrEmpty(text))
                return;

            CancelHide();
            label.text = text;
            canvasGroup.alpha = 1f;
            gameObject.SetActive(true);
            hideCoroutine = StartCoroutine(AutoHideRoutine());
        }

        /// <summary>
        /// Shows the ability bar for an actor using an ability.
        /// Format: "{CharacterClass} uses {AbilityName}"
        /// </summary>
        public void Show(ActorInstance user, Ability ability)
        {
            if (ability == null)
                return;

            string text = user != null
                ? $"{user.characterClass} uses {ability.name}"
                : ability.name;

            Show(text);
        }

        /// <summary>
        /// Shows the ability bar for an actor using a consumable item.
        /// Format: "{CharacterClass} uses {ItemName}"
        /// </summary>
        public void Show(ActorInstance user, ItemDefinition item)
        {
            if (item == null)
                return;

            string text = user != null
                ? $"{user.characterClass} uses {item.DisplayName}"
                : item.DisplayName;

            Show(text);
        }

        /// <summary>
        /// Shows the ability bar for an actor performing a named action.
        /// Format: "{CharacterClass} {verb} {actionName}"
        /// </summary>
        public void ShowAction(ActorInstance actor, string verb, string actionName)
        {
            if (string.IsNullOrEmpty(actionName))
                return;

            string text = actor != null
                ? $"{actor.characterClass} {verb} {actionName}"
                : actionName;

            Show(text);
        }

        /// <summary>
        /// Immediately hides the ability bar.
        /// </summary>
        public void Hide()
        {
            CancelHide();
            canvasGroup.alpha = 0f;
        }

        /// <summary>
        /// Fades out the ability bar over fadeDuration seconds.
        /// </summary>
        public void FadeOut()
        {
            CancelHide();
            hideCoroutine = StartCoroutine(FadeOutRoutine());
        }

        /// <summary>Returns whether the cancel hide condition is met.</summary>
        private void CancelHide()
        {
            if (hideCoroutine != null)
            {
                StopCoroutine(hideCoroutine);
                hideCoroutine = null;
            }
        }

        /// <summary>Coroutine that executes the auto hide sequence.</summary>
        private IEnumerator AutoHideRoutine()
        {
            yield return new WaitForSeconds(displayDuration);
            yield return FadeOutRoutine();
        }

        /// <summary>Coroutine that executes the fade out sequence.</summary>
        private IEnumerator FadeOutRoutine()
        {
            float startAlpha = canvasGroup.alpha;
            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / fadeDuration);
                yield return null;
            }

            canvasGroup.alpha = 0f;
            hideCoroutine = null;
        }

        #endregion
    }
}
