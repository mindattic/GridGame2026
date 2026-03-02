using Scripts.Helpers;
using Scripts.Libraries;
using System.Collections.Generic;
using UnityEngine;
using g = Scripts.Helpers.GameHelper;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Factories;
using Scripts.Hub;
using Scripts.Instances.Actor;
using Scripts.Instances.Board;
using Scripts.Instances.SynergyLine;
using Scripts.Inventory;
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
/// DOTTEDLINEINSTANCE - Individual dotted line segment.
/// 
/// PURPOSE:
/// Represents a single segment of a dotted line on the board,
/// used for movement paths or visual connections.
/// 
/// SEGMENT TYPES:
/// Various segment shapes (horizontal, vertical, corners, etc.)
/// defined by DottedLineSegment enum.
/// 
/// CONNECTIVITY:
/// Tracks connected grid locations for path building:
/// - top: Location above
/// - right: Location to right
/// - bottom: Location below
/// - left: Location to left
/// 
/// PROPERTIES:
/// - location: Grid position of this segment
/// - segment: Type of segment (shape)
/// - connectedLocations: Adjacent connected positions
/// 
/// RELATED FILES:
/// - DottedLineManager.cs: Manages all lines
/// - DottedLineFactory.cs: Creates line GameObjects
/// - StageDottedLine.cs: Stage configuration
/// </summary>
public class DottedLineInstance : MonoBehaviour
{
    //Fields
    SpriteRenderer spriteRenderer;
    public Vector2Int location;
    public DottedLineSegment segment;
    public List<Vector2Int> connectedLocations = new List<Vector2Int>();

    //Properties
    public Vector2Int top => location + new Vector2Int(0, -1);
    public Vector2Int right => location + new Vector2Int(1, 0);
    public Vector2Int bottom => location + new Vector2Int(0, 1);
    public Vector2Int left => location + new Vector2Int(-1, 0);


    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public Transform parent
    {
        get => gameObject.transform.parent;
        set => gameObject.transform.SetParent(value, true);
    }

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

    public Sprite sprite
    {
        get => spriteRenderer.sprite;
        set => spriteRenderer.sprite = value;
    }


    public int sortingOrder
    {
        get => spriteRenderer.sortingOrder;
        set => spriteRenderer.sortingOrder = value;
    }







    public void SetColor()
    {
        spriteRenderer.color = ColorHelper.Translucent.Yellow;
    }

    public void ResetColor()
    {
        spriteRenderer.color = ColorHelper.Translucent.White;
    }

    public void Spawn(DottedLineSegment segment, Vector2Int location)
    {
        this.segment = segment;
        this.location = location;
        this.position = Geometry.GetPositionByLocation(this.location);
        this.spriteRenderer.transform.localScale = g.TileScale;

        //Show resources
        var line = SpriteLibrary.Sprites["DottedLine"];
        var turn = SpriteLibrary.Sprites["DottedLineTurn"];
        var arrow = SpriteLibrary.Sprites["DottedLineArrow"];

        connectedLocations.Clear(); //Ensure connectedLocations are reset
        connectedLocations.Add(location); //SpawnActor self-currentLocation to connectedLocations

        switch (this.segment)
        {
            case DottedLineSegment.Vertical:
                sprite = line;
                rotation = Quaternion.identity;
                connectedLocations.AddRange(new[] { top, bottom });
                break;

            case DottedLineSegment.Horizontal:
                sprite = line;
                rotation = Quaternion.Euler(0, 0, 90);
                connectedLocations.AddRange(new[] { left, right });
                break;

            case DottedLineSegment.TurnTopLeft:
                sprite = turn;
                rotation = Quaternion.Euler(0, 0, -180);
                connectedLocations.AddRange(new[] { top, left });
                break;

            case DottedLineSegment.TurnTopRight:
                sprite = turn;
                rotation = Quaternion.Euler(0, 0, 90);
                connectedLocations.AddRange(new[] { top, right });
                break;

            case DottedLineSegment.TurnBottomLeft:
                sprite = turn;
                rotation = Quaternion.Euler(0, 0, -90);
                connectedLocations.AddRange(new[] { bottom, left });
                break;

            case DottedLineSegment.TurnBottomRight:
                sprite = turn;
                rotation = Quaternion.identity;
                connectedLocations.AddRange(new[] { bottom, right });
                break;

            case DottedLineSegment.ArrowUp:
                sprite = arrow;
                rotation = Quaternion.identity;
                connectedLocations.Add(bottom);
                break;

            case DottedLineSegment.ArrowDown:
                sprite = arrow;
                rotation = Quaternion.Euler(0, 0, 180);
                connectedLocations.Add(top);
                break;

            case DottedLineSegment.ArrowLeft:
                sprite = arrow;
                rotation = Quaternion.Euler(0, 0, 90);
                connectedLocations.Add(right);
                break;

            case DottedLineSegment.ArrowRight:
                sprite = arrow;
                rotation = Quaternion.Euler(0, 0, -90);
                connectedLocations.Add(left);
                break;

            default:
                g.LogManager.Warning($"Unhandled segment type: {segment}");
                break;
        }
    }

    public void Despawn()
    {
        StopAllCoroutines();
        g.DottedLineManager.Despawn(this);
    }
}



}
