using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Assets.Helpers; // CharacterClass
using Assets.Scripts.Libraries; // ActorLibrary

/// <summary>
/// PartySectionController owns the party selection portion of the Hub.
/// It exposes integration points for the existing PartyManager and provides a loadout layer
/// for items and skills so combat scenes can query equipped configurations.
/// </summary>
public class PartySectionController : MonoBehaviour
{
    private HubManager hub;

    // Simple UI placeholders (assigned by editor builder or manually).
    public RectTransform rosterContainer; // heroes not in party
    public RectTransform partyContainer;  // active party heroes
    public RectTransform detailContainer; // stat + equipment/skill detail

    // Local model for hero loadouts.
    public PartyLoadout partyLoadout = new PartyLoadout();

    /// <summary>
    /// Initializes this controller for the owning HubManager.
    /// </summary>
    public void Initialize(HubManager hubManager)
    {
        hub = hubManager;
        // TODO: Load current party from PartyManager or save system.
    }

    /// <summary>
    /// Called when this section becomes active. Refreshes UI and models.
    /// </summary>
    public void OnActivated()
    {
        // TODO: Refresh roster, party list, and selected hero detail.
    }

    /// <summary>
    /// Sets party composition using external data (e.g. PartyManager export).
    /// </summary>
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
