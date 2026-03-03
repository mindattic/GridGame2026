// --- File: Assets/Scripts/Managers/SelectionManager.cs ---
using Scripts.Helpers;
using Scripts.Sequences;
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
/// SELECTIONMANAGER - Tracks and manages the currently selected hero.
/// 
/// PURPOSE:
/// Manages hero selection state during player turns. When a hero is selected,
/// shows their info card, enables ability buttons, and prepares for drag input.
/// 
/// SELECTION FLOW:
/// 1. Player taps hero → Select(actor) called
/// 2. g.Actors.SelectedActor updated
/// 3. ActorCard.Assign() shows hero info
/// 4. AbilityButtonManager.Show() displays abilities
/// 5. SortingManager.OnActorFocus() brings hero to front
/// 
/// SELECTED ACTOR STATES (SelectedActorState):
/// - Idle: Selected but not being moved
/// - PickedUp: Player started dragging
/// - Moving: Hero being dragged to new position
/// - Dropped: Hero released, awaiting pincer check
/// 
/// KEY METHODS:
/// - Select(actor): Sets the selected actor
/// - Pickup(): Begins dragging the selected hero
/// - Drop(): Releases the hero at current position
/// - CancelPick(): Cancels drag, returns to original position
/// 
/// GUARD CONDITIONS:
/// - Won't change selection during ability targeting modes
/// - Won't select dead/inactive actors
/// - During forced turn mode, may restrict which heroes can be selected
/// 
/// RELATED FILES:
/// - InputManager.cs: Calls Select() on tap
/// - ActorCard.cs: Displays selected hero info
/// - AbilityButtonManager.cs: Shows hero abilities
/// - GhostManager.cs: Shows drag preview
/// - SortingManager.cs: Brings selected actor to front
/// 
/// ACCESS: g.SelectionManager
/// SELECTED ACTOR: g.Actors.SelectedActor
/// </summary>
public class SelectionManager : MonoBehaviour
{
    #region State Machine

    /// <summary>
    /// Tracks the state of the currently selected actor during drag operations.
    /// </summary>
    public enum SelectedActorState
    {
        /// <summary>Selected but not being moved.</summary>
        Idle,
        /// <summary>Player started dragging.</summary>
        PickedUp,
        /// <summary>Hero being dragged to new position.</summary>
        Moving,
        /// <summary>Hero released, awaiting pincer check.</summary>
        Dropped
    }

    #endregion

    #region Fields

    private float dragThreshold;
    private SelectedActorState selectedState = SelectedActorState.Idle;

    #endregion

    #region Initialization

    /// <summary>Initializes component references and state.</summary>
    public void Awake()
    {
        dragThreshold = g.TileSize * 0.25f;
    }

    #endregion

    #region Selection

    /// <summary>
    /// Selects an actor (or the actor at touch position if null).
    /// Updates UI, shows card and abilities.
    /// </summary>
    public void Select(ActorInstance actor = null)
    {
        // Guard: During ability targeting modes, don't change selection
        var mode = g.InputManager?.InputMode ?? InputMode.PlayerTurn;
        if (mode == InputMode.AnyTarget || mode == InputMode.LinearTarget)
            return;

        var target = actor ?? TouchHelper.GetActorAtTouchPosition();

        // Don't unselect when clicking outside of an actor
        if (target == null || !target.IsPlaying)
        {
            return;
        }

        // If unchanged, just refresh visuals
        if (g.Actors.SelectedActor == target)
        {
            g.Card.Assign();
#if UNITY_EDITOR
            GameManager.instance.reloadThumbnailSettings = true;
#endif
            return;
        }

        g.Actors.SelectedActor = target;
        g.SortingManager.OnActorFocus();

        // Show abilities only when a hero is focused
        if (g.Actors.SelectedActor.IsHero)
            g.AbilityButtonManager.Show(g.Actors.SelectedActor);
        else
            g.AbilityButtonManager.Hide();

        g.TouchOffset = g.Actors.SelectedActor.Position - g.TouchPosition3D;

 // Reset unified state and compatibility fields
 selectedState = SelectedActorState.Idle;
 g.Actors.MovingHero = null;

 // Board: toggle focus indicators
 g.Actors.All.ForEach(x => x.Render.SetFocusIndicatorEnabled(x == g.Actors.SelectedActor));

 // Always assign card when selecting
 g.Card.Assign();

#if UNITY_EDITOR
 GameManager.instance.reloadThumbnailSettings = true;
#endif
 }

  /// <summary>Drag.</summary>
  public void Drag()
  {
  // Guard: Only allow dragging during PlayerTurn mode (not during ability targeting)
  if (g.InputManager?.InputMode != InputMode.PlayerTurn)
  return;

  // Only allow dragging during hero turn and when selected actor is a hero
  if (!g.TurnManager.IsHeroTurn || g.Actors.SelectedActor == null || g.Actors.SelectedActor.IsEnemy)
  return;

 var mode = g.TurnSelectionMode;
 bool restrictToActive = mode == Scripts.Models.TurnSelectionMode.ActiveOnly;
 var actor = g.Actors.SelectedActor;
 if (restrictToActive && actor != g.TurnManager.ActiveActor)
 return;

 // Require a press/hold
 bool pressing = Input.GetMouseButton(0) || Input.touchCount >0;
 if (!pressing) return;

 // Initial pickup: transition to PickedUp and start following cursor softly
 if (selectedState == SelectedActorState.Idle)
 {
 selectedState = SelectedActorState.PickedUp;
 g.Actors.MovingHero = actor; // maintain compatibility with systems that track MovingHero
 g.TouchOffset = actor.Position - g.TouchPosition3D;

 if (!actor.Flags.IsMoving)
 actor.Move.MoveTowardCursor();

 return;
 }

 // Continue to follow the cursor while pressed
 if (!actor.Flags.IsMoving)
 actor.Move.MoveTowardCursor();

 // If already moving, no need to re-evaluate threshold
 if (selectedState == SelectedActorState.Moving)
 return;

 float moved = Vector3.Distance(actor.Position, actor.currentTile.position);
 if (moved >= dragThreshold)
 {
 // Promote to Moving: begin visual timeline advance
 selectedState = SelectedActorState.Moving;
 g.SortingManager.OnHeroDrag();

 // TimelineBar starts moving via OnHeroStartMove
 g.TimelineBar?.OnHeroStartMove();

 g.Card.Clear();
 g.AudioManager.Play("Click");
 g.ActorManager.CheckEnemyAP();
 }
 }

  /// <summary>Drop.</summary>
  public void Drop()
  {
  // Guard: Only process drops during PlayerTurn mode to prevent state contamination
  // from ability targeting or other input modes
  if (g.InputManager?.InputMode != InputMode.PlayerTurn)
  return;

  var actor = g.Actors.SelectedActor;

  // Always pause TimelineBar tag movement on any drop (manual or auto)
  g.TimelineBar?.OnHeroStopMove();

  // Reset all tile colors on any drop
  g.TileManager?.Reset();

  // Refresh mana UI to ensure accumulated mana is reflected
  g.ManaPoolManager?.RefreshUI();

  // Only drop if we have a selected hero in Moving state during hero turn
  bool canDrop =
  g.TurnManager.IsHeroTurn &&
  actor != null &&
  actor.IsHero &&
  selectedState == SelectedActorState.Moving;

 if (!canDrop)
 {
 // If something was moving, snap back to location gracefully
 if (actor != null && actor.Flags.IsMoving)
 {
 actor.Move.ToLocation();
 actor.Flags.IsMoving = false;
 actor.transform.localRotation = Quaternion.Euler(Vector3.zero);
 }

 // Always despawn all support lines on drop
 g.SupportLineManager.Clear();

 // Reset to idle if we had any non-idle state
 if (selectedState != SelectedActorState.Idle)
 selectedState = SelectedActorState.Idle;

 g.Actors.MovingHero = null; // clear compatibility field
 return;
 }

 // Transition to Dropped
 selectedState = SelectedActorState.Dropped;

 // Ensure the actor stops exactly at its location
 actor.Move.ToLocation();
 actor.Flags.IsMoving = false;
 g.SortingManager.OnSelectedHeroDrop();

 // Always despawn all support lines on drop
 g.SupportLineManager.Clear();

 // Suspend all touch input during resolution; restore below if staying in hero turn
 g.InputManager.InputMode = InputMode.None;

 // Clear compatibility field at the start of resolution
 g.Actors.MovingHero = null;

 // Determine if an enemy was queued by the Timeline (tag hit TriggerX)
 bool enemyQueuedByTimeline = g.TurnManager != null && g.TurnManager.HasQueuedEnemyAfterHero;

 bool anyPincer = g.PincerAttackManager.Check(Team.Hero, actor);

 if (!anyPincer)
 {
 // Always resolve deaths that may have been caused by movement effects
 g.SequenceManager.Add(new DeathSequence());
 g.SequenceManager.Execute();

 if (enemyQueuedByTimeline)
 {
 // Timeline triggered turn end: hand off to enemy
 g.SequenceManager.Add(new EndTurnSequence());
 g.SequenceManager.Execute();
 }
 else
 {
 // Manual drop: stay in hero turn
 g.InputManager.InputMode = InputMode.PlayerTurn;
 selectedState = SelectedActorState.Idle;
 }
 }
 else
 {
 // When pincer finishes, decide whether to end turn or restore player input.
 System.Action onResolved = null;
 onResolved = () =>
 {
 g.PincerAttackManager.OnResolved -= onResolved;
 if (enemyQueuedByTimeline)
 {
 g.SequenceManager.Add(new EndTurnSequence());
 g.SequenceManager.Execute();
 }
 else
 {
 g.InputManager.InputMode = InputMode.PlayerTurn;
 }
 selectedState = SelectedActorState.Idle;
 };
 g.PincerAttackManager.OnResolved += onResolved;
 }
 }

 /// <summary>
 /// Returns true if a hero is currently being moved (PickedUp or Moving state).
 /// Used by TimelineTriggerSequence to determine if a ForceHeroDropSequence is needed.
 /// </summary>
 public bool IsHeroBeingMoved => 
 selectedState == SelectedActorState.PickedUp || 
 selectedState == SelectedActorState.Moving;

 /// <summary>
 /// Resets the selection state to Idle. Called by ForceHeroDropSequence after dropping.
 /// </summary>
 public void ResetState()
 {
 selectedState = SelectedActorState.Idle;
 }

 #endregion
}

}
