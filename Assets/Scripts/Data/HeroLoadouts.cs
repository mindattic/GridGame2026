using System.Collections.Generic;
using Scripts.Helpers;
using Scripts.Models;

namespace Scripts.Data
{
    /// <summary>
    /// HEROLOADOUTS - Per-character-class default ability bar (6 slots).
    ///
    /// <para>The 6-slot Row-13 ability bar follows the currently selected hero; this lookup
    /// returns that hero's loadout. Mana orbs themselves are <b>party-wide</b> (the team shares
    /// one <see cref="ManaBank"/>), so different heroes can have different spells but they all
    /// draw from the same orb line.</para>
    ///
    /// <para>V1: every class falls through to <see cref="ManaAbilities.Slots"/> (a uniform
    /// default). Add a per-class entry to <see cref="perClass"/> to give a hero a distinct
    /// 6-slot loadout. Long-term, this will be sourced from
    /// <c>HeroEquipmentSave.AbilityBarSlots</c> (the player-assigned bar saved per-hero).</para>
    /// </summary>
    public static class HeroLoadouts
    {
        /// <summary>The 6-slot ability loadout for the given character class.</summary>
        public static IReadOnlyList<ManaAbility> For(CharacterClass characterClass)
        {
            if (perClass.TryGetValue(characterClass, out var list) && list != null) return list;
            return ManaAbilities.Slots;
        }

        /// <summary>Install or replace the per-class loadout at runtime (Debug Window's random-abilities button uses this).</summary>
        public static void Set(CharacterClass characterClass, IReadOnlyList<ManaAbility> loadout)
        {
            perClass[characterClass] = loadout;
        }

        /// <summary>Per-class overrides — gives each common class a distinct identity bar.
        /// Classes not listed fall through to <see cref="ManaAbilities.Slots"/>. Direct mutation
        /// is discouraged; use <see cref="Set"/> instead.</summary>
        private static readonly Dictionary<CharacterClass, IReadOnlyList<ManaAbility>> perClass =
            new Dictionary<CharacterClass, IReadOnlyList<ManaAbility>>
            {
                // Per-slot Item instances via NewPotion(stackSize) so each slot's charges are
                // independent — Cleric carries 3, Paladin 3, Alchemist 5+5 (10 total uses in 2
                // slots), Assassain 3.
                { CharacterClass.Cleric,    new [] { ManaAbilities.Heal,    ManaAbilities.Heal,     ManaAbilities.Frost,    ManaAbilities.NewPotion(3), null, null } },
                { CharacterClass.Paladin,   new [] { ManaAbilities.Heal,    ManaAbilities.Fireball, ManaAbilities.NewPotion(3), null, null, null } },
                { CharacterClass.Barbarian, new [] { ManaAbilities.Fireball,ManaAbilities.Bolt,     ManaAbilities.NewPotion(3), null, null, null } },
                { CharacterClass.Alchemist, new [] { ManaAbilities.Frost,   ManaAbilities.NewPotion(5), ManaAbilities.Steal,ManaAbilities.Heal, ManaAbilities.NewPotion(5), ManaAbilities.NewConsumable("Sleep Dart", 5, Scripts.Data.Items.ItemData_Consumables.SleepDart.Id) } },
                { CharacterClass.Assassain,  new [] { ManaAbilities.Steal,    ManaAbilities.Mug,      ManaAbilities.Bolt,         ManaAbilities.NewPotion(3), null, null } },
                // Ninja classes get Teleport — pick an empty tile, instantly relocate, and any
                // pincer the new position completes resolves automatically. Free, costs a turn.
                { CharacterClass.GreenNinja, new [] { ManaAbilities.Teleport, ManaAbilities.Steal,    ManaAbilities.Fireball,     ManaAbilities.NewPotion(3), null, null } },
                { CharacterClass.RedNinja,   new [] { ManaAbilities.Teleport, ManaAbilities.Mug,      ManaAbilities.Bolt,         ManaAbilities.NewPotion(3), null, null } },
            };
    }
}
