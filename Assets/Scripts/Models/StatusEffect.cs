namespace Scripts.Models
{
    /// <summary>
    /// STATUSEFFECT - One active buff/debuff instance on an actor.
    ///
    /// <para>Plain data: a kind, a magnitude (meaning depends on kind — HP/turn for DoT/Regen,
    /// a 0..1 fraction for Protect), and how many turns it has left. Multiple instances of the
    /// same kind coexist and are aggregated by StatusList.</para>
    /// </summary>
    public sealed class StatusEffect
    {
        public StatusKind Kind;
        public float Magnitude;
        public int RemainingTurns;
    }
}
