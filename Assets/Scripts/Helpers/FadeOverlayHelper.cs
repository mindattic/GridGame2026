// --- File: Assets/Scripts/Helpers/FadeOverlayHelper.cs ---
using UnityEngine;
using UnityEngine.SceneManagement;
using c = Scripts.Helpers.CanvasHelper;
using Scripts.Canvas;
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

namespace Scripts.Helpers
{
    /// <summary>
    /// FADEOVERLAYHELPER - Cached access to scene fade overlay.
    /// 
    /// PURPOSE:
    /// Provides fast, cached access to the FadeOverlayInstance component
    /// used for scene transition fade effects.
    /// 
    /// CACHING STRATEGY:
    /// - Caches overlay reference once per scene
    /// - Auto-refreshes on scene change via sceneLoaded event
    /// - Lazy initialization on first access
    /// 
    /// FADE TRANSITIONS:
    /// ```
    /// Scene A                    Scene B
    ///   ↓                          ↓
    /// [Visible] → FadeOut → [Black] → LoadScene → [Black] → FadeIn → [Visible]
    /// ```
    /// 
    /// USAGE:
    /// ```csharp
    /// // Access the overlay
    /// var overlay = FadeOverlayHelper.Overlay;
    /// overlay.FadeOut(() => LoadNextScene());
    /// overlay.FadeIn();
    /// ```
    /// 
    /// RELATED FILES:
    /// - FadeOverlayInstance.cs: Fade animation behavior
    /// - SceneHelper.cs: Uses for scene transitions
    /// - FadeOverlayFactory.cs: Creates overlay prefab
    /// </summary>
    public static class FadeOverlayHelper
    {
        #region Cached References

        private static FadeOverlayInstance overlay;
        private static RectTransform rect;

        #endregion

        #region Properties

        /// <summary>
        /// Fast access to the cached FadeOverlayInstance.
        /// Performs one-time lookup if cache is empty.
        /// </summary>
        public static FadeOverlayInstance Overlay
        {
            get
            {
                if (overlay == null) Cache();
                return overlay;
            }
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize on first scene after load and refresh cache on scene changes.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            overlay = null;
            SceneManager.sceneLoaded += OnSceneLoaded;
            Cache();
        }

        /// <summary>Scene change callback that refreshes the cached reference.</summary>
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

        #endregion
    }
}
