using System;
using System.Threading.Tasks;
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
using Scripts.Managers;
using Scripts.Models;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;

namespace Scripts.Utilities
{
    /// <summary>
    /// GAMEREADY - Initialization synchronization barrier.
    /// 
    /// PURPOSE:
    /// Prevents null-reference races by providing a centralized
    /// readiness signal that dependent components can await.
    /// 
    /// USAGE:
    /// ```csharp
    /// // In GameManager.Start(), after all init:
    /// GameReady.Confirm();
    /// 
    /// // In dependent components:
    /// void Awake() => GameReady.Begin(this);
    /// // -or-
    /// GameReady.WhenReady(this, () => Initialize());
    /// // -or-
    /// await GameReady.Ready;
    /// ```
    /// 
    /// PROPERTIES:
    /// - IsReady: True after Confirm() called
    /// - Ready: Awaitable Task
    /// - OnReady: Event fired once on confirmation
    /// 
    /// RELATED FILES:
    /// - GameManager.cs: Calls Confirm()
    /// </summary>
    public static class GameReady
    {
        private static TaskCompletionSource<bool> tsc = new TaskCompletionSource<bool>();

        /// <summary>Awaitable task that completes when game is ready.</summary>
        public static Task Ready => tsc.Task;

        /// <summary>True after readiness was confirmed.</summary>
        public static bool IsReady => tsc.Task.IsCompletedSuccessfully;

        /// <summary>Event fired once when readiness is confirmed.</summary>
        public static event Action OnReady;

        /// <summary>
        /// Signals that the game has finished initialization.
        /// Idempotent: subsequent calls are ignored.
        /// </summary>
        public static void Confirm()
        {
            if (IsReady) return;                  // Do nothing if already signaled
            tsc.TrySetResult(true);               // Complete the task for awaiters

            var h = OnReady;                      // Snapshot to avoid race conditions
            OnReady = null;                       // Ensure single invocation
            h?.Invoke();                          // Notify listeners
        }

        /// <summary>
        /// Gates a MonoBehaviour until the game is ready.
        /// Disables the component now and re-enables it automatically when <see cref="Confirm"/> is called.
        /// </summary>
        /// <param name="owner">The component to gate.</param>
        /// <remarks>
        /// - Safe to call from Awake().
        /// - No effect if already ready.
        /// - Do NOT call this on the system that will invoke <see cref="Confirm"/> (e.g., GameManager).
        /// </remarks>
        public static void Begin(MonoBehaviour owner)
        {
            if (owner == null || IsReady) return;           // Nothing to gate
            owner.enabled = false;                           // Pause lifecycle (Start/Update won't run)
            WhenReady(owner, () => owner.enabled = true);    // Resume once ready
        }

        /// <summary>
        /// Schedules an action to run once the game is ready, or runs immediately if already ready.
        /// The subscription is removed if the owner is destroyed before readiness.
        /// </summary>
        /// <param name="owner">Owning component; used to auto-unsubscribe if it gets destroyed.</param>
        /// <param name="action">Action to invoke upon readiness.</param>
        /// <remarks>
        /// Use this to delay one-off work (e.g., event subscriptions) without disabling the component.
        /// </remarks>
        public static void WhenReady(MonoBehaviour owner, Action action)
        {
            if (IsReady)
            {
                action?.Invoke();       // Already ready: run immediately
                return;
            }

            void Handler()
            {
                if (owner == null)      // Owner destroyed before ready: clean up subscription
                {
                    OnReady -= Handler;
                    return;
                }
                action?.Invoke();
                OnReady -= Handler;      // One-shot subscription
            }

            OnReady += Handler;
        }
    }
}
