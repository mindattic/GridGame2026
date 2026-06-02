using System.Collections.Generic;

namespace Scripts.Models
{
    /// <summary>
    /// MANABANK - The shared team line of mana spheres (replaces the old time-accrual mana pool).
    ///
    /// <para>An <b>ordered, capped line of orbs</b> (default 12 slots), rendered left→right as a row
    /// of colored spheres. Orbs are <b>harvested</b> from the heroes you bring along — completing a
    /// pincer mints orbs of the participating heroes' colors (V1: every orb is <see cref="ManaType.Blue"/>;
    /// colors arrive with the recipe combos later). Orbs are spent to pay ability costs
    /// (see ManaAbility / ManaAbilities). Pure data — no Unity — so it's unit-testable and shown by any UI.</para>
    ///
    /// <para>USAGE:
    /// <code>
    /// bank.Add(ManaType.Blue);                 // a hero contributed an orb to the line
    /// if (bank.CanAfford(ManaAbilities.Fireball.Cost))
    ///     bank.Spend(ManaAbilities.Fireball.Cost);
    /// </code></para>
    /// </summary>
    public sealed class ManaBank
    {
        public const int DefaultCapacity = 12;

        private readonly List<ManaType> orbs = new List<ManaType>();

        public int Capacity { get; }

        public ManaBank(int capacity = DefaultCapacity) { Capacity = capacity; }

        /// <summary>
        /// DEBUG: when true, <see cref="CanAfford"/> ignores color and only checks that the total
        /// number of orbs in the line is >= the recipe's total cost (so any 3 orbs can cast a 3-orb
        /// spell). Spending then consumes from the leftmost orbs regardless of color. Used while we
        /// experiment with the bar before color identity is locked in.
        /// </summary>
        public bool AllowAnyColor { get; set; } = false;

        /// <summary>Orbs currently on the line.</summary>
        public int Total => orbs.Count;

        /// <summary>True once the line is at capacity — further harvested orbs are dropped.</summary>
        public bool IsFull => orbs.Count >= Capacity;

        public int Count(ManaType type)
        {
            int n = 0;
            foreach (var o in orbs) if (o == type) n++;
            return n;
        }

        /// <summary>
        /// Appends orbs of a color to the end of the line, up to capacity. Returns the number
        /// actually added (less than <paramref name="amount"/> if the line filled up).
        /// </summary>
        public int Add(ManaType type, int amount = 1)
        {
            int added = 0;
            for (int i = 0; i < amount && !IsFull; i++) { orbs.Add(type); added++; }
            return added;
        }

        /// <summary>True if the line holds every orb the cost requires. With <see cref="AllowAnyColor"/>,
        /// only the TOTAL count is checked. US-033 rule B: <see cref="ManaType.Colorless"/> "wild" orbs
        /// (minted by crits, US-031) substitute for any colored requirement — the bank's pressure valve.</summary>
        public bool CanAfford(ManaRecipe cost)
        {
            if (cost == null) return false;
            if (AllowAnyColor)
            {
                int total = 0;
                foreach (var c in cost.Costs) total += c.Amount;
                return orbs.Count >= total;
            }

            int colorlessAvail = Count(ManaType.Colorless);
            foreach (var c in cost.Costs)
            {
                if (c.Type == ManaType.Colorless)
                {
                    // An explicit Colorless requirement can only be paid by Colorless orbs.
                    if (colorlessAvail < c.Amount) return false;
                    colorlessAvail -= c.Amount;
                }
                else
                {
                    int have = Count(c.Type);
                    if (have >= c.Amount) continue;
                    int shortfall = c.Amount - have;
                    if (colorlessAvail < shortfall) return false; // wilds cover the rest
                    colorlessAvail -= shortfall;
                }
            }
            return true;
        }

        /// <summary>Spends the cost's orbs if affordable (removes from the line). With <see cref="AllowAnyColor"/>,
        /// consumes leftmost regardless of color. Otherwise pays each requirement with its own color
        /// (leftmost-first, §3.1.5), falling back to Colorless wild orbs for any shortfall (US-033 rule B).</summary>
        public bool Spend(ManaRecipe cost)
        {
            if (!CanAfford(cost)) return false;
            if (AllowAnyColor)
            {
                int total = 0;
                foreach (var c in cost.Costs) total += c.Amount;
                for (int k = 0; k < total && orbs.Count > 0; k++) orbs.RemoveAt(0);
                return true;
            }

            // Pay explicit Colorless requirements first so colored fallbacks don't consume the
            // wild orbs those requirements need.
            foreach (var c in cost.Costs)
                if (c.Type == ManaType.Colorless)
                    for (int k = 0; k < c.Amount; k++) orbs.Remove(ManaType.Colorless);

            // Colored requirements: spend the matching color (leftmost), else a Colorless wild orb.
            foreach (var c in cost.Costs)
            {
                if (c.Type == ManaType.Colorless) continue;
                for (int k = 0; k < c.Amount; k++)
                    if (!orbs.Remove(c.Type))
                        orbs.Remove(ManaType.Colorless);
            }
            return true;
        }

        /// <summary>Empties the line (battle reset).</summary>
        public void Clear() => orbs.Clear();

        /// <summary>The ordered line, for the UI to render left→right.</summary>
        public IReadOnlyList<ManaType> Orbs => orbs;
    }
}
