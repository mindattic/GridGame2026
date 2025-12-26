// --- File: Assets/Scripts/Managers/InputManager.cs ---
using Assets.Helpers;
using Assets.Scripts.Sequences;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using g = Assets.Helpers.GameHelper;

public class InputManager : MonoBehaviour
{
    private Vector3 initialTouchPosition;
    public float dragThreshold;

    /// <summary>
    /// Raised whenever the input mode changes.
    /// </summary>
    public event Action<InputMode> OnInputModeChanged;

    private InputMode inputMode = InputMode.PlayerTurn;

    // Ability user cache for targeting flows (do not reuse SelectedHero to avoid side-effects)
    // Moved to AbilityManager. Keep only a pass-through accessor for compatibility if needed.
    private ActorInstance pendingAbilityUser => g.AbilityManager != null ? g.AbilityManager.PendingAbilityUser : null;

    /// <summary>
    /// Current input mode for the screen.
    /// </summary>
    public InputMode InputMode
    {
        get => inputMode;
        set
        {
            // Allow mode changes even during sequences; Update() already blocks input while executing
            if (inputMode == value) return;
            inputMode = value;
            OnInputModeChanged?.Invoke(inputMode);
        }
    }

    /// <summary>
    /// Fired when a successful dodge timing occurs during EnemyTurn.
    /// </summary>
    public event Action OnDodge;

    /// <summary>
    /// Fired when a successful parry timing occurs during EnemyTurn.
    /// </summary>
    public event Action OnParry;

    private const float DodgeWindowSeconds = 0.20f;
    private const float ParryWindowSeconds = 0.10f;

    private float lastEnemyTurnTapTime = -999f;
    private ActorInstance lastEnemyTurnTappedHero = null;

    /// <summary>
    /// True if the selected hero is currently being dragged.
    /// </summary>
    public bool isDragging => g.Actors.HasMovingHero && g.Actors.MovingHero.Flags.IsMoving;

    // --------------------------------------------------------------------------------------------
    // Gate input until current press is released (used when timer forces a Drop)
    // --------------------------------------------------------------------------------------------
    private bool requireTouchRelease;

    /// <summary>
    /// Prevent any input from being processed until all touches/mouse buttons are released.
    /// </summary>
    public void RequireTouchRelease() { requireTouchRelease = true; }

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
        g.TitleBar?.Hide();
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
