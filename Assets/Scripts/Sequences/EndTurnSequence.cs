// --- File: Assets/Scripts/Sequences/EndTurnSequence.cs ---
using System.Collections;
using g = Scripts.Helpers.GameHelper;
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
using Scripts.Managers;
using Scripts.Models;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Sequences
{
    /// <summary>
    /// ENDTURNSEQUENCE - Completes the current turn and advances to next.
    /// 
    /// PURPOSE:
    /// Final sequence in a turn's sequence chain. Calls TurnManager.NextTurn()
    /// to advance the game state to the next turn.
    /// 
    /// TURN TRANSITION:
    /// - Yields one frame for pacing (visual smoothness)
    /// - Calls g.TurnManager.NextTurn()
    /// - TurnManager determines if next is hero or enemy turn
    /// 
    /// WHAT NEXTTURN() DOES:
    /// 1. Increments CurrentTurn counter
    /// 2. Notifies StageManager (wave spawning check)
    /// 3. Checks for queued enemies (from timeline triggers)
    /// 4. Returns to hero window or starts enemy turn
    /// 
    /// SEQUENCE CHAIN POSITION:
    /// Always LAST in a sequence chain:
    /// - PincerAttackSequence
    /// - DeathSequence
    /// - WaveCheckSequence
    /// - EndTurnSequence ← Here
    /// 
    /// For enemy turns:
    /// - EnemyTakeTurnSequence
    /// - EnemyMoveSequence
    /// - EnemyAttackSequence
    /// - DeathSequence
    /// - EndTurnSequence ← Here
    /// 
    /// IMPORTANT:
    /// Does NOT touch timeline directly - TurnManager handles that
    /// to avoid double-advancing the timeline bar.
    /// 
    /// RELATED FILES:
    /// - TurnManager.cs: NextTurn() method
    /// - StageManager.cs: OnTurnAdvanced() for wave checks
    /// - TimelineBarInstance.cs: Timeline state management
    /// </summary>
    public class EndTurnSequence : SequenceEvent
    {
        /// <summary>
        /// Yields one frame then advances to next turn.
        /// </summary>
        public override IEnumerator ProcessRoutine()
        {
            yield return Wait.None();

            g.TurnManager.NextTurn();
        }
    }
}
