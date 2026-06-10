namespace Scripts.Data.Config
{
    /// <summary>
    /// PACINGCONFIG - Single source of truth for combat-feel timing beats.
    /// <para>PURPOSE: Centralizes every "breathing room" duration in the combat flow so the
    /// game's pace is tuned in one file instead of literals scattered across sequences,
    /// animations, and canvas widgets. The Intermission accessors in Common.cs read from
    /// here, so existing call sites keep their vocabulary. Per the effect-cadence rule,
    /// nothing the player must read may resolve in a single frame — every value here that
    /// gates a readable moment stays at or above ~0.12 s (human perception floor).</para>
    /// <para>USAGE: yield return Wait.For(PacingConfig.BeforeEnemyAttackSeconds);</para>
    /// <para>RELATED FILES: Common.cs (Intermission/Interval), ActionTitleConfig.cs,
    /// TimelineBarConfig.cs, EnemyAttackSequence.cs, EnemyMoveSequence.cs,
    /// CombatTextInstance.cs, ActorAnimation.cs, BattleWonSequence.cs,
    /// BattleLostSequence.cs, CounterAttackSequence.cs, ForceHeroDropSequence.cs</para>
    /// </summary>
    public static class PacingConfig
    {
        // ── Enemy turn beats ─────────────────────────────────────────────────
        // Pause before an enemy starts its slide — telegraphs "the enemy is acting"
        // so its movement doesn't read as an instant teleport off the turn handoff.
        public const float BeforeEnemyMoveSeconds = 0.35f;

        // Pause between the enemy reaching its target and the attack swing — the
        // player needs a beat to register who is about to be hit.
        public const float BeforeEnemyAttackSeconds = 0.45f;

        // ── Hero beats ───────────────────────────────────────────────────────
        // Pause before hero pincer damage lands (after the drop locks in).
        public const float BeforePlayerAttackSeconds = 0.2f;

        // Settle beat after a forced hero drop (timeline trigger fired mid-drag),
        // before the enemy turn begins. Was 0.05 s — imperceptible.
        public const float HeroDropSettleSeconds = 0.25f;

        // ── Counter-attack ───────────────────────────────────────────────────
        // Hold after the "Counter!" text spawns before the counter-blow executes,
        // so the callout is readable. Was 0.1 s.
        public const float CounterAnnounceSeconds = 0.35f;

        // ── Floating combat text ─────────────────────────────────────────────
        // Damage/heal numbers stay fully opaque this long before fading...
        public const float CombatTextHoldSeconds = 0.45f;
        // ...then fade out over this long (motion continues while fading).
        public const float CombatTextFadeSeconds = 0.30f;

        // ── Battle end ───────────────────────────────────────────────────────
        // Hold on the Victory/Defeat banner before fading to PostBattle.
        public const float BattleEndHoldSeconds = 1.2f;

        // ── Boss phases ──────────────────────────────────────────────────────
        // Beat after a boss phase-transition announcement before the turn resumes.
        public const float BossPhaseBeatSeconds = 0.4f;

        // ── Strike / dodge animation (ActorAnimation) ────────────────────────
        // Attacker pulls back before the strike.
        public const float AttackWindupSeconds = 0.15f;
        // Attacker lunges into the hit. Floor of perceptibility — keep >= 0.12.
        public const float AttackLungeSeconds = 0.12f;
        // Attacker eases back to its tile after the hit.
        public const float AttackReturnSeconds = 0.30f;
        // Defender's evasive twist on a miss. Was 0.075 s — flicker territory.
        public const float DodgeTwistSeconds = 0.12f;
        // Defender untwists back to neutral.
        public const float DodgeReturnSeconds = 0.20f;
    }
}
