// --- File: Assets/Scripts/Helpers/CanvasHelper.cs ---
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Assets.Helpers
{
    /// <summary>
    /// CANVASHELPER - Cached access to the scene's UI Canvas.
    /// 
    /// PURPOSE:
    /// Provides fast, cached access to the current scene's Canvas and
    /// related components without repeated GameObject.Find calls.
    /// 
    /// CACHING STRATEGY:
    /// - Caches Canvas reference once per scene
    /// - Automatically refreshes on scene change
    /// - Lazy initialization on first access
    /// 
    /// PROPERTIES:
    /// - Canvas: The scene's main Canvas component
    /// - CanvasRect: The Canvas RectTransform for sizing calculations
    /// - CanvasScaler: The CanvasScaler for resolution handling
    /// 
    /// USAGE:
    /// ```csharp
    /// // Instead of: GameObject.Find("Canvas").GetComponent<Canvas>()
    /// var canvas = CanvasHelper.Canvas;
    /// var screenWidth = CanvasHelper.CanvasRect.rect.width;
    /// ```
    /// 
    /// AUTO-REFRESH:
    /// Subscribes to SceneManager.sceneLoaded to automatically
    /// refresh cache when scenes change.
    /// 
    /// RELATED FILES:
    /// - ProfileSelectManager.cs: Uses for UI sizing
    /// - StageSelectManager.cs: Uses for button layout
    /// - All UI managers: Use for canvas access
    /// </summary>
    public static class CanvasHelper
    {
        #region Cached References

        private static Canvas canvas;
        private static RectTransform canvasRect;
        private static CanvasScaler canvasScalar;

        #endregion

        #region Properties

        /// <summary>
        /// Fast access to the cached Canvas.
        /// Performs one-time lookup if cache is empty.
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
        /// Fast access to the Canvas RectTransform for sizing calculations.
        /// </summary>
        public static RectTransform CanvasRect
        {
            get
            {
                if (canvasRect == null) Cache();
                return canvasRect;
            }
        }

        /// <summary>
        /// Fast access to the CanvasScaler for resolution handling.
        /// </summary>
        public static CanvasScaler CanvasScaler
        {
            get
            {
                if (canvasRect == null) Cache();
                return canvasScalar;
            }
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes the helper after first scene load and sets up
        /// listener for scene changes to auto-refresh cache.
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

        #endregion
    }
}
