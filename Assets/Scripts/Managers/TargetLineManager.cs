using Assets.Scripts.Factories;
using System;
using System.Linq;
using UnityEngine;
using g = Assets.Helpers.GameHelper;

/// <summary>
/// TARGETLINEMANAGER - Manages ability targeting line UI.
/// 
/// PURPOSE:
/// Creates and manages the targeting line shown when player is selecting
/// a target for an ability. Line connects ability button to cursor/target.
/// 
/// VISUAL APPEARANCE:
/// ```
/// [Ability Button] ─────────────► [Target Hero]
///                     ↑
///              targeting line
/// ```
/// 
/// TARGETING FLOW:
/// 1. Player clicks ability button
/// 2. BeginTargeting() switches to AnyTarget input mode
/// 3. TargetLine renders from button to cursor
/// 4. OnTargetTouch() snaps line to valid targets
/// 5. OnTargetConfirmed() fires callback with selected target
/// 6. EndTargeting() cleans up line
/// 
/// SNAP BEHAVIOR:
/// - lockRadius: Distance for auto-snap to valid targets
/// - SnapToTarget(): Moves line endpoint to actor position
/// 
/// RELATED FILES:
/// - TargetLineFactory.cs: Creates line GameObjects
/// - TargetLineInstance.cs: Line rendering component
/// - AbilityManager.cs: Manages ability targeting state
/// - AbilityButtonManager.cs: Triggers targeting mode
/// 
/// ACCESS: g.TargetLineManager
/// </summary>
public class TargetLineManager : MonoBehaviour
{
    #region Fields

    private Camera mainCamera;
    private float lockRadius;

    private ActorInstance hoveredTarget;
    private Vector3 buttonOrigin;
    private Action<ActorInstance> onTargetConfirmed;
    private TargetLineInstance instance;
    private ActorInstance lastClicked;

    #endregion

    #region Initialization

    private void Awake()
    {
        mainCamera = Camera.main;
        lockRadius = g.TileSize / 2f;
    }

    #endregion

    #region Targeting Flow

    /// <summary>
    /// Begins ability targeting mode. Spawns line from button position.
    /// </summary>
    public void BeginTargeting(Vector3 fromWorldPosition, Action<ActorInstance> onConfirmed)
    {
        // 1) switch global input mode
        g.InputManager.InputMode = InputMode.AnyTarget;

        // 2) store callback & origin
        buttonOrigin = fromWorldPosition;
        onTargetConfirmed = onConfirmed;
        lastClicked = null;

        // 3) create targeting line
        var go = TargetLineFactory.Create();
        go.transform.position = Vector3.zero;
        go.transform.rotation = Quaternion.identity;
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
    /// Called when player taps an actor while in targeting mode.
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

    #endregion
}
