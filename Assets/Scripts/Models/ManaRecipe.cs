using System.Collections.Generic;

namespace Scripts.Models
{
    /// <summary>One line of a mana recipe: how many orbs of a given color a spell costs.</summary>
    public readonly struct ManaCost
    {
        public readonly ManaType Type;
        public readonly int Amount;
        public ManaCost(ManaType type, int amount) { Type = type; Amount = amount; }
    }

    /// <summary>
    /// MANARECIPE - A spell's mana cost in colored orbs (e.g., Fireball = 2 Red).
    /// Declarative data; the <see cref="ManaBank"/> checks/spends against it. Adding a cost is one
    /// entry in <see cref="Scripts.Data.ManaAbilities"/>.
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

        /// <summary>Human-readable cost, e.g. "2 Red". For tooltips.</summary>
        public string Describe()
        {
            if (Costs.Count == 0) return "Free";
            var parts = new List<string>();
            foreach (var c in Costs) parts.Add($"{c.Amount} {c.Type}");
            return string.Join(" + ", parts);
        }
    }

    /// <summary>
    /// What an <see cref="Scripts.Canvas.AbilityBar"/> slot holds. Every slot is one of these
    /// three kinds; each behaves distinctly in the click → resolve flow.
    ///
    /// <list type="bullet">
    ///   <item><b>Skill</b> — usually class-based; reusable for FREE (no orb cost, no charges).
    ///   Each use "costs the player's turn" — after the dispatcher resolves, the AbilityBar
    ///   auto-advances the timeline to the next enemy. Example: Steal, Mug.</item>
    ///
    ///   <item><b>Spell</b> — pays mana orbs from the team's <see cref="ManaBank"/>. Goes through
    ///   a <see cref="Scripts.Canvas.SpellCastBar"/> countdown before resolving. Cost is a
    ///   <see cref="ManaRecipe"/>. Example: Fireball (2 Red), Bolt (2 Red + 1 Blue).</item>
    ///
    ///   <item><b>Item</b> — consumable from a PER-SLOT stack. Each instance carries its own
    ///   <see cref="ManaAbility.MaxStackSize"/> (the stack cap) and <see cref="ManaAbility.Charges"/>
    ///   (the live count). Two stacks of 5 potions = TWO slots, each draining independently.
    ///   Instant on click. Restocked at vendor/alchemist. Example: Potion (3-stack default).</item>
    /// </list>
    /// </summary>
    public enum AbilityKind { Skill, Spell, Item }

    /// <summary>
    /// MANAABILITY - A single entry that lives in an <see cref="Scripts.Canvas.AbilityBar"/> slot.
    /// Three kinds (see <see cref="AbilityKind"/>): Skill / Spell / Item. The constructor you call
    /// picks the kind; the fields that don't apply stay at sentinel values (-1 for charges, null
    /// for cost, 0 for cast time).
    ///
    /// <para>This class is named <c>ManaAbility</c> rather than <c>Ability</c> to avoid a collision
    /// with the legacy <see cref="Ability"/> type in <c>Instances/AbilityButton.cs</c>. The
    /// AbilityBar holds these instances; the catalog of all of them is in
    /// <see cref="Scripts.Data.ManaAbilities"/>.</para>
    /// </summary>
    public sealed class ManaAbility
    {
        public const float DefaultSpellCastSeconds = 1.5f;

        public string Name { get; }
        public AbilityKind Kind { get; }

        /// <summary>Spell-only: the mana-orb cost. Null for Skills and Items.</summary>
        public ManaRecipe Cost { get; }

        /// <summary>Item-only: remaining stack count (0 = empty). -1 for Skills and Spells.</summary>
        public int Charges { get; private set; }

        /// <summary>Item-only: the maximum stack size this slot can hold. -1 for Skills and Spells.
        /// Vendor/alchemist sales replenish up to this cap; buying past it spawns a new slot.</summary>
        public int MaxStackSize { get; }

        /// <summary>Spell-only: time (seconds) the cast bar takes to shrink before resolving. 0 for
        /// Skills (instant) and Items (instant).</summary>
        public float CastTimeSeconds { get; }

        /// <summary>Skill-only: turns the skill is locked after use (0 = no cooldown). The remaining
        /// countdown is tracked per-hero by <see cref="Scripts.Managers.SkillCooldownManager"/> — NOT
        /// here — because Skill ManaAbility instances are shared statics across multiple loadouts.</summary>
        public int CooldownTurns { get; }

        /// <summary>Item-only: the ItemDefinition Id this slot was minted from (null for Skills/Spells
        /// and the plain Potion template). Lets <c>AbilityBar.HandleItem</c> recover the item's
        /// <c>OnUseSpellName</c> so a consumable can cast a spell on use (US-042, e.g. Sleep Dart).</summary>
        public string SourceItemId { get; }

        /// <summary>Spell ctor — pays orbs from the team bank.</summary>
        public ManaAbility(string name, ManaRecipe cost, float castTimeSeconds = DefaultSpellCastSeconds)
        {
            Name = name;
            Kind = AbilityKind.Spell;
            Cost = cost;
            Charges = -1;
            MaxStackSize = -1;
            CastTimeSeconds = castTimeSeconds;
            CooldownTurns = 0;
            SourceItemId = null;
        }

        /// <summary>Item ctor — per-slot consumable. <paramref name="maxStackSize"/> caps the stack;
        /// the slot starts FULL (Charges = MaxStackSize) on construction. Each call returns a NEW
        /// instance so stacks don't share state across slots. <paramref name="sourceItemId"/> links
        /// back to the ItemDefinition (for OnUseSpellName routing); null for the generic Potion.</summary>
        public ManaAbility(string name, int maxStackSize, string sourceItemId = null)
        {
            Name = name;
            Kind = AbilityKind.Item;
            Cost = null;
            MaxStackSize = maxStackSize;
            Charges = maxStackSize;
            CastTimeSeconds = 0f;
            CooldownTurns = 0;
            SourceItemId = sourceItemId;
        }

        /// <summary>Skill ctor — free, reusable, "costs a turn." The bool param is a tag marker
        /// to distinguish this ctor from the Item one (which also takes a non-string second arg).
        /// <paramref name="cooldownTurns"/> locks the skill for N turn-cycles after use (0 = none).</summary>
        public ManaAbility(string name, bool _isSkill, int cooldownTurns = 0)
        {
            Name = name;
            Kind = AbilityKind.Skill;
            Cost = null;
            Charges = -1;
            MaxStackSize = -1;
            CastTimeSeconds = 0f;
            CooldownTurns = cooldownTurns;
            SourceItemId = null;
        }

        /// <summary>Item-only: consume one charge from the stack. Returns false if empty or not an Item.</summary>
        public bool TryConsumeCharge()
        {
            if (Kind != AbilityKind.Item || Charges <= 0) return false;
            Charges--;
            return true;
        }

        /// <summary>Item-only: add charges (vendor purchase / alchemist craft), clamped to <see cref="MaxStackSize"/>.</summary>
        public void Refill(int amount = 1)
        {
            if (Kind != AbilityKind.Item || amount <= 0) return;
            Charges = System.Math.Min(MaxStackSize, Charges + amount);
        }
    }
}
