using Assets.Helper;
using Assets.Scripts.Models;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using g = Assets.Helpers.GameHelper;

/// <summary>
/// TileManager is responsible for managing tile-specific behaviors on the board.
/// It accesses the global TileMap and the list of TileInstances from the GameManager.
/// </summary>
public class TileManager : MonoBehaviour
{
    public void Reset()
    {
        foreach (var tile in g.Tiles)
        {
            // Reset each tile's sprite color to a translucent white.
            tile.spriteRenderer.color = ColorHelper.Tile.White;
        }
    }

    /// <summary>
    /// Updates the tile colors when the selected hero's location changes.
    /// It sets the color of the tile at the previous location to white and
    /// the new location tile to yellow, providing visual feedback.
    /// </summary>
    /// <param name="previousLocation">The grid location that was previously selected.</param>
    /// <param name="newLocation">The new grid location that is now selected.</param>
    //public void OnSelectedHeroLocationChanged(Vector2Int previousLocation, Vector2Int newLocation)
    //{
    //    // Show the previous tile's color back to white.
    //    tileMap.GetTile(previousLocation).color = ColorHelper.Tile.White;
    //    // Highlight the new tile by setting its color to yellow.
    //    tileMap.GetTile(newLocation).color = ColorHelper.Tile.Yellow;
    //}


    public void Hightlight(Vector2Int previous, Vector2Int current)
    {
        g.TileMap.GetTile(previous).color = ColorHelper.Tile.White;
        g.TileMap.GetTile(current).color = ColorHelper.Tile.Yellow;
    }

    /// <summary>
    /// Tint tiles in 4 cardinal directions from a source until an occupied tile or board edge is reached.
    /// Does not tint the occupied tile itself. Used by LinearTarget mode.
    /// </summary>
    public void HighlightLinearPaths(Vector2Int source)
    {
        Reset();

        // 4 directions: up, right, down, left
        var dirs = new Vector2Int[] { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
        foreach (var d in dirs)
        {
            var loc = source + d;
            while (true)
            {
                if (!g.TileMap.ContainsLocation(loc)) break;
                var tile = g.TileMap.GetTile(loc);
                if (tile == null) break;
                if (tile.IsOccupied) break; // stop before the first occupied tile

                tile.color = ColorHelper.Tile.Yellow;
                loc += d;
            }
        }
    }
}
