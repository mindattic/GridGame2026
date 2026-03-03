using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using g = Scripts.Helpers.GameHelper;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Factories;
using Scripts.Helpers;
using Scripts.Hub;
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

namespace Scripts.Instances
{
/// <summary>
/// TILEINSTANCE - Single cell on the game board grid.
/// 
/// PURPOSE: Represents one tile in the tactical grid where actors can stand.
/// 
/// COORDINATES: Vector2Int(column, row)
/// - Column 0 = leftmost, Column 5 = rightmost (default 6 cols)
/// - Row 0 = topmost, Row 7 = bottommost (default 8 rows)
/// 
/// KEY PROPERTIES:
/// - location: Grid position as Vector2Int
/// - IsOccupied: True if an actor currently stands on this tile
/// - Occupier: The ActorInstance on this tile (null if empty)
/// 
/// SPATIAL HELPERS:
/// - IsAdjacentTo(loc): True if this tile is cardinally adjacent to location
/// - IsSameRow(loc): True if same Y coordinate
/// - IsSameColumn(loc): True if same X coordinate
/// - IsNorthOf/SouthOf/EastOf/WestOf: Directional checks
/// 
/// EVENTS:
/// - onSelectedPlayerLeaveLocation: Fired when selected hero leaves this tile
/// - onSelectedPlayerEnterLocation: Fired when selected hero enters this tile
/// 
/// LLM CONTEXT:
/// Tiles form the board grid. Use g.TileMap.GetTile(location) to get a tile.
/// Actors move between tiles by updating their location Vector2Int.
/// </summary>
public class TileInstance : MonoBehaviour
{
    /// <summary>True if any living actor's location matches this tile's location.</summary>
    public bool IsOccupied => g.Actors.All.Any(x => x.IsPlaying && x.location == location);

    /// <summary>Returns the actor standing on this tile, or null if empty.</summary>
    public ActorInstance Occupier => g.Actors.All.FirstOrDefault(x => x.location == location);

    /// <summary>Event fired when the selected player leaves this tile location.</summary>
    public System.Action<Vector2Int> onSelectedPlayerLeaveLocation;

    /// <summary>Event fired when the selected player enters this tile location.</summary>
    public System.Action<Vector2Int> onSelectedPlayerEnterLocation;

    /// <summary>Tile name (wrapper for GameObject.name).</summary>
    public string Name
    {
        get => name;
        set => name = value;  // Fixed: was causing infinite recursion with "Name = value"
    }

    /// <summary>Parent transform of this tile.</summary>
    public Transform parent
    {
        get => gameObject.transform.parent;
        set => gameObject.transform.SetParent(value, true);
    }

    /// <summary>World position of this tile.</summary>
    public Vector3 position
    {
        get => gameObject.transform.position;
        set => gameObject.transform.position = value;
    }
    public Quaternion rotation
    {
        get => gameObject.transform.rotation;
        set => gameObject.transform.rotation = value;
    }
    public Vector3 scale
    {
        get => gameObject.transform.localScale;
        set => gameObject.transform.localScale = value;
    }
    public Sprite sprite
    {
        get => spriteRenderer.sprite;
        set => spriteRenderer.sprite = value;
    }
    public Color color
    {
        get => spriteRenderer.color;
        set => spriteRenderer.color = value;
    }
    /// <summary>Returns whether the is same column condition is met.</summary>
    public bool IsSameColumn(Vector2Int other) => this.location.x == other.x;
    /// <summary>Returns whether the is same row condition is met.</summary>
    public bool IsSameRow(Vector2Int other) => this.location.y == other.y;
    /// <summary>Returns whether the is north of condition is met.</summary>
    public bool IsNorthOf(Vector2Int other) => IsSameColumn(other) && this.location.y == other.y - 1;
    /// <summary>Returns whether the is east of condition is met.</summary>
    public bool IsEastOf(Vector2Int other) => IsSameRow(other) && this.location.x == other.x + 1;
    /// <summary>Returns whether the is south of condition is met.</summary>
    public bool IsSouthOf(Vector2Int other) => IsSameColumn(other) && this.location.y == other.y + 1;
    /// <summary>Returns whether the is west of condition is met.</summary>
    public bool IsWestOf(Vector2Int other) => IsSameRow(other) && this.location.x == other.x - 1;
    /// <summary>Returns whether the is north west of condition is met.</summary>
    public bool IsNorthWestOf(Vector2Int other) => this.location.x == other.x - 1 && this.location.y == other.y - 1;
    /// <summary>Returns whether the is north east of condition is met.</summary>
    public bool IsNorthEastOf(Vector2Int other) => this.location.x == other.x + 1 && this.location.y == other.y - 1;
    /// <summary>Returns whether the is south west of condition is met.</summary>
    public bool IsSouthWestOf(Vector2Int other) => this.location.x == other.x - 1 && this.location.y == other.y + 1;
    /// <summary>Returns whether the is south east of condition is met.</summary>
    public bool IsSouthEastOf(Vector2Int other) => this.location.x == other.x + 1 && this.location.y == other.y + 1;
    /// <summary>Returns whether the is adjacent to condition is met.</summary>
    public bool IsAdjacentTo(Vector2Int other) => (IsSameColumn(other) || IsSameRow(other)) && Vector2Int.Distance(this.location, other) == 1;


    //Fields
    public Vector2Int location;
    public SpriteRenderer spriteRenderer;

    //Method which is used for initialization tasks that need to occur before the game starts 
    /// <summary>Initializes component references and state.</summary>
    public void Awake()
    {
        spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
    }

    /// <summary>Performs initial setup after all Awake calls complete.</summary>
    public void Start()
    {
        
    }

    /// <summary>Initializes initialize.</summary>
    public void Initialize(int col, int row)
    {
        location = new Vector2Int(col, row);
        position = Geometry.CalculatePositionByLocation(location);
        transform.localScale = g.TileScale;
    }
}

}
