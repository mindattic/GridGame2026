using Scripts.Models;
using System.Collections;
using g = Scripts.Helpers.GameHelper;
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
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Sequences
{
    public class PortraitPopInSequence : SequenceEvent
    {
        private ActorInstance actor;
        public PortraitPopInSequence(ActorInstance actor)
        {
            this.actor = actor;
        }

        /// <summary>Coroutine that executes the process sequence.</summary>
        public override IEnumerator ProcessRoutine()
        {
            float scale;

            if (actor.HasAdjacent(Direction.North, 1))
                scale = 0.075f;
            else if (actor.HasAdjacent(Direction.North, 2))
                scale = 0.1f;
            else
                scale = 0.1666f;

            yield return g.PortraitManager.PopInRoutine(actor, scale);
        }
    }


}
