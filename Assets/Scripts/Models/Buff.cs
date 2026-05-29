namespace Scripts.Models
{
    /// <summary>Whether a buff helps the bearer or harms them.</summary>
    public enum BuffKind { Buff, Debuff }

    /// <summary>How long a buff lasts. <see cref="Turns"/> = decrements once per the bearer's turn;
    /// <see cref="Ticks"/> = decrements on each timeline-clock tick. Other modes can be added.</summary>
    public enum BuffDurationUnit { Turns, Ticks }

    /// <summary>
    /// BUFF - Static definition of a buff/debuff (Protection, Burning, Frozen, Wet, Warm, Sleep…).
    ///
    /// <para>Pure data — the runtime instance is <see cref="BuffInstance"/>. A buff declares its
    /// gameplay knobs (damage-reduction %, immobile flag, tick-damage, etc.) so the few hook sites
    /// (damage formula, action gating, timeline-tick) can opt-in without each one knowing every
    /// individual buff name.</para>
    /// </summary>
    public sealed class Buff
    {
        public string Id { get; }
        public string DisplayName { get; }
        public BuffKind Kind { get; }
        public BuffDurationUnit DurationUnit { get; }
        public int DefaultDuration { get; }

        // Gameplay knobs (any combination — a buff with all zeros is just a flag).
        /// <summary>Multiplier applied to incoming damage (0.15 = 15% reduction).</summary>
        public float IncomingDamageReductionPercent { get; }
        /// <summary>If true, bearer cannot move/act while the buff is active (Frozen, Sleep).</summary>
        public bool Immobile { get; }
        /// <summary>If > 0, deals this much damage to the bearer each tick (Burning).</summary>
        public float DamagePerTick { get; }
        /// <summary>If true, the buff is consumed when the bearer takes damage (Sleep).</summary>
        public bool BreaksOnDamage { get; }
        /// <summary>If true, the buff is consumed when the bearer is moved/displaced (Sleep).</summary>
        public bool BreaksOnMove { get; }
        /// <summary>When this buff expires, optionally apply a follow-up (Fire→Warm, Frozen→Wet).</summary>
        public string OnExpireApplyId { get; }

        public Buff(
            string id,
            string displayName,
            BuffKind kind,
            BuffDurationUnit durationUnit,
            int defaultDuration,
            float incomingDamageReductionPercent = 0f,
            bool immobile = false,
            float damagePerTick = 0f,
            bool breaksOnDamage = false,
            bool breaksOnMove = false,
            string onExpireApplyId = null)
        {
            Id = id;
            DisplayName = displayName;
            Kind = kind;
            DurationUnit = durationUnit;
            DefaultDuration = defaultDuration;
            IncomingDamageReductionPercent = incomingDamageReductionPercent;
            Immobile = immobile;
            DamagePerTick = damagePerTick;
            BreaksOnDamage = breaksOnDamage;
            BreaksOnMove = breaksOnMove;
            OnExpireApplyId = onExpireApplyId;
        }
    }

    /// <summary>A runtime instance of a <see cref="Buff"/> on a specific bearer with remaining duration.</summary>
    public sealed class BuffInstance
    {
        public Buff Definition { get; }
        public int RemainingDuration { get; set; }

        public BuffInstance(Buff definition, int duration)
        {
            Definition = definition;
            RemainingDuration = duration;
        }

        public bool IsExpired => RemainingDuration <= 0;
    }
}
