using UnityEngine;
using Scripts.Instances.Actor;
using Scripts.Utilities;

namespace Scripts.Services
{
    /// <summary>The three ways an in-flight cast can react to taking damage (game_bible.md §13.4).</summary>
    public enum CastInterruptOutcome
    {
        /// <summary>Common — the cast is interrupted; MP stays consumed, effect does not apply.</summary>
        Fail,
        /// <summary>Uncommon — the cast survives but is delayed: its timeline icon is pushed back
        /// (u decreases) and briefly stunned. No MP refund; the spell still resolves later.</summary>
        Pushback,
        /// <summary>Rare (LCK-driven) — the caster shrugs off the hit and the cast resolves on the
        /// spot. The dramatic snap-to-u=1 + ClutchSequence juice is US-025.</summary>
        Clutch
    }

    /// <summary>
    /// CASTINTERRUPTRESOLVER - Rolls the {Fail | Pushback | Clutch} outcome when a casting hero takes
    /// damage (US-024). Replaces the old unconditional-Fail behavior.
    ///
    /// <para>ROLL ORDER (per the bible): <b>Clutch first</b> (instant-resolve wins over everything),
    /// then <b>Pushback vs Fail</b>. Dominant factor is the caster's <b>Luck</b>; secondary is the
    /// caster's Wisdom (poise) and the attacker's Strength (pushes toward Fail).</para>
    ///
    /// <para>RELATED FILES: TimelineBarInstance.InterruptCastsByOwner (caller), EnemyAttackSequence
    /// (origin), CastingState (Fail path), ClutchSequence (US-025, the Clutch juice).</para>
    /// </summary>
    public static class CastInterruptResolver
    {
        /// <summary>Clutch base rate per point of Luck — LCK 10 ≈ 5%, LCK 20 ≈ 10%.</summary>
        public const float ClutchChancePerLuck = 1f / 200f;
        /// <summary>Designer cap on the Clutch chance (Luck 50 would otherwise hit 25%).</summary>
        public const float ClutchMaxChance = 0.25f;
        /// <summary>Designer cap on the Pushback (cast-survives) chance.</summary>
        public const float PushbackMaxChance = 0.60f;

        public static CastInterruptOutcome Resolve(ActorInstance caster, ActorInstance attacker)
        {
            float lck = caster?.Stats?.Luck ?? 0f;
            float wis = caster?.Stats?.Wisdom ?? 0f;
            float atkStr = attacker?.Stats?.Strength ?? 0f;

            // 1) Clutch — checked first; an instant resolve trumps the other outcomes.
            float clutchChance = Mathf.Clamp(lck * ClutchChancePerLuck, 0f, ClutchMaxChance);
            if (RNG.Float(0f, 1f) < clutchChance)
                return CastInterruptOutcome.Clutch;

            // 2) Pushback vs Fail — the cast survives (delayed) when the caster's poise (LCK + WIS)
            //    beats the blow; a stronger attacker drags the result toward a clean Fail.
            float surviveChance = Mathf.Clamp((lck + wis) / 100f - atkStr / 400f, 0f, PushbackMaxChance);
            if (RNG.Float(0f, 1f) < surviveChance)
                return CastInterruptOutcome.Pushback;

            return CastInterruptOutcome.Fail;
        }
    }
}
