using System.Linq;
using UnityEngine;
using g = Assets.Helpers.GameHelper;

namespace Assets.Helpers
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
