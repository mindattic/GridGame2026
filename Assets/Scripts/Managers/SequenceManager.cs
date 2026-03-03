// --- File: Assets/Scripts/Managers/SequenceManager.cs ---
using Scripts.Models;
using Scripts.Sequences;
using System;
using System.Collections;
using UnityEngine;
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
/// SEQUENCEMANAGER - Async gameplay event queue and executor.
/// 
/// PURPOSE:
/// Manages a queue of SequenceEvent objects (IEnumerator coroutines) that execute
/// in order. Used for combat animations, turn resolution, VFX, and any gameplay
/// that needs to happen sequentially.
/// 
/// USAGE:
/// ```csharp
/// g.SequenceManager.Add(new PincerAttackSequence(participants));
/// g.SequenceManager.Add(new VictorySequence());
/// g.SequenceManager.Execute(); // Start processing queue
/// ```
/// 
/// EXECUTION MODEL:
/// - Sequences execute one at a time, FIFO order
/// - New sequences can be added during execution (will run after current batch)
/// - AddFirst() inserts at front for priority sequences
/// - IsExecuting property indicates if queue is being processed
/// 
/// EVENTS:
/// - OnSequenceComplete: Fired when entire queue is drained
/// - OnSequenceEventStarted: Fired when individual sequence begins
/// - OnSequenceEventCompleted: Fired when individual sequence ends
/// 
/// SEQUENCE TYPES (see Assets/Scripts/Sequences/):
/// - PincerAttackSequence: Resolves pincer combat
/// - EnemyTakeTurnSequence: Enemy AI turn resolution
/// - EnemyAttackSequence: Single enemy attack animation
/// - VictorySequence/DefeatSequence: Battle end handling
/// - SpawnWaveSequence: Enemy wave spawning
/// - TimelineTriggerSequence: Timeline-based turn triggers
/// 
/// LLM CONTEXT:
/// This is the core async gameplay coordinator. When combat happens, sequences
/// are queued and executed in order. Check IsExecuting to know if sequences
/// are running. Access via g.SequenceManager.
/// </summary>
public class SequenceManager : MonoBehaviour
{
    #region Fields

    /// <summary>Queue of pending sequence events to execute.</summary>
    private QueueCollection<SequenceEvent> queue = new QueueCollection<SequenceEvent>();

    /// <summary>True while the queue is being processed.</summary>
    private bool isExecuting;

    /// <summary>Reference to currently running coroutine.</summary>
    private Coroutine runningCoroutine;

    #endregion

    #region Properties

    /// <summary>Name of the most recently started sequence (for debugging).</summary>
    public string lastStartedSequenceName { get; private set; }

    /// <summary>Name of the most recently completed sequence (for debugging).</summary>
    public string lastCompletedSequenceName { get; private set; }

    /// <summary>True if sequences are currently being processed.</summary>
    public bool IsExecuting => isExecuting;

    /// <summary>Number of sequences waiting in queue.</summary>
    public int Count => queue.Count;

    #endregion

    #region Events

    /// <summary>Fired when the entire queue has been drained.</summary>
    public event Action OnSequenceComplete;

    /// <summary>Fired when an individual sequence begins execution.</summary>
    public event Action<SequenceEvent> OnSequenceEventStarted;

    /// <summary>Fired when an individual sequence completes.</summary>
    public event Action<SequenceEvent> OnSequenceEventCompleted;

    #endregion

    #region Queue Operations

    /// <summary>
    /// Adds a SequenceEvent to the end of the queue.
    /// Call Execute() to begin processing if not already running.
    /// </summary>
    public void Add(SequenceEvent e)
    {
        if (e == null)
            return;

        // Debug: track when EnemyAttackSequence is added
        if (e.GetType().Name == "EnemyAttackSequence")
            UnityEngine.Debug.Log($"[SequenceManager] ADD EnemyAttackSequence - queue count before: {queue.Count}, stack: {UnityEngine.StackTraceUtility.ExtractStackTrace()}");

        queue.Add(e);
    }

    /// <summary>
    /// Adds a SequenceEvent to the FRONT of the queue (priority insertion).
    /// Use for sequences that must execute before pending ones.
    /// </summary>
    public void AddFirst(SequenceEvent e)
    {
        if (e == null)
            return;

        queue.AddFirst(e);
    }

    #endregion

    #region Execution

    /// <summary>
    /// Begins processing the sequence queue if not already executing.
    /// Sequences run one at a time until queue is empty.
    /// </summary>
    public void Execute()
    {
        //Nothing is queued
        if (queue.Count == 0)
            return;

        //Already draining the queue. Items added during execution will be picked up.
        if (isExecuting)
            return;

        runningCoroutine = StartCoroutine(ExecuteRoutine());
    }

    /// <summary>
    /// Executes all queued SequenceEvents one by one, then raises OnSequenceComplete.
    /// Controls the isExecuting state and the coroutine handle.
    /// </summary>
    public IEnumerator ExecuteRoutine()
    {
        if (isExecuting)
            yield break;

        isExecuting = true;
        SequenceEvent current = null;

        try
        {
            while (queue.Count > 0)
            {
                current = queue.Remove();

                // Track start
                lastStartedSequenceName = current?.GetType().Name;

                OnSequenceEventStarted?.Invoke(current);

                // Run to completion
                if (current != null)
                    yield return StartCoroutine(current.ProcessRoutine());

                // Track completion
                lastCompletedSequenceName = current?.GetType().Name;

                OnSequenceEventCompleted?.Invoke(current);
            }

            // Batch complete
            OnSequenceComplete?.Invoke();
        }
        finally
        {
            isExecuting = false;
            runningCoroutine = null;
        }
    }

    /// <summary>Called when the component becomes disabled.</summary>
    private void OnDisable()
    {
        // DespawnRoutine active run
        if (runningCoroutine != null)
        {
            StopCoroutine(runningCoroutine);
            runningCoroutine = null;
        }

        // Reset and drop pending items to avoid leaking into next scene
        isExecuting = false;
        queue.Clear();
    }

    /// <summary>
    /// Returns a human readable line that summarizes recent activity.
    /// </summary>
    public string GetDetails()
    {
        string started = lastStartedSequenceName ?? "-";
        string completed = lastCompletedSequenceName ?? "-";

        return $"Executing={isExecuting}\nStarted={started}\nCompleted={completed}\nQueueCount={queue.Count}";
    }

    #endregion
}

}
