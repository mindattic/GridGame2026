namespace Scripts.Models
{
    /// <summary>
    /// GLYPHTYPE - A token in the shared team glyph bank (the resource that replaces mana).
    ///
    /// <para>Glyphs are drawn by disrupting enemy charges: most interrupted casts drop the common
    /// <see cref="Colorless"/>; typed casts drop their type (a physical charge like Uppercut drops
    /// <see cref="Physical"/>, a spell drops <see cref="Magic"/> or its element). Skills are paid
    /// for with glyph recipes (see GlyphRecipes), so the team's offense is fueled by reading and
    /// interrupting the enemy's intentions.</para>
    /// </summary>
    public enum GlyphType
    {
        Colorless, // most common — dropped by most interrupted skills; pays for low-level abilities
        Physical,  // from disrupting a physical charge (e.g., Uppercut)
        Magic,     // from disrupting a generic/arcane magic cast
        Fire,
        Ice,
        Thunder,
        Light,
        Dark
    }
}
