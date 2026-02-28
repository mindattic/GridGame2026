using Assets.Scripts.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using g = Assets.Helpers.GameHelper;

/// <summary>
/// GEOMETRY - Spatial utilities for grid-to-world coordinate conversion.
/// 
/// PURPOSE:
/// Provides helper methods for converting between grid coordinates (Vector2Int)
/// and world positions (Vector3), calculating distances, finding adjacent tiles,
/// and determining spatial relationships between actors.
/// 
/// COORDINATE SYSTEMS:
/// - Grid: Vector2Int(column, row) - 0-indexed from top-left
/// - World: Vector3(x, y, z) - Unity world space
/// - Column 0 = leftmost, Row 0 = topmost
/// 
/// KEY METHODS:
/// 
/// POSITION CONVERSION:
/// - CalculatePositionByLocation(loc): Grid → World position
/// - GetPositionByLocation(loc): Grid → World (via TileMap)
/// - GetLocationByPosition(pos): World → Grid location
/// 
/// TILE FINDING:
/// - GetClosestTile(pos): Find nearest tile to world position
/// - GetTileAtLocation(loc): Get tile at grid location
/// - GetLocationsBetween(a, b): All grid locations between two points
/// 
/// SPATIAL CHECKS:
/// - IsSameRow(a, b): True if same Y coordinate
/// - IsSameColumn(a, b): True if same X coordinate
/// - IsAdjacent(a, b): True if cardinally adjacent
/// - GetDirection(from, to): Direction enum between locations
/// - GetDistance(a, b): Manhattan distance between locations
/// 
/// PINCER DETECTION SUPPORT:
/// - GetLocationsBetween() is key for pincer validation
/// - Returns all tiles in a straight line between two locations
/// 
/// LLM CONTEXT:
/// Use this class for any grid/world coordinate work. It's used heavily by
/// PincerAttackManager for detecting valid pincers, by InputManager for
/// drag-drop positioning, and by ActorMovement for pathfinding.
/// </summary>
public class Geometry
{
    // Default constructor (not used but provided for completeness).
    public Geometry() { }

    #region Position Conversion

    /// <summary>
    /// Calculates a world position from a given grid location using the board offset and tile size.
    /// </summary>
    /// <param name="location">Grid coordinates (Vector2Int) of the tile.</param>
    /// <returns>World position (Vector3) corresponding to the grid location.</returns>
    public static Vector3 CalculatePositionByLocation(Vector2Int location)
    {
        // Calculate s position: start from board offset and add tileSize multiplied by the s coordinate.
        float x = g.Board.offset.x + (g.TileSize * location.x);
        // Calculate y position: start from board offset and subtract tileSize multiplied by the y coordinate.
        float y = g.Board.offset.y + -(g.TileSize * location.y);
        return new Vector3(x, y, 0);
    }

    /// <summary>
    /// Retrieves the world position corresponding to a grid location using the TileMap lookup.
    /// </summary>
    /// <param name="location">Grid coordinates (Vector2Int).</param>
    /// <returns>World position (Vector3) of the tile.</returns>
    public static Vector3 GetPositionByLocation(Vector2Int location)
    {
        return g.TileMap.GetPosition(location);
    }

    /// <summary>
    /// Retrieves the grid location corresponding to a given world position using the TileMap lookup.
    /// </summary>
    /// <param name="position">World position (Vector3).</param>
    /// <returns>Grid coordinates (Vector2Int) of the tile.</returns>
    public static Vector2Int GetLocationByPosition(Vector3 position)
    {
        return g.TileMap.GetLocation(position);
    }

    #endregion

    #region Tile Finding

    /// <summary>
    /// Finds the tile actors closest to a given world position.
    /// </summary>
    /// <param name="position">World position to search near.</param>
    /// <returns>The closest TileInstance based on Euclidean distance.</returns>
    public static TileInstance GetClosestTile(Vector3 position)
    {
        return g.Tiles.OrderBy(x => Vector3.Distance(x.transform.position, position)).First();
    }

    /// <summary>
    /// Finds the tile actors at a specified grid location.
    /// </summary>
    /// <param name="location">Grid location (Vector2Int) to look up.</param>
    /// <returns>The TileInstance at that location.</returns>
    public static TileInstance GetClosestTile(Vector2Int location)
    {
        // Return the first tile with an exact match of the location.
        return g.Tiles.First(x => x.location == location);
        // Alternatively, you could order by distance if needed.
        // return tiles.OrderBy(s => Vector2Int.Distance(s.boardLocation, boardLocation)).First();
    }

    // The following methods provide spatial relationship checks between ActorInstances or grid locations.

    // ActorInstance overloads:
    public static bool IsSameColumn(ActorInstance a, ActorInstance b)
        => a.location.x == b.location.x;
    public static bool IsSameRow(ActorInstance a, ActorInstance b)
        => a.location.y == b.location.y;
    public static bool IsNorthOf(ActorInstance a, ActorInstance b)
        => IsSameColumn(a, b) && a.location.y == b.location.y - 1;
    public static bool IsEastOf(ActorInstance a, ActorInstance b)
        => IsSameRow(a, b) && a.location.x == b.location.x + 1;
    public static bool IsSouthOf(ActorInstance a, ActorInstance b)
        => IsSameColumn(a, b) && a.location.y == b.location.y + 1;
    public static bool IsWestOf(ActorInstance a, ActorInstance b)
        => IsSameRow(a, b) && a.location.x == b.location.x - 1;
    public static bool IsNorthWestOf(ActorInstance a, ActorInstance b)
        => a.location.x == b.location.x - 1 && a.location.y == b.location.y - 1;
    public static bool IsNorthEastOf(ActorInstance a, ActorInstance b)
        => a.location.x == b.location.x + 1 && a.location.y == b.location.y - 1;
    public static bool IsSouthWestOf(ActorInstance a, ActorInstance b)
        => a.location.x == b.location.x - 1 && a.location.y == b.location.y + 1;
    public static bool IsSouthEastOf(ActorInstance a, ActorInstance b)
        => a.location.x == b.location.x + 1 && a.location.y == b.location.y + 1;

    public static bool IsAdjacentTo(ActorInstance a, ActorInstance b)
        => (IsSameColumn(a, b) || IsSameRow(a, b)) && Vector2Int.Distance(a.location, b.location).Equals(1);

    // Grid location overloads:
    public static bool IsSameColumn(Vector2Int a, Vector2Int b) => a.x == b.x;
    public static bool IsSameRow(Vector2Int a, Vector2Int b) => a.y == b.y;
    public static bool IsNorthOf(Vector2Int a, Vector2Int b) => IsSameColumn(a, b) && a.y == b.y - 1;
    public static bool IsEastOf(Vector2Int a, Vector2Int b) => IsSameRow(a, b) && a.x == b.x + 1;
    public static bool IsSouthOf(Vector2Int a, Vector2Int b) => IsSameColumn(a, b) && a.y == b.y + 1;
    public static bool IsWestOf(Vector2Int a, Vector2Int b) => IsSameRow(a, b) && a.x == b.x - 1;
    public static bool IsNorthWestOf(Vector2Int a, Vector2Int b) => a.x == b.x - 1 && a.y == b.y - 1;
    public static bool IsNorthEastOf(Vector2Int a, Vector2Int b) => a.x == b.x + 1 && a.y == b.y - 1;
    public static bool IsSouthWestOf(Vector2Int a, Vector2Int b) => a.x == b.x - 1 && a.y == b.y + 1;
    public static bool IsSouthEastOf(Vector2Int a, Vector2Int b) => a.x == b.x + 1 && a.y == b.y + 1;
    public static bool IsAdjacentTo(Vector2Int a, Vector2Int b)
        => (IsSameColumn(a, b) || IsSameRow(a, b)) && Vector2Int.Distance(a, b).Equals(1);

    /// <summary>
    /// Given two adjacent ActorInstances, returns the direction from actor 'a' to actor 'b'.
    /// If not adjacent, returns AdjacentDirection.None.
    /// Note: The returned direction is opposite to the relative position of 'a' (i.e., if 'a' is north of 'b', return South).
    /// </summary>
    public static Direction AdjacentDirectionTo(ActorInstance a, ActorInstance b)
    {
        if (!IsAdjacentTo(a, b)) return Direction.None;
        if (IsNorthOf(a, b)) return Direction.South;
        if (IsEastOf(a, b)) return Direction.West;
        if (IsSouthOf(a, b)) return Direction.North;
        if (IsWestOf(a, b)) return Direction.East;
        return Direction.None;
    }

    /// <summary>
    /// Calculates the primary Move direction from point 'a' to point 'b' based on their differences.
    /// Returns East/West if horizontal difference is greater; otherwise, returns North/South.
    /// </summary>
    public static Direction CalculateDirection(Vector2 a, Vector2 b)
    {
        Vector2 difference = b - a;
        if (Mathf.Abs(difference.x) > Mathf.Abs(difference.y))
        {
            // Horizontal Move: positive s indicates East, negative indicates West.
            return difference.x > 0 ? Direction.East : Direction.West;
        }
        else
        {
            // Vertical Move: positive y indicates North, negative indicates South.
            return difference.y > 0 ? Direction.North : Direction.South;
        }
    }

    /// <summary>
    /// Determines the attackResult location for an attacker relative to a defender.
    /// If the attacker is already adjacent to the defender, returns the attacker's location.
    /// Otherwise, returns the defender's location (i.e. swap positions).
    /// </summary>
    public static Vector2Int GetClosestAttackLocation(Vector2Int attackerLocation, Vector2Int defenderLocation)
    {
        // Check if the attacker is already adjacent to the defender.
        if (IsAdjacentTo(attackerLocation, defenderLocation))
            return attackerLocation;
        // If not adjacent, return the defender's location.
        return defenderLocation;

        /*
        // Alternative logic (commented out) to find unoccupied or adjacent tiles if needed.
        */
    }

    /// <summary>
    /// Returns the first unoccupied tile that is exactly one unit away from the given location.
    /// </summary>
    public static TileInstance GetClosestUnoccupiedTileByLocation(Vector2Int other)
    {
        return g.Tiles.FirstOrDefault(x => !x.IsOccupied && Vector2Int.Distance(x.location, other) == 1);
    }

    /// <summary>
    /// Returns the first unoccupied tile adjacent to the specified grid location.
    /// </summary>
    public static TileInstance GetClosestUnoccupiedAdjacentTileByLocation(Vector2Int other)
    {
        return g.Tiles.FirstOrDefault(x => !x.IsOccupied && x.IsAdjacentTo(other));
    }



    public static ActorInstance GetClosestOpponent(ActorInstance attacker, IEnumerable<AttackResult> results)
    {
        return results
            .Select(r => r.Opponent)
            .Where(o => o != null)
            .OrderBy(o => Mathf.Abs(attacker.location.x - o.location.x) + Mathf.Abs(attacker.location.y - o.location.y))
            .FirstOrDefault();
    }


    /// <summary>
    /// Returns a playing actor on the opposite team who is adjacent; 
    /// if none found, returns a random actor from the opposite team.
    /// </summary>
    public static ActorInstance GetAdjacentOpponent(ActorInstance actor)
    {
        if (actor == null)
            return null;

        var opponent = g.Actors.All.FirstOrDefault(x => x.IsPlaying && x.team != actor.team && IsAdjacentTo(x.location, actor.location));

        return opponent.Exists() ? opponent : null;
    }

    /// <summary>
    /// Returns the first tile that is adjacent (occupied or not) to the specified grid location.
    /// </summary>
    public static TileInstance GetClosestAdjacentTileByLocation(Vector2Int other)
    {
        return g.Tiles.FirstOrDefault(x => x.IsAdjacentTo(other));
    }

    /// <summary>
    /// Returns a new world position by shifting the given position in the specified direction by the specified amount.
    /// </summary>
    public static Vector3 GetDirectionalPosition(Vector3 position, Direction direction, float amount)
    {
        return direction switch
        {
            Direction.North => new Vector3(position.x, position.y + amount, position.z),
            Direction.East => new Vector3(position.x + amount, position.y, position.z),
            Direction.South => new Vector3(position.x, position.y - amount, position.z),
            Direction.West => new Vector3(position.x - amount, position.y, position.z),
            _ => position,
        };
    }

    /// <summary>
    /// Determines if a given grid location is at one of the four corners of the board.
    /// This uses hardcoded corner definitions based on the TileMap.
    /// </summary>
    public static bool IsInCorner(Vector2Int location)
    {
        return location == g.TileMap.GetLocation(1, 1)   // Top-left (A1)
            || location == g.TileMap.GetLocation(1, 6)   // Bottom-left (A6)
            || location == g.TileMap.GetLocation(8, 1)   // Top-right (H1)
            || location == g.TileMap.GetLocation(8, 6);  // Bottom-right (H6)
    }


    /// <summary>
    /// Determines if a given grid location is at one of the four corners of the board.
    /// This uses hardcoded corner definitions based on the TileMap.
    /// </summary>
    public static bool IsInBounds(Vector2Int location)
    {
        return location.x >= 1 && location.x <= g.Board.columnCount
            && location.y >= 1 && location.y <= g.Board.rowCount;
    }

    /// <summary>
    /// Calculates the percentage along the line segment between startPosition and endPosition
    /// at which the currentPosition lies.
    /// Returns 0 if the start and end positions are the same.
    /// </summary>
    public static float GetPercentageBetween(Vector3 startPosition, Vector3 endPosition, Vector3 currentPosition)
    {
        // Calculate the vector representing the entire line segment (AB).
        Vector3 AB = endPosition - startPosition;
        // Calculate the vector from the start to the CurrentProfile position (AC).
        Vector3 AC = currentPosition - startPosition;

        // Prevent division by zero if the segment length is zero.
        if (AB.magnitude == 0)
            return 0;

        // Return the fraction of the segment that AC represents.
        float percentage = AC.magnitude / AB.magnitude;
        return percentage;
    }

    /// <summary>
    /// Returns a list of grid locations that lie strictly between two given grid locations.
    /// Only works for locations in the same row or same column.
    /// </summary>
    public static List<Vector2Int> GetLocationsBetween(Vector2Int start, Vector2Int end)
    {
        var result = new List<Vector2Int>();

        // If the two locations share the same row.
        if (start.y == end.y)
        {
            int minX = Mathf.Min(start.x, end.x);
            int maxX = Mathf.Max(start.x, end.x);

            // Add every intermediate column between minX and maxX.
            foreach (int x in Enumerable.Range(minX + 1, maxX - (minX + 1)))
            {
                result.Add(new Vector2Int(x, start.y));
            }
        }
        // If the two locations share the same column.
        else if (start.x == end.x)
        {
            int minY = Mathf.Min(start.y, end.y);
            int maxY = Mathf.Max(start.y, end.y);

            // Add every intermediate row between minY and maxY.
            foreach (int y in Enumerable.Range(minY + 1, maxY - (minY + 1)))
            {
                result.Add(new Vector2Int(start.x, y));
            }
        }

        return result;
    }


    /// <summary>
    /// Returns the grid location one step from `location` in the given `direction`.
    /// If direction is None or unrecognized, returns the original location.
    /// </summary>
    public static Vector2Int GetAdjacentLocationInDirection(Vector2Int location, Direction direction)
    {
        Vector2Int offset = direction switch
        {
            Direction.North => Vector2Int.up,
            Direction.East => Vector2Int.right,
            Direction.South => Vector2Int.down,
            Direction.West => Vector2Int.left,
            _ => Vector2Int.zero
        };

        return location + offset;
    }

    /// <summary>
    /// Returns the grid location one step from `location` in the given `direction`.
    /// If direction is None or unrecognized, returns the original location.
    /// </summary>
    public static Vector2Int GetLocationInDirection(Vector2Int location, Direction direction)
    {
        Vector2Int offset = direction switch
        {
            Direction.North => Vector2Int.up,                           // ( 0,  1)
            Direction.NorthEast => Vector2Int.up + Vector2Int.right,    // ( 1,  1)
            Direction.East => Vector2Int.right,                         // ( 1,  0)
            Direction.SouthEast => Vector2Int.down + Vector2Int.right,  // ( 1, -1)
            Direction.South => Vector2Int.down,                         // ( 0, -1)
            Direction.SouthWest => Vector2Int.down + Vector2Int.left,   // (-1, -1)
            Direction.West => Vector2Int.left,                          // (-1,  0)
            Direction.NorthWest => Vector2Int.up + Vector2Int.left,     // (-1,  1)
            _ => Vector2Int.zero
        };
        return location + offset;
    }


    ///<summary>
    /// Nested classes for calculating values relative to tiles, considering factors like device aspect ratio and screen size.
    ///</summary>
    public static class Tile
    {
        public static class Relative
        {
            /// <summary>
            /// Calculates a translation vector relative to tileSize.
            /// </summary>
            public static Vector3 Translation(float x, float y, float z)
            {
                return new Vector3(
                    g.TileSize * (x / g.TileSize),
                    g.TileSize * (y / g.TileSize),
                    g.TileSize * (z / g.TileSize));
            }
            public static Vector3 Translation(Vector3 v) => Translation(v.x, v.y, v.z);

            /// <summary>
            /// Calculates a scale vector relative to tileSize.
            /// </summary>
            public static Vector3 Scale(float x, float y, float z)
            {
                return new Vector3(
                    g.TileSize * x,
                    g.TileSize * y,
                    g.TileSize * z);
            }
            public static Vector3 Scale(Vector3 v) => Scale(v.x, v.y, v.z);
        }
    }

    /// <summary>
    /// Determines the starting actor (i.e., the one who should initiate an Animation) between two g.Actors.All.
    /// The decision is based on the dominant axis difference: vertical if the y difference is greater,
    /// or horizontal otherwise.
    /// </summary>
    public static ActorInstance GetStartActor(ActorInstance actor1, ActorInstance actor2)
    {
        // Determine the dominant axis based on the differences in grid coordinates.
        Axis axis = Math.Abs(actor1.location.y - actor2.location.y) > Math.Abs(actor1.location.x - actor2.location.x)
            ? Axis.Vertical
            : Axis.Horizontal;

        return GetStartActor(actor1, actor2, axis);
    }

    /// <summary>
    /// Determines the starting actor based on the specified dominant axis.
    /// For horizontal, the actor with the lower s value (more left) is chosen.
    /// For vertical, the actor with the higher y value (closer to the top) is chosen.
    /// </summary>
    public static ActorInstance GetStartActor(ActorInstance actor1, ActorInstance actor2, Axis axis)
    {
        return axis == Axis.Horizontal
            ? actor1.location.x <= actor2.location.x ? actor1 : actor2
            : actor1.location.y >= actor2.location.y ? actor1 : actor2;
    }

    /// <summary>
    /// Creates a quaternion (rotation) from Euler angles (s, y, z).
    /// </summary>
    public static Quaternion Rotation(float x, float y, float z)
    {
        return Quaternion.Euler(new Vector3(x, y, z));
    }

    /// <summary>
    /// Overload for Rotation that accepts a Vector3 of Euler angles.
    /// </summary>
    public static Quaternion Rotation(Vector3 v) => Rotation(v.x, v.y, v.z);

    #endregion
}
