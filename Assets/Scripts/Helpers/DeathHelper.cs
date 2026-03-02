using Scripts.Helpers;
using System.Collections;
using System.Linq;
using UnityEngine;
using g = Scripts.Helpers.GameHelper;
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
