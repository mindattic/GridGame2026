using System.Collections.Generic;
using Assets.Helpers; // CharacterClass enum

/// <summary>
/// HeroLoadout tracks equipped skills, items, and equipment for a single hero.
/// </summary>
public class HeroLoadout
{
    public CharacterClass CharacterClass;
    public List<SkillDefinition> Skills = new List<SkillDefinition>();
    public List<ItemDefinition> Items = new List<ItemDefinition>();
    public List<ItemDefinition> Equipment = new List<ItemDefinition>();
}

/// <summary>
/// PartyLoadout groups all hero loadouts for the active party and provides helper accessors.
/// </summary>
public class PartyLoadout
{
    public Dictionary<CharacterClass, HeroLoadout> HeroLoadouts = new Dictionary<CharacterClass, HeroLoadout>();

    /// <summary>
    /// Gets or creates the loadout for a hero.
    /// </summary>
    public HeroLoadout Get(CharacterClass hero)
    {
        if (!HeroLoadouts.TryGetValue(hero, out var loadout))
        {
            loadout = new HeroLoadout { CharacterClass = hero };
            HeroLoadouts[hero] = loadout;
        }
        return loadout;
    }
}
