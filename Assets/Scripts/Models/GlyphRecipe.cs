using System.Collections.Generic;

namespace Scripts.Models
{
    /// <summary>One line of a glyph recipe: how many of a given glyph type a skill costs.</summary>
    public readonly struct GlyphCost
    {
        public readonly GlyphType Type;
        public readonly int Amount;
        public GlyphCost(GlyphType type, int amount) { Type = type; Amount = amount; }
    }

    /// <summary>
    /// GLYPHRECIPE - A skill's glyph cost (e.g., Meteor Slam = 1 Fire + 2 Physical). Declarative
    /// data; the GlyphBank checks/spends against it. Adding a skill cost is one entry in GlyphRecipes.
    /// </summary>
    public sealed class GlyphRecipe
    {
        public string Name { get; }
        public IReadOnlyList<GlyphCost> Costs { get; }

        public GlyphRecipe(string name, params GlyphCost[] costs)
        {
            Name = name;
            Costs = costs ?? new GlyphCost[0];
        }

        /// <summary>Human-readable cost, e.g. "1 Fire + 2 Physical". For tooltips.</summary>
        public string Describe()
        {
            if (Costs.Count == 0) return "Free";
            var parts = new List<string>();
            foreach (var c in Costs) parts.Add($"{c.Amount} {c.Type}");
            return string.Join(" + ", parts);
        }
    }
}
