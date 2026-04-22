namespace Scripts.Data.Config
{
    /// <summary>
    /// TARGETLINEINSTANCECONFIG - Static tuning values for TargetLineInstance.
    /// <para>PURPOSE: Compile-time constants for the FFXII-style targeting-arc visuals —
    /// curve resolution, glow halo, tapered ends, and the traveling direction bead.</para>
    /// <para>USAGE: Referenced from TargetLineInstance + TargetLineFactory.</para>
    /// <para>RELATED FILES: TargetLineInstance.cs, TargetLineFactory.cs</para>
    /// </summary>
    public static class TargetLineInstanceConfig
    {
        // Seconds to fade alpha in/out.
        public const float FadeDuration = 0.1f;

        // Line segment count (curve resolution). Valid range 2..100.
        public const int Segments = 32;

        // Arc-height fraction of chord length. The Bezier control point sits this far from
        // the chord's midpoint along the screen-perpendicular. Bigger = more pronounced bow.
        public const float ArcHeightFraction = 0.55f;

        // ---- FFXII-style visual tuning ----

        // Core stroke width (world units) at the thickest point. Tapered ends go to 0.
        public const float CoreWidth = 0.04f;

        // Glow halo width multiplier relative to core width.
        public const float GlowWidthMultiplier = 2f;

        // Glow halo peak alpha (core = 1.0).
        public const float GlowAlpha = 0.35f;

        // Width curve shape for tapered ends: 0 at both ends, peak in the middle.
        // Implemented via AnimationCurve with (0,0)(0.5,1)(1,0) quadratic-ish shape.

        // ---- Direction bead (source → destination) ----

        // Bead travel speed along the normalized arc (loops per second).
        public const float BeadLoopsPerSecond = 0.8f;

        // Bead diameter in world units (roughly one-third of a tile).
        public const float BeadSize = 0.35f;

        // Bead pulse amplitude (0..1). Scale wobbles between (1-amp) and (1+amp).
        public const float BeadPulseAmplitude = 0.25f;

        // Bead pulse frequency in Hz.
        public const float BeadPulseHz = 4.0f;
    }
}
