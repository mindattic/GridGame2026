namespace Scripts.Data.Config
{
    /// <summary>
    /// TIMELINEBARCONFIG - Static tuning values for TimelineBarInstance.
    /// <para>PURPOSE: Replaces the former [SerializeField] tuning fields on
    /// TimelineBarInstance with compile-time constants. Two dead [SerializeField]
    /// fields were removed during migration: <c>tagPrefab</c> (replaced by
    /// TimelineTagFactory) and <c>maxReleaseDelay</c> (never referenced in code).
    /// Scene object references (barRect, tagsRoot, triggerPointRect, spawnPointRect)
    /// are now plain runtime fields — Awake() creates or caches them.</para>
    /// <para>USAGE: Referenced from TimelineBarInstance.RebuildLayout /
    /// UnitsPerSecFromSpeed / SpawnTag / PushbackOnAttack / EnforceQueueSpacing.</para>
    /// <para>RELATED FILES: TimelineBarInstance.cs, TimelineTag.cs,
    /// TimelineTagFactory.cs, TimelineTriggerSequence.cs</para>
    /// </summary>
    public static class TimelineBarConfig
    {
        // ── Layout ───────────────────────────────────────────────────────────
        // Percent of canvas width used for the timeline length.
        public const float CanvasPercent = 0.96f;

        // ── Tuning ───────────────────────────────────────────────────────────
        // Base time in seconds for an enemy with Speed 10 to cross the full bar.
        public const float CrossingTimeSeconds = 8f;

        // Vertical spacing between duplicate tags.
        public const float TagRowHeight = 14f;

        // Developer debug log toggle.
        public const bool DebugLogs = false;

        // ── Pushback on Attack ───────────────────────────────────────────────
        // Minimum pushback when enemy is at the far right.
        public const float PushbackBase = 0.05f;

        // Maximum pushback when enemy is at the trigger point.
        public const float PushbackMax = 0.4f;

        // Base stun duration in seconds after pushback.
        public const float BaseStunDuration = 1f;

        // ── Queue Coordination ───────────────────────────────────────────────
        // Minimum time gap between enemy releases.
        public const float MinimumReleaseGap = 1.5f;
    }
}
