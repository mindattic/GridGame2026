using Scripts.Helpers;
using Scripts.Factories;
using Scripts.Instances.Actor;
using System;
using System.Collections;
using UnityEngine;
using g = Scripts.Helpers.GameHelper;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Hub;
using Scripts.Instances;
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
/// GHOSTMANAGER - Creates ghost trail effects during hero movement.
/// 
/// PURPOSE:
/// When a hero is being dragged, spawns semi-transparent "ghost" copies
/// of the actor sprite that fade out, creating a motion trail effect.
/// 
/// VISUAL EFFECT:
/// ```
///        [Hero]  ← Current position
///      👻        ← Ghost (50% opacity)
///    👻          ← Ghost (30% opacity)
///  👻            ← Ghost (10% opacity, fading)
/// ```
/// 
/// SPAWN LOGIC:
/// - Tracks actor position each frame
/// - When distance > threshold (TileSize/12), spawns ghost
/// - Ghost fades out over time and self-destructs
/// 
/// USAGE:
/// ```csharp
/// g.GhostManager.Play(hero);   // Start trail
/// // ... player drags hero ...
/// g.GhostManager.Stop();       // Stop trail
/// ```
/// 
/// LIFECYCLE:
/// 1. Play(actor) starts the spawn coroutine
/// 2. CheckSpawnRoutine() monitors movement
/// 3. Spawn() creates ghost via GhostFactory
/// 4. Stop() ends the coroutine
/// 
/// RELATED FILES:
/// - GhostFactory.cs: Creates ghost GameObjects
/// - GhostInstance.cs: Ghost animation component
/// - InputManager.cs: Calls Play/Stop during drag
/// - SelectionManager.cs: Manages drag state
/// 
/// ACCESS: g.GhostManager
/// </summary>
public class GhostManager : MonoBehaviour
{
    #region Fields

    /// <summary>Actor being tracked for ghost spawning.</summary>
    ActorInstance actor;

    /// <summary>Minimum distance to travel before spawning ghost.</summary>
    float threshold;

    /// <summary>Last position where ghost was spawned.</summary>
    Vector3 previousPosition;

    #endregion

    #region Initialization

    /// <summary>Performs initial setup after all Awake calls complete.</summary>
    private void Start()
    {
        threshold = g.TileSize / 12;
    }

    #endregion

    #region Public Methods

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

    #endregion

    #region Spawn Logic

    /// <summary>
    /// Monitors actor movement and spawns ghosts when threshold exceeded.
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
    /// Creates a ghost trail instance at the actor's current position.
    /// </summary>
    private void Spawn()
    {
        // Use factory instead of Instantiate(prefab)
        var go = GhostFactory.Create();
        go.transform.position = Vector2.zero;
        go.transform.rotation = Quaternion.identity;
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

    #endregion
}

}
