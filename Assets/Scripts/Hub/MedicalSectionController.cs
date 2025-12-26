using System.Collections.Generic;
using UnityEngine;
using Assets.Helpers; // CharacterClass
using Assets.Scripts.Libraries; // ActorLibrary

/// <summary>
/// MedicalSectionController provides healing and resurrection services for heroes.
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

    public void HealHero(CharacterClass hero)
    {
        heroHealth[hero] = GetMaxHP(hero);
    }

    public void HealAll()
    {
        var keys = new List<CharacterClass>(heroHealth.Keys);
        foreach (var k in keys) heroHealth[k] = GetMaxHP(k);
    }

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
