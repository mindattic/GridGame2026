using System.Collections.Generic;
using UnityEngine;
using Scripts.Helpers;
using g = Scripts.Helpers.GameHelper;
using Scripts.Sequences;
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
    /// ABILITYMANAGER - Controls ability targeting and casting.
    /// 
    /// PURPOSE:
    /// Centralized controller for the ability system. Handles target selection,
    /// validation, casting, and ability effects.
    /// 
    /// ABILITY FLOW:
    /// 1. Player taps ability button → AbilityButtonManager calls BeginTargeting()
    /// 2. InputMode changes to AnyTarget or LinearTarget
    /// 3. Player taps valid targets → ToggleTarget() adds/removes from targetList
    /// 4. Player confirms → Cast() executes ability
    /// 5. Cancel → CancelTargeting() returns to PlayerTurn mode
    /// 
    /// TARGETING MODES (AbilityTargetingMode):
    /// - AnyTarget: Select any valid target(s)
    /// - LinearTarget: Select targets in a line (for line attacks)
    /// - Self: No target selection needed
    /// 
    /// TARGET VALIDATION:
    /// Each ability defines valid targets via:
    /// - TargetType: Ally, Enemy, Self, Any
    /// - Range: Maximum distance from caster
    /// - RequiresLOS: Line of sight check
    /// 
    /// KEY PROPERTIES:
    /// - currentAbility: Ability being targeted/cast
    /// - currentUser: Actor casting the ability
    /// - targetList: Selected targets
    /// - PendingAbilityUser: Cached user during targeting
    /// 
    /// INTERACTION LOCKS:
    /// Abilities blocked when:
    /// - IsEnemyTurn (not player's turn)
    /// - IsSequenceExecuting (combat in progress)
    /// - InputMode == None (input disabled)
    /// 
    /// RELATED FILES:
    /// - Ability.cs: Ability data definition
    /// - AbilityButtonManager.cs: UI buttons for abilities
    /// - AbilityCastConfirm.cs: Confirmation dialog
    /// - AbilityButtonFactory.cs: Creates ability button UI
    /// - InputManager.cs: Handles targeting input mode
    /// 
    /// ACCESS: g.AbilityManager
    /// </summary>
    public class AbilityManager : MonoBehaviour
    {
        #region Fields

        private Ability currentAbility;
        private ActorInstance currentUser;
        private readonly List<ActorInstance> targetList = new();
        private ActorInstance pendingAbilityUser;

        /// <summary>Cached user during ability targeting flow.</summary>
        public ActorInstance PendingAbilityUser => pendingAbilityUser;

        #endregion

        #region Interaction Guards

        private static bool IsEnemyTurn => g.TurnManager.IsEnemyTurn;
        private static bool IsSequenceExecuting => g.SequenceManager.IsExecuting;
        /// <summary>Returns whether the is interaction locked condition is met.</summary>
        private static bool IsInteractionLocked() => IsEnemyTurn || IsSequenceExecuting || g.InputManager.InputMode == InputMode.None;

        #endregion

        #region Targeting

        /// <summary>
        /// Begins target selection for an ability.
        /// Changes InputMode to targeting state.
        /// </summary>
        public void BeginTargeting(ActorInstance user, Ability ability)
        {
            if (IsInteractionLocked()) return;

            ClearAllIndicators();
            targetList.Clear();

            currentUser = user;
            currentAbility = ability;

            // Clear movement state to prevent contamination
            ClearMovementState();

            g.AbilityCastConfirm.ClearTitle();
            g.InputManager.InputMode = ability.TargetingMode == AbilityTargetingMode.Linear ? InputMode.LinearTarget : InputMode.AnyTarget;
            g.InputManager.RequireTouchRelease();
        }

        /// <summary>Caches the pending ability user.</summary>
        public void BeginAbilityTargeting(ActorInstance user)
        {
            if (IsInteractionLocked()) return;
            pendingAbilityUser = user;
        }

        /// <summary>Clears the pending ability user.</summary>
        public void ClearPendingUser() => pendingAbilityUser = null;

        /// <summary>
        /// Toggles selection state for a clicked actor.
        /// Adds/removes from targetList based on validity.
        /// </summary>
        public void ToggleTarget(ActorInstance actor)
        {
            if (IsInteractionLocked()) return;
            if (currentAbility == null || currentUser == null) return;
            if (actor == null || !actor.IsPlaying) return;
            if (!IsValidTargetForAbility(currentAbility, currentUser, actor)) return;

            // Deselect if already selected
            if (targetList.Contains(actor))
            {
                targetList.Remove(actor);

        #endregion
                actor.Render.SetTargetIndicatorEnabled(false);
                if (targetList.Count == 0) g.AbilityCastConfirm.FadeOut();
                OnSelectionChanged(false);
                return;
            }

            int capacity = Mathf.Max(1, currentAbility.TotalNumberOfTargets);

            if (capacity == 1)
            {
                ClearAllIndicators();
                targetList.Clear();
                Select(actor);
                g.AbilityCastConfirm.SetTitle(currentAbility.name);
                g.AbilityCastConfirm.Toggle();
                OnSelectionChanged(true);
                return;
            }

            if (targetList.Count < capacity)
            {
                Select(actor);
                g.AbilityCastConfirm.SetTitle(currentAbility.name);
                g.AbilityCastConfirm.Toggle();
                OnSelectionChanged(true);
            }
            else
            {
                var oldest = targetList[0];
                oldest.Render.SetTargetIndicatorEnabled(false);
                targetList.RemoveAt(0);
                Select(actor);
                g.AbilityCastConfirm.SetTitle(currentAbility.name);
                g.AbilityCastConfirm.Toggle();
                OnSelectionChanged(true);
            }
        }

        /// <summary>Updates the ability target.</summary>
        public void UpdateAbilityTarget(Touch touch)
        {
            if (IsInteractionLocked() || touch.phase != TouchPhase.Began) return;
            var target = TouchHelper.GetActorAtTouchPosition();
            if (target == null || !target.IsPlaying) return;
            ToggleTarget(target);
        }

        /// <summary>Updates the linear target.</summary>
        public void UpdateLinearTarget(Touch touch)
        {
            if (IsInteractionLocked() || touch.phase != TouchPhase.Began) return;
            var hero = pendingAbilityUser;
            var target = TouchHelper.GetActorAtTouchPosition();
            if (hero == null || target == null || !target.IsPlaying) return;
            if (!IsValidLinearTarget(hero, target)) return;
            ClearAllIndicators();
            targetList.Clear();
            Select(target);
            g.AbilityCastConfirm.Toggle();
            OnSelectionChanged(true);
        }

        /// <summary>
        /// Invoked by UI CastButton. Executes the ability on all selected targets.
        /// </summary>
        public void OnCastButtonClicked()
        {
            if (IsInteractionLocked()) return;
            if (currentAbility == null || currentUser == null) return;
            if (targetList.Count == 0) return;

            if (!g.ManaPoolManager.Spend(currentUser.IsHero ? Team.Hero : Team.Enemy, currentAbility.ManaCost))
                return;

            g.InputManager.InputMode = InputMode.None;

            // Announce the ability on the ability bar
            g.AbilityBar?.Show(currentUser, currentAbility);

            var startPosition = g.Card.PortraitWorldPosition();

            switch (currentAbility.Effect)
            {
                case AbilityEffect.Heal:
                    foreach (var t in targetList)
                        g.SequenceManager.Add(new HealAbilitySequence(startPosition, t));
                    break;
                case AbilityEffect.Trap:
                    foreach (var t in targetList)
                        g.SequenceManager.Add(new TrapSequence(startPosition, t));
                    break;
                case AbilityEffect.Smite:
                    foreach (var t in targetList)
                        g.SequenceManager.Add(new SmiteSequence(t));
                    break;
                case AbilityEffect.ShieldRush:
                    var target = targetList[0];
                    if (!IsValidLinearTarget(currentUser, target)) { CancelTargeting(); return; }
                    g.TileManager.Reset();
                    g.SequenceManager.Add(new ShieldBashSequence(currentUser, target));
                    break;
                case AbilityEffect.UseItem:
                    if (currentAbility.SourceItem != null)
                    {
                        // Show ability bar announcement
                        g.AbilityBar?.Show(currentUser, currentAbility.SourceItem);
                        foreach (var t in targetList)
                            g.SequenceManager.Add(new UseItemSequence(currentUser, t, currentAbility.SourceItem));
                    }
                    break;
                default:
                    foreach (var t in targetList)
                        g.SequenceManager.Add(new SequenceCallback(() => currentAbility.Activate(currentUser, t)));
                    break;
            }

            // Hide ability cast confirm UI immediately when cast begins
            g.AbilityCastConfirm.FadeOut();

            g.SequenceManager.Add(new SequenceCallback(() => { CancelTargetingInternal(); ClearFocusAndUI(); }));
            g.SequenceManager.Add(new EndTurnSequence());
            g.SequenceManager.Execute();
        }

        /// <summary> clear focus and ui..Groups[0].Value.ToUpper() lear focus and ui.</summary>
        private void ClearFocusAndUI()
        {
            ClearMovementState();
            g.Actors.SelectedActor = null;
            foreach (var a in g.Actors.All)
                a.Render.SetFocusIndicatorEnabled(false);
            g.AbilityButtonManager.Hide();
            g.Card.Clear();
        }

        /// <summary>
        /// Clears any movement-related state to prevent position contamination
        /// between input modes (e.g., ability targeting vs player turn).
        /// </summary>
        private void ClearMovementState()
        {
            // Clear the MovingHero reference
            g.Actors.MovingHero = null;

            // Clear TouchOffset to prevent position calculation issues
            g.TouchOffset = Vector3.zero;

            // Ensure no actors think they are still being dragged
            if (currentUser != null && currentUser.Flags != null)
            {
                currentUser.Flags.IsMoving = false;
            }
        }

        /// <summary>Returns whether the cancel targeting internal condition is met.</summary>
        private void CancelTargetingInternal()
        {
            ClearAllIndicators();
            targetList.Clear();
            currentAbility = null;
            currentUser = null;
            ClearPendingUser();
            g.AbilityCastConfirm.ClearTitle();
            g.TileManager?.Reset();
        }

        /// <summary>Returns whether the cancel targeting condition is met.</summary>
        public void CancelTargeting()
        {
            CancelTargetingInternal();
            g.InputManager.InputMode = InputMode.PlayerTurn;
            g.InputManager.RequireTouchRelease();
        }

        /// <summary>Handles the cancel button clicked event event.</summary>
        public void OnCancelButtonClickedEvent()
        {
            if (IsSequenceExecuting) return;
            g.TileManager.Reset();
            CancelTargeting();
            if (g.Actors.HasMovingHero)
            {
                g.Actors.MovingHero.Move.ToLocation();
                g.Actors.MovingHero.Flags.IsMoving = false;
                g.Actors.MovingHero.transform.localRotation = Quaternion.Euler(Vector3.zero);
            }
            if (pendingAbilityUser != null)
            {
                pendingAbilityUser.Move.ToLocation();
                pendingAbilityUser.Flags.IsMoving = false;
                pendingAbilityUser.transform.localRotation = Quaternion.Euler(Vector3.zero);
            }
            ClearPendingUser();
            g.TouchOffset = Vector3.zero;
        }

        /// <summary>Show cancel buttons this component.</summary>
        public void ShowCancelButton() => g.AbilityCastConfirm.ShowButtons();
        /// <summary>Hide cancel buttons this component.</summary>
        public void HideCancelButton() => g.AbilityCastConfirm.HideButtons();

        /// <summary>Select.</summary>
        private void Select(ActorInstance actor)
        {
            targetList.Add(actor);
            actor.Render.SetTargetIndicatorEnabled(true);
        }

        /// <summary> clear all indicators..Groups[0].Value.ToUpper() lear all indicators.</summary>
        private void ClearAllIndicators()
        {
            foreach (var a in targetList)
                a.Render.SetTargetIndicatorEnabled(false);
            AbilityCastConfirm.instance.FadeOut();
            g.AbilityCastConfirm.CanvasGroup.alpha = 0f;
            g.AbilityCastConfirm.CanvasGroup.interactable = false;
            g.AbilityCastConfirm.CanvasGroup.blocksRaycasts = false;
        }

        /// <summary>Handles the selection changed event.</summary>
        private void OnSelectionChanged(bool added)
        {
            if (added)
                AbilityCastConfirm.instance.Toggle();
            else
                AbilityCastConfirm.instance.FadeOut();
        }

        /// <summary>Returns whether the is valid target for ability condition is met.</summary>
        private static bool IsValidTargetForAbility(Ability ability, ActorInstance user, ActorInstance target)
        {
            return ability.type switch
            {
                AbilityType.TargetOpponent => target.IsEnemy && target.IsPlaying,
                _ => target.IsPlaying
            };
        }

        /// <summary>Returns whether the is valid linear target condition is met.</summary>
        private static bool IsValidLinearTarget(ActorInstance hero, ActorInstance target)
        {
            if (hero == null || target == null) return false;
            if (!hero.IsHero || !target.IsPlaying) return false;
            bool aligned = hero.location.x == target.location.x || hero.location.y == target.location.y;
            if (!aligned) return false;
            var between = g.TileMap.EnumerateBetween(hero.location, target.location);
            foreach (var t in between)
                if (t != null && t.IsOccupied) return false;
            return true;
        }
    }
}
