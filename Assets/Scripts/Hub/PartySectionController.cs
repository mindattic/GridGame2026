using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Assets.Helpers;
using Assets.Scripts.Libraries;

/// <summary>
/// PARTYSECTIONCONTROLLER - Hub party management section.
/// 
/// PURPOSE:
/// Manages the party selection UI in the Hub, allowing players
/// to add/remove heroes and manage their loadouts.
/// 
/// FEATURES:
/// - View roster of available heroes
/// - Add/remove heroes from active party
/// - View hero stats and details
/// - Manage equipment and skills per hero
/// 
/// UI CONTAINERS:
/// - rosterContainer: Heroes not in party
/// - partyContainer: Active party members
/// - detailContainer: Selected hero details
/// 
/// RELATED FILES:
/// - HubManager.cs: Hub scene controller
/// - PartyManager.cs: Battle party data
/// - HeroLoadout.cs: Hero equipment/skills
/// </summary>
public class PartySectionController : MonoBehaviour
{
    private HubManager hub;

    public RectTransform rosterContainer;
    public RectTransform partyContainer;
    public RectTransform detailContainer;

    public PartyLoadout partyLoadout = new PartyLoadout();

    /// <summary>Initializes this controller.</summary>
    public void Initialize(HubManager hubManager)
    {
        hub = hubManager;
    }

    /// <summary>Called when section becomes active.</summary>
    public void OnActivated()
    {
        // TODO: Refresh roster and party UI
    }

    /// <summary>Sets party from external data.</summary>
    public void SetPartyFromExistingManager(IEnumerable<CharacterClass> classes)
    {
        partyLoadout.HeroLoadouts.Clear();
        if (classes == null) return;
        foreach (var c in classes)
        {
            if (!partyLoadout.HeroLoadouts.ContainsKey(c))
                partyLoadout.HeroLoadouts[c] = new HeroLoadout { CharacterClass = c };
        }
    }

    /// <summary>
    /// Export equipped party data for combat startup.
    /// </summary>
    public PartyLoadout ExportPartyToGameScene()
    {
        // TODO: Persist or hand off to combat initialization pipeline.
        return partyLoadout;
    }

    /// <summary>
    /// Equip a skill to a hero if allowed by tags and constraints.
    /// </summary>
    public bool EquipSkill(CharacterClass hero, SkillDefinition skill)
    {
        if (skill == null) return false;
        if (!partyLoadout.HeroLoadouts.TryGetValue(hero, out var loadout)) return false;
        if (!SkillLibrary.CanHeroEquip(hero, skill)) return false;
        if (!loadout.Skills.Contains(skill)) loadout.Skills.Add(skill);
        return true;
    }

    /// <summary>
    /// Equip an item to a hero (e.g. potion for battle use).
    /// </summary>
    public bool EquipItem(CharacterClass hero, ItemDefinition item)
    {
        if (item == null) return false;
        if (!partyLoadout.HeroLoadouts.TryGetValue(hero, out var loadout)) return false;
        if (!loadout.Items.Contains(item)) loadout.Items.Add(item);
        return true;
    }
}
