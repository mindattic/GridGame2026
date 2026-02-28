using UnityEngine;

namespace Assets.Helper
{
    /// <summary>
    /// LOCATIONHELPER - Board location constants and utilities.
    /// 
    /// PURPOSE:
    /// Provides sentinel values and utilities for board grid locations.
    /// 
    /// CONSTANTS:
    /// - Nowhere: Sentinel value for "no valid location" (-1000, -1000)
    /// 
    /// USAGE:
    /// ```csharp
    /// // Check if location is valid
    /// if (location == LocationHelper.Nowhere) return;
    /// 
    /// // Return sentinel when no tile found
    /// return tile == null ? LocationHelper.Nowhere : tile.location;
    /// ```
    /// 
    /// WHY -1000?
    /// Value far outside any reasonable board bounds ensures
    /// it won't accidentally match valid coordinates.
    /// 
    /// RELATED FILES:
    /// - TileInstance.cs: Uses for invalid locations
    /// - RNG.cs: Returns Nowhere when no valid tile found
    /// - Geometry.cs: Coordinate calculations
    /// </summary>
    public static class LocationHelper
    {
        /// <summary>Sentinel value representing "no valid location".</summary>
        public static Vector2Int Nowhere = new Vector2Int(-1000, -1000);
    }
}