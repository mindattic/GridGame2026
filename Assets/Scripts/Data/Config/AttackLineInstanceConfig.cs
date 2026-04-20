namespace Scripts.Data.Config
{
    /// <summary>
    /// ATTACKLINEINSTANCECONFIG - Static tuning values for AttackLineInstance.
    /// <para>PURPOSE: Replaces the former [SerializeField] fadeDuration on
    /// AttackLineInstance with a compile-time constant.</para>
    /// <para>USAGE: Referenced from AttackLineInstance.FadeInRoutine / DespawnRoutine.</para>
    /// <para>RELATED FILES: AttackLineInstance.cs, AttackLineFactory.cs</para>
    /// </summary>
    public static class AttackLineInstanceConfig
    {
        // Seconds to fade alpha on spawn or despawn.
        public const float FadeDuration = 0.5f;
    }
}
