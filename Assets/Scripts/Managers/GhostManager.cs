using Assets.Helper;
using Assets.Scripts.Libraries;
using Game.Behaviors.Actor;
using System;
using System.Collections;
using UnityEngine;
using g = Assets.Helpers.GameHelper;

public class GhostManager : MonoBehaviour
{
    // Fields
    private GameObject ghostPrefab;
    ActorInstance actor;
    float threshold;
    Vector3 previousPosition;

    public void Awake()
    {
        ghostPrefab = PrefabLibrary.Prefabs["GhostPrefab"];
    }

    private void Start()
    {
        threshold = g.TileSize / 12;
    }

    /// <summary>
    /// Starts spawning ghost trail effects for the given actor.
    /// </summary>
    public void Play(ActorInstance actor)
    {
        this.actor = actor;
        previousPosition = this.actor.Position;
        StartCoroutine(CheckSpawnRoutine());
    }

    /// <summary>
    /// Stops ghost trail spawning.
    /// </summary>
    public void Stop()
    {
        actor = null;
    }

    /// <summary>
    /// Checks the actor's movement to determine when to spawn ghost effects.
    /// </summary>
    private IEnumerator CheckSpawnRoutine()
    {
        while (actor != null && actor.IsActive && actor.IsAlive)
        {
            var distance = Vector3.Distance(actor.Position, previousPosition);
            if (distance >= threshold)
            {
                previousPosition = actor.Position;
                Spawn();
            }

            yield return Wait.None();
        }
    }

    /// <summary>
    /// Spawns a ghost trail instance at the actor's current position.
    /// </summary>
    private void Spawn()
    {
        var go = Instantiate(ghostPrefab, Vector2.zero, Quaternion.identity);
        var instance = go.GetComponent<GhostInstance>();
        instance.name = $"Ghost_{Guid.NewGuid():N}";
        instance.parent = g.Board.transform;
        instance.Spawn(actor);
    }

    /// <summary>
    /// Clears all ghost objects from the scene without using tags.
    /// </summary>
    public void Clear()
    {
        var instances = GameObject.FindObjectsByType<GhostInstance>(FindObjectsSortMode.None);
        foreach (var instance in instances)
        {
            Destroy(instance.gameObject);
        }
    }
}
