using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using g = Assets.Helpers.GameHelper;

public class VisualEffectManager : MonoBehaviour
{
    // Holds active VFX instances by unique name.
    private readonly Dictionary<string, VisualEffectInstance> collection = new Dictionary<string, VisualEffectInstance>();

    /// <summary>
    /// Creates a wrapper GameObject that hosts a VFXInstance. The VFXInstance will instantiate the asset prefab itself.
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

    /// <summary>
    /// Fire-and-forget spawn at a world position. Optionally runs a routine after its own sequence.
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
    /// Spawns a VFX and yields until its lifecycle finishes (including optional routine and duration).
    /// Use this when subsequent gameplay should wait for the impact to complete.
    /// </summary>
    public IEnumerator PlayRoutine(VisualEffectAsset asset, Vector3 position, IEnumerator routine = null)
    {
        var instance = CreateInstance(asset, position, null);
        if (instance == null)
            yield break;

        float tileSize = Mathf.Max(0.0001f, g.TileScale.x);
        yield return instance.SpawnRoutine(asset, position, tileSize, routine);
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
