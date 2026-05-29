using System.Collections.Generic;

namespace Scripts.Models
{
    /// <summary>One line of a mana recipe: how many spheres of a given color a skill costs.</summary>
    public readonly struct ManaCost
    {
        public readonly ManaType Type;
        public readonly int Amount;
        public ManaCost(ManaType type, int amount) { Type = type; Amount = amount; }
    }

    /// <summary>
    /// MANARECIPE - An ability's mana cost in colored spheres (e.g., Fireball = 2 Blue).
    /// Declarative data; the ManaBank checks/spends against it. Adding a cost is one entry in
    /// ManaAbilities.
    /// </summary>
    public sealed class ManaRecipe
    {
        public string Name { get; }
        public IReadOnlyList<ManaCost> Costs { get; }

        public ManaRecipe(string name, params ManaCost[] costs)
        {
            Name = name;
            Costs = costs ?? new ManaCost[0];
        }

        /// <summary>Human-readable cost, e.g. "2 Blue". For tooltips.</summary>
        public string Describe()
        {
            if (Costs.Count == 0) return "Free";
            var parts = new List<string>();
            foreach (var c in Costs) parts.Add($"{c.Amount} {c.Type}");
            return string.Join(" + ", parts);
        }
    }

    /// <summary>Whether an ability slot holds a castable spell or a consumable item.</summary>
    public enum AbilityKind { Spell, Item }

    /// <summary>
    /// MANAABILITY - One of the (up to 6) ability bar slots.
    ///
    /// <para><b>Spell</b> — costs mana orbs from the team's ManaBank line. <see cref="Cost"/> is
    /// non-null; <see cref="Charges"/> is <c>-1</c> (unused).</para>
    /// <para><b>Item</b> — does NOT cost mana. Has a finite number of <see cref="Charges"/>
    /// (consumptions); each use decrements it. Restocked outside combat by buying at the vendor or
    /// crafting at the alchemist. <see cref="Cost"/> is null.</para>
    /// </summary>
    public sealed class ManaAbility
    {
        public const float DefaultSpellCastSeconds = 1.5f;

        public string Name { get; }
        public AbilityKind Kind { get; }

        /// <summary>Spell-only: the mana-orb cost. Null for items.</summary>
        public ManaRecipe Cost { get; }

        /// <summary>Item-only: remaining consumptions (0 = empty). -1 for spells.</summary>
        public int Charges { get; private set; }

        /// <summary>Item-only: cap charges can be refilled to. -1 for spells.</summary>
        public int MaxCharges { get; }

        /// <summary>
        /// Spell-only: time (seconds) the cast takes on the timeline before it resolves and pauses
        /// everything else. 0 for items (instant). Future tuning: scale this by caster's WIS/INT
        /// via <see cref="Scripts.Utilities.Formulas.CastTime"/>.
        /// </summary>
        public float CastTimeSeconds { get; }

        /// <summary>Spell ctor.</summary>
        public ManaAbility(string name, ManaRecipe cost, float castTimeSeconds = DefaultSpellCastSeconds)
        {
            Name = name;
            Kind = AbilityKind.Spell;
            Cost = cost;
            Charges = -1;
            MaxCharges = -1;
            CastTimeSeconds = castTimeSeconds;
        }

        /// <summary>Item ctor (filled to max on construction; <paramref name="maxCharges"/> is the cap). Items are instant.</summary>
        public ManaAbility(string name, int maxCharges)
        {
            Name = name;
            Kind = AbilityKind.Item;
            Cost = null;
            MaxCharges = maxCharges;
            Charges = maxCharges;
            CastTimeSeconds = 0f;
        }

        /// <summary>Item-only: consume one charge. Returns false if empty (or if this is a spell).</summary>
        public bool TryConsumeCharge()
        {
            if (Kind != AbilityKind.Item || Charges <= 0) return false;
            Charges--;
            return true;
        }

        /// <summary>Item-only: add charges (e.g., vendor purchase / alchemist craft), clamped to MaxCharges.</summary>
        public void Refill(int amount = 1)
        {
            if (Kind != AbilityKind.Item || amount <= 0) return;
            Charges = System.Math.Min(MaxCharges, Charges + amount);
        }
    }
}
