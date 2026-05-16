namespace Scripts.Data.Config
{
    /// <summary>
    /// ACTIONTITLECONFIG - Static tuning values for the top-center ActionTitle banner.
    /// <para>PURPOSE: Replaces the former [SerializeField] tuning fields on ActionTitle with
    /// compile-time constants. UI references (label, canvasGroup) are resolved at runtime via
    /// GetComponent / GetComponentInChildren in ActionTitle.Awake rather than Inspector
    /// drag-drop.</para>
    /// <para>USAGE: Referenced from ActionTitle.AutoHideRoutine / FadeOutRoutine.</para>
    /// <para>RELATED FILES: ActionTitle.cs, AbilityManager.cs, EnemyAttackSequence.cs,
    /// UseItemSequence.cs, ChangeEquippedWeaponSequence.cs (planned)</para>
    /// </summary>
    public static class ActionTitleConfig
    {
        // Seconds the banner remains fully visible before beginning to fade out.
        public const float DisplayDuration = 2f;

        // Seconds to animate alpha from 1→0 during fade-out.
        public const float FadeDuration = 0.5f;
    }
}
