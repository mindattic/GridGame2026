// --- File: Assets/Scripts/Helpers/CanvasHelper.cs ---
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Assets.Helpers
{
    /// <summary>
    /// Caches the current scene's Canvas and Canvas RectTransform so callers
    /// always get the correct references without repeated GameObject.Find calls.
    /// Looks up the "Canvas" GameObject once per scene load and stores references
    /// for fast access across the scene's lifetime.
    /// </summary>
    public static class CanvasHelper
    {
        // Cached references
        private static Canvas canvas;
        private static RectTransform canvasRect;
        private static CanvasScaler canvasScalar;

        /// <summary>
        /// Fast access to the cached Canvas.
        /// If the cache is empty, performs a one-time lookup for the current scene.
        /// </summary>
        public static Canvas Canvas
        {
            get
            {
                if (canvas == null) Cache();
                return canvas;
            }
        }

        /// <summary>
        /// Fast access to the cached Canvas RectTransform.
        /// If the cache is empty, performs a one-time lookup for the current scene.
        /// </summary>
        public static RectTransform CanvasRect
        {
            get
            {
                if (canvasRect == null) Cache();
                return canvasRect;
            }
        }

        public static CanvasScaler CanvasScaler
        {
            get
            {
                if (canvasRect == null) Cache();
                return canvasScalar;
            }
        }

        /// <summary>
        /// Initializes the helper after the first scene load and sets up a listener
        /// for future scene changes so the cache is refreshed automatically.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            // Clear any old references before caching
            canvas = null;
            canvasRect = null;

            // Subscribe to scene change events
            SceneManager.sceneLoaded += OnSceneLoaded;

            // Cache immediately for the first active scene
            Cache();
        }

        /// <summary>
        /// Event handler for scene load — refreshes the cached references
        /// whenever a new scene becomes active.
        /// </summary>
        private static void OnSceneLoaded(Scene _, LoadSceneMode __)
        {
            Cache();
        }

        /// <summary>
        /// Performs the actual lookup for the Canvas and Canvas RectTransform
        /// in the current scene and stores them for fast future access.
        /// Safe if the Canvas object is missing.
        /// </summary>
        private static void Cache()
        {
            var go = GameObject.Find("Canvas");
            if (go == null)
            {
                canvas = null;
                canvasRect = null;
                return;
            }

            canvas = go.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvasRect = null;
                return;
            }

            canvasRect = canvas.GetComponent<RectTransform>();


            canvasScalar = canvas.GetComponent<CanvasScaler>();


        }
    }
}
