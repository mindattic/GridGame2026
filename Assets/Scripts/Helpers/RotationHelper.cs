using UnityEngine;

namespace Assets.Helper
{

    public static class RotationHelper
    {
        ///<summary>
        ///Assumes sprite is facing right, if facing up subtract 90 from angle (or fix sprite)
        ///</summary>
        ///<param name="target"></param>
        ///<param name="source"></param>
        ///<returns></returns>
        public static Quaternion ByDirection(Vector3 target, Vector3 source)
        {
            var direction = target - source;
            var angle = Vector2.SignedAngle(Vector2.right, direction);
            var targetRotation = new Vector3(0, 0, angle);
            var rotation = Quaternion.Euler(targetRotation);
            return rotation;
        }
    }
}