using UnityEngine;

namespace Scripts.Data.Config
{
    /// <summary>
    /// TARGETMODEOVERLAYCONFIG - Static tuning values for TargetModeOverlay fading and rendering.
    /// <para>PURPOSE: Replaces the former [SerializeField] tuning fields on
    /// TargetModeOverlay with compile-time constants. The <c>Color</c> entry uses
    /// <c>static readonly</c> because Color is not a const-legal type.</para>
    /// <para>USAGE: Referenced from TargetModeOverlay.Awake / HandleModeChanged /
    /// ApplyInstant / ApplySorting / SetAlpha.</para>
    /// <para>RELATED FILES: TargetModeOverlay.cs, AbilityManager.cs, InputManager.cs</para>
    /// </summary>
    public static class TargetModeOverlayConfig
    {
        // ── Fade parameters ──────────────────────────────────────────────────
        public const float MinAlpha = 0f;          // Fully transparent
        public const float MaxAlpha = 0.3333f;     // Visible overlay alpha
        public const float Duration = 0.1f;        // Fade time (unscaled)

        // ── Color (RGB; alpha driven by fade) ────────────────────────────────
        public static readonly Color OverlayColor = new Color(0f, 0f, 0f, 1f);

        // ── Rendering ────────────────────────────────────────────────────────
        public const string SortingLayerName = "Default";
        public const int    OrderInLayer     = 50;
    }
}
