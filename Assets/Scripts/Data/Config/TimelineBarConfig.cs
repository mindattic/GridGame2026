namespace Scripts.Data.Config
{
    /// <summary>
    /// TIMELINEBARCONFIG - Static tuning values for TimelineBarInstance.
    /// <para>PURPOSE: Replaces the former [SerializeField] tuning fields on
    /// TimelineBarInstance with compile-time constants. Two dead [SerializeField]
    /// fields were removed during migration: <c>tagPrefab</c> (replaced by
    /// TimelineIconFactory) and <c>maxReleaseDelay</c> (never referenced in code).
    /// Scene object references (barRect, iconsRoot, triggerPointRect, spawnPointRect)
    /// are now plain runtime fields — Awake() creates or caches them.</para>
    /// <para>USAGE: Referenced from TimelineBarInstance.RebuildLayout /
    /// UnitsPerSecFromSpeed / SpawnTag / PushbackOnAttack / EnforceQueueSpacing.</para>
    /// <para>RELATED FILES: TimelineBarInstance.cs, TimelineIcon.cs,
    /// TimelineIconFactory.cs, TimelineTriggerSequence.cs</para>
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
        // Minimum pushback when enemy is at the far left (just spawned).
        public const float PushbackBase = 0.05f;

        // Maximum pushback when enemy is at the trigger point (right edge).
        public const float PushbackMax = 0.4f;

        // Base stun duration in seconds after pushback.
        public const float BaseStunDuration = 1f;

        // Strategic Zone on the RIGHT side of the bar (the "loaded" end) —
        // enemies whose icon sits at u >= (1 - ZoneU) (i.e., inside the Zone)
        // are vulnerable to pushback. Enemies further left (u < 1 - ZoneU)
        // still take damage but their icon is NOT pushed back along the
        // timeline. Width is ~35% of the bar measured from the trigger.
        public const float ZoneU = 0.35f;

        // Translucent fill color for the Zone strip on the bar.
        public static readonly UnityEngine.Color ZoneFillColor =
            new UnityEngine.Color(0.85f, 0.10f, 0.10f, 0.18f);

        // ── Queue Coordination ───────────────────────────────────────────────
        // Minimum time gap between enemy releases.
        public const float MinimumReleaseGap = 1.5f;

        // Minimum u-distance between two visible tags before they're considered overlapping
        // and need to be re-spaced. ~6% of the bar's full width.
        public const float MinSpatialGap = 0.06f;
    }
}
