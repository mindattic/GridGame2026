using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Helper
{

    public static class CoroutineHelper
    {
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