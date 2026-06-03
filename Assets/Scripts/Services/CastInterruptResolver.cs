using UnityEngine;
using Scripts.Instances.Actor;
using Scripts.Models;
using Scripts.Utilities;

namespace Scripts.Services
{
    /// <summary>What a single landed hit does to an in-flight cast (game_bible.md §13.4 stagger model).</summary>
    public enum CastInterruptOutcome
    {
        /// <summary>Rare LCK "miracle save" — the cast shrugs the hit entirely (US-025 adds the
        /// snap-to-resolve + flash/SFX). Checked first.</summary>
        Clutch,
        /// <summary>WIS poise shrugged the hit off — no delay added.</summary>
        Resisted,
        /// <summary>Cast survives but is pushed back (its remaining cast time grows).</summary>
        Delayed,
        /// <summary>Accumulated delay now exceeds the spell's original cast time — cast is cancelled.</summary>
        Cancelled
    }

    public struct CastInterruptResult
    {
        public CastInterruptOutcome Outcome;
        /// <summary>Seconds of cast-time this hit added (0 if Resisted).</summary>
        public float DelayAdded;
    }

    /// <summary>
    /// CASTINTERRUPTRESOLVER - The "cast stagger" interrupt model (US-024, revised 2026-06-02).
    ///
    /// <para>When a casting actor takes a landing hit, the cast is <b>pushed back on the timeline</b>:
    /// its remaining cast time increases by a delay. Delays <b>accumulate</b>; once the total exceeds
    /// the spell's original cast time, the cast is <b>cancelled</b>. <b>Wisdom is the caster's poise</b> —
    /// higher WIS both reduces the delay per hit AND gives a chance to shrug a hit off entirely;
    /// attacker Strength increases the delay. Replaces the old {Fail | Pushback | Clutch} LCK roll
    /// (no Clutch — US-025 obsolete). Pure logic; the caller (`TimelineBar.InterruptCastsByOwner`)
    /// applies the push (`TimelineIcon.DelayCast`) or the cancel (`CastingState.Interrupt`).</para>
    /// </summary>
    public static class CastInterruptResolver
    {
        /// <summary>Baseline cast-time added per landed hit (seconds), before STR/WIS scaling.</summary>
        public const float BaseInterruptDelay = 0.6f;
        /// <summary>Divisor for the attacker-Strength term (higher STR → more delay).</summary>
        public const float StrengthScale = 20f;
        /// <summary>Divisor for the caster-Wisdom term (higher WIS → less delay).</summary>
        public const float WisdomDelayScale = 15f;
        /// <summary>Per-point WIS chance to fully resist a hit (shrug it off).</summary>
        public const float WisdomResistPerPoint = 0.015f;
        /// <summary>Cap on the WIS resist chance.</summary>
        public const float MaxResistChance = 0.60f;
        /// <summary>Clutch (US-025 miracle save) base rate per point of LCK — LCK 10 ≈ 5%, LCK 20 ≈ 10%.</summary>
        public const float ClutchChancePerLuck = 1f / 200f;
        /// <summary>Cap on the Clutch chance.</summary>
        public const float ClutchMaxChance = 0.20f;

        public static CastInterruptResult Resolve(ActorInstance caster, ActorInstance attacker, CastingState cast)
        {
            float lck = caster?.Stats?.Luck ?? 0f;
            float wis = caster?.Stats?.Wisdom ?? 0f;
            float atk = attacker?.Stats?.Strength ?? 0f;

            // Clutch FIRST — rare, LCK-driven miracle save: the cast shrugs the hit entirely
            // (US-025 adds the snap-to-resolve + flash/SFX). LCK is the primary stat here.
            float clutch = Mathf.Clamp(lck * ClutchChancePerLuck, 0f, ClutchMaxChance);
            if (RNG.Float(0f, 1f) < clutch)
                return new CastInterruptResult { Outcome = CastInterruptOutcome.Clutch, DelayAdded = 0f };

            // WIS poise: a hit may be shrugged off with no effect.
            float resist = Mathf.Clamp(wis * WisdomResistPerPoint, 0f, MaxResistChance);
            if (RNG.Float(0f, 1f) < resist)
                return new CastInterruptResult { Outcome = CastInterruptOutcome.Resisted, DelayAdded = 0f };

            // Delay grows with attacker STR, shrinks with caster WIS.
            float delay = BaseInterruptDelay * (1f + atk / StrengthScale) / (1f + wis / WisdomDelayScale);

            float total = (cast?.AccumulatedInterruptDelay ?? 0f) + delay;
            float original = cast?.TotalCastTime ?? 0f;
            var outcome = (original > 0f && total >= original)
                ? CastInterruptOutcome.Cancelled
                : CastInterruptOutcome.Delayed;
            return new CastInterruptResult { Outcome = outcome, DelayAdded = delay };
        }
    }
}
