namespace Scripts.Data.Config
{
    /// <summary>
    /// SYNERGYSPARKCONFIG - Static tuning values for spark particles traveling along synergy lines.
    /// <para>PURPOSE: SynergySpark is a plain C# helper (not a MonoBehaviour), but its 16 tuning
    /// values were decorated with [SerializeField] — noise that Unity ignores, and that the
    /// SerializedField ban scanner correctly flags. Moved to compile-time config for clarity.</para>
    /// <para>USAGE: Referenced directly from SynergySpark.Init/Spawn.</para>
    /// <para>RELATED FILES: SynergySpark.cs, SynergyStrand.cs, SynergyLineStrand.cs</para>
    /// </summary>
    public static class SynergySparkConfig
    {
        // ── Spawn window on the path ─────────────────────────────────────────
        public const float MinT = 0.01f;
        public const float MaxT = 0.08f;

        // ── Motion ───────────────────────────────────────────────────────────
        public const float MinBaseSpeed      = 0.2f;
        public const float MaxBaseSpeed      = 0.6f;
        public const float RevActiveSpeedMul = 1.2f;

        // ── Size and lifetime ────────────────────────────────────────────────
        public const float MinSize     = 0.10f;
        public const float MaxSize     = 0.16f;
        public const float MinLifetime = 0.40f;
        public const float MaxLifetime = 2.0f;

        // ── Offset jitter along the local perpendicular ──────────────────────
        public const float MinOffsetJitter = -1f;
        public const float MaxOffsetJitter = 1f;

        // ── Rate and speed randomization ─────────────────────────────────────
        public const float SpawnRateMin = 10f;
        public const float SpawnRateMax = 16f;
        public const float SpeedMulMin  = 0.85f;
        public const float SpeedMulMax  = 1.35f;

        // ── Sprite library key ───────────────────────────────────────────────
        public const string TextureKey = "SynergySpark";
    }
}
