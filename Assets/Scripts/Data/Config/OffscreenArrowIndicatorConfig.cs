namespace Scripts.Data.Config
{
    /// <summary>
    /// OFFSCREENARROWINDICATORCONFIG - Static tuning values for OffscreenArrowIndicator.
    /// <para>PURPOSE: Replaces the former [SerializeField] tuning fields on
    /// OffscreenArrowIndicator with compile-time constants. The <c>target</c>
    /// and <c>worldCamera</c> references are now runtime state set via public
    /// properties; <c>margin</c> remains an instance field (has a public setter)
    /// seeded from DefaultMargin.</para>
    /// <para>USAGE: Referenced from OffscreenArrowIndicator.Awake / ApplyAlpha.</para>
    /// <para>RELATED FILES: OffscreenArrowIndicator.cs, OverworldManager.cs</para>
    /// </summary>
    public static class OffscreenArrowIndicatorConfig
    {
        // Pixels kept between the screen edge and the arrow icon.
        public const float DefaultMargin = 40f;

        // How quickly canvas-group alpha moves toward its target value per second.
        public const float FadeSpeed = 10f;
    }
}
