using System.Collections.Generic;
using System.Text;
using Scripts.Models;

namespace Scripts.Data
{
    /// <summary>
    /// MANAABILITIES - The (up to 6) ability bar slots.
    ///
    /// <para><b>Spells</b> cost mana orbs from the ManaBank line, declared as a WUBRG-style
    /// <see cref="ManaRecipe"/>. Each color is currently a placeholder identity — see project
    /// design memory for the eventual W=heal/U=control/B=drain/R=damage/G=growth pie.</para>
    /// <para><b>Items</b> have charges (no mana cost); restocked at vendor / alchemist.</para>
    /// <para>Debug shortcut: <see cref="ManaBank.AllowAnyColor"/> lets 1–4 orbs of ANY color pay
    /// any 1–4-cost spell — for experimenting with the bar before color identity is locked.</para>
    /// </summary>
    public static class ManaAbilities
    {
        // ── Spells (color-typed cost) ──
        public static readonly ManaAbility Heal     = Spell("Heal",     (ManaType.White, 1));                            // (W)
        public static readonly ManaAbility Fireball = Spell("Fireball", (ManaType.Red, 2));                              // (R)(R)
        public static readonly ManaAbility Frost    = Spell("Frost",    (ManaType.Blue, 2));                             // (U)(U)
        public static readonly ManaAbility Bolt     = Spell("Bolt",     (ManaType.Red, 2), (ManaType.Blue, 1));          // (R)(R)(U)

        // ── Items (charges, no mana cost) ──
        public static readonly ManaAbility Potion   = Item("Potion", maxCharges: 3);

        // ── SIDE-NOTE: deferred ──
        // Ether — design idea: consumable that auto-grants mana orbs. Parked.

        /// <summary>The 6 bar slots in display order (slot 6 reserved — Ether candidate).</summary>
        public static readonly IReadOnlyList<ManaAbility> Slots = new[]
        {
            Heal, Fireball, Frost, Bolt, Potion, /* slot 6: reserved */ null
        };

        /// <summary>
        /// Cost icon string for a tooltip/label, e.g. "(R)(R)(U)" or "(W)" or "3/3" for items.
        /// Uses a single capital letter per color matching MTG conventions (W,U,B,R,G,C for Colorless).
        /// </summary>
        public static string CostIcons(ManaAbility ability)
        {
            if (ability == null) return "";
            if (ability.Kind == AbilityKind.Item) return $"{ability.Charges}/{ability.MaxCharges}";
            if (ability.Cost == null || ability.Cost.Costs.Count == 0) return "Free";

            var sb = new StringBuilder();
            foreach (var c in ability.Cost.Costs)
                for (int i = 0; i < c.Amount; i++)
                    sb.Append('(').Append(IconLetter(c.Type)).Append(')');
            return sb.ToString();
        }

        public static char IconLetter(ManaType t)
        {
            switch (t)
            {
                case ManaType.White: return 'W';
                case ManaType.Blue:  return 'U'; // U for blUe — MTG convention
                case ManaType.Black: return 'B';
                case ManaType.Red:   return 'R';
                case ManaType.Green: return 'G';
                default:             return 'C'; // Colorless
            }
        }

        private static ManaAbility Spell(string name, params (ManaType type, int amount)[] costs)
        {
            var manaCosts = new ManaCost[costs.Length];
            for (int i = 0; i < costs.Length; i++) manaCosts[i] = new ManaCost(costs[i].type, costs[i].amount);
            return new ManaAbility(name, new ManaRecipe(name, manaCosts));
        }

        private static ManaAbility Item(string name, int maxCharges) =>
            new ManaAbility(name, maxCharges);
    }
}
