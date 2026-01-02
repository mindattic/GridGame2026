// --- File: Assets/Scripts/Managers/SelectionManager.cs ---
using Assets.Helpers;
using Assets.Scripts.Sequences;
using UnityEngine;
using g = Assets.Helpers.GameHelper;

public class SelectionManager : MonoBehaviour
{
 public enum SelectedActorState
 {
 Idle,
 PickedUp,
 Moving,
 Dropped
 }

 private float dragThreshold;
 private SelectedActorState selectedState = SelectedActorState.Idle;

 public void Awake()
 {
 dragThreshold = g.TileSize *0.25f;
 }

 public void Select(ActorInstance actor = null)
 {
 var target = actor ?? TouchHelper.GetActorAtTouchPosition();

 // Do not unselect when clicking outside of an actor anymore.
 if (target == null || !target.IsPlaying)
 {
 return;
 }

 // If unchanged, just refresh visuals and make sure card shows
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

 public void Drag()
 {
 // Only allow dragging during hero turn and when selected actor is a hero
 if (!g.TurnManager.IsHeroTurn || g.Actors.SelectedActor == null || g.Actors.SelectedActor.IsEnemy)
 return;

 var mode = g.TurnSelectionMode;
 bool restrictToActive = mode == Assets.Scripts.Models.TurnSelectionMode.ActiveOnly;
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

  public void Drop()
  {
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
}
