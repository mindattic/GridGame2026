using Assets.Helper;
using System.Collections;
using System.Linq;
using UnityEngine;
using g = Assets.Helpers.GameHelper;

namespace Assets.Helpers
{
    public static class DeathHelper
    {
        public static void Process(MonoBehaviour context) => context.StartCoroutine(ProcessRoutine());
        public static IEnumerator ProcessRoutine()
        {
            // find everyone who’s flagged as dying
            var dyingActors = g.Actors.All.Where(x => x.IsDying).ToList();
            if (dyingActors.IsNullOrEmpty())
                yield break;

            // wait until all their HP‐bars are empty
            //yield return new WaitUntil(() => dyingActors.All(x => x.HealthBar.isEmpty));

            // now actually kill them
            foreach (var actor in dyingActors)
            {
                actor.Die();
                yield return Wait.For(Interval.QuarterSecond);
            }
        }
    }

}
