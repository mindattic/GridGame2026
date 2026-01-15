using System.Collections;
using TMPro;
using UnityEngine;

namespace Assets.Scripts.Canvas
{
    /// <summary>
    /// Controls the AbilityBar UI element that displays ability names when executed.
    /// Attach this to the Canvas/AbilityBar GameObject.
    /// </summary>
    [DisallowMultipleComponent]
    public class AbilityBar : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TextMeshProUGUI label;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Settings")]
        [Tooltip("How long the ability name stays visible before fading out.")]
        [SerializeField] private float displayDuration = 2f;
        [Tooltip("How long the fade out animation takes.")]
        [SerializeField] private float fadeDuration = 0.5f;

        private Coroutine hideCoroutine;

        private void Awake()
        {
            // Auto-find label if not assigned
            if (label == null)
                label = GetComponentInChildren<TextMeshProUGUI>();

            // Auto-find or add CanvasGroup for fading
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();

            // Start hidden
            Hide();
        }

        /// <summary>
        /// Shows the ability bar with the specified ability name.
        /// Auto-hides after displayDuration seconds.
        /// </summary>
        public void Show(string abilityName)
        {
            if (string.IsNullOrEmpty(abilityName))
                return;

            // Stop any pending hide
            if (hideCoroutine != null)
            {
                StopCoroutine(hideCoroutine);
                hideCoroutine = null;
            }

            // Set text and show
            if (label != null)
                label.text = abilityName;

            if (canvasGroup != null)
                canvasGroup.alpha = 1f;

            gameObject.SetActive(true);

            // Start auto-hide timer
            hideCoroutine = StartCoroutine(AutoHideRoutine());
        }

        /// <summary>
        /// Shows the ability bar with the ability name and the user's name.
        /// Format: "UserName uses AbilityName"
        /// </summary>
        public void Show(string userName, string abilityName)
        {
            if (string.IsNullOrEmpty(abilityName))
                return;

            string displayText = string.IsNullOrEmpty(userName) 
                ? abilityName 
                : $"{userName} uses {abilityName}";

            Show(displayText);
        }

        /// <summary>
        /// Shows the ability bar for an Ability object.
        /// </summary>
        public void Show(Ability ability)
        {
            if (ability == null)
                return;

            Show(ability.name);
        }

        /// <summary>
        /// Shows the ability bar for an Ability used by an actor.
        /// </summary>
        public void Show(ActorInstance user, Ability ability)
        {
            if (ability == null)
                return;

            string userName = user != null ? user.characterClass.ToString() : null;
            Show(userName, ability.name);
        }

        /// <summary>
        /// Immediately hides the ability bar.
        /// </summary>
        public void Hide()
        {
            if (hideCoroutine != null)
            {
                StopCoroutine(hideCoroutine);
                hideCoroutine = null;
            }

            if (canvasGroup != null)
                canvasGroup.alpha = 0f;
        }

        /// <summary>
        /// Fades out the ability bar over fadeDuration seconds.
        /// </summary>
        public void FadeOut()
        {
            if (hideCoroutine != null)
            {
                StopCoroutine(hideCoroutine);
                hideCoroutine = null;
            }

            hideCoroutine = StartCoroutine(FadeOutRoutine());
        }

        private IEnumerator AutoHideRoutine()
        {
            // Wait for display duration
            yield return new WaitForSeconds(displayDuration);

            // Then fade out
            yield return FadeOutRoutine();
        }

        private IEnumerator FadeOutRoutine()
        {
            if (canvasGroup == null)
            {
                Hide();
                yield break;
            }

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
    }
}
