using System.Collections.Generic;
using UnityEngine;
using Assets.Helpers;
using g = Assets.Helpers.GameHelper;
using Assets.Scripts.Sequences;

namespace Assets.Scripts.Managers
{
    /// <summary>
    /// Centralized ability targeting and casting controller.
    /// Tracks current ability, selected targets (with indicator), and handles Cast/Cancel.
    /// Also owns ability-specific input helpers (selection and special-case execution like Shield Bash).
    /// </summary>
    public class AbilityManager : MonoBehaviour
    {
        private Ability currentAbility;
        private ActorInstance currentUser;

        // Selected targets for current ability (show/hide their target indicators)
        private readonly List<ActorInstance> targetList = new();

        // Cancel/Cast buttons (optional)
        private GameObject cancelButton;
        private GameObject castButton;

        // Ability user cache for targeting flows (e.g., Shield Bash owner)
        private ActorInstance pendingAbilityUser;
        public ActorInstance PendingAbilityUser => pendingAbilityUser;

        private void Awake()
        {
            // Cache CancelButton/CastButton if present and start hidden
            var cancelObj = GameObject.Find("CancelButton");
            if (cancelObj != null)
            {
                cancelButton = cancelObj;
                cancelButton.SetActive(false);
            }
            var castObj = GameObject.Find("CastButton");
            if (castObj != null)
            {
                castButton = castObj;
                castButton.SetActive(false);
            }
        }

        private static bool IsEnemyTurn => g.TurnManager != null && g.TurnManager.IsEnemyTurn;
        private static bool IsSequenceExecuting => g.SequenceManager != null && g.SequenceManager.IsExecuting;
        private static bool IsInteractionLocked() => IsEnemyTurn || IsSequenceExecuting || (g.InputManager != null && g.InputManager.InputMode == InputMode.None);

        /// <summary>
        /// Begin selecting targets for an ability. Shows Cancel/Cast buttons and switches input mode.
        /// </summary>
        public void BeginTargeting(ActorInstance user, Ability ability)
        {
            if (IsInteractionLocked()) return;

            ClearAllIndicators();
            targetList.Clear();

            currentUser = user;
            currentAbility = ability;

            // Any-actor flow by default; linear flow will still call this to set state
            g.InputManager.InputMode = ability.TargetingMode == AbilityTargetingMode.Linear ? InputMode.LinearTarget : InputMode.AnyTarget;
            ShowCancelButton();
            ShowCastButton();
            g.InputManager.RequireTouchRelease();
        }

        /// <summary>
        /// Prepare a user for a targeting flow that requires linear targeting (e.g., ShieldRush).
        /// </summary>
        public void BeginAbilityTargeting(ActorInstance user)
        {
            if (IsInteractionLocked()) return;
            pendingAbilityUser = user;
        }

        /// <summary>
        /// Clear any cached ability user.
        /// </summary>
        public void ClearPendingUser()
        {
            pendingAbilityUser = null;
        }

        /// <summary>
        /// Toggle selection state for a clicked actor according to ability rules and capacity.
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
                actor.Render.SetTargetIndicatorEnabled(false);
                return;
            }

            int capacity = Mathf.Max(1, currentAbility.TotalNumberOfTargets);

            // If capacity is 1, replace existing selection
            if (capacity == 1)
            {
                ClearAllIndicators();
                targetList.Clear();
                Select(actor);
                return;
            }

            // Multi-target: if we still have room, add; otherwise replace oldest
            if (targetList.Count < capacity)
            {
                Select(actor);
            }
            else
            {
                // Replace the oldest selection to keep UX simple
                var oldest = targetList[0];
                if (oldest != null)
                    oldest.Render.SetTargetIndicatorEnabled(false);
                targetList.RemoveAt(0);
                Select(actor);
            }
        }

        /// <summary>
        /// Tap handler for AnyTarget: tap toggles selection; Cast button confirms.
        /// </summary>
        public void UpdateAbilityTarget(Touch touch)
        {
            if (IsInteractionLocked() || touch.phase != TouchPhase.Began) return;
            var target = TouchHelper.GetActorAtTouchPosition(); if (target == null || !target.IsPlaying) return; ToggleTarget(target);
        }

        /// <summary>
        /// Tap handler for LinearTarget: choose a valid line-of-sight target, then press Cast.
        /// </summary>
        public void UpdateLinearTarget(Touch touch)
        {
            if (IsInteractionLocked() || touch.phase != TouchPhase.Began) return;
            var hero = pendingAbilityUser; var target = TouchHelper.GetActorAtTouchPosition();
            if (hero == null || target == null || !target.IsPlaying) return; if (!IsValidLinearTarget(hero, target)) return;
            ClearAllIndicators(); targetList.Clear(); Select(target);
        }

        // Spend MP to cast, gate against enemy turn / sequences, and end the hero turn after a cast.
        private ManaPoolManager GetMana()
        {
            var go = GameObject.Find("Game");
            return go != null ? go.GetComponent<ManaPoolManager>() : null;
        }

        /// <summary>
        /// Invoked by UI CastButton. Executes the ability on all selected targets.
        /// Bind your Canvas/CastButton onClick to this method.
        /// </summary>
        public void OnCastButtonClicked()
        {
            if (IsInteractionLocked()) return;
            if (currentAbility == null || currentUser == null) return;
            if (targetList.Count == 0) return;

            var mana = GetMana();
            if (mana != null)
            {
                if (!mana.Spend(currentUser.IsHero ? Team.Hero : Team.Enemy, Mathf.Max(0, currentAbility.ManaCost)))
                {
                    return;
                }
            }

            HideCastButton(); HideCancelButton();
            g.InputManager.InputMode = InputMode.None; // lock input during sequence batch

            var startPosition = g.Card != null ? g.Card.PortraitWorldPosition() : currentUser.transform.position;

            switch (currentAbility.Effect)
            {
                case AbilityEffect.Heal:
                    foreach (var t in targetList) if (t != null) g.SequenceManager.Add(new HealAbilitySequence(startPosition, t));
                    break;
                case AbilityEffect.Trap:
                    foreach (var t in targetList) if (t != null) g.SequenceManager.Add(new TrapSequence(startPosition, t));
                    break;
                case AbilityEffect.Smite:
                    foreach (var t in targetList) if (t != null) g.SequenceManager.Add(new SmiteSequence(t));
                    break;
                case AbilityEffect.ShieldRush:
                    var target = targetList[0]; if (target == null || !IsValidLinearTarget(currentUser, target)) { CancelTargeting(); return; }
                    g.TileManager.Reset();
                    g.SequenceManager.Add(new ShieldBashSequence(currentUser, target));
                    break;
                default:
                    foreach (var t in targetList) if (t != null) g.SequenceManager.Add(new SequenceCallback(() => currentAbility.Activate(currentUser, t)));
                    break;
            }

            // End player action -> clear focus and UI, then advance turn; TurnManager.NextTurn will restore InputMode
            g.SequenceManager.Add(new SequenceCallback(() => { CancelTargetingInternal(); ClearFocusAndUI(); }));
            g.SequenceManager.Add(new EndTurnSequence());
            g.SequenceManager.Execute();
        }

        private void ClearFocusAndUI()
        {
            // Unfocus the hero; TurnManager will set focus for next side
            g.Actors.SelectedActor = null;
            if (g.Actors.All != null)
            {
                foreach (var a in g.Actors.All)
                {
                    if (a != null) a.Render.SetFocusIndicatorEnabled(false);
                }
            }
            g.AbilityButtonManager?.Hide();
            g.Card?.Clear();
        }

        private void CancelTargetingInternal()
        {
            ClearAllIndicators(); targetList.Clear(); currentAbility = null; currentUser = null;
            HideCastButton(); HideCancelButton();
            ClearPendingUser();
            // Explicitly hide the TitleBar when leaving targeting/casting
            g.TitleBar?.Hide();
        }

        /// <summary>
        /// Cancels targeting, clears indicators and state, hides overlay/buttons, and restores PlayerTurn.
        /// </summary>
        public void CancelTargeting()
        {
            // User-initiated cancel: restore PlayerTurn immediately
            CancelTargetingInternal();
            g.InputManager.InputMode = InputMode.PlayerTurn; // this also hides TargetModeOverlay via mode change
            g.InputManager.RequireTouchRelease();
        }

        /// <summary>
        /// Bind this to the Canvas/CancelButton OnClick. Cancels targeting and returns to PlayerTurn.
        /// Also resets movement states for selected/pending users and clears offsets.
        /// </summary>
        public void OnCancelButtonClickedEvent()
        {
            if (IsSequenceExecuting) return; // ignore during sequence
            g.TileManager.Reset(); CancelTargeting();
            if (g.Actors.HasMovingHero)
            { g.Actors.MovingHero.Move.ToLocation(); g.Actors.MovingHero.Flags.IsMoving = false; g.Actors.MovingHero.transform.localRotation = Quaternion.Euler(Vector3.zero); }
            if (pendingAbilityUser != null)
            { pendingAbilityUser.Move.ToLocation(); pendingAbilityUser.Flags.IsMoving = false; pendingAbilityUser.transform.localRotation = Quaternion.Euler(Vector3.zero); }
            ClearPendingUser(); g.TouchOffset = Vector3.zero;
        }

        public void ShowCancelButton() { if (cancelButton != null) cancelButton.SetActive(true); }
        public void HideCancelButton() { if (cancelButton != null) cancelButton.SetActive(false); }
        public void ShowCastButton() { if (castButton != null) castButton.SetActive(true); }
        public void HideCastButton() { if (castButton != null) castButton.SetActive(false); }

        private void Select(ActorInstance actor) { targetList.Add(actor); actor.Render.SetTargetIndicatorEnabled(true); }
        private void ClearAllIndicators() { foreach (var a in targetList) if (a != null) a.Render.SetTargetIndicatorEnabled(false); }

        private static bool IsValidTargetForAbility(Ability ability, ActorInstance user, ActorInstance target)
        {
            switch (ability.type)
            {
                case AbilityType.TargetAny: return target.IsPlaying;
                case AbilityType.TargetAlly: return target.IsPlaying;
                case AbilityType.TargetOpponent: return target.IsEnemy && target.IsPlaying;
                default: return target.IsPlaying;
            }
        }

        private static bool IsValidLinearTarget(ActorInstance hero, ActorInstance target)
        {
            if (hero == null || target == null) return false; if (!hero.IsHero || !target.IsPlaying) return false;
            bool aligned = hero.location.x == target.location.x || hero.location.y == target.location.y; if (!aligned) return false;
            var between = g.TileMap.EnumerateBetween(hero.location, target.location); foreach (var t in between) if (t != null && t.IsOccupied) return false; return true;
        }
    }
}