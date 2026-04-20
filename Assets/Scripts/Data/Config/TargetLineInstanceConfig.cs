namespace Scripts.Data.Config
{
    /// <summary>
    /// TARGETLINEINSTANCECONFIG - Static tuning values for TargetLineInstance.
    /// <para>PURPOSE: Replaces the former [SerializeField] tuning fields on
    /// TargetLineInstance with compile-time constants.</para>
    /// <para>USAGE: Referenced from TargetLineInstance.Awake / UpdateArcPoints /
    /// FadeRoutine.</para>
    /// <para>RELATED FILES: TargetLineInstance.cs, TargetLineFactory.cs</para>
    /// </summary>
    public static class TargetLineInstanceConfig
    {
        // Seconds to fade alpha in/out.
        public const float FadeDuration = 0.1f;

        // Line segment count (curve resolution). Valid range 2..100.
        public const int Segments = 32;

        // Base arc height; UpdateArcPoints currently uses dynamic distance-based
        // arc height and treats this as a reference value.
        public const float ArcHeight = 1f;
    }
}
