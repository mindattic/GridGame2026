using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using scene = Assets.Helpers.SceneHelper;

namespace Assets.Scripts
{
    /// <summary>
    /// Centralized scene transition service with a loading screen.
    /// Attach this component only in the LoadingScreen scene.
    /// Provides overlay from black, delayed progress UI, and progress UI overlay in.
    /// Tracks current and previous scene names and supports editor bootstrap when started on LoadingScreen.
    /// </summary>
    public sealed class SceneLoader : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private Slider progressBar;
        [SerializeField] private TextMeshProUGUI progressLabel;

        [Header("Overlay Groups")]
        [Tooltip("Fullscreen black overlay CanvasGroup that fades from 1 to 0.")]
        [SerializeField] private CanvasGroup fadePanel;
        [Tooltip("Container CanvasGroup for progress UI that fades from 0 to 1.")]
        [SerializeField] private CanvasGroup progressPanel;

        [Header("Timings")]
        [SerializeField] private float fadeInDuration = 0.4f;
        [SerializeField] private float uiShowDelay = 1.0f;
        [SerializeField] private float minimumVisibleTime = 0.5f;

        [Header("Behavior")]
        [SerializeField] private bool activateWhenReady = true;

        [Header("Editor Bootstrap")]
        [Tooltip("If true and the game starts on the LoadingScreen, automatically load Bootstrap Scene.")]
        [SerializeField] private bool autoLoadWhenLaunchedDirectly = true;
        [Tooltip("Scene to load automatically when LoadingScreen is launched directly.")]
        [SerializeField] private string bootstrapScene = "TitleScreen";

        private static string targetSceneName;
        private static LoadSceneMode targetLoadMode = LoadSceneMode.Single;
        private static Action onLoadedCallback;

        private static string previousScene = "TitleScreen";
        private static string currentScene = "TitleScreen";

        private bool isProgressVisible;
        private bool isFadeCompleted;

        // Tracks progress even while the UI is hidden so it can be applied instantly when shown
        private float latestProgress;

        /// <summary>
        /// Load a target scene by first switching to the LoadingScreen, then loading asynchronously.
        /// </summary>
        public static void Load(string sceneName, LoadSceneMode mode = LoadSceneMode.Single, Action onLoaded = null)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogError("SceneLoader.Load was given an empty scene name.");
                return;
            }

            previousScene = currentScene;
            currentScene = sceneName;

            targetSceneName = sceneName;
            targetLoadMode = mode;
            onLoadedCallback = onLoaded;

            // Always route through LoadingScreen
            SceneManager.LoadScene(scene.LoadingScreen, LoadSceneMode.Single);
        }

        /// <summary>
        /// Load the previously tracked scene through the LoadingScreen.
        /// </summary>
        public static void LoadPreviousScene(string defaultScene = "Game", LoadSceneMode mode = LoadSceneMode.Single, Action onLoaded = null)
        {
            if (string.IsNullOrWhiteSpace(previousScene))
                previousScene = defaultScene;

            string target = previousScene;

            previousScene = currentScene;
            currentScene = target;

            targetSceneName = target;
            targetLoadMode = mode;
            onLoadedCallback = onLoaded;

            SceneManager.LoadScene(scene.LoadingScreen, LoadSceneMode.Single);
        }

        /// <summary>
        /// Name of the currently tracked scene.
        /// </summary>
        public static string GetCurrentScene() => currentScene;

        /// <summary>
        /// Name of the previously tracked scene.
        /// </summary>
        public static string GetPreviousScene() => previousScene;

        /// <summary>
        /// Prepare fades, handle bootstrap if launched directly, then begin async loading if a target exists.
        /// </summary>
        private void Start()
        {
            // Prepare overlay groups
            if (fadePanel != null)
            {
                fadePanel.alpha = 1f;
                fadePanel.blocksRaycasts = true;
                fadePanel.interactable = false;
            }

            if (progressPanel != null)
            {
                progressPanel.alpha = 0f;
                progressPanel.blocksRaycasts = false;
                progressPanel.interactable = false;
            }

            // Ensure progress widgets start hidden if no progressPanel is assigned
            SetProgressVisible(false);
            latestProgress = 0f;

            // If we pressed Play on LoadingScreen directly and no target is set, bootstrap once without reloading LoadingScreen
            if (string.IsNullOrEmpty(targetSceneName))
            {
                if (autoLoadWhenLaunchedDirectly && !string.IsNullOrWhiteSpace(bootstrapScene))
                {
                    // Set up a normal load for the bootstrap scene and continue as if it had been requested
                    previousScene = scene.LoadingScreen;
                    currentScene = bootstrapScene;

                    targetSceneName = bootstrapScene;
                    targetLoadMode = LoadSceneMode.Single;
                    onLoadedCallback = null;

                    // Fall through into the normal loading flow below
                }
                else
                {
                    // Nothing to load, just clear the overlay so the scene is interactable in-editor
                    if (fadePanel != null)
                    {
                        fadePanel.alpha = 0f;
                        fadePanel.blocksRaycasts = false;
                    }
                    return;
                }
            }

            // Normal loading behavior
            StartCoroutine(FadeFromBlackRoutine(fadeInDuration));
            StartCoroutine(ShowUIAfterDelay(uiShowDelay, fadeInDuration));
            StartCoroutine(LoadRoutine());
        }

        /// <summary>
        /// Overlay the black overlay from 1 to 0 in unscaled time.
        /// </summary>
        private IEnumerator FadeFromBlackRoutine(float duration)
        {
            if (fadePanel == null || duration <= 0f)
            {
                isFadeCompleted = true;
                yield break;
            }

            float t = 0f;
            float start = fadePanel.alpha;

            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float a = Mathf.Lerp(start, 0f, Mathf.Clamp01(t / duration));
                fadePanel.alpha = a;
                yield return null;
            }

            fadePanel.alpha = 0f;
            fadePanel.blocksRaycasts = false;
            isFadeCompleted = true;
        }

        /// <summary>
        /// After a delay, overlay in the progress UI and immediately apply the latest known progress.
        /// </summary>
        private IEnumerator ShowUIAfterDelay(float delay, float duration)
        {
            if (delay > 0f)
                yield return new WaitForSecondsRealtime(delay);

            while (!isFadeCompleted)
                yield return null;

            if (progressPanel != null && duration > 0f)
            {
                SetUIContainerActive(true);

                float t = 0f;
                progressPanel.alpha = 0f;

                // Apply the latest progress as soon as UI becomes visible
                UpdateUI(latestProgress);

                while (t < duration)
                {
                    t += Time.unscaledDeltaTime;
                    progressPanel.alpha = Mathf.Clamp01(t / duration);
                    yield return null;
                }

                progressPanel.alpha = 1f;
                progressPanel.blocksRaycasts = true;
            }
            else
            {
                SetProgressVisible(true);
                UpdateUI(latestProgress);
            }

            isProgressVisible = true;
        }

        /// <summary>
        /// Load target scene asynchronously, track progress every frame, update UI when visible,
        /// and allow activation after the minimum visible time.
        /// </summary>
        private IEnumerator LoadRoutine()
        {
            float startTime = Time.realtimeSinceStartup;

            AsyncOperation op = SceneManager.LoadSceneAsync(targetSceneName, targetLoadMode);
            op.allowSceneActivation = false;

            // Update until the 0.9 ready plateau
            while (op.progress < 0.9f)
            {
                latestProgress = Mathf.Clamp01(op.progress / 0.9f);

                if (isProgressVisible)
                    UpdateUI(latestProgress);

                yield return null;
            }

            // Reached activation plateau
            latestProgress = 1f;
            if (isProgressVisible)
                UpdateUI(latestProgress);

            // Guarantee a minimum on-screen time
            float elapsed = Time.realtimeSinceStartup - startTime;
            if (elapsed < minimumVisibleTime)
                yield return new WaitForSecondsRealtime(minimumVisibleTime - elapsed);

            // Activate
            if (activateWhenReady)
                op.allowSceneActivation = true;

            while (!op.isDone)
                yield return null;

            Action done = onLoadedCallback;

            onLoadedCallback = null;
            targetSceneName = null;
            targetLoadMode = LoadSceneMode.Single;

            done?.Invoke();
        }

        /// <summary>
        /// Toggle child widgets active when no CanvasGroup is used for them.
        /// </summary>
        private void SetProgressVisible(bool visible)
        {
            if (progressBar != null)
                progressBar.gameObject.SetActive(visible);

            if (progressLabel != null)
                progressLabel.gameObject.SetActive(visible);
        }

        /// <summary>
        /// Ensure the progress UI container is active before animating its CanvasGroup.
        /// </summary>
        private void SetUIContainerActive(bool active)
        {
            if (progressPanel != null)
            {
                if (progressPanel.gameObject.activeSelf != active)
                    progressPanel.gameObject.SetActive(active);

                SetProgressVisible(true);
            }
            else
            {
                SetProgressVisible(active);
            }
        }

        /// <summary>
        /// Apply normalized progress to the bar and label.
        /// </summary>
        private void UpdateUI(float t)
        {
            float clamped = Mathf.Clamp01(t);

            if (progressBar != null)
                progressBar.value = clamped;

            if (progressLabel != null)
                progressLabel.text = Mathf.RoundToInt(clamped * 100f) + "%";
        }
    }
}
