namespace Scripts.Models
{
    /// <summary>
    /// DAMAGETYPE - The element of damage a spell or attack delivers. Used by actor elemental
    /// resistances (per-class multipliers in <c>ActorData.Resistances</c>) and by buff×element
    /// interactions (Lightning × Wet, Fire × Warm, etc.).
    /// </summary>
    public enum DamageType
    {
        Physical,
        Fire,
        Ice,
        Lightning,
        Poison,
        Holy,
        Dark,
        Arcane,
    }
}
