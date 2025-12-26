// Assets/Scripts/Instances/SynergyLineManager.cs
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using g = Assets.Helpers.GameHelper;
using Assets.Scripts.Libraries;
using Assets.Helpers;


/// <summary>
/// Spawns and tracks SynergyLine instances between two ActorInstances.
/// Prevents duplicates, including reverse-order duplicates (A,B) vs (B,A).
/// </summary>
public class SynergyLineManager : MonoBehaviour
{
    private GameObject synergyLinePrefab;

    // Active lines keyed by an order-independent pair key
    private readonly Dictionary<string, SynergyLineInstance> collection = new Dictionary<string, SynergyLineInstance>();

    private void Awake()
    {
        synergyLinePrefab = PrefabLibrary.Prefabs["SynergyLinePrefab"];
    }

    /// <summary>
    /// Spawns a synergy instance between the two actors if one does not already exist.
    /// Order-independent: (supporter, attacker) is treated the same as (attacker, supporter).
    /// </summary>
    public void Spawn(ActorInstance supporter, ActorInstance attacker)
    {
        string key = GenerateKey(supporter, attacker);
        if (key == null) return;

        // Clean up stale entry (e.g., instance destroyed externally).
        if (collection.TryGetValue(key, out var existing))
        {
            if (existing == null) collection.Remove(key);
            else return; // already active
        }

        var go = Instantiate(synergyLinePrefab, transform);
        go.name = key;

        var instance = go.GetComponent<SynergyLineInstance>();
        instance.Spawn(supporter, attacker);
        g.SortingManager.OnSynergyLineSpawn(instance);
        collection[key] = instance;
    }

    public void Despawn(ActorInstance a, ActorInstance b)
    {
        string key = GenerateKey(a, b);
        if (key == null) return;

        if (collection.TryGetValue(key, out var instance) && instance != null)
        {
            instance.Despawn();
            StartCoroutine(DespawnRoutine(key, instance));
        }
    }

    /// <summary>
    /// Waits until the instance is actually destroyed, then removes the key.
    /// </summary>
    private IEnumerator DespawnRoutine(string key, SynergyLineInstance instance)
    {
        // Unity null check becomes true after Destroy
        while (instance != null) yield return null;
        collection.Remove(key);
    }

    public void Clear()
    {
        // Make a copy to avoid modifying the dictionary while iterating
        var items = new List<KeyValuePair<string, SynergyLineInstance>>(collection);

        foreach (var kv in items)
        {
            var key = kv.Key;
            var instance = kv.Value;

            if (instance != null)
            {
                instance.Despawn();
                StartCoroutine(DespawnRoutine(key, instance));
            }
            else
            {
                collection.Remove(key);
            }
        }
    }

    /// <summary>
    /// Builds an order-independent key from two actors based on reference identity.
    /// Ensures (A,B) and (B,A) produce the same key.
    /// </summary>
    private static string GenerateKey(ActorInstance a, ActorInstance b)
    {
        if (a == null || b == null) return null;

        CharacterClass na = a.characterClass;
        CharacterClass nb = b.characterClass;

        // Order independent by sorting the names
        bool aFirst = string.CompareOrdinal(na.ToString(), nb.ToString()) <= 0;
        string first = aFirst ? na.ToString() : nb.ToString();
        string second = aFirst ? nb.ToString() : na.ToString();

        return $"SynergyLine_{first}{second}";
    }
}
