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

        /// <summary>For self-targeted actions (EquipWeapon, future buffs that target the
        /// caster): registers <paramref name="self"/> as the implicit target and pops up the
        /// confirm dialog. OnCastButtonClicked then proceeds normally because Count > 0.</summary>
        public void ConfirmSelfTarget(ActorInstance self)
        {
            if (currentAbility == null || self == null) return;
            ClearAllIndicators();
            targetList.Clear();
            Select(self);
            g.AbilityCastConfirm.SetTitleFor(currentAbility);
            g.AbilityCastConfirm.Toggle();
            OnSelectionChanged(true);
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
                g.AbilityCastConfirm.SetTitleFor(currentAbility);
                g.AbilityCastConfirm.Toggle();
                OnSelectionChanged(true);
                return;
            }

            if (targetList.Count < capacity)
            {
                Select(actor);
                g.AbilityCastConfirm.SetTitleFor(currentAbility);
                g.AbilityCastConfirm.Toggle();
                OnSelectionChanged(true);
            }
            else
            {
                var oldest = targetList[0];
                oldest.Render.SetTargetIndicatorEnabled(false);
                targetList.RemoveAt(0);
                Select(actor);
                g.AbilityCastConfirm.SetTitleFor(currentAbility);
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

            // Cast-time branch: ability has CastTimeSeconds > 0 → defer the resolution
            // behind a spell icon on the timeline. The icon's center represents the moment
            // of resolution; train-cascade in TimelineBar.SpawnSpellIcon handles overlaps.
            // Item abilities and multi-target casts stay on the instant path for now.
            if (currentAbility.CastTimeSeconds > 0f
                && currentAbility.SourceItem == null
                && targetList.Count == 1
                && g.TimelineBar != null)
            {
                BeginCastTimeAbility();
                return;
            }

            if (!g.ManaPoolManager.Spend(currentUser.IsHero ? Team.Hero : Team.Enemy, currentAbility.ManaCost))
                return;

            g.InputManager.InputMode = InputMode.None;

            // Announce the action on the top-center title banner. Verb-specific overloads
            // (Cast/Use/Equip) fire from the per-effect branches below — only the generic
            // fallback announces here so we don't double-fire.
            if (!currentAbility.IsItemAbility && !currentAbility.IsWeaponAbility)
                g.ActionTitle?.Show(currentAbility?.name);

            var startPosition = g.ActorPanel.PortraitWorldPosition();

            switch (currentAbility.Effect)
            {
                case AbilityEffect.Heal:
                    foreach (var t in targetList)
                        g.SequenceManager.Add(new HealAbilitySequence(startPosition, t, currentUser));
                    break;
                case AbilityEffect.Trap:
                    foreach (var t in targetList)
                        g.SequenceManager.Add(new TrapSequence(startPosition, t));
                    break;
                case AbilityEffect.Fire:
                case AbilityEffect.Fireball:
                case AbilityEffect.Fira:
                case AbilityEffect.Ice:
                case AbilityEffect.Thunder:
                case AbilityEffect.Smite:
                    {
                        TryGetMagicEffect(currentAbility.Effect, out var element, out var vfxKey);
                        foreach (var t in targetList)
                            g.SequenceManager.Add(new MagicAttackSequence(currentUser, t, element, vfxKey));
                    }
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
                        // Per-battle use cap: consumables can be equipped instead of abilities,
                        // but unlike mana-powered skills they have a limited supply per battle
                        // (tactical pick-and-choose). Gate + count here so the button badge
                        // and UpdateInteractable see the increment.
                        if (!currentAbility.HasUsesRemaining) break;
                        currentAbility.UsesThisBattle += 1;

                        // "Using {Item}" on the top title banner.
                        g.ActionTitle?.Use(currentAbility.SourceItem);
                        foreach (var t in targetList)
                            g.SequenceManager.Add(new UseItemSequence(currentUser, t, currentAbility.SourceItem));
                    }
                    break;
                case AbilityEffect.EquipWeapon:
                    // Bar slot held a weapon — fire the swap sequence (handles save mutation +
                    // ActionTitle.Equip() banner). No target required; targeting is Self.
                    if (currentAbility.SourceWeapon != null)
                        g.SequenceManager.Add(new ChangeEquippedWeaponSequence(currentUser, currentAbility.SourceWeapon));
                    break;
                case AbilityEffect.Esuna:
                    foreach (var t in targetList)
                    {
                        int removed = t.Statuses.ClearDebuffs();
                        g.CombatTextManager.Spawn(removed > 0 ? "Cured!" : "No effect", t.Position, "Heal");
                    }
                    break;
                case AbilityEffect.Protect:
                    foreach (var t in targetList)
                    {
                        t.Statuses.Apply(new StatusEffect { Kind = StatusKind.Protect, Magnitude = 0.4f, RemainingTurns = 3 });
                        g.CombatTextManager.Spawn("Protect", t.Position, "Heal");
                    }
                    break;
                case AbilityEffect.Regen:
                    foreach (var t in targetList)
                    {
                        float regen = Mathf.Max(2f, (currentUser.Stats.Intelligence + currentUser.Stats.Wisdom) * 0.4f);
                        t.Statuses.Apply(new StatusEffect { Kind = StatusKind.Regen, Magnitude = regen, RemainingTurns = 5 });
                        g.CombatTextManager.Spawn("Regen", t.Position, "Heal");
                    }
                    break;
                default:
                    foreach (var t in targetList)
                        g.SequenceManager.Add(new SequenceCallback(() => currentAbility.Activate(currentUser, t)));
                    break;
            }

            // Hide ability cast confirm UI immediately when cast begins
            g.AbilityCastConfirm.FadeOut();

            // Resolve any deaths the cast caused (no-op if none) before handing off the turn,
            // exactly like the pincer flow.
            g.SequenceManager.Add(new DeathSequence());
            g.SequenceManager.Add(new SequenceCallback(() => { CancelTargetingInternal(); ClearFocusAndUI(); }));
            g.SequenceManager.Add(new EndTurnSequence());
            g.SequenceManager.Execute();
        }

        /// <summary>
        /// Spawns a spell icon on the timeline that takes CastTimeSeconds to traverse 0→1.
        /// When it reaches the trigger, the deferred resolution closure runs and ends the turn.
        /// Player stays in PlayerTurn state during the cast — other heroes can still be moved.
        /// </summary>
        private void BeginCastTimeAbility()
        {
            if (!g.ManaPoolManager.Spend(currentUser.IsHero ? Team.Hero : Team.Enemy, currentAbility.ManaCost))
                return;

            // Capture by value — the targeting state gets cleared below.
            var caster = currentUser;
            var ability = currentAbility;
            var target = targetList[0];
            var castStart = g.ActorPanel.PortraitWorldPosition();

            // "Casting {Spell}" — the cast-time path that ticks on the timeline before resolving.
            g.ActionTitle?.Cast(ability);

            var state = new CastingState(caster, ability, target);

            // Cyan cast arc: bound caster (actor) → target (actor). Color picks the
            // targeting kind out of the visual mix (red=enemy-select, cyan=heal/buff).
            // Hidden on cast resolution and on interrupt — both paths converge below.
            var castArcKey = "cast:" + caster.name;
            g.TargetLineManager?.Show2D(castArcKey,
                Models.TargetPoint.Actor(caster),
                Models.TargetPoint.Actor(target),
                Color.cyan);

            g.TimelineBar.SpawnSpellIcon(state,
                onComplete: spellIcon =>
                {
                    // Cast resolved — icon is parked at u=1 in Resolving mode, input is
                    // suspended via TurnManager.IsResolvingCast. Run the same effect path
                    // as the instant cast, then fade the spell icon and clear the gate.
                    switch (ability.Effect)
                    {
                        case AbilityEffect.Heal:
                            g.SequenceManager.Add(new HealAbilitySequence(castStart, target, caster));
                            break;
                        case AbilityEffect.Fire:
                        case AbilityEffect.Fireball:
                        case AbilityEffect.Fira:
                        case AbilityEffect.Ice:
                        case AbilityEffect.Thunder:
                        case AbilityEffect.Smite:
                            {
                                TryGetMagicEffect(ability.Effect, out var element, out var vfxKey);
                                g.SequenceManager.Add(new MagicAttackSequence(caster, target, element, vfxKey));
                            }
                            break;
                        default:
                            g.SequenceManager.Add(new SequenceCallback(() => ability.Activate(caster, target)));
                            break;
                    }
                    g.SequenceManager.Add(new DeathSequence());
                    g.SequenceManager.Add(new EndTurnSequence());
                    g.SequenceManager.Add(new SequenceCallback(() =>
                    {
                        spellIcon?.FadeAndDestroy(0.25f);
                        g.TargetLineManager?.Hide(castArcKey);
                        g.TurnManager?.EndCastResolution();
                    }));
                    g.SequenceManager.Execute();
                },
                onInterrupted: () =>
                {
                    // Future-proofing: interrupt path isn't wired yet (see CLAUDE.md "cast
                    // interruption — fail/pushback/clutch"), but when it lights up the arc
                    // must vanish with the spell icon. Idempotent — safe if already hidden.
                    g.TargetLineManager?.Hide(castArcKey);
                });

            // Hide the cast-confirm UI but keep the player in their turn — caster is busy
            // mid-cast, but the other heroes can still be planned with.
            g.AbilityCastConfirm.FadeOut();
            CancelTargetingInternal();
            ClearFocusAndUI();

            // Restore PlayerTurn input — BeginTargeting flipped us into AnyTarget /
            // LinearTarget, and CancelTargetingInternal does NOT reset InputMode (only
            // CancelTargeting does, which would undo other state too). Without this
            // the player is locked out of all input until cast resolution.
            g.InputManager.InputMode = InputMode.PlayerTurn;
            g.InputManager.RequireTouchRelease();

            // The cast can only resolve if the timeline keeps loading. Resume the bar
            // immediately so the spell icon advances — BeginHeroWindow paused it at
            // turn start, and the player has no other hero movement to trigger it.
            g.TimelineBar?.OnHeroStartMove();
        }

        /// <summary> clear focus and ui..Groups[0].Value.ToUpper() lear focus and ui.</summary>
        private void ClearFocusAndUI()
        {
            ClearMovementState();
            g.Actors.SelectedActor = null;
            foreach (var a in g.Actors.All)
                a.Render.SetFocusIndicatorEnabled(false);
            g.AbilityButtonManager.Hide();
            g.ActorPanel.Clear();
            // Selection has been wiped — drop the red enemy-select arc with it.
            g.SelectionManager?.HideEnemySelectArc();
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
            // Restore the hero portrait on the ActorPanel — AssignAbility swapped it to the
            // ability icon during the click; cancelling means we go back to "this hero is selected".
            if (g.Actors.HasSelectedActor) g.ActorPanel?.Assign();
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

        /// <summary>
        /// Maps an offensive-magic AbilityEffect to its damage element + impact VFX key.
        /// Returns false for effects that are not direct-damage magic (heals, buffs, passives,
        /// item/weapon slots) so the caller can route them elsewhere.
        /// </summary>
        /// <summary>Maps an offensive-magic <see cref="AbilityEffect"/> to its damage element + impact
        /// VFX key. Public so enemy charge-casts (US-026 <c>EnemyChargeSequence</c>) resolve through the
        /// same single source of truth heroes use.</summary>
        public static bool TryGetMagicEffect(AbilityEffect effect, out ElementalDamageType element, out string vfxKey)
        {
            switch (effect)
            {
                case AbilityEffect.Fire:     element = ElementalDamageType.Fire;      vfxKey = "Flame";              return true;
                case AbilityEffect.Fireball: element = ElementalDamageType.Fire;      vfxKey = "Fireball";           return true;
                case AbilityEffect.Fira:     element = ElementalDamageType.Fire;      vfxKey = "FireRain";           return true;
                case AbilityEffect.Ice:      element = ElementalDamageType.Ice;       vfxKey = "IceSparkle";         return true;
                case AbilityEffect.Thunder:  element = ElementalDamageType.Lightning; vfxKey = "LightningExplosion"; return true;
                case AbilityEffect.Smite:    element = ElementalDamageType.Light;     vfxKey = "GodRays";            return true;
                default:                     element = ElementalDamageType.Arcane;    vfxKey = null;                 return false;
            }
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
