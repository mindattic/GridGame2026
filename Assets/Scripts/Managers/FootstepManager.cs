using Scripts.Helpers;
using Scripts.Factories;
using Scripts.Libraries;
using Scripts.Instances.Actor;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
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
using Scripts.Models;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Managers
{
/// <summary>
/// FOOTSTEPMANAGER - Spawns footstep effects during actor movement.
/// 
/// PURPOSE:
/// Creates footstep dust/trail effects as characters move across the board.
/// Alternates between left and right foot positions.
/// 
/// VISUAL APPEARANCE:
/// ```
/// [Actor moving right]
///   👣    👣    👣
///  left  right  left
/// ```
/// 
/// SPAWN LOGIC:
/// - Tracks target transform position each frame
/// - When distance > threshold, spawns footstep
/// - Alternates isRightFoot for left/right offset
/// 
/// THRESHOLD:
/// Default 0.01 units - very sensitive to movement.
/// 
/// RELATED FILES:
/// - FootstepFactory.cs: Creates footstep GameObjects
/// - FootstepInstance.cs: Footstep fade/animation
/// - ActorMovement.cs: Actor movement triggering footsteps
/// 
/// ACCESS: g.FootstepManager
/// </summary>
public class FootstepManager : MonoBehaviour
{
    #region Fields

    [SerializeField] Transform target;
    Vector3 previousPosition;
    bool isRightFoot = false;
    float threshold;

    #endregion

    #region Initialization

    public void Awake()
    {
        threshold = 0.01f;
    }

    private void Start()
    {
        if (target == null)
            return;

        previousPosition = target.position;
        StartCoroutine(CheckSpawnRoutine());
    }

    #endregion

    #region Public Methods

    /// <summary>Stops footstep spawning and resets foot alternation.</summary>
    public void Stop()
    {
        isRightFoot = false;
    }

    #endregion

    #region Spawn Logic

    /// <summary>Monitors movement and spawns footsteps when threshold exceeded.</summary>
    private IEnumerator CheckSpawnRoutine()
    {

        var distance = Vector3.Distance(target.position, previousPosition);
        if (distance >= threshold)
        {
            Spawn();
        }

        yield return Wait.None();

    }

    /// <summary>Spawns a single footstep at the current position.</summary>
    private void Spawn()
    {
        // Use factory instead of Instantiate(prefab)
        GameObject go = FootstepFactory.Create();
        go.transform.position = Vector2.zero;
        go.transform.rotation = Quaternion.identity;
        var instance = go.GetComponent<FootstepInstance>();
        instance.sprite = SpriteLibrary.Sprites["Footstep"];
        instance.name = $"Footstep_{Guid.NewGuid():N}";
        instance.parent = target.parent;
        instance.Spawn(target.position, RotationHelper.ByDirection(target.position, previousPosition), isRightFoot);
        previousPosition = target.position;
        isRightFoot = !isRightFoot;
    }

    /// <summary>
    /// Clears all footstep objects from the scene.
    /// </summary>
    public void Clear()
    {
        var instances = GameObject.FindObjectsByType<FootstepInstance>(FindObjectsSortMode.None);
        foreach (var instance in instances)
        {
            Destroy(instance.gameObject);
        }
    }

    #endregion
}

}
