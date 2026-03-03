using Scripts.Helpers;
using System;
using System.Collections.Generic;
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
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Models
{
    /// <summary>
    /// Fast tile index and lookup service.
    /// Backed by a 1-based 2D grid for O(1) location hits, plus a world-position map.
    /// Also supports A1 style coordinate names and common iteration helpers.
    /// </summary>
    public class TileMap
    {
        // 1-based grid: grid[col, row] holds the entry at that location.
        // Sized to [maxCol + 1, maxRow + 1] so index 0 is unused.
        private TileEntry[,] grid;

        // Exact world position lookups.
        private readonly Dictionary<Vector3, TileEntry> positionToEntry = new Dictionary<Vector3, TileEntry>();

        // Optional name lookups like "A3".
        private readonly Dictionary<string, Vector2Int> nameToLocation = new Dictionary<string, Vector2Int>(StringComparer.OrdinalIgnoreCase);

        // Current bounds of the grid. These reflect the largest col and row seen so far.
        private int maxCol;
        private int maxRow;

        // Grid origin and size, used for world to tile conversions.
        public Vector3 gridOrigin;
        public float tileSize;

        private static readonly Vector2Int[] Adjacents =
        {
            new Vector2Int(0, 1),   // up
            new Vector2Int(1, 0),   // right
            new Vector2Int(0, -1),  // down
            new Vector2Int(-1, 0)   // left
        };

        // Neighbor offsets (8-way).
        private static readonly Vector2Int[] Neighbors =
        {
            new Vector2Int(0, 1),  new Vector2Int(1, 0),
            new Vector2Int(0, -1), new Vector2Int(-1, 0),
            new Vector2Int(1, 1),  new Vector2Int(-1, 1),
            new Vector2Int(1, -1), new Vector2Int(-1, -1)
        };

        /// <summary>
        /// Adds a new tile using grid location and world position.
        /// Updates fast indexes and coordinate names.
        /// </summary>
        public void Add(Vector2Int location, Vector3 position, TileInstance tile)
        {
            if (tile == null) throw new ArgumentNullException(nameof(tile));
            if (location.x <= 0 || location.y <= 0) throw new ArgumentException("TileMap expects 1-based locations");

            EnsureGridSize(location.x, location.y);

            var entry = new TileEntry(location, position, tile);

            grid[location.x, location.y] = entry;
            positionToEntry[position] = entry;

            var name = ColumnToLetter(location.x) + location.y;
            nameToLocation[name] = location;
        }

        /// <summary>
        /// Adds a TileInstance using its inherent location and position.
        /// </summary>
        public void Add(TileInstance tile)
        {
            if (tile == null) throw new ArgumentNullException(nameof(tile));
            Add(tile.location, tile.position, tile);
        }

        /// <summary>
        /// Gets world position for location or PositionHelper.Nowhere if missing.
        /// O(1).
        /// </summary>
        public Vector3 GetPosition(Vector2Int location)
        {
            var e = GetEntry(location);
            return e != null ? e.Position : PositionHelper.Nowhere;
        }

        /// <summary>
        /// Gets grid location for an exact world position or LocationHelper.Nowhere if missing.
        /// O(1) on exact match.
        /// </summary>
        public Vector2Int GetLocation(Vector3 position)
        {
            return positionToEntry.TryGetValue(position, out var e) ? e.Location : LocationHelper.Nowhere;
        }

        /// <summary>
        /// Gets a grid location for column and row, throws if outside current bounds or empty.
        /// O(1).
        /// </summary>
        public Vector2Int GetLocation(int col, int row)
        {
            if (!InBounds(col, row)) throw new UnityException($"No tile found at column {col}, row {row}");
            var e = grid[col, row];
            if (e == null) throw new UnityException($"No tile found at column {col}, row {row}");
            return e.Location;
        }

        /// <summary>
        /// Gets a tile by location, or null if missing. O(1).
        /// </summary>
        public TileInstance GetTile(Vector2Int location)
        {
            var e = GetEntry(location);
            return e != null ? e.Tile : null;
        }

        /// <summary>
        /// Gets a tile by exact world position, or null if missing. O(1).
        /// </summary>
        public TileInstance GetTile(Vector3 position)
        {
            return positionToEntry.TryGetValue(position, out var e) ? e.Tile : null;
        }

        /// <summary>
        /// Convert a world position to the nearest tile using gridOrigin and tileSize.
        /// O(1).
        /// </summary>
        public TileInstance GetClosestTileEfficient(Vector3 position)
        {
            if (tileSize <= 0f) return null;

            float rx = (position.x - gridOrigin.x) / tileSize;
            float ry = (gridOrigin.y - position.y) / tileSize; // inverted Y

            int col = Mathf.RoundToInt(rx) + 1;
            int row = Mathf.RoundToInt(ry) + 1;

            if (!InBounds(col, row)) return null;

            var e = grid[col, row];
            return e != null ? e.Tile : null;
        }

        /// <summary>
        /// Returns true if a tile exists at location. O(1).
        /// </summary>
        public bool ContainsLocation(Vector2Int location)
        {
            if (!InBounds(location.x, location.y)) return false;
            return grid[location.x, location.y] != null;
        }

        /// <summary>
        /// Returns true if a tile exists at exact world position. O(1).
        /// </summary>
        public bool ContainsPosition(Vector3 position)
        {
            return positionToEntry.ContainsKey(position);
        }

        /// <summary>
        /// Removes a tile by location if present. O(1).
        /// </summary>
        public void RemoveByLocation(Vector2Int location)
        {
            if (!InBounds(location.x, location.y)) return;

            var e = grid[location.x, location.y];
            if (e == null) return;

            grid[location.x, location.y] = null;
            positionToEntry.Remove(e.Position);

            var name = ColumnToLetter(location.x) + location.y;
            nameToLocation.Remove(name);
        }

        /// <summary>
        /// Removes a tile by exact world position if present. O(1).
        /// </summary>
        public void RemoveByPosition(Vector3 position)
        {
            if (!positionToEntry.TryGetValue(position, out var e)) return;

            positionToEntry.Remove(position);
            if (InBounds(e.Location.x, e.Location.y) && grid[e.Location.x, e.Location.y] == e)
                grid[e.Location.x, e.Location.y] = null;

            var name = ColumnToLetter(e.Location.x) + e.Location.y;
            nameToLocation.Remove(name);
        }

        /// <summary>
        /// Try get tile at location. No throw. O(1).
        /// </summary>
        public bool TryGetTile(Vector2Int location, out TileInstance tile)
        {
            var e = GetEntry(location);
            tile = e != null ? e.Tile : null;
            return tile != null;
        }

        /// <summary>
        /// Get tile or throw if missing. O(1).
        /// </summary>
        public TileInstance GetTileOrThrow(Vector2Int location)
        {
            var t = GetTile(location);
            if (t == null) throw new UnityException($"No tile at {location}");
            return t;
        }

        /// <summary>
        /// Get grid location from a board name like "A3". Returns Vector2Int.zero if invalid.
        /// O(1) after Add has registered names.
        /// </summary>
        public Vector2Int GetLocation(string coordName)
        {
            if (string.IsNullOrWhiteSpace(coordName)) return Vector2Int.zero;
            return nameToLocation.TryGetValue(coordName.Trim(), out var loc) ? loc : Vector2Int.zero;
        }

        /// <summary>
        /// Get board name like "C1" for a location. Returns null if out of bounds or empty.
        /// O(1).
        /// </summary>
        public string GetName(Vector2Int location)
        {
            if (!InBounds(location.x, location.y)) return null;
            if (grid[location.x, location.y] == null) return null;
            return ColumnToLetter(location.x) + location.y;
        }

        // 8-way neighbors (adjacent + diagonals)
        /// <summary>Gets the neighbors.</summary>
        public IEnumerable<TileInstance> GetNeighbors(Vector2Int location, bool includeOccupied = true)
        {
            for (int i = 0; i < Neighbors.Length; i++)
            {
                var l = location + Neighbors[i];
                if (!InBounds(l.x, l.y)) continue;
                var e = grid[l.x, l.y];
                if (e != null && (includeOccupied || !e.Tile.IsOccupied))
                    yield return e.Tile;
            }
        }

        // 4-way neighbors (adjacent only: up, right, down, left)
        /// <summary>Gets the adjacent neighbors.</summary>
        public IEnumerable<TileInstance> GetAdjacentNeighbors(Vector2Int location, bool includeOccupied = true)
        {
            for (int i = 0; i < Adjacents.Length; i++)
            {
                var l = location + Adjacents[i];
                if (!InBounds(l.x, l.y)) continue;
                var e = grid[l.x, l.y];
                if (e != null && (includeOccupied || !e.Tile.IsOccupied))
                    yield return e.Tile;
            }
        }


        /// <summary>
        /// Finds the first unoccupied neighbor, preferring adjacency order given above.
        /// Returns null if none. O(1) per checked neighbor.
        /// </summary>
        public TileInstance FindFirstFreeAdjacent(Vector2Int location)
        {
            foreach (var t in GetAdjacentNeighbors(location))
                if (!t.IsOccupied) return t;
            return null;
        }

        /// <summary>
        /// Finds the first unoccupied neighbor, preferring adjacency order given above.
        /// Returns null if none. O(1) per checked neighbor.
        /// </summary>
        public TileInstance FindFirstFreeNeighbor(Vector2Int location)
        {
            foreach (var t in GetNeighbors(location))
                if (!t.IsOccupied) return t;
            return null;
        }

        /// <summary>
        /// Enumerates a full row from minCol to maxCol inclusive. Yields existing tiles only.
        /// O(width of range).
        /// </summary>
        public IEnumerable<TileInstance> EnumerateRow(int row, int minCol, int maxCol)
        {
            if (row <= 0 || minCol <= 0 || maxCol <= 0) yield break;
            int end = Math.Min(maxCol, maxCol > 0 ? maxCol : maxCol);
            int start = Math.Max(1, minCol);

            for (int c = start; c <= maxCol; c++)
            {
                if (!InBounds(c, row)) continue;
                var e = grid[c, row];
                if (e != null) yield return e.Tile;
            }
        }

        /// <summary>
        /// Enumerates a full column from minRow to maxRow inclusive. Yields existing tiles only.
        /// O(height of range).
        /// </summary>
        public IEnumerable<TileInstance> EnumerateColumn(int col, int minRow, int maxRow)
        {
            if (col <= 0 || minRow <= 0 || maxRow <= 0) yield break;

            for (int r = Math.Max(1, minRow); r <= maxRow; r++)
            {
                if (!InBounds(col, r)) continue;
                var e = grid[col, r];
                if (e != null) yield return e.Tile;
            }
        }

        /// <summary>
        /// Enumerates tiles in the rectangle defined by a and b inclusive, left to right then top to bottom.
        /// Yields existing tiles only. O(area).
        /// </summary>
        public IEnumerable<TileInstance> EnumerateRect(Vector2Int a, Vector2Int b)
        {
            int minC = Math.Min(a.x, b.x);
            int maxC = Math.Max(a.x, b.x);
            int minR = Math.Min(a.y, b.y);
            int maxR = Math.Max(a.y, b.y);

            for (int c = minC; c <= maxC; c++)
                for (int r = minR; r <= maxR; r++)
                {
                    if (!InBounds(c, r)) continue;
                    var e = grid[c, r];
                    if (e != null) yield return e.Tile;
                }
        }

        /// <summary>
        /// Enumerates tiles strictly between two aligned locations. Empty if not aligned.
        /// O(distance).
        /// </summary>
        public IEnumerable<TileInstance> EnumerateBetween(Vector2Int a, Vector2Int b)
        {
            if (a.x == b.x)
            {
                int c = a.x;
                int start = Math.Min(a.y, b.y) + 1;
                int end = Math.Max(a.y, b.y) - 1;
                for (int r = start; r <= end; r++)
                {
                    if (!InBounds(c, r)) continue;
                    var e = grid[c, r];
                    if (e != null) yield return e.Tile;
                }
            }
            else if (a.y == b.y)
            {
                int r = a.y;
                int start = Math.Min(a.x, b.x) + 1;
                int end = Math.Max(a.x, b.x) - 1;
                for (int c = start; c <= end; c++)
                {
                    if (!InBounds(c, r)) continue;
                    var e = grid[c, r];
                    if (e != null) yield return e.Tile;
                }
            }
        }

        /// <summary>
        /// Parses a board name like "A3" into a location. Throws on invalid input.
        /// </summary>
        public Vector2Int GetLocationOrThrow(string coordName)
        {
            var loc = GetLocation(coordName);
            if (loc == Vector2Int.zero) throw new ArgumentException($"Invalid or unknown coordinate: {coordName}");
            return loc;
        }

        // Internal helpers

        /// <summary>Gets the entry.</summary>
        private TileEntry GetEntry(Vector2Int location)
        {
            if (!InBounds(location.x, location.y)) return null;
            return grid[location.x, location.y];
        }

        /// <summary>In bounds.</summary>
        private bool InBounds(int col, int row)
        {
            return col > 0 && row > 0 && col <= maxCol && row <= maxRow && grid != null;
        }

        /// <summary>Ensure grid size.</summary>
        private void EnsureGridSize(int needCol, int needRow)
        {
            if (grid != null && needCol <= maxCol && needRow <= maxRow) return;

            int newMaxCol = Math.Max(needCol, Math.Max(1, maxCol));
            int newMaxRow = Math.Max(needRow, Math.Max(1, maxRow));

            // Grow by power-of-two style steps to reduce reallocations.
            newMaxCol = NextCapacity(newMaxCol, maxCol);
            newMaxRow = NextCapacity(newMaxRow, maxRow);

            var newGrid = new TileEntry[newMaxCol + 1, newMaxRow + 1];

            if (grid != null)
            {
                for (int c = 1; c <= maxCol; c++)
                    for (int r = 1; r <= maxRow; r++)
                        newGrid[c, r] = grid[c, r];
            }

            grid = newGrid;
            maxCol = newMaxCol;
            maxRow = newMaxRow;
        }

        /// <summary>Next capacity.</summary>
        private static int NextCapacity(int need, int current)
        {
            if (current <= 0) return Math.Max(need, 8);
            int cap = current;
            while (cap < need) cap *= 2;
            return cap;
        }

        /// <summary>Column to letter.</summary>
        private static string ColumnToLetter(int col)
        {
            string result = string.Empty;
            while (col > 0)
            {
                col--;
                result = (char)('A' + (col % 26)) + result;
                col /= 26;
            }
            return result;
        }

        /// <summary>
        /// Entry payload for a tile.
        /// </summary>
        private class TileEntry
        {
            public Vector2Int Location { get; }
            public Vector3 Position { get; }
            public TileInstance Tile { get; }

            public TileEntry(Vector2Int location, Vector3 position, TileInstance tile)
            {
                Location = location;
                Position = position;
                Tile = tile;
            }
        }

        /// <summary>
        /// Returns true if the given world position falls within the current board bounds.
        /// Checks against gridOrigin, tileSize, and maxCol/maxRow.
        /// Does not require the position to match an exact tile center.
        /// </summary>
        //public bool IsInsideBoard(Vector3 worldPosition)
        //{
        //    if (tileSize <= 0f || grid == null)
        //        return false;

        //    // Convert world position to board-relative coordinates
        //    float rx = (worldPosition.x - gridOrigin.x) / tileSize;
        //    float ry = (gridOrigin.y - worldPosition.y) / tileSize; // inverted Y

        //    int col = Mathf.FloorToInt(rx) + 1;
        //    int row = Mathf.FloorToInt(ry) + 1;

        //    return InBounds(col, row);
        //}
    }
}
