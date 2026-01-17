using System.Collections;
using TMPro;
using UnityEngine;

namespace Assets.Scripts.Canvas
{
    /// <summary>
    /// Displays ability names when executed, then fades out automatically.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasGroup))]
    public class AbilityBar : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TextMeshProUGUI label;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Settings")]
        [SerializeField] private float displayDuration = 2f;
        [SerializeField] private float fadeDuration = 0.5f;

        private Coroutine hideCoroutine;

        private void Awake()
        {
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
            if (label == null)
                label = GetComponentInChildren<TextMeshProUGUI>();
            
            Hide();
        }

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

        private void CancelHide()
        {
            if (hideCoroutine != null)
            {
                StopCoroutine(hideCoroutine);
                hideCoroutine = null;
            }
        }

        private IEnumerator AutoHideRoutine()
        {
            yield return new WaitForSeconds(displayDuration);
            yield return FadeOutRoutine();
        }

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
    }
}
