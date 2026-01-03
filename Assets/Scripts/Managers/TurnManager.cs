// --- File: Assets/Scripts/Managers/TurnManager.cs ---
using Assets.Scripts.Sequences;
using UnityEngine;
using g = Assets.Helpers.GameHelper;
using Assets.Scripts.Models;
using System.Linq;
using System.Collections.Generic;

namespace Assets.Scripts.Managers
{
 public class TurnManager : MonoBehaviour
 {
 public bool IsHeroTurn { get; private set; }
 public bool IsEnemyTurn => !IsHeroTurn;
 public int CurrentTurn =0;
 public ActorInstance ActiveActor { get; private set; }

 private ActorInstance queuedEnemyAfterHero;
 private ActorInstance lastEnemy;

 // Expose whether an enemy is queued due to a timeline tag reaching TriggerX
 public bool HasQueuedEnemyAfterHero => queuedEnemyAfterHero != null && queuedEnemyAfterHero.IsPlaying;

 private ManaPoolManager GetMana()
 {
 var go = GameObject.Find("Game");
 return go != null ? go.GetComponent<ManaPoolManager>() : null;
 }

 public void Initialize() { BeginHeroWindow(); }

 public void QueueEnemyAfterHero(ActorInstance enemy)
 {
 if (enemy != null && enemy.IsEnemy) queuedEnemyAfterHero = enemy;
 }

 private void SelectActiveOrFallback()
 {
 if (ActiveActor != null) { g.SelectionManager.Select(ActiveActor); return; }
 var any = g.Actors.Heroes?.FirstOrDefault(a => a != null && a.IsPlaying) ?? g.Actors.All?.FirstOrDefault(a => a != null && a.IsPlaying);
 if (any != null) g.SelectionManager.Select(any);
 }

 public void NextTurn()
 {
 // end-of-window housekeeping
 bool endingEnemyTurn = !IsHeroTurn; // if we were in enemy turn before advancing

 CurrentTurn++;
 g.StageManager?.OnTurnAdvanced();

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

 private void BeginHeroWindow()
 {
 IsHeroTurn = true;
 ActiveActor = null;
 var mana = GetMana(); if (mana != null) mana.OnTurnStarted(Team.Hero);
 g.InputManager.InputMode = InputMode.PlayerTurn;

 // Make sure the timeline shows all enemies for the upcoming planning window
 g.TimelineBar?.EnsureTagsForAllEnemies(true);
 // Timeline movement is player-driven; keep it paused until the hero starts moving
 g.TimelineBar?.OnHeroStopMove();

 // Auto-bank if remaining time until next enemy is too short for player to act
 float remainingTime = g.TimelineBar?.GetSecondsUntilNextEnemyReachesLeft() ?? float.MaxValue;
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

 public void BeginEnemyTurn(ActorInstance enemy)
 {
 if (enemy == null || !enemy.IsPlaying) return;
 // Prevent starting another enemy turn if already in an enemy turn
 if (IsEnemyTurn && ActiveActor == enemy) return;
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

 private void UpdateActiveIndicators()
 {
 foreach (var a in g.Actors.All)
 {
 if (a == null || !a.IsPlaying) continue;
 a.Render.SetActiveIndicatorEnabled(a == ActiveActor);
 }
 }

 // Optional hook: call from outside after EndTurnSequence if needed
 public void NotifyEnemyTurnFinished()
 {
 if (lastEnemy != null) g.TimelineBar?.OnEnemyTurnFinished(lastEnemy);
 }

 private void HandleHeroTurnFocus() { }
 public void ApplyHeroTurnDesaturation(List<ActorInstance> ignoreList = null) { }
 public void RestoreFullSaturation(List<ActorInstance> ignoreList = null) { }
 }
}
