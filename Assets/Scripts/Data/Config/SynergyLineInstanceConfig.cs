namespace Scripts.Data.Config
{
    /// <summary>
    /// SYNERGYLINEINSTANCECONFIG - Static tuning values for SynergyLineInstance.
    /// <para>PURPOSE: Replaces the former [SerializeField] fields with compile-time
    /// constants. SynergyLineInstance is spawned programmatically per pair of
    /// linked actors; these tuning values used to live on the GameObject's
    /// Inspector but are now code-authored.</para>
    /// <para>USAGE: Referenced from SynergyLineInstance.Configure / LoopRoutine /
    /// ApplySettingsToStrands.</para>
    /// <para>RELATED FILES: SynergyLineInstance.cs, SynergyLineStrandConfig.cs</para>
    /// </summary>
    public static class SynergyLineInstanceConfig
    {
        // Number of parallel strands rendered per link
        public const int WaveformCount = 4;

        // Base geometry for each strand (scaled per-strand by weight)
        public const float BaseRadius = 0.07f;
        public const float BaseWidth  = 0.012f;

        // Fade timings
        public const float FadeInTime  = 0.20f;
        public const float FadeOutTime = 0.20f;

        // Sorting offsets within the synergy layer
        public const int OrderOffsetPerWave = 1;
        public const int ExtraFrontBias     = -2;

        // Per-strand curve resolution
        public const int StrandSegmentCount = 32;
    }
}
