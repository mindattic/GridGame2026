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
