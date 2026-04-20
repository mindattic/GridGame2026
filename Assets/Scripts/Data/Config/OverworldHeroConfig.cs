namespace Scripts.Data.Config
{
    /// <summary>
    /// OVERWORLDHEROCONFIG - Static tuning values for OverworldHero movement, collision, and follow-leader behavior.
    /// <para>PURPOSE: Replaces the former [SerializeField] tuning fields on OverworldHero
    /// with compile-time constants. Scene YAML previously stored these as per-instance
    /// overrides — all 4 instances in Overworld.unity held identical defaults, so no
    /// tuning is lost by the move.</para>
    /// <para>NOTE: The <c>leader</c> field on OverworldHero remains an instance field
    /// (it holds a runtime Transform reference set via SetLeader()).</para>
    /// <para>USAGE: OverworldHero instance fields are initialized from these constants
    /// to preserve runtime-mutation setter APIs (SetMoveSpeed, SetSnapThreshold, etc.).</para>
    /// <para>RELATED FILES: OverworldHero.cs, OverworldHero.FollowCursor.cs, OverworldHero.Collision.cs</para>
    /// </summary>
    public static class OverworldHeroConfig
    {
        // ── Movement ─────────────────────────────────────────────────────────
        public const float MoveSpeed                = 2.5f;
        public const float SnapThreshold            = 0.05f;
        public const bool  RequireVisibleToMove     = true;
        public const bool  IgnoreClicksWhenOffscreen = false;
        public const bool  AllowVirtualJoystick     = true;
        public const bool  IdleWhileOffscreen       = true;

        // ── Collision toggle ─────────────────────────────────────────────────
        public const bool EnableCollision = false;

        // ── Leader/follower ──────────────────────────────────────────────────
        public const float FollowSpeed        = 2.3f;
        public const float FollowDistance     = 0.75f;
        public const float ArriveBuffer       = 0.05f;
        public const float CatchupMultiplier  = 2.0f;
        public const float TeleportIfBeyond   = 25f;

        // ── Party collision ──────────────────────────────────────────────────
        public const bool IgnorePartyCollisions = true;

        // ── Collision radius + forward-cast coverage ─────────────────────────
        public const float CollisionRadiusWorld          = 0.1f;
        public const float ForwardCoverageBlockThreshold = 0.5f;
        public const int   ForwardCoverageSamples        = 16;

        // ── Physics cast-and-slide (optional) ────────────────────────────────
        public const float Skin                = 0.01f;
        public const int   MaxSlideIterations  = 3;
        public const float MaxCastStepDistance = 0.25f;
    }
}
