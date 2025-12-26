// --- File: Assets/Scripts/Managers/SequenceManager.cs ---
using Assets.Scripts.Models;
using Assets.Scripts.Sequences;
using System;
using System.Collections;
using UnityEngine;

public class SequenceManager : MonoBehaviour
{

    //Members
    private QueueCollection<SequenceEvent> queue = new QueueCollection<SequenceEvent>();
    private bool isExecuting;
    private Coroutine runningCoroutine;

    //Properties
    public string lastStartedSequenceName { get; private set; }
    public string lastCompletedSequenceName { get; private set; }
    public bool IsExecuting => isExecuting;
    public int Count => queue.Count;

    //Events
    public event Action OnSequenceComplete;
    public event Action<SequenceEvent> OnSequenceEventStarted;
    public event Action<SequenceEvent> OnSequenceEventCompleted;

    /// <summary>
    /// Adds a SequenceEvent to the end of the queue.
    /// </summary>
    public void Add(SequenceEvent e)
    {
        if (e == null)
            return;

        queue.Add(e);
    }

    /// <summary>
    /// Adds a SequenceEvent to the front of the queue.
    /// </summary>
    public void AddFirst(SequenceEvent e)
    {
        if (e == null)
            return;

        queue.AddFirst(e);
    }

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
}
