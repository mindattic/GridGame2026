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
    public static class TouchHelper
    {
        /// <summary>
        /// Returns the first ActorInstance at the current touch position, or null if none are found.
        /// </summary>
        public static ActorInstance GetActorAtTouchPosition()
        {
            return Physics2D
                .OverlapPointAll(g.TouchPosition3D)?
                .Select(collider => collider.GetComponent<ActorInstance>())
                .FirstOrDefault(actor => actor != null);
        }
    }
}
