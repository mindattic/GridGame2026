using System;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.Utilities
{
    /// <summary>
    /// Centralized readiness barrier for game-wide initialization.
    /// Use this to delay components that depend on scene singletons (e.g., GameManager) until
    /// the game signals it is fully initialized. Prevents null-reference races during scene loads.
    /// </summary>
    /// <remarks>
    /// - Call <see cref="Confirm"/> once, typically at the end of GameManager.Start(), after all systems are ready.
    /// - Call <see cref="Begin(MonoBehaviour)"/> in Awake() of dependent components to auto-disable and re-enable when ready.
    /// - Or use <see cref="WhenReady(MonoBehaviour, Action)"/> to run code once the game is ready.
    /// - Threading: Keep all calls on Unity's main thread; enabling/disabling components must be done on the main thread.
    /// </remarks>
    public static class GameReady
    {
        /// <summary>
        /// Backing task used to await readiness in async contexts.
        /// Completes exactly once when <see cref="Confirm"/> is called.
        /// </summary>
        private static TaskCompletionSource<bool> tsc = new TaskCompletionSource<bool>();

        /// <summary>
        /// Awaitable task that completes when the game is ready.
        /// Example: await GameReady.Ready; inside an async Start().
        /// </summary>
        public static Task Ready => tsc.Task;

        /// <summary>
        /// True after readiness was confirmed. Safe to query in guards for Update/Start.
        /// </summary>
        public static bool IsReady => tsc.Task.IsCompletedSuccessfully;

        /// <summary>
        /// Event fired exactly once when readiness is confirmed.
        /// Subscribers added after readiness will never be invoked (use <see cref="IsReady"/> to short-circuit).
        /// </summary>
        public static event Action OnReady;

        /// <summary>
        /// Signals that the game has finished initialization.
        /// Idempotent: subsequent calls are ignored.
        /// </summary>
        /// <remarks>
        /// Typical call site: at the end of GameManager.Start(), after all systems are initialized.
        /// This invokes <see cref="OnReady"/> once and transitions <see cref="Ready"/> to a completed task.
        /// </remarks>
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
