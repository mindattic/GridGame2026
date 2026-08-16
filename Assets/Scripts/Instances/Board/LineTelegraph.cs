using System.Collections.Generic;
using UnityEngine;
using g = Scripts.Helpers.GameHelper;
using Scripts.Helpers;

namespace Scripts.Instances.Board
{
    /// <summary>
    /// LINETELEGRAPH - The persistent red-tile warning for a charging line attack (US-138).
    ///
    /// <para>PURPOSE: TileManager.Reset() repaints the whole board on every drop, so a one-shot
    /// tint would vanish the moment the player acts. This component re-applies the threat tint
    /// every LateUpdate for the life of the charge (a handful of tiles — negligible), and
    /// releases the tiles back to white when destroyed (charge resolved or interrupted).</para>
    ///
    /// <para>RELATED FILES: EnemyChargeSequence.cs (spawns/destroys), LineThreat.cs (the math),
    /// TileManager.cs (the reset this survives).</para>
    /// </summary>
    public class LineTelegraph : MonoBehaviour
    {
        /// <summary>Line-attack threat tint (US-138).</summary>
        public static readonly Color ThreatTint = new Color(1f, 0.25f, 0.2f, 1f);
        /// <summary>Armed-trap tint (US-139).</summary>
        public static readonly Color TrapTint = new Color(0.75f, 0.35f, 0.9f, 1f);

        private List<Vector2Int> tiles = new List<Vector2Int>();
        private Color tint = Color.white;

        /// <summary>Spawns a persistent tint over <paramref name="threatTiles"/>; destroy to clear.
        /// Defaults to the line-threat red; pass <see cref="TrapTint"/> for traps.</summary>
        public static LineTelegraph Show(List<Vector2Int> threatTiles, Color? color = null)
        {
            var go = new GameObject("LineTelegraph");
            var telegraph = go.AddComponent<LineTelegraph>();
            telegraph.tiles = threatTiles ?? new List<Vector2Int>();
            telegraph.tint = color ?? ThreatTint;
            return telegraph;
        }

        /// <summary>Retargets the marker's tiles (e.g. a trap consumed → repaint the rest).</summary>
        public void SetTiles(List<Vector2Int> newTiles)
        {
            // Release tiles no longer covered before adopting the new set.
            var map = g.TileMap;
            if (map != null && tiles != null)
                foreach (var loc in tiles)
                    if (newTiles == null || !newTiles.Contains(loc))
                    {
                        var t = map.GetTile(loc);
                        if (t != null) t.color = ColorHelper.Tile.White;
                    }
            tiles = newTiles ?? new List<Vector2Int>();
        }

        private void LateUpdate()
        {
            var map = g.TileMap;
            if (map == null) return;
            foreach (var loc in tiles)
            {
                var tile = map.GetTile(loc);
                if (tile != null) tile.color = tint;
            }
        }

        private void OnDestroy()
        {
            // Teardown-safe: never resurrect managers from OnDestroy (Singleton.HasLiveInstance).
            if (!Scripts.Managers.GameManager.HasLiveInstance) return;
            var map = g.TileMap;
            if (map == null) return;
            foreach (var loc in tiles)
            {
                var tile = map.GetTile(loc);
                if (tile != null) tile.color = ColorHelper.Tile.White;
            }
        }
    }
}
