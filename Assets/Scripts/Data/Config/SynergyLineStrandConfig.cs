using UnityEngine;

namespace Scripts.Data.Config
{
    /// <summary>
    /// SYNERGYLINESTRANDCONFIG - Static tuning values for SynergyLineStrand rendering.
    /// <para>PURPOSE: Replaces the former [SerializeField] fields on SynergyLineStrand
    /// with compile-time constants. Structurally mirrors SynergyStrandConfig; kept
    /// separate because the two MonoBehaviours exist as independent tuning surfaces
    /// (see SynergyStrand.cs doc: "Similar to SynergyLineStrand but may have
    /// different parameters or be used in different contexts").</para>
    /// <para>USAGE: Referenced directly from SynergyLineStrand.Awake/Configure/Tick.</para>
    /// <para>RELATED FILES: SynergyLineStrand.cs, SynergyStrandConfig.cs</para>
    /// </summary>
    public static class SynergyLineStrandConfig
    {
        // ── Core ─────────────────────────────────────────────────────────────
        public const float CoreAlpha = 0.55f;

        // ── Halo randomization ranges (Configure picks one value from each) ─
        public const bool HaloRandomize = true;
        public static readonly Vector2 HaloWidthScaleRange     = new Vector2(2.2f, 3.1f);
        public static readonly Vector2 HaloAlphaRange          = new Vector2(0.14f, 0.26f);
        public static readonly Vector2 HaloPulseAmpRange       = new Vector2(0.22f, 0.36f);
        public static readonly Vector2 HaloPulseSpeedMultRange = new Vector2(0.75f, 1.25f);
        public static readonly Vector2 HaloHDRBoostRange       = new Vector2(1.10f, 1.60f);
        public static readonly Vector2 HaloPhaseOffsetRange    = new Vector2(0.0f, 6.283185f);

        // ── Rev burst ────────────────────────────────────────────────────────
        public const float RevChancePerSecond = 0.12f;
        public const float RevPeakMultiplier  = 2.2f;
        public const float RevAccelTime       = 0.20f;
        public const float RevDecelTime       = 0.60f;
        public const float RevCooldownMin     = 0.60f;
        public const float RevCooldownMax     = 1.60f;

        // ── Prewarm ──────────────────────────────────────────────────────────
        public const float PrewarmSeconds = 0.25f;
        public const int   PrewarmSteps   = 16;

        // ── Geometry ─────────────────────────────────────────────────────────
        public const float Frequency      = 2.2f;
        public const float NoiseAmplitude = 0.015f;
        public const float NoiseScale     = 2.5f;
        public const float NoiseSpeed     = 0.18f;
        public static readonly AnimationCurve RadiusOverT = AnimationCurve.Linear(0f, 1f, 1f, 1f);

        // ── Tropical tint ────────────────────────────────────────────────────
        public const float HueSpeed      = 0.06f;
        public const float HuePhase      = 0.12f;
        public const float HueRange      = 0.06f;
        public const float SatBase       = 0.90f;
        public const float SatRange      = 0.08f;
        public const float ValBase       = 1.00f;
        public const float ValPulseAmp   = 0.08f;
        public const float ValPulseSpeed = 0.70f;

        // ── Halo base (fallback when HaloRandomize is false) ─────────────────
        public const bool  UseHalo        = true;
        public const float GlowWidthScale = 2.6f;
        public const float GlowAlpha      = 0.20f;
        public const float GlowPulseAmp   = 0.28f;
        public const float GlowPulseSpeed = 0.90f;
        public const float GlowHDRBoost   = 1.35f;
    }
}
