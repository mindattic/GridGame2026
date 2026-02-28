using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using g = Assets.Helpers.GameHelper;

/// <summary>
/// VISUALEFFECTMANAGER - Spawns and manages visual effects (VFX).
/// 
/// PURPOSE:
/// Central manager for spawning particle effects, impact effects, and other
/// visual feedback during combat and gameplay.
/// 
/// VFX SPAWNING:
/// - Spawn(): Fire-and-forget VFX at position
/// - PlayRoutine(): Spawn and yield until complete
/// - SpawnAt(): Spawn attached to an actor
/// 
/// VFX LIFECYCLE:
/// 1. CreateInstance() creates wrapper GameObject
/// 2. VisualEffectInstance component added
/// 3. Asset prefab instantiated
/// 4. Effect plays for asset.Duration
/// 5. Auto-destroys on completion
/// 
/// ASSET TYPES (VisualEffectAsset):
/// VFX defined in VisualEffectLibrary with:
/// - Name: Unique identifier
/// - Prefab: Particle system or effect prefab
/// - Duration: Lifetime in seconds
/// - Scale: Size multiplier
/// 
/// COMMON EFFECTS:
/// - Impact_Physical: Melee hit effect
/// - Impact_Magical: Spell hit effect
/// - Heal: Healing sparkles
/// - Death: Actor death effect
/// - Spawn: Actor spawn effect
/// 
/// OPTIONAL ROUTINES:
/// Spawn methods accept optional IEnumerator routines that run
/// after the VFX starts, useful for chaining effects.
/// 
/// RELATED FILES:
/// - VisualEffectLibrary.cs: Asset registry
/// - VisualEffectAsset.cs: Effect definition
/// - VisualEffectInstance.cs: Runtime effect component
/// - CombatTextManager.cs: Floating damage numbers
/// 
/// ACCESS: g.VisualEffectManager
/// </summary>
public class VisualEffectManager : MonoBehaviour
{
    #region Collection

    /// <summary>Active VFX instances keyed by unique name.</summary>
    private readonly Dictionary<string, VisualEffectInstance> collection = new Dictionary<string, VisualEffectInstance>();

    #endregion

    #region Instance Creation

    /// <summary>
    /// Creates a wrapper GameObject for a VFX instance.
    /// </summary>
    private VisualEffectInstance CreateInstance(VisualEffectAsset asset, Vector3 position, Transform parentOverride = null)
    {
        if (asset == null)
            return null;

        var go = new GameObject();
        string key = $"VFX_{asset.Name}_{Guid.NewGuid():N}";
        go.name = key;
        go.transform.position = position;

        Transform parent = parentOverride != null ? parentOverride : (g.Board != null ? g.Board.transform : null);
        if (parent != null)
            go.transform.SetParent(parent, worldPositionStays: true);

        var instance = go.AddComponent<VisualEffectInstance>();
        if (!collection.ContainsKey(key))
            collection.Add(key, instance);

        return instance;
    }

    #endregion

    #region Spawn Methods

    /// <summary>
    /// Fire-and-forget spawn at a world position.
    /// Optionally runs a routine after effect starts.
    /// </summary>
    public void Spawn(VisualEffectAsset asset, Vector3 position, IEnumerator routine = null)
    {
        var instance = CreateInstance(asset, position, null);
        if (instance == null)
            return;

        float tileSize = Mathf.Max(0.0001f, g.TileScale.x);
        instance.Spawn(asset, position, tileSize, routine);
    }

    /// <summary>
    /// Spawns VFX and yields until lifecycle finishes.
    /// Use when gameplay should wait for effect completion.
    /// </summary>
    public IEnumerator PlayRoutine(VisualEffectAsset asset, Vector3 position, IEnumerator routine = null)
    {
        var instance = CreateInstance(asset, position, null);
        if (instance == null)
            yield break;

        float tileSize = Mathf.Max(0.0001f, g.TileScale.x);
        yield return instance.SpawnRoutine(asset, position, tileSize, routine);

    #endregion
    }

    /// <summary>
    /// Spawn an instance and return it, while also providing a yieldable routine to wait for completion.
    /// </summary>
    public (VisualEffectInstance instance, IEnumerator routine) SpawnAndWait(VisualEffectAsset asset, Vector3 position, Transform parentOverride = null, IEnumerator routine = null)
    {
        var inst = CreateInstance(asset, position, parentOverride);
        if (inst == null)
            return (null, null);
        float tileSize = Mathf.Max(0.0001f, g.TileScale.x);
        return (inst, inst.SpawnRoutine(asset, position, tileSize, routine));
    }

    /// <summary>
    /// Spawns a VFX, parents it to the provided transform so it follows movement,
    /// starts its lifecycle, and returns the VFXInstance.
    /// </summary>
    public VisualEffectInstance SpawnInstance(VisualEffectAsset asset, Vector3 position, Transform parent, IEnumerator routine = null)
    {
        var instance = CreateInstance(asset, position, parent);
        if (instance == null)
            return null;

        float tileSize = Mathf.Max(0.0001f, g.TileScale.x);
        instance.Spawn(asset, position, tileSize, routine);
        return instance;
    }

    /// <summary>
    /// Destroys and unregisters a VFX instance by name.
    /// </summary>
    public void Despawn(string name)
    {
        if (!collection.TryGetValue(name, out var inst) || inst == null)
            return;

        Destroy(inst.gameObject);
        collection.Remove(name);
    }

    /// <summary>
    /// Destroys all VFX instances from the scene without using tags.
    /// </summary>
    public void Clear()
    {
        var instances = GameObject.FindObjectsByType<VisualEffectInstance>(FindObjectsSortMode.None);
        foreach (var instance in instances)
            Destroy(instance.gameObject);

        collection.Clear();
    }
}
