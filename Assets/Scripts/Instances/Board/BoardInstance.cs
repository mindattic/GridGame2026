using Assets.Scripts.Factories;
using Game.Models;
using UnityEngine;
using g = Assets.Helpers.GameHelper;
using Assets.Helpers;

/// <summary>
/// BOARDINSTANCE - The tactical grid where combat takes place.
/// 
/// PURPOSE:
/// Represents the game board as a grid of tiles. Handles board positioning,
/// bounds calculation, and tile generation.
/// 
/// DEFAULT SIZE: 6 columns x 8 rows
/// 
/// COORDINATE SYSTEM:
/// - Column 0 = leftmost, Column 5 = rightmost
/// - Row 0 = topmost, Row 7 = bottommost
/// - Tiles addressed as Vector2Int(column, row)
/// 
/// KEY PROPERTIES:
/// - columnCount/rowCount: Grid dimensions
/// - offset: World-space origin (top-left corner)
/// - bounds: World-space rectangle enclosing the board
/// - center: Center point in world space
/// - worldEdges: Edge midpoints in world space
/// - screenEdges: Edge midpoints in screen space
/// 
/// INITIALIZATION FLOW:
/// 1. Initialize() called by BoardManager
/// 2. AssignPosition(): Centers board in playable area
/// 3. AssignBounds(): Computes world-space bounds
/// 4. GenerateTiles(): Creates TileInstance grid via TileFactory
/// 
/// LAYOUT CALCULATION:
/// The board is centered between top UI (timeline, coins) and bottom UI (card).
/// Uses GameManager.topReservePercent and bottomReservePercent to calculate
/// the usable play area.
/// 
/// LLM CONTEXT:
/// Access board via g.BoardManager.board. Tiles are stored in g.TileMap.
/// Use Geometry class to convert between grid locations and world positions.
/// </summary>
public class BoardInstance : MonoBehaviour
{
    #region Fields

    /// <summary>Number of columns (horizontal tiles). Default 6.</summary>
    [HideInInspector] public int columnCount = 6;

    /// <summary>Number of rows (vertical tiles). Default 8.</summary>
    [HideInInspector] public int rowCount = 8;

    /// <summary>Board origin in world space (top-left corner offset).</summary>
    [HideInInspector] public Vector2 offset;

    /// <summary>World-space rectangle enclosing the entire board.</summary>
    [HideInInspector] public RectFloat bounds;

    /// <summary>Center point of the board in world space.</summary>
    [HideInInspector] public Vector2 center;

    /// <summary>Edge midpoints in world space (top, right, bottom, left).</summary>
    [HideInInspector] public RectVector3 worldEdges;

    /// <summary>Edge midpoints in screen space (top, right, bottom, left).</summary>
    [HideInInspector] public RectVector3 screenEdges;

    #endregion

    #region Initialization

    /// <summary>
    /// Initializes the board by positioning it, computing bounds, and generating tiles.
    /// Called by BoardManager during scene setup.
    /// </summary>
    public void Initialize()
    {
        // Guard against being invoked on a destroyed instance (can happen on scene reloads)
        if (this == null) return;

        AssignPosition();
        AssignBounds();
        GenerateTiles();
    }

    #endregion

    #region Position Calculation

    /// <summary>
    /// Calculates and applies the board's world-space origin offset.
    /// Centers the board horizontally and vertically within the usable band
    /// (after reserving top/bottom UI space).
    /// </summary>
    private void AssignPosition()
    {
        if (this == null) return;

        // Visible world rect and GameManager layout reserves
        Rect vr = UnitConversionHelper.World.VisibleRect();
        var gm = GameManager.instance;
        float topReserve = 0f;
        float bottomReserve = 0f;
        if (gm != null)
        {
            topReserve = vr.height * Mathf.Clamp01(gm.topReservePercent);
            bottomReserve = vr.height * Mathf.Clamp01(gm.bottomReservePercent);
        }

        // Compute usable band and its vertical center
        float usableMinY = vr.yMin + bottomReserve;
        float usableMaxY = vr.yMax - topReserve;
        float usableCenterY = (usableMinY + usableMaxY) * 0.5f;
        float usableCenterX = vr.center.x;

        float t = g.TileSize;

        // Solve for offset so board center == usable center
        // CenterX = offset.x + (cols*t)/2 + t/2 => offset.x = usableCenterX - (cols*t)/2 - t/2
        float x = usableCenterX - (columnCount * t) * 0.5f - t * 0.5f;
        // CenterY = offset.y - (rows*t)/2 - t/2 => offset.y = usableCenterY + (rows*t)/2 + t/2
        float y = usableCenterY + (rowCount * t) * 0.5f + t * 0.5f;

        offset = new Vector2(x, y);

        // Move this transform to match the computed offset.
        transform.position = offset;
    }

    #endregion

    #region Bounds Calculation

    /// <summary>
    /// Computes world-space bounds from the offset, tile size, and board dimensions.
    /// Also caches center point and edge midpoints.
    /// </summary>
    private void AssignBounds()
    {
        if (this == null) return;

        bounds = new RectFloat();

        bounds.Top = offset.y - g.TileSize * 0.5f;
        bounds.Right = offset.x + (g.TileSize * columnCount) + g.TileSize * 0.5f;
        bounds.Bottom = offset.y - (g.TileSize * rowCount) - g.TileSize * 0.5f;
        bounds.Left = offset.x + g.TileSize * 0.5f;

        center = new Vector2(
            (bounds.Left + bounds.Right) * 0.5f,
            (bounds.Top + bounds.Bottom) * 0.5f
        );

        // Store all four edge midpoints in RectVector3
        worldEdges = new RectVector3(
            new Vector3(center.x, bounds.Top, 0f),    // Top
            new Vector3(bounds.Right, center.y, 0f),  // Right
            new Vector3(center.x, bounds.Bottom, 0f), // Bottom
            new Vector3(bounds.Left, center.y, 0f)    // Left
        );

        // Convert world-space worldEdges to screen-space worldEdges
        var cam = Camera.main;
        if (cam != null)
        {
            screenEdges = new RectVector3(
                cam.WorldToScreenPoint(worldEdges.Top),
                cam.WorldToScreenPoint(worldEdges.Right),
                cam.WorldToScreenPoint(worldEdges.Bottom),
                cam.WorldToScreenPoint(worldEdges.Left)
            );
        }
    }

    /// <summary>
    /// Instantiates tile prefabs for each grid position, initializes them, and registers them in global maps.
    /// </summary>
    private void GenerateTiles()
    {
        if (this == null) return;

        // Create tiles for each grid cell using factory
        for (int col = 1; col <= columnCount; col++)
        {
            for (int row = 1; row <= rowCount; row++)
            {
                var go = TileFactory.Create();
                go.transform.position = Vector2.zero;
                go.transform.rotation = Quaternion.identity;

                var instance = go.GetComponent<TileInstance>();
                instance.parent = transform;
                instance.name = $"Tile_{col}x{row}";
                instance.Initialize(col, row);

                g.TileMap.Add(instance);
            }
        }

        // Set grid origin and tile sizing for the TileMap.
        g.TileMap.gridOrigin = g.TileMap.GetTile(new Vector2Int(1, 1)).position;
        g.TileMap.tileSize = g.TileSize;

        // Cache all tiles from the scene into the global list.
        var tileObjects = GameObject.FindObjectsByType<TileInstance>(FindObjectsSortMode.None);
        foreach (var obj in tileObjects)
        {
            var tile = obj.GetComponent<TileInstance>();
            if (tile != null)
            {
                g.Tiles.Add(tile);
            }
        }
    }

    /// <summary>
    /// Returns true if a grid location is within board bounds.
    /// </summary>
    public bool InBounds(Vector2Int location)
    {
        return location.x >= 1 && location.x <= columnCount
            && location.y >= 1 && location.y <= rowCount;
    }

    /// <summary>
    /// Returns true if the given world position is within the board’s world-space bounds.
    /// </summary>
    public bool IsInsideBoard(Vector3 worldPosition)
    {
        return worldPosition.x >= bounds.Left &&
               worldPosition.x <= bounds.Right &&
               worldPosition.y <= bounds.Top &&
               worldPosition.y >= bounds.Bottom;
    }

    #endregion

}
