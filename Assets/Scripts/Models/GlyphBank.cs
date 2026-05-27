using System.Collections.Generic;

namespace Scripts.Models
{
    /// <summary>
    /// GLYPHBANK - The shared team pool of glyph tokens (replaces the per-team mana bar).
    ///
    /// <para>Glyphs are added by disrupting enemy charges (see ElementDrawResolver) and spent to
    /// pay skill recipes (see GlyphRecipe / GlyphRecipes). Pure data — no Unity, no scene access —
    /// so it's unit-testable and can be owned by a manager / battle state and shown by any UI.</para>
    ///
    /// <para>USAGE:
    /// <code>
    /// bank.Add(GlyphType.Physical);              // disrupted an Uppercut
    /// if (bank.CanAfford(GlyphRecipes.Heal2))    // 1 Colorless + 1 Magic
    ///     bank.Spend(GlyphRecipes.Heal2);
    /// </code></para>
    /// </summary>
    public sealed class GlyphBank
    {
        private readonly Dictionary<GlyphType, int> glyphs = new Dictionary<GlyphType, int>();

        public int Count(GlyphType type) => glyphs.TryGetValue(type, out var n) ? n : 0;

        public int Total
        {
            get { int sum = 0; foreach (var kv in glyphs) sum += kv.Value; return sum; }
        }

        /// <summary>Adds glyphs of a type to the shared pool (e.g., from a disrupted enemy charge).</summary>
        public void Add(GlyphType type, int amount = 1)
        {
            if (amount <= 0) return;
            glyphs[type] = Count(type) + amount;
        }

        /// <summary>True if the bank holds every glyph the recipe requires.</summary>
        public bool CanAfford(GlyphRecipe recipe)
        {
            if (recipe == null) return false;
            foreach (var cost in recipe.Costs)
                if (Count(cost.Type) < cost.Amount) return false;
            return true;
        }

        /// <summary>Spends the recipe's glyphs if affordable. Returns false (and spends nothing) otherwise.</summary>
        public bool Spend(GlyphRecipe recipe)
        {
            if (!CanAfford(recipe)) return false;
            foreach (var cost in recipe.Costs)
                glyphs[cost.Type] = Count(cost.Type) - cost.Amount;
            return true;
        }

        /// <summary>Empties the pool (battle reset).</summary>
        public void Clear() => glyphs.Clear();

        /// <summary>Read-only view for UI (the glyph bar) — counts per type.</summary>
        public IReadOnlyDictionary<GlyphType, int> All => glyphs;
    }
}
