using System.Collections;

namespace Assets.Scripts.Sequences
{
    /// <summary>
    /// SEQUENCEEVENT - Abstract base class for all async gameplay sequences.
    /// 
    /// PURPOSE:
    /// Base class that all sequence types inherit from. Provides the interface
    /// for the SequenceManager to execute sequences uniformly.
    /// 
    /// IMPLEMENTATION:
    /// Subclasses must implement ProcessRoutine() as an IEnumerator coroutine.
    /// The coroutine can yield for timing, animations, or other async operations.
    /// 
    /// COMMON YIELD PATTERNS:
    /// ```csharp
    /// yield return Wait.None();           // One frame
    /// yield return Wait.Seconds(0.5f);    // Half second
    /// yield return someCoroutine;         // Wait for another coroutine
    /// yield return new WaitForSeconds(t); // Unity's built-in wait
    /// ```
    /// 
    /// SEQUENCE TYPES (inherit from this):
    /// - PincerAttackSequence: Pincer combat resolution
    /// - EnemyTakeTurnSequence: Enemy turn orchestration
    /// - EnemyMoveSequence: Enemy movement
    /// - EnemyAttackSequence: Enemy attack
    /// - DeathSequence: Death processing
    /// - EndTurnSequence: Turn advancement
    /// - VictorySequence: Stage victory
    /// - DefeatSequence: Party wipe
    /// - SpawnWaveSequence: Enemy wave spawning
    /// - TimelineTriggerSequence: Timeline tag triggers
    /// 
    /// EXECUTION:
    /// ```csharp
    /// g.SequenceManager.Add(new MySequence());
    /// g.SequenceManager.Execute();
    /// ```
    /// 
    /// RELATED FILES:
    /// - SequenceManager.cs: Queues and executes sequences
    /// - Wait.cs: Common yield return helpers
    /// </summary>
    public abstract class SequenceEvent
    {
        /// <summary>
        /// Main execution coroutine for this sequence.
        /// Must be implemented by all sequence types.
        /// </summary>
        /// <returns>IEnumerator for coroutine execution.</returns>
        public abstract IEnumerator ProcessRoutine();
    }
}
