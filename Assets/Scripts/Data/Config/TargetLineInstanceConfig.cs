namespace Scripts.Data.Config
{
    /// <summary>
    /// TARGETLINEINSTANCECONFIG - Static tuning values for both TargetLine3DInstance and TargetLine2DInstance.
    /// <para>PURPOSE: Compile-time constants for the FFXII-style targeting-arc visuals —
    /// curve resolution, glow halo, tapered ends, and the traveling direction bead. 3D values
    /// are in world units; 2D values are in canvas pixels.</para>
    /// <para>USAGE: Referenced from TargetLine3DInstance/Factory + TargetLine2DInstance/Factory.</para>
    /// <para>RELATED FILES: TargetLine3DInstance.cs, TargetLine3DFactory.cs, TargetLine2DInstance.cs, TargetLine2DFactory.cs</para>
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

        // ---- FFXII-style visual tuning (3D / world-space) ----

        // Core stroke width (world units) at the thickest point. Tapered ends go to 0.
        public const float CoreWidth = 0.04f;

        // Glow halo width multiplier relative to core width.
        public const float GlowWidthMultiplier = 2f;

        // Glow halo peak alpha (core = 1.0).
        public const float GlowAlpha = 0.35f;

        // Width curve shape for tapered ends: 0 at both ends, peak in the middle.
        // Implemented via AnimationCurve with (0,0)(0.5,1)(1,0) quadratic-ish shape.

        // ---- Direction bead (source → destination, 3D / world-space) ----

        // Bead travel speed along the normalized arc (loops per second).
        public const float BeadLoopsPerSecond = 0.8f;

        // Bead diameter in world units (roughly one-third of a tile).
        public const float BeadSize = 0.35f;

        // Bead pulse amplitude (0..1). Scale wobbles between (1-amp) and (1+amp).
        public const float BeadPulseAmplitude = 0.25f;

        // Bead pulse frequency in Hz.
        public const float BeadPulseHz = 4.0f;

        // ---- 2D / canvas-space overrides (pixels) ----

        // Core stroke width at the thickest point, in canvas pixels.
        public const float CoreWidth2D = 8f;

        // Glow halo peak width in canvas pixels. Kept as an explicit constant (not derived
        // from CoreWidth2D × GlowWidthMultiplier) so 2D tuning can drift independently of 3D.
        public const float GlowWidth2D = 20f;

        // Bead diameter in canvas pixels.
        public const float BeadSize2D = 40f;
    }
}
