using System.Collections.Generic;
using System.Text;
using Scripts.Models;

namespace Scripts.Data
{
    /// <summary>
    /// MANAABILITIES - The catalog of every entry that can live in an
    /// <see cref="Scripts.Canvas.AbilityBar"/> slot, grouped by kind:
    ///
    /// <list type="bullet">
    ///   <item><b>Skills</b> — free, reusable, "costs a turn" (Steal, Mug). Built with the
    ///   skill-marker constructor; <see cref="ManaAbility.Kind"/> = <see cref="AbilityKind.Skill"/>.</item>
    ///
    ///   <item><b>Spells</b> — pay mana orbs from the <see cref="ManaBank"/>, declared as a WUBRG
    ///   <see cref="ManaRecipe"/>. Each color's gameplay identity is the eventual MTG-style pie
    ///   (W=heal/U=control/B=drain/R=damage/G=growth) — placeholders for now.</item>
    ///
    ///   <item><b>Items</b> — per-slot consumable stacks. Each instance carries its own
    ///   <see cref="ManaAbility.MaxStackSize"/>; mint per-slot via <see cref="NewPotion"/> so
    ///   stacks of the same item don't share charges. Restocked at vendor/alchemist.</item>
    /// </list>
    ///
    /// <para>Debug shortcut: <see cref="ManaBank.AllowAnyColor"/> lets 1–N orbs of ANY color pay
    /// any 1–N-cost Spell — for experimenting with the bar before color identity is locked.</para>
    /// </summary>
    public static class ManaAbilities
    {
        // ── Skills (free, reusable, cost a player's turn; locked for N turn-cycles after use) ──
        public static readonly ManaAbility Steal    = new ManaAbility("Steal",    _isSkill: true, cooldownTurns: 3);
        public static readonly ManaAbility Mug      = new ManaAbility("Mug",      _isSkill: true, cooldownTurns: 2);
        public static readonly ManaAbility Teleport = new ManaAbility("Teleport", _isSkill: true, cooldownTurns: 3);

        // ── Spells (mana-orb cost) ──
        // IMPORTANT: a ManaAbility must map 1:1 to a single SpellDefinition. AbilityBar.ResolveSpell
        // finds the spell by ability-reference and returns the FIRST SpellLibrary.All entry that
        // matches, so sharing one instance across SpellDefinitions silently resolves the wrong spell
        // (the old Heal→Sleep bug). Give every castable spell its OWN ability below — never reuse one.
        public static readonly ManaAbility Heal     = Spell("Heal",     (ManaType.White, 1));                            // (W)
        public static readonly ManaAbility Fireball = Spell("Fireball", (ManaType.Red, 2));                              // (R)(R)
        public static readonly ManaAbility Frost    = Spell("Frost",    (ManaType.Blue, 2));                             // (U)(U)
        public static readonly ManaAbility Bolt     = Spell("Bolt",     (ManaType.Red, 2), (ManaType.Blue, 1));          // (R)(R)(U)

        // Secondary spells — dedicated ability each (not yet on a HeroLoadout, but now uniquely
        // resolvable). Costs mirror game_bible.md §7.
        public static readonly ManaAbility Sleep     = Spell("Sleep",     (ManaType.White, 1));                          // (W)
        public static readonly ManaAbility Silence   = Spell("Silence",   (ManaType.White, 1));                          // (W)
        public static readonly ManaAbility Poison    = Spell("Poison",    (ManaType.Blue, 2));                           // (U)(U)
        public static readonly ManaAbility Slow      = Spell("Slow",      (ManaType.Blue, 2));                           // (U)(U)
        public static readonly ManaAbility MassHeal  = Spell("MassHeal",  (ManaType.White, 1));                          // (W)
        public static readonly ManaAbility Antidote  = Spell("Antidote",  (ManaType.White, 1));                          // (W)
        public static readonly ManaAbility Scan      = Spell("Scan",      (ManaType.White, 1));                          // (W)
        public static readonly ManaAbility Meteor    = Spell("Meteor",    (ManaType.Red, 2));                            // (R)(R)
        public static readonly ManaAbility ShockWave = Spell("ShockWave", (ManaType.Red, 2), (ManaType.Blue, 1));        // (R)(R)(U)
        public static readonly ManaAbility CrossHit  = Spell("CrossHit",  (ManaType.Red, 2), (ManaType.Blue, 1));        // (R)(R)(U)

        // ── Items (per-slot stack; vendor/alchemist restocks) ──
        /// <summary>Default 3-stack Potion template — useful for "vendor sells this" or debug.</summary>
        public static readonly ManaAbility Potion   = Item("Potion", maxStackSize: 3);

        /// <summary>Mint a fresh per-slot Potion stack with the given <paramref name="stackSize"/>.
        /// Each call returns a NEW instance so its <see cref="ManaAbility.Charges"/> counter is
        /// independent — two 5-stack slots give the hero 10 total uses across two slots, each
        /// draining on its own.</summary>
        public static ManaAbility NewPotion(int stackSize) => new ManaAbility("Potion", stackSize);

        /// <summary>Mint a fresh per-slot consumable bar entry linked to its ItemDefinition Id, so
        /// <c>AbilityBar.HandleItem</c> can route it through the item's <c>OnUseSpellName</c>
        /// (US-042, e.g. Sleep Dart → Sleep). Each call is a new instance with its own charges.</summary>
        public static ManaAbility NewConsumable(string displayName, int stackSize, string itemId) =>
            new ManaAbility(displayName, stackSize, itemId);

        // ── SIDE-NOTE: deferred ──
        // Ether — design idea: consumable that auto-grants mana orbs. Parked.

        /// <summary>The 6 bar slots in display order (slot 6 reserved — Ether candidate). Used as
        /// the default loadout for any character class without a per-class override in
        /// <see cref="HeroLoadouts"/>.</summary>
        public static readonly IReadOnlyList<ManaAbility> Slots = new[]
        {
            Heal, Fireball, Frost, Bolt, Potion, /* slot 6: reserved */ null
        };

        /// <summary>
        /// Cost-icon label for a tooltip/slot, e.g. "(R)(R)(U)" for Bolt, "3/3" for a Potion stack,
        /// or "Free" for a Skill. Uses MTG single-letter convention (W/U/B/R/G/C).
        /// </summary>
        public static string CostIcons(ManaAbility ability)
        {
            if (ability == null) return "";
            if (ability.Kind == AbilityKind.Skill) return "Free";
            if (ability.Kind == AbilityKind.Item)  return $"{ability.Charges}/{ability.MaxStackSize}";
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

        private static ManaAbility Item(string name, int maxStackSize) =>
            new ManaAbility(name, maxStackSize);
    }
}
