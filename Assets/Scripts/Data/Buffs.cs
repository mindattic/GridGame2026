using System.Collections.Generic;
using Scripts.Models;

namespace Scripts.Data
{
    /// <summary>
    /// BUFFS - Catalog of every buff/debuff in the game.
    ///
    /// <para><b>Protection</b> is fully wired (Shield button → 1-turn 15% DR on all heroes; hook in
    /// Formulas/damage code reads <see cref="BuffSystem.GetIncomingDamageMultiplier"/>).</para>
    ///
    /// <para>ALL hooks are live (verified 2026-08-15, US-134): tick damage/regen —
    /// EndTurnSequence → ActorInstance.TickStatusesRoutine; immobility — BuffSystem.IsImmobile
    /// gates EnemyPlanner; Slowed — TimelineIcon effective-speed ×SlowedTimelineMultiplier
    /// (US-011); Silenced — AbilityBar blocks Spell slots (US-012); Blinded —
    /// Formulas.CalculateHitType ×BlindedAccuracyMultiplier (US-013); lightning×wet —
    /// SpellEffectDispatcher ×LightningWhenWetMultiplier.</para>
    /// </summary>
    public static class Buffs
    {
        // Tuning constants for cross-buff interactions (designer-tunable).
        public const float LightningWhenWetMultiplier = 1.5f;   // lightning vs Wet → ×1.5 damage
        public const float SleepWhenWarmMultiplier   = 1.5f;   // sleep applied to Warm target → ×1.5 duration (US-014; no success roll exists yet)
        public const float BlindedAccuracyMultiplier = 0.5f;   // Blinded attacker → hit chance ×0.5 (US-013, bible §8.1)
        public const float SlowedTimelineMultiplier  = 0.5f;   // Slowed → timeline icon advances at ×0.5 speed (US-011)

        // ── Buffs ──
        public static readonly Buff Protection = new Buff(
            id: "protection",
            displayName: "Protection",
            kind: BuffKind.Buff,
            durationUnit: BuffDurationUnit.Turns,
            defaultDuration: 1,
            incomingDamageReductionPercent: 0.15f); // 15% DR for 1 turn

        // ── Debuffs ──
        public static readonly Buff Burning = new Buff(
            id: "burning",
            displayName: "Burning",
            kind: BuffKind.Debuff,
            durationUnit: BuffDurationUnit.Ticks,
            defaultDuration: 5,
            damagePerTick: 4f,           // balance pass: US-135
            onExpireApplyId: "warm");    // fire wears off → Warm

        public static readonly Buff Frozen = new Buff(
            id: "frozen",
            displayName: "Frozen",
            kind: BuffKind.Debuff,
            durationUnit: BuffDurationUnit.Turns,
            defaultDuration: 1,
            immobile: true,
            onExpireApplyId: "wet");     // ice melts → Wet

        public static readonly Buff Wet = new Buff(
            id: "wet",
            displayName: "Wet",
            kind: BuffKind.Debuff,
            durationUnit: BuffDurationUnit.Ticks,
            defaultDuration: 6);          // lightning×wet handled in damage formula via LightningWhenWetMultiplier

        public static readonly Buff Warm = new Buff(
            id: "warm",
            displayName: "Warm",
            kind: BuffKind.Debuff,
            durationUnit: BuffDurationUnit.Ticks,
            defaultDuration: 3);          // sleep×warm handled at Sleep application via SleepWhenWarmMultiplier

        public static readonly Buff Poisoned = new Buff(
            id: "poisoned",
            displayName: "Poisoned",
            kind: BuffKind.Debuff,
            durationUnit: BuffDurationUnit.Ticks,
            defaultDuration: 6,
            damagePerTick: 3f);              // ticks apply via EndTurnSequence → TickStatusesRoutine

        public static readonly Buff Slowed = new Buff(
            id: "slowed",
            displayName: "Slowed",
            kind: BuffKind.Debuff,
            durationUnit: BuffDurationUnit.Turns,
            defaultDuration: 2);             // wired: TimelineIcon effective speed ×SlowedTimelineMultiplier (US-011)

        public static readonly Buff Silenced = new Buff(
            id: "silenced",
            displayName: "Silenced",
            kind: BuffKind.Debuff,
            durationUnit: BuffDurationUnit.Turns,
            defaultDuration: 2);             // wired: AbilityBar blocks Spell slots while Silenced (US-012)

        public static readonly Buff Blinded = new Buff(
            id: "blinded",
            displayName: "Blinded",
            kind: BuffKind.Debuff,
            durationUnit: BuffDurationUnit.Turns,
            defaultDuration: 2);             // wired: Formulas.CalculateHitType ×BlindedAccuracyMultiplier (US-013)

        public static readonly Buff Sleep = new Buff(
            id: "sleep",
            displayName: "Asleep",
            kind: BuffKind.Debuff,
            durationUnit: BuffDurationUnit.Turns,
            defaultDuration: 3,
            immobile: true,
            breaksOnDamage: true,
            breaksOnMove: true);

        /// <summary>All buffs by Id — for OnExpireApplyId follow-ups.</summary>
        public static readonly IReadOnlyDictionary<string, Buff> ById = new Dictionary<string, Buff>
        {
            { Protection.Id, Protection },
            { Burning.Id, Burning },
            { Frozen.Id, Frozen },
            { Wet.Id, Wet },
            { Warm.Id, Warm },
            { Sleep.Id, Sleep },
            { Poisoned.Id, Poisoned },
            { Slowed.Id, Slowed },
            { Silenced.Id, Silenced },
            { Blinded.Id, Blinded },
        };
    }
}
