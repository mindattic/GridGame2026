using Scripts.Models;

namespace Scripts.Data
{
    /// <summary>
    /// GLYPHRECIPES - Declarative skill costs in the glyph economy. Each entry is what a skill
    /// charges from the shared GlyphBank. Add a new castable skill = add a recipe here.
    ///
    /// <para>Low-level skills lean on the common Colorless glyph; stronger / combo skills demand
    /// typed glyphs you can only get by interrupting the matching enemy cast — so the player's
    /// power scales with how well they read and disrupt the enemy.</para>
    /// </summary>
    public static class GlyphRecipes
    {
        // Low level — payable with the common colorless glyphs most disruptions drop.
        public static readonly GlyphRecipe Heal1 = new GlyphRecipe("Heal",
            new GlyphCost(GlyphType.Colorless, 1));

        // Heal2 needs a Magic glyph — i.e., you must have interrupted an enemy magic cast.
        public static readonly GlyphRecipe Heal2 = new GlyphRecipe("Heal+",
            new GlyphCost(GlyphType.Colorless, 1),
            new GlyphCost(GlyphType.Magic, 1));

        // Elemental nukes cost their element (drawn from disrupting that element's cast).
        public static readonly GlyphRecipe Fire1 = new GlyphRecipe("Fire",
            new GlyphCost(GlyphType.Fire, 1));
        public static readonly GlyphRecipe Ice1 = new GlyphRecipe("Ice",
            new GlyphCost(GlyphType.Ice, 1));
        public static readonly GlyphRecipe Thunder1 = new GlyphRecipe("Thunder",
            new GlyphCost(GlyphType.Thunder, 1));

        // Combo / signature move: Fire + Physical + Physical.
        public static readonly GlyphRecipe MeteorSlam = new GlyphRecipe("Meteor Slam",
            new GlyphCost(GlyphType.Fire, 1),
            new GlyphCost(GlyphType.Physical, 2));
    }
}
