using UnityEngine;

namespace Assets.Helper
{
    /// <summary>
    /// ROTATIONHELPER - Rotation calculation utilities.
    /// 
    /// PURPOSE:
    /// Provides methods for calculating rotations, particularly
    /// for sprites that need to face toward a target.
    /// 
    /// USAGE:
    /// ```csharp
    /// transform.rotation = RotationHelper.ByDirection(target.position, transform.position);
    /// ```
    /// 
    /// NOTE:
    /// Assumes sprite is facing right. If facing up, subtract 90
    /// from angle or fix the sprite orientation.
    /// </summary>
    public static class RotationHelper
    {
        /// <summary>Calculates rotation to face from source toward target.</summary>
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