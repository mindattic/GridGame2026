namespace Scripts.Data.Config
{
    /// <summary>
    /// ABILITYBARCONFIG - Static tuning values for AbilityBar.
    /// <para>PURPOSE: Replaces the former [SerializeField] tuning fields on
    /// AbilityBar with compile-time constants. The two UI references
    /// (label, canvasGroup) are now resolved at runtime via GetComponent /
    /// GetComponentInChildren in AbilityBar.Awake rather than Inspector drag-drop.</para>
    /// <para>USAGE: Referenced from AbilityBar.AutoHideRoutine / FadeOutRoutine.</para>
    /// <para>RELATED FILES: AbilityBar.cs, AbilityManager.cs</para>
    /// </summary>
    public static class AbilityBarConfig
    {
        // Seconds the bar remains fully visible before beginning to fade out.
        public const float DisplayDuration = 2f;

        // Seconds to animate alpha from 1→0 during fade-out.
        public const float FadeDuration = 0.5f;
    }
}
