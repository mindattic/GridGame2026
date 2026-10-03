namespace Scripts.Services
{
    /// <summary>
    /// SPELLCASTCAPACITY - How many hero spells may be loading on the timeline at once.
    ///
    /// <para>Up to <see cref="MaxHeroCastsInFlight"/> hero cast icons can ride the timeline together;
    /// a further cast-time spell is refused at click time and again at target confirm, before any
    /// orbs are spent. Instant spells never load, so they are never refused. Enemy charge icons share
    /// the lane but are not counted.</para>
    ///
    /// <para>RELATED FILES: AbilityBar.cs (the gate), TimelineBarInstance.cs (HeroCastsInFlight).</para>
    /// </summary>
    public static class SpellCastCapacity
    {
        public const int MaxHeroCastsInFlight = 4;

        /// <summary>True when a new cast-time spell must be refused.</summary>
        public static bool IsFull(int heroCastsInFlight) => heroCastsInFlight >= MaxHeroCastsInFlight;

        /// <summary>True when a spell with <paramref name="castTimeSeconds"/> may start now.</summary>
        public static bool CanStart(float castTimeSeconds, int heroCastsInFlight)
            => castTimeSeconds <= 0f || !IsFull(heroCastsInFlight);
    }
}
