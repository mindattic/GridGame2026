using System.Collections.Generic;
using UnityEngine;
using Scripts.Helpers;
using Scripts.Libraries;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Factories;
using Scripts.Instances;
using Scripts.Instances.Actor;
using Scripts.Instances.Board;
using Scripts.Instances.SynergyLine;
using Scripts.Inventory;
using Scripts.Managers;
using Scripts.Models;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Hub
{
/// <summary>
/// MEDICALSECTIONCONTROLLER - Hub healing/resurrection section.
/// 
/// PURPOSE:
/// Provides healing and resurrection services for heroes
/// in the Hub medical facility.
/// 
/// SERVICES:
/// - HealHero: Restore single hero to full HP
/// - HealAll: Restore all heroes to full HP
/// - Resurrect: Revive fallen hero
/// 
/// RELATED FILES:
/// - HubManager.cs: Hub scene controller
/// - ActorLibrary.cs: Hero stat lookup
/// </summary>
public class MedicalSectionController : MonoBehaviour
{
    private HubManager hub;
    private Dictionary<CharacterClass, float> heroHealth = new Dictionary<CharacterClass, float>();

    public void Initialize(HubManager hubManager)
    {
        hub = hubManager;
    }

    public void OnActivated() { }

    /// <summary>Restore hero to full HP.</summary>
    public void HealHero(CharacterClass hero)
    {
        heroHealth[hero] = GetMaxHP(hero);
    }

    /// <summary>Restore all heroes to full HP.</summary>
    public void HealAll()
    {
        var keys = new List<CharacterClass>(heroHealth.Keys);
        foreach (var k in keys) heroHealth[k] = GetMaxHP(k);
    }

    /// <summary>Revive fallen hero.</summary>
    public void Resurrect(CharacterClass hero)
    {
        if (!heroHealth.ContainsKey(hero)) return;
        if (heroHealth[hero] > 0f) return;
        heroHealth[hero] = GetMaxHP(hero);
    }

    private float GetMaxHP(CharacterClass hero)
    {
        var data = ActorLibrary.Get(hero);
        if (data == null || data.BaseStats == null) return 10f;
        return Mathf.Max(1f, data.BaseStats.Vitality * 5f);
    }
}

}
