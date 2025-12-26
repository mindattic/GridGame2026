using Assets.Scripts.Libraries;
using System;
using System.Linq;
using UnityEngine;
using g = Assets.Helpers.GameHelper;

public class TargetLineManager : MonoBehaviour
{
    private Camera mainCamera;
    private TargetLineInstance targetLinePrefab;
    private float lockRadius;

    private ActorInstance hoveredTarget;
    private Vector3 buttonOrigin;
    private Action<ActorInstance> onTargetConfirmed;
    private TargetLineInstance instance;
    private ActorInstance lastClicked;

    private void Awake()
    {
        mainCamera = Camera.main;
        lockRadius = g.TileSize / 2f;

        if (!PrefabLibrary.Prefabs.TryGetValue("TargetLinePrefab", out var prefabGO))
            Debug.LogError("TargetLinePrefab not found in PrefabRepo.");
        else if ((targetLinePrefab = prefabGO.GetComponent<TargetLineInstance>()) == null)
            Debug.LogError("TargetLinePrefab is missing TargetLineInstance.");
    }

    /// <summary>
    /// Called when an ability button is clicked.
    /// Switches into AbilityTarget mode, spawns the line, and snaps to a random actor.
    /// </summary>
    public void BeginTargeting(Vector3 fromWorldPosition, Action<ActorInstance> onConfirmed)
    {
        // 1) switch global input mode
        g.InputManager.InputMode = InputMode.AnyTarget;

        // 2) store callback & origin
        buttonOrigin = fromWorldPosition;
        onTargetConfirmed = onConfirmed;
        lastClicked = null;

        // 3) instantiate line
        var go = Instantiate(targetLinePrefab.gameObject, Vector3.zero, Quaternion.identity);
        instance = go.GetComponent<TargetLineInstance>();
        instance.name = $"TargetLine_{Guid.NewGuid():N}";
        instance.parent = g.Board.transform;
        instance.buttonPosition = buttonOrigin;
        instance.cursorPosition = buttonOrigin;

        // 4) snap to a random actor initially
        if (g.Actors.Heroes.Any())
        {
            var randomHero = RNG.Hero;
            SnapToTarget(randomHero);
        }
    }

    /// <summary>
    /// Call when the player taps a actor while in AbilityTarget mode.
    /// </summary>
    public void OnTargetTouch(ActorInstance hero)
    {
        if (g.InputManager.InputMode != InputMode.AnyTarget)
            return;

        if (hero == lastClicked)
        {
            // double-click: confirm
            onTargetConfirmed?.Invoke(hero);
            EndTargeting();
        }
        else
        {
            // first click: just snap
            SnapToTarget(hero);
            lastClicked = hero;
        }
    }

    private void SnapToTarget(ActorInstance actor)
    {
        hoveredTarget = actor;
        instance.cursorPosition = actor.Position;
        instance.UpdateArcPoints(buttonOrigin, actor.Position);
    }

    private void EndTargeting()
    {
        // cleanup line
        if (instance != null)
        {
            instance.Despawn();
            Destroy(instance.gameObject);
            instance = null;
        }
        onTargetConfirmed = null;
        lastClicked = null;

        // restore normal input
        g.InputManager.InputMode = InputMode.PlayerTurn;
    }
}
