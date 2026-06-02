// --- File: Assets/Scripts/Managers/TurnManager.cs ---
using Scripts.Sequences;
using UnityEngine;
using g = Scripts.Helpers.GameHelper;
using Scripts.Models;
using System.Linq;
using System.Collections.Generic;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Factories;
using Scripts.Helpers;
using Scripts.Hub;
using Scripts.Instances;
using Scripts.Instances.Actor;
using Scripts.Instances.Board;
using Scripts.Instances.SynergyLine;
using Scripts.Inventory;
using Scripts.Libraries;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Managers
{
    /// <summary>
    /// TURNMANAGER - Controls turn order and turn flow in combat.
    /// 
    /// PURPOSE:
    /// Manages the turn-based combat system, alternating between hero turns
    /// (player input) and enemy turns (AI-controlled).
    /// 
    /// TURN STRUCTURE:
    /// 1. Hero Window - Player can move any hero, triggers pincer attacks
    /// 2. Enemy Turn - Individual enemy takes action via EnemyTakeTurnSequence
    /// 3. Repeat until victory/defeat
    /// 
    /// KEY PROPERTIES:
    /// - IsHeroTurn: True during hero input phase
    /// - IsEnemyTurn: True during enemy action phase  
    /// - CurrentTurn: Incremented each turn cycle
    /// - ActiveActor: Currently acting character (for UI highlighting)
    /// 
    /// TIMELINE INTEGRATION:
    /// Works with TimelineBarInstance to display turn order visually.
    /// Timeline tags can trigger enemy turns when they reach trigger position.
    /// QueueEnemyAfterHero() stores an enemy to act after current hero window.
    /// 
    /// TURN FLOW:
    /// 1. Initialize() → BeginHeroWindow()
    /// 2. Player drops hero → PincerAttackManager.Check()
    /// 3. Combat resolves → NextTurn()
    /// 4. If enemy queued → BeginEnemyTurn(enemy)
    /// 5. Enemy acts → NextTurn() → BeginHeroWindow()
    /// 
    /// LLM CONTEXT:
    /// Access via g.TurnManager. Check IsHeroTurn to know if player input
    /// is expected. Call NextTurn() after combat sequences complete.
    /// </summary>
    public class TurnManager : MonoBehaviour
    {
        #region Turn State

        /// <summary>True when it's the player's turn to move heroes.</summary>
        public bool IsHeroTurn { get; private set; }

        /// <summary>True when an enemy is taking their turn.</summary>
        public bool IsEnemyTurn => !IsHeroTurn;

        /// <summary>Current turn number (increments each NextTurn call).</summary>
        public int CurrentTurn = 0;

        /// <summary>The actor currently taking their turn (for UI highlighting).</summary>
        public ActorInstance ActiveActor { get; private set; }

        /// <summary>
        /// True while a spell cast is mid-resolution (icon parked at u=1, effect playing out).
        /// Acts as a third turn state alongside hero/enemy turns — input is suspended
        /// (InputMode = None) regardless of whose window owned the cast.
        /// </summary>
        public bool IsResolvingCast { get; private set; }

        #endregion

        #region Queued Enemy State

        /// <summary>Enemy queued to act after current hero window (from timeline trigger).</summary>
        private ActorInstance queuedEnemyAfterHero;

        /// <summary>Last enemy that took a turn (for cleanup).</summary>
        private ActorInstance lastEnemy;

        /// <summary>True if an enemy is queued due to timeline tag reaching trigger position.</summary>
        public bool HasQueuedEnemyAfterHero => queuedEnemyAfterHero != null && queuedEnemyAfterHero.IsPlaying;

        #endregion

        #region Initialization

        /// <summary>Gets ManaPoolManager reference.</summary>
        private ManaPoolManager GetMana()
        {
            var go = GameObject.Find("Game");
            return go != null ? go.GetComponent<ManaPoolManager>() : null;
        }

        /// <summary>Initializes turn manager and begins first hero window.</summary>
        public void Initialize() { SkillCooldownManager.Clear(); ThreatTracker.Clear(); BeginHeroWindow(); }

        #endregion

        #region Enemy Queuing

        /// <summary>
        /// Queues an enemy to take their turn after the current hero window ends.
        /// Called by timeline system when enemy tag reaches trigger position.
        /// </summary>
        public void QueueEnemyAfterHero(ActorInstance enemy)
        {
            if (enemy != null && enemy.IsEnemy) queuedEnemyAfterHero = enemy;
        }

        #endregion

        #region Selection

        /// <summary>Selects the active actor or falls back to any available hero/actor.</summary>
        private void SelectActiveOrFallback()
        {
            if (ActiveActor != null) { g.SelectionManager.Select(ActiveActor); return; }
            var any = g.Actors.Heroes?.FirstOrDefault(a => a != null && a.IsPlaying) ?? g.Actors.All?.FirstOrDefault(a => a != null && a.IsPlaying);
            if (any != null) g.SelectionManager.Select(any);
        }

        #endregion

        #region Turn Advancement

        /// <summary>
        /// Advances to the next turn. Handles enemy queue, wave spawning, and turn cycling.
        /// Called after combat sequences complete.
        /// </summary>
        public void NextTurn()
        {
            // end-of-window housekeeping
            bool endingEnemyTurn = !IsHeroTurn; // if we were in enemy turn before advancing

            CurrentTurn++;
            g.StageManager?.OnTurnAdvanced();

            // US-016: turn-unit buffs (Slowed/Silenced/Blinded) decrement at the END of the
            // bearer's turn — end-of-turn semantics (Legion panel, 2026-05-31) so a "2 Turns"
            // debuff affects 2 of the bearer's own turns rather than losing one to off-by-one.
            // NextTurn is the single turn boundary; tick whichever side just finished.
            if (endingEnemyTurn)
            {
                if (lastEnemy != null) BuffSystem.TickTurn(lastEnemy);
            }
            else
            {
                // Heroes share one free-form window, so tick them all once as that window ends.
                var heroesEndingTurn = g.Actors.Heroes;
                if (heroesEndingTurn != null)
                    foreach (var h in heroesEndingTurn)
                        if (h != null && h.IsPlaying) BuffSystem.TickTurn(h);
            }

            // If an enemy was queued (from timeline tag hit) take their turn now
            if (queuedEnemyAfterHero != null && queuedEnemyAfterHero.IsPlaying)
            {
                var enemy = queuedEnemyAfterHero; queuedEnemyAfterHero = null;
                BeginEnemyTurn(enemy);
                return;
            }

            // If we are finishing an enemy's turn, reset that enemy's timeline state before returning to hero
 if (endingEnemyTurn)
 {
 NotifyEnemyTurnFinished();
 }


 BeginHeroWindow();
 }

 /// <summary>Begin hero window.</summary>
 private void BeginHeroWindow()
 {
 IsHeroTurn = true;
 ActiveActor = null;
 // Tick skill cooldowns once per turn-cycle: a skill locked for N turns reactivates when its
 // per-hero counter reaches 0 at the start of a window (see SkillCooldownManager / AbilityBar).
 SkillCooldownManager.TickAll();
 var mana = GetMana(); if (mana != null) mana.OnTurnStarted(Team.Hero);
 g.InputManager.InputMode = InputMode.PlayerTurn;

 // Reset the trigger flag so new enemy triggers can happen
 g.TimelineBar?.ResetTriggerFlag();
 
 // Make sure the timeline shows all enemies for the upcoming planning window
 g.TimelineBar?.EnsureTagsForAllEnemies(true);
 // Timeline movement is player-driven; keep it paused until the hero starts moving
 g.TimelineBar?.OnHeroStopMove();

 // Auto-bank if remaining time until next enemy is too short for player to act
 float remainingTime = g.TimelineBar?.GetSecondsUntilNextEnemyReachesTrigger() ?? float.MaxValue;
 if (remainingTime < 0.1f)
 {
 g.ManaPoolManager?.OnBankButtonClicked();
 return; // Bank will handle starting the enemy turn
 }

 SelectActiveOrFallback();
 UpdateActiveIndicators();
 g.SequenceManager.Add(new HeroStartSequence());
 g.SequenceManager.Execute();
 }

 /// <summary>Begin enemy turn.</summary>
 public void BeginEnemyTurn(ActorInstance enemy)
 {
 if (enemy == null || !enemy.IsPlaying) return;
 // Prevent starting another enemy turn if already in an enemy turn (any enemy)
 if (IsEnemyTurn) return;
 
 IsHeroTurn = false; ActiveActor = enemy; lastEnemy = enemy;
 var mana = GetMana(); if (mana != null) mana.OnTurnStarted(Team.Enemy);
 g.InputManager.InputMode = InputMode.EnemyTurn;
 g.SelectionManager.Select(enemy);
 UpdateActiveIndicators();
 // Notify bar that enemy has started
 g.TimelineBar?.OnEnemyTurnStarted(enemy);
 g.SequenceManager.Add(new EnemyTakeTurnSequence(enemy));
 g.SequenceManager.Execute();
 }

 /// <summary>Updates the active indicators.</summary>
 private void UpdateActiveIndicators()
 {
 foreach (var a in g.Actors.All)
 {
 if (a == null || !a.IsPlaying) continue;
 a.Render.SetActiveIndicatorEnabled(a == ActiveActor);
 }
 }

 // Optional hook: call from outside after EndTurnSequence if needed
 /// <summary>Notify enemy turn finished.</summary>
 public void NotifyEnemyTurnFinished()
 {
 if (lastEnemy != null) g.TimelineBar?.OnEnemyTurnFinished(lastEnemy);
 }

 /// <summary>
 /// Marks the start of a cast-resolution window: input is suspended, the timeline is
 /// frozen (all other icons stop marching, mana accrual halts, no enemy triggers can
 /// fire), and the spell icon is parked at u=1 while effect sequences play out. Caller
 /// is the spell-icon onReached closure in TimelineBarInstance.SpawnSpellIcon.
 /// </summary>
 public void BeginCastResolution()
 {
 IsResolvingCast = true;
 if (g.InputManager != null) g.InputManager.InputMode = InputMode.None;
 g.TimelineBar?.PauseForCastResolution();
 }

 /// <summary>
 /// Marks the end of a cast-resolution window. Subsequent BeginHeroWindow /
 /// BeginEnemyTurn restores the appropriate InputMode — this just clears the flag
 /// so other systems stop treating the bar as suspended.
 /// </summary>
 public void EndCastResolution()
 {
 IsResolvingCast = false;
 }

 #endregion
 }
}
