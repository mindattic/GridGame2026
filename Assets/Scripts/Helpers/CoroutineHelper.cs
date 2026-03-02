using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Factories;
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
using Scripts.Utilities;

namespace Scripts.Helpers
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
