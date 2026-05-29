namespace Scripts.Models
{
    /// <summary>
    /// MANATYPE - The color of a single mana sphere in the team's mana line (the resource
    /// that replaces the old time-accrual mana pool).
    ///
    /// <para>Mana is a <b>line of colored spheres</b>. Colors are deliberately <b>abstract</b> (a
    /// Magic-style 5-color pie, WUBRG) — a sphere's color is an <i>ingredient</i>, not a fixed
    /// element. Spells are recipes of colors (see ManaAbilities); the effect→color mapping is a
    /// designer-tunable layer on top, not baked into the orb.</para>
    ///
    /// <para>Spheres are <i>drawn</i> two ways:</para>
    /// <list type="bullet">
    ///   <item>Interrupting an enemy's charging cast in the Prepare Zone — this <b>cancels the
    ///   enemy's attack</b> AND awards a sphere of that charge's color.</item>
    ///   <item>Critical hits sometimes drop the common <see cref="Colorless"/> sphere.</item>
    /// </list>
    /// </summary>
    public enum ManaType
    {
        Colorless, // generic/wild — the common drop (crits, weak charges); cheap basics & filler
        White,     // W
        Blue,      // U
        Black,     // B
        Red,       // R
        Green      // G
    }
}
