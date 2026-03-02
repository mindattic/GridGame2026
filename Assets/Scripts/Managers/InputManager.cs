// --- File: Assets/Scripts/Managers/InputManager.cs ---
using Scripts.Helpers;
using Scripts.Sequences;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using g = Scripts.Helpers.GameHelper;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Factories;
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
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Managers
{
/// <summary>
/// INPUTMANAGER - Handles all touch/mouse input for hero control.
/// 
/// PURPOSE:
/// Central input handler for the Game scene. Processes touch/mouse events
/// and translates them into hero selection, dragging, and placement.
/// 
/// INPUT MODES (InputMode enum):
/// - PlayerTurn: Normal hero control (select, drag, drop)
/// - EnemyTurn: Limited input (dodge/parry timing windows)
/// - Paused: All input blocked
/// - AbilityTargeting: Selecting ability targets
/// 
/// HERO DRAG FLOW:
/// 1. Touch down on hero → Select hero, begin potential drag
/// 2. Move beyond dragThreshold → Enter dragging state
/// 3. Ghost preview shows potential drop location
/// 4. Touch up → Drop hero, check for pincer attacks
/// 
/// DODGE/PARRY SYSTEM:
/// During EnemyTurn, tapping a hero within timing windows triggers:
/// - OnDodge: Tap within DodgeWindowSeconds of enemy attack
/// - OnParry: Tap within ParryWindowSeconds (tighter window)
/// 
/// KEY PROPERTIES:
/// - InputMode: Current input state (PlayerTurn, EnemyTurn, etc.)
/// - isDragging: True if a hero is being dragged
/// - dragThreshold: Minimum movement to trigger drag
/// 
/// EVENTS:
/// - OnInputModeChanged: Fired when InputMode changes
/// - OnDodge: Fired on successful dodge timing
/// - OnParry: Fired on successful parry timing
/// 
/// INPUT GATING:
/// - RequireTouchRelease(): Blocks input until touch released
/// - Prevents accidental double-drops
/// 
/// RELATED FILES:
/// - SelectionManager.cs: Tracks selected hero
/// - GhostManager.cs: Drag preview visualization
/// - PincerAttackManager.cs: Called after hero drop
/// - TurnManager.cs: Determines if hero input allowed
/// 
/// ACCESS: g.InputManager
/// </summary>
public class InputManager : MonoBehaviour
{
    #region Fields

    private Vector3 initialTouchPosition;

    /// <summary>Minimum drag distance to trigger hero movement.</summary>
    public float dragThreshold;

    #endregion

    #region Input Mode

    /// <summary>Raised when input mode changes.</summary>
    public event Action<InputMode> OnInputModeChanged;

    private InputMode inputMode = InputMode.PlayerTurn;

    // Ability user cache (delegated to AbilityManager)
    private ActorInstance pendingAbilityUser => g.AbilityManager != null ? g.AbilityManager.PendingAbilityUser : null;

    /// <summary>
    /// Current input mode. Setting this fires OnInputModeChanged.
    /// </summary>
    public InputMode InputMode
    {
        get => inputMode;
        set
        {
            if (inputMode == value) return;
            inputMode = value;
            OnInputModeChanged?.Invoke(inputMode);
        }
    }

    #endregion

    #region Dodge/Parry System

    /// <summary>Fired on successful dodge timing during EnemyTurn.</summary>
    public event Action OnDodge;

    /// <summary>Fired on successful parry timing during EnemyTurn.</summary>
    public event Action OnParry;

    private const float DodgeWindowSeconds = 0.20f;
    private const float ParryWindowSeconds = 0.10f;

    private float lastEnemyTurnTapTime = -999f;
    private ActorInstance lastEnemyTurnTappedHero = null;

    #endregion

    #region Drag State

    /// <summary>True if the selected hero is currently being dragged.</summary>
    public bool isDragging => g.Actors.HasMovingHero && g.Actors.MovingHero.Flags.IsMoving;

    // Gate input until current press is released
    private bool requireTouchRelease;

    /// <summary>
    /// Blocks input until all touches/mouse buttons are released.
    /// Used to prevent accidental double-drops.
    /// </summary>
    public void RequireTouchRelease() { requireTouchRelease = true; }

    #endregion

    private static bool AnyPointerDown() => Input.touchCount > 0 || Input.GetMouseButton(0);

    private void Awake()
    {
        dragThreshold = GameManager.instance.tileSize * 0.01f;
    }

    /// <summary>
    /// Prepare a user for a targeting flow.
    /// </summary>
    public void BeginAbilityTargeting(ActorInstance user) => g.AbilityManager?.BeginAbilityTargeting(user);

    /// <summary>
    /// Show the global Cancel button (if found).
    /// </summary>
    public void ShowCancelButton() => g.AbilityManager?.ShowCancelButton();

    /// <summary>
    /// Hide the global Cancel button (if found).
    /// </summary>
    public void HideCancelButton() => g.AbilityManager?.HideCancelButton();

    /// <summary>
    /// Bind this to the Canvas/CancelButton OnClick. Cancels targeting and returns to PlayerTurn.
    /// </summary>
    public void OnCancelButtonClickedEvent()
    {
        // Also hide the TitleBar explicitly
        g.AbilityCastConfirm?.ClearTitle();
        g.AbilityManager?.OnCancelButtonClickedEvent();
    }

    /// <summary>
    /// Ability targeting flow. Tap toggles selection; CastButton confirms.
    /// </summary>
    private void UpdateAbilityTarget(Touch touch) => g.AbilityManager?.UpdateAbilityTarget(touch);

    // Linear target: select an enemy in same row/column with clear line; move hero and bump
    private void UpdateLinearTarget(Touch touch) => g.AbilityManager?.UpdateLinearTarget(touch);

    /// <summary>
    /// Player turn flow. Focus on touch, drag past threshold, drop on release.
    /// </summary>
    private void UpdatePlayerTurn(Touch touch)
    {
        switch (touch.phase)
        {
            case TouchPhase.Began:
                g.SelectionManager.Select();
                initialTouchPosition = g.TouchPosition3D;
                break;

            case TouchPhase.Moved:
                if (Vector3.Distance(initialTouchPosition, g.TouchPosition3D) > dragThreshold)
                {
                    // Prevent drag if selected hero is rooted (futureproof if heroes can be rooted)
                    if (g.Actors.HasMovingHero && g.Actors.MovingHero.Flags.RootedTurnsRemaining > 0)
                        return;
                    g.SelectionManager.Drag();
                }
                break;

            case TouchPhase.Ended:
            case TouchPhase.Canceled:
                g.SelectionManager.Drop();
                break;
        }
    }

    /// <summary>
    /// Enemy turn flow. Tapping a hero triggers a brief dodge animation and opens timing windows.
    /// Parry window is the first half; Dodge window is the full duration.
    /// </summary>
    private void UpdateEnemyTurn(Touch touch)
    {
        if (touch.phase == TouchPhase.Began)
        {
            var actor = TouchHelper.GetActorAtTouchPosition();
            if (actor != null && actor.IsPlaying && actor.IsHero)
            {
                actor.Animation.Dodge();

                lastEnemyTurnTapTime = Time.time;
                lastEnemyTurnTappedHero = actor;
            }
        }
    }

    /// <summary>
    /// Called by combat at the exact enemy impact frame.
    /// Evaluates timing against Parry first, then Dodge, and raises the corresponding event.
    /// </summary>
    public void OnEnemyAttackOccurred()
    {
        if (inputMode != InputMode.EnemyTurn) return;

        var dt = Time.time - lastEnemyTurnTapTime;

        if (dt >= 0f && dt <= ParryWindowSeconds)
        {
            OnParry?.Invoke();
            lastEnemyTurnTapTime = -999f;
            return;
        }

        if (dt >= 0f && dt <= DodgeWindowSeconds)
        {
            OnDodge?.Invoke();
            lastEnemyTurnTapTime = -999f;
            return;
        }
    }

    private void Update()
    {
        if (GameManager.instance.inputManager == null) return;
        if (g.PauseMenu.IsPaused) return;

        // Block all input while sequences are executing
        if (g.SequenceManager != null && g.SequenceManager.IsExecuting)
            return;

        if (requireTouchRelease)
        {
            if (!AnyPointerDown()) requireTouchRelease = false;
            return;
        }

        if (Input.touchCount > 0)
        {
            var touch = Input.GetTouch(0);

            switch (InputMode)
            {
                case InputMode.AnyTarget:   UpdateAbilityTarget(touch); break;
                case InputMode.LinearTarget: UpdateLinearTarget(touch);  break;
                case InputMode.PlayerTurn:   UpdatePlayerTurn(touch);    break;
                case InputMode.EnemyTurn:    UpdateEnemyTurn(touch);     break;
            }
        }
        else
        {
            switch (InputMode)
            {
                case InputMode.AnyTarget:
                    if (Input.GetMouseButtonDown(0))
                    {
                        var target = TouchHelper.GetActorAtTouchPosition();
                        if (target != null && target.IsPlaying)
                            g.AbilityManager?.ToggleTarget(target);
                    }
                    break;
                case InputMode.LinearTarget:
                    if (Input.GetMouseButtonDown(0))
                    {
                        var hero = pendingAbilityUser;
                        var target = TouchHelper.GetActorAtTouchPosition();
                        if (hero != null && target != null)
                            g.AbilityManager?.ToggleTarget(target);
                    }
                    break;
                case InputMode.PlayerTurn:
                    if (Input.GetMouseButtonDown(0)) { g.SelectionManager.Select(); initialTouchPosition = g.TouchPosition3D; }
                    else if (Input.GetMouseButton(0)) { if (Vector3.Distance(initialTouchPosition, g.TouchPosition3D) > dragThreshold) g.SelectionManager.Drag(); }
                    else if (Input.GetMouseButtonUp(0)) { g.SelectionManager.Drop(); }
                    break;
                case InputMode.EnemyTurn:
                    if (Input.GetMouseButtonDown(0))
                    {
                        var actor = TouchHelper.GetActorAtTouchPosition();
                        if (actor != null && actor.IsPlaying && actor.IsHero)
                        {
                            actor.Animation.Dodge();
                            lastEnemyTurnTapTime = Time.time;
                            lastEnemyTurnTappedHero = actor;
                        }
                    }
                    break;
            }
        }
    }
}

}
