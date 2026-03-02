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
    /// ALIGNMENTHELPER - Range and bounds checking utilities.
    /// 
    /// PURPOSE:
    /// Provides simple methods for checking if values are
    /// within ranges or between bounds.
    /// 
    /// METHODS:
    /// - IsInRange: Check if value is within +/- range of target
    /// - IsBetween: Check if value is between two bounds (exclusive)
    /// </summary>
    public static class AlignmentHelper
    {
        /// <summary>Check if a is within +/- range of b.</summary>
        public static bool IsInRange(float a, float b, float range)
        {
            return a <= b + range && a >= b - range;
        }

        /// <summary>Check if a is between b and c (exclusive).</summary>
        public static bool IsBetween(float a, float b, float c)
        {
            return a > b && a < c;
        }
    }
}
