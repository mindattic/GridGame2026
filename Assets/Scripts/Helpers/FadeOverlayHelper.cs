// --- File: Assets/Scripts/Helpers/FadeHelper.cs ---
using UnityEngine;
using UnityEngine.SceneManagement;
using c = Assets.Helpers.CanvasHelper;

namespace Assets.Helpers
{
    /// <summary>
    /// Caches the current scene's FadeOverlayInstance so callers always get the correct reference.
    /// Looks up the FadeRoutine GameObject by name using GameObjectHelper.Overworld.FadeRoutine once per scene load.
    /// </summary>
    public static class FadeOverlayHelper
    {
        // Cached references
        private static FadeOverlayInstance overlay;
        private static RectTransform rect;

        /// <summary>
        /// Fast access to the cached FadeOverlayInstance.
        /// If the cache is empty, performs a one time lookup for the current scene.
        /// </summary>
        public static FadeOverlayInstance Overlay
        {
            get
            {
                if (overlay == null) Cache();
                return overlay;
            }
        }

        /// <summary>
        /// Initialize on first scene after load and refresh cache when scenes change.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            overlay = null;
            SceneManager.sceneLoaded += OnSceneLoaded;
            Cache();
        }

        /// <summary>
        /// Scene change callback that refreshes the cached reference.
        /// </summary>
        private static void OnSceneLoaded(Scene _, LoadSceneMode __)
        {
            Cache();
        }

        /// <summary>
        /// Performs the actual lookup and caches the FadeOverlayInstance.
        /// Safe if the GameObject is missing.
        /// </summary>
        private static void Cache()
        {
            if (c.Canvas == null)
            {
                overlay = null;
                return;
            }

            var go = c.Canvas.transform.Find("FadeOverlay");
            if (go == null)
            {
                overlay = null;
                return;
            }

            overlay = go.GetComponent<FadeOverlayInstance>();
            rect = go.GetComponent<RectTransform>();
        }

    }
}
