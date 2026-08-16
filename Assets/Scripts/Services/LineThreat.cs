using System.Collections.Generic;
using UnityEngine;

namespace Scripts.Services
{
    /// <summary>
    /// LINETHREAT - Pure math for line-shaped enemy attacks (US-138 / GG-A5).
    ///
    /// <para>PURPOSE: A line-caster locks a CARDINAL direction toward its target at telegraph
    /// time; the threatened tiles run from the tile beside the caster to the board edge. The
    /// direction locks when the charge starts — sliding out of (or displacing allies out of)
    /// the line before the cast resolves is the counter-play. No scene access; the board size
    /// comes in as arguments (1-based grid, cols 1..maxCol, rows 1..maxRow).</para>
    ///
    /// <para>RELATED FILES: EnemyChargeSequence.cs (consumer), EnemyChargeCatalog.cs (which
    /// enemies line-cast), LineTelegraph.cs (the visual).</para>
    /// </summary>
    public static class LineThreat
    {
        /// <summary>The cardinal direction from <paramref name="origin"/> toward
        /// <paramref name="target"/> — dominant axis wins, X on ties.</summary>
        public static Vector2Int DirectionToward(Vector2Int origin, Vector2Int target)
        {
            int dx = target.x - origin.x;
            int dy = target.y - origin.y;
            if (dx == 0 && dy == 0) return new Vector2Int(1, 0); // degenerate — fire east
            if (Mathf.Abs(dx) >= Mathf.Abs(dy))
                return new Vector2Int(dx >= 0 ? 1 : -1, 0);
            return new Vector2Int(0, dy >= 0 ? 1 : -1);
        }

        /// <summary>Every tile strictly beyond <paramref name="origin"/> in
        /// <paramref name="direction"/>, to the board edge (1-based bounds inclusive).</summary>
        public static List<Vector2Int> TilesInLine(Vector2Int origin, Vector2Int direction, int maxCol, int maxRow)
        {
            var tiles = new List<Vector2Int>();
            var cur = origin + direction;
            while (cur.x >= 1 && cur.x <= maxCol && cur.y >= 1 && cur.y <= maxRow)
            {
                tiles.Add(cur);
                cur += direction;
            }
            return tiles;
        }

        /// <summary>Convenience: the locked line from caster toward target, edge to edge.</summary>
        public static List<Vector2Int> ComputeThreat(Vector2Int origin, Vector2Int target, int maxCol, int maxRow)
            => TilesInLine(origin, DirectionToward(origin, target), maxCol, maxRow);
    }
}
