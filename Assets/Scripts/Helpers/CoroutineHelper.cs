using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Helper
{
    /// <summary>
    /// COROUTINEHELPER - Coroutine coordination utilities.
    /// 
    /// PURPOSE:
    /// Provides methods for running multiple coroutines
    /// and waiting for all to complete.
    /// 
    /// USAGE:
    /// ```csharp
    /// yield return CoroutineHelper.WaitForAll(this,
    ///     MoveRoutine(),
    ///     FadeRoutine(),
    ///     ScaleRoutine()
    /// );
    /// ```
    /// 
    /// RELATED FILES:
    /// - Wait.cs: Simple wait utilities
    /// </summary>
    public static class CoroutineHelper
    {
        /// <summary>Runs all coroutines in parallel and waits for all to complete.</summary>
        public static IEnumerator WaitForAll(MonoBehaviour context, params IEnumerator[] coroutines)
        {
            var runningCoroutines = new List<Coroutine>();

            foreach (var coroutine in coroutines)
            {
                runningCoroutines.Add(context.StartCoroutine(coroutine));
            }

            foreach (var coroutine in runningCoroutines)
            {
                yield return coroutine;
            }
        }
    }
}