namespace Scripts.Data.Config
{
    /// <summary>
    /// MAPICONCONFIG - Static tuning values for MapIcon hover/bob animation.
    /// <para>PURPOSE: Replaces the former [SerializeField] tuning fields on
    /// MapIcon with compile-time constants. The <c>mode</c> field remains a
    /// runtime instance field because SetHoverEnabled() mutates it.</para>
    /// <para>USAGE: MapIcon instance initializes its mode from DefaultMode and
    /// reads the rest directly from this config each Update tick.</para>
    /// <para>RELATED FILES: MapIcon.cs, WorldMapInstance.cs, StageButtonInstance.cs</para>
    /// </summary>
    public static class MapIconConfig
    {
        // Default hover mode — MapIcon seeds its instance field with this value
        // so SetHoverEnabled() can still flip it at runtime. Encoded as bool to
        // avoid a cross-namespace dependency on Scripts.Canvas.MapIcon.HoverMode.
        public const bool DefaultHoverEnabled = true;

        // Vertical movement range (pixels for UI, local units for world-space).
        public const float Amplitude = 8f;

        // Oscillation frequency in cycles per second.
        public const float Speed = 1.2f;

        // Starting phase offset in radians.
        public const float PhaseOffset = 0f;

        // If true, ignore Time.timeScale.
        public const bool UseUnscaledTime = true;
    }
}
