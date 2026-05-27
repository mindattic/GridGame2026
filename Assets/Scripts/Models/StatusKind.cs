namespace Scripts.Models
{
    /// <summary>
    /// STATUSKIND - Every buff/debuff an actor can carry. Buffs and debuffs share one enum;
    /// the buff/debuff split is data (see StatusList.IsDebuff), not separate types.
    /// </summary>
    public enum StatusKind
    {
        // Damage-over-time debuffs (Magnitude = HP lost per turn)
        Burn,
        Poison,
        Bleed,

        // Heal-over-time buff (Magnitude = HP gained per turn)
        Regen,

        // Mitigation buff (Magnitude = fraction of incoming damage prevented, 0..1)
        Protect,

        // Control debuffs (mechanical hooks land as they are wired)
        Slow,
        Silence,
        Stun
    }
}
