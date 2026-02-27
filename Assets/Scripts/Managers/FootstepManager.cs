using Assets.Helper;
using Assets.Scripts.Factories;
using Assets.Scripts.Libraries;
using Game.Behaviors.Actor;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using g = Assets.Helpers.GameHelper;

public class FootstepManager : MonoBehaviour
{
    [SerializeField] Transform target;
    Vector3 previousPosition;
    bool isRightFoot = false;
    float threshold;

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


    /// <summary>
    /// Stops playing footstep effects.
    /// </summary>
    public void Stop()
    {
        isRightFoot = false;
    }

    /// <summary>
    /// Checks the distance traveled by the actor to decide when to spawn footsteps.
    /// </summary>
    private IEnumerator CheckSpawnRoutine()
    {

        var distance = Vector3.Distance(target.position, previousPosition);
        if (distance >= threshold)
        {
            Spawn();
        }

        yield return Wait.None();

    }

    /// <summary>
    /// Spawns a single footstep instance at the actor's position.
    /// </summary>
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
    /// Clears all footstep objects from the scene without using tags.
    /// </summary>
    public void Clear()
    {
        var instances = GameObject.FindObjectsByType<FootstepInstance>(FindObjectsSortMode.None);
        foreach (var instance in instances)
        {
            Destroy(instance.gameObject);
        }
    }
}
