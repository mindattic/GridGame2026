using System.Collections.Generic;
using Assets.Helpers;

/// <summary>
/// HEROLOADOUT - Individual hero's equipment and skills.
/// 
/// PURPOSE:
/// Tracks equipped skills, consumable items, and equipment
/// for a single hero character.
/// 
/// SLOTS:
/// - Skills: Equipped active/passive abilities
/// - Items: Consumable items for battle
/// - Equipment: Weapons, armor, accessories
/// 
/// RELATED FILES:
/// - PartyLoadout: Groups all hero loadouts
/// - PlayerInventory.cs: Item ownership
/// - SkillDefinition.cs: Skill data
/// </summary>
public class HeroLoadout
{
    public CharacterClass CharacterClass;
    public List<SkillDefinition> Skills = new List<SkillDefinition>();
    public List<ItemDefinition> Items = new List<ItemDefinition>();
    public List<ItemDefinition> Equipment = new List<ItemDefinition>();
}

/// <summary>
/// PARTYLOADOUT - All hero loadouts for active party.
/// 
/// PURPOSE:
/// Groups HeroLoadout instances for each party member
/// and provides accessor methods.
/// </summary>
public class PartyLoadout
{
    public Dictionary<CharacterClass, HeroLoadout> HeroLoadouts = new Dictionary<CharacterClass, HeroLoadout>();

    /// <summary>Gets or creates the loadout for a hero.</summary>
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
