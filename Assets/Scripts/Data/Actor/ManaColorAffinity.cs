using Scripts.Helpers;
using Scripts.Models;

namespace Scripts.Data.Actor
{
    /// <summary>
    /// MANACOLORAFFINITY - Per-class WUBRG mana affinity: the orb color a hero mints when it
    /// contributes to a pincer (US-030), replacing the V1 all-Blue placeholder. So the team bank's
    /// color profile reflects party composition (§23.2.1).
    ///
    /// <para>Mapping resolved from game_bible.md §23.2 + the Legion panel (2026-06-02): the five
    /// unambiguous classes plus Paladin=White (anchored by the §23.2.1 "3 Paladins = W/W/W" example)
    /// and Alchemist=Green ("never runs out of resources" = Green ramp/economy). Unlisted classes
    /// (enemies, future heroes) default to Blue until assigned.</para>
    /// </summary>
    public static class ManaColorAffinity
    {
        public static ManaType For(CharacterClass cls)
        {
            switch (cls)
            {
                case CharacterClass.Cleric:     return ManaType.White;
                case CharacterClass.Paladin:    return ManaType.White;
                case CharacterClass.Barbarian:  return ManaType.Red;
                case CharacterClass.Alchemist:  return ManaType.Green;
                case CharacterClass.Assassain:  return ManaType.Black;
                case CharacterClass.GreenNinja: return ManaType.Green;
                case CharacterClass.RedNinja:   return ManaType.Red;
                default:                        return ManaType.Blue;
            }
        }
    }
}
