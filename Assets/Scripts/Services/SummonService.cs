using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Scripts.Helpers;
using Scripts.Inventory;
using Scripts.Models;

namespace Scripts.Services
{
    /// <summary>
    /// SUMMONSERVICE - Pure recruit rules for the Summon vendor (US-132 / GG-A5).
    ///
    /// <para>PURPOSE: The BRAIN for roster growth — which classes are summonable, what the next
    /// recruit costs, and the save mutation for a recruit. No scene access, no g. switchboard,
    /// so the rules unit-test without a scene (same pattern as PincerDetector/EnemyPlanner).</para>
    ///
    /// <para>RULES: a fixed summonable pool (the built hero classes beyond the starting trio);
    /// deliberate gold purchase, never a random pull (GG's "not a gacha" pillar — RFC 0002 owns
    /// any V2 loosening). Cost rises with each hero beyond the starting three, so the roster is
    /// a long-term gold sink alongside gear.</para>
    ///
    /// <para>RELATED FILES: SummonManager.cs, SummonBuilder.cs, ProfileHelper.cs (DefaultRoster
    /// = the starting trio), docs/AMENDMENTS.md GG-A5.</para>
    /// </summary>
    public static class SummonService
    {
        public const int BaseCost = 250;
        public const int CostPerRecruit = 250;
        private const int StartingRosterSize = 3;

        /// <summary>Every class the Summon Circle offers, in display order.</summary>
        public static readonly IReadOnlyList<CharacterClass> Pool = new List<CharacterClass>
        {
            CharacterClass.GreenNinja,
            CharacterClass.RedNinja,
            CharacterClass.Pugilist,
            CharacterClass.Ronin,
            CharacterClass.Sellsword,
            CharacterClass.Thief,
            CharacterClass.Vampire,
        };

        /// <summary>True when the class is already on the save's roster.</summary>
        public static bool IsOwned(SaveState save, CharacterClass characterClass)
            => save?.Roster?.Members != null
               && save.Roster.Members.Any(m => m.CharacterClass == characterClass);

        /// <summary>Cost of the NEXT recruit: rises with every hero recruited past the trio.</summary>
        public static int NextCost(SaveState save)
        {
            int rosterCount = save?.Roster?.Members?.Count ?? StartingRosterSize;
            int recruited = Mathf.Max(0, rosterCount - StartingRosterSize);
            return BaseCost + CostPerRecruit * recruited;
        }

        /// <summary>
        /// Recruits <paramref name="characterClass"/>: deducts gold from <paramref name="inventory"/>
        /// and appends the class to the save's roster at zero XP. Refuses (false, no mutation) when
        /// the class isn't in the pool, is already owned, or gold is short. Caller persists.
        /// </summary>
        public static bool TryRecruit(SaveState save, PlayerInventory inventory, CharacterClass characterClass)
        {
            if (save?.Roster?.Members == null || inventory == null) return false;
            if (!Pool.Contains(characterClass)) return false;
            if (IsOwned(save, characterClass)) return false;

            int cost = NextCost(save);
            if (inventory.Gold < cost) return false;

            inventory.Gold -= cost;
            save.Roster.Members.Add(new CharacterLevelPair(characterClass));
            return true;
        }
    }
}
