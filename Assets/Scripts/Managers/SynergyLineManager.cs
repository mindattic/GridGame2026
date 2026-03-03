// Assets/Scripts/Managers/SynergyLineManager.cs
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using g = Scripts.Helpers.GameHelper;
using Scripts.Factories;
using Scripts.Helpers;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Hub;
using Scripts.Instances;
using Scripts.Instances.Actor;
using Scripts.Instances.Board;
using Scripts.Instances.SynergyLine;
using Scripts.Inventory;
using Scripts.Libraries;
using Scripts.Models;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Managers
{
/// <summary>
/// SYNERGYLINEMANAGER - Manages synergy connection lines between allies.
/// 
/// PURPOSE:
/// Creates and tracks animated lines connecting allied actors that have
/// synergy bonuses. Lines show team coordination visually.
/// 
/// VISUAL APPEARANCE:
/// ```
/// [Hero A] ≈≈≈≈≈≈≈≈≈≈ [Hero B]
///              ↑
///     synergy line (animated)
/// ```
/// 
/// SYNERGY MECHANIC:
/// When allies are adjacent or share class/element bonuses,
/// synergy lines connect them to show the buff is active.
/// 
/// ORDER-INDEPENDENT KEYING:
/// Lines keyed by actor pair in order-independent manner:
/// (A, B) and (B, A) produce the same key to prevent duplicates.
/// 
/// LIFECYCLE:
/// 1. SynergySystem detects synergy between actors
/// 2. Spawn() creates animated line between them
/// 3. Line animates with particle effects
/// 4. Despawn() removes when synergy ends
/// 
/// RELATED FILES:
/// - SynergyLineFactory.cs: Creates line GameObjects
/// - SynergyLineInstance.cs: Line behavior/animation
/// - SynergyStrandFactory.cs: Creates strand particles
/// - SortingManager.cs: Handles line sorting
/// 
/// ACCESS: g.SynergyLineManager
/// </summary>
public class SynergyLineManager : MonoBehaviour
{
    /// <summary>Active synergy lines keyed by order-independent pair key.</summary>
    private readonly Dictionary<string, SynergyLineInstance> collection = new Dictionary<string, SynergyLineInstance>();

    /// <summary>
    /// Spawns a synergy line between two actors if not already exists.
    /// Order-independent: (A, B) equals (B, A).
    /// </summary>
    public void Spawn(ActorInstance supporter, ActorInstance attacker)
    {
        string key = GenerateKey(supporter, attacker);
        if (key == null) return;

        // Clean up stale entry
        if (collection.TryGetValue(key, out var existing))
        {
            if (existing == null) collection.Remove(key);
            else return; // already active
        }

        var go = SynergyLineFactory.Create(transform);
        go.name = key;

        var instance = go.GetComponent<SynergyLineInstance>();
        instance.Spawn(supporter, attacker);
        g.SortingManager.OnSynergyLineSpawn(instance);
        collection[key] = instance;
    }

    /// <summary>Removes synergy line between two actors.</summary>
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
    /// Waits until instance destroyed, then removes key.
    /// </summary>
    private IEnumerator DespawnRoutine(string key, SynergyLineInstance instance)
    {
        // Unity null check becomes true after Destroy
        while (instance != null) yield return null;
        collection.Remove(key);
    }

    /// <summary> clear..Groups[0].Value.ToUpper() lear.</summary>
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

}
