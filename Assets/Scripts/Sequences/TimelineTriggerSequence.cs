using System.Collections;
using g = Assets.Helpers.GameHelper;

namespace Assets.Scripts.Sequences
{
    /// <summary>
    /// TIMELINETRIGGERSEQUENCE - Handles enemy tag reaching timeline trigger.
    /// 
    /// PURPOSE:
    /// When an enemy's timeline tag reaches the trigger point (left edge),
    /// this sequence initiates their turn, handling any in-progress hero actions.
    /// 
    /// TRIGGER FLOW:
    /// 1. Timeline tag moves left during hero window
    /// 2. Tag reaches trigger point (LeftX position)
    /// 3. TimelineBarInstance creates this sequence
    /// 4. If hero being moved → ForceHeroDropSequence first
    /// 5. TurnManager.BeginEnemyTurn(enemy) starts enemy turn
    /// 
    /// FORCE DROP HANDLING:
    /// If the player is mid-drag when an enemy tag triggers:
    /// - ForceHeroDropSequence drops the hero immediately
    /// - Pincer check happens for the forced drop position
    /// - Then enemy turn begins
    /// 
    /// This prevents the player from "stalling" by holding a hero.
    /// 
    /// SEQUENCE FLOW:
    /// ```
    /// [Hero being dragged] → Tag reaches trigger
    ///          ↓
    /// ForceHeroDropSequence (if needed)
    ///          ↓
    /// TurnManager.BeginEnemyTurn()
    ///          ↓
    /// EnemyTakeTurnSequence (queued by TurnManager)
    /// ```
    /// 
    /// RELATED FILES:
    /// - TimelineBarInstance.cs: Creates this when tag triggers
    /// - TimelineTag.cs: Tag that reached trigger
    /// - ForceHeroDropSequence.cs: Handles forced hero drop
    /// - TurnManager.cs: BeginEnemyTurn() method
    /// - SelectionManager.cs: IsHeroBeingMoved property
    /// </summary>
    public sealed class TimelineTriggerSequence : SequenceEvent
    {
        private readonly ActorInstance triggeringEnemy;

        /// <summary>Creates sequence for the enemy whose tag triggered.</summary>
        public TimelineTriggerSequence(ActorInstance enemy)
        {
            triggeringEnemy = enemy;
        }

        /// <summary>
        /// Forces hero drop if needed, then begins enemy turn.
        /// </summary>
        public override IEnumerator ProcessRoutine()
        {
            UnityEngine.Debug.Log($"[TimelineTriggerSequence] ProcessRoutine for {triggeringEnemy?.name ?? "null"}");

            if (triggeringEnemy == null || !triggeringEnemy.IsPlaying)
                yield break;

            // Step 1: Force drop any hero that's being moved
            var hero = g.Actors.SelectedActor;
            if (hero != null && hero.IsHero && g.SelectionManager.IsHeroBeingMoved)
            {
                g.SequenceManager.Add(new ForceHeroDropSequence(hero));
                yield return g.SequenceManager.ExecuteRoutine();
            }

            // Step 2: Begin the enemy turn
            g.TurnManager.BeginEnemyTurn(triggeringEnemy);
        }
    }
}
