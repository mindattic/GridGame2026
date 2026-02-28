using Assets.Scripts.Factories;
using System;
using System.Collections.Generic;
using UnityEngine;
using g = Assets.Helpers.GameHelper;

/// <summary>
/// DOTTEDLINEMANAGER - Manages movement path visualization lines.
/// 
/// PURPOSE:
/// Creates and manages dotted line segments that show movement paths
/// when a hero is being dragged to a new position.
/// 
/// VISUAL APPEARANCE:
/// ```
/// [Hero] · · · · · · · · · [Destination]
///        ↑ dotted line path
/// ```
/// 
/// SEGMENT TYPES (DottedLineSegment):
/// - Horizontal: Left-right segments
/// - Vertical: Up-down segments
/// - Corner: L-shaped turn segments
/// - Start/End: Path terminus markers
/// 
/// LIFECYCLE:
/// 1. Player starts dragging hero
/// 2. Path calculated from start to drag position
/// 3. Spawn() called for each segment
/// 4. Lines update as drag position changes
/// 5. Clear() when drag ends
/// 
/// RELATED FILES:
/// - DottedLineFactory.cs: Creates line GameObjects
/// - DottedLineInstance.cs: Individual line segment
/// - InputManager.cs: Triggers line creation during drag
/// - Geometry.cs: Path calculation utilities
/// 
/// ACCESS: g.DottedLineManager
/// </summary>
public class DottedLineManager : MonoBehaviour
{
    /// <summary>All active dotted line segments.</summary>
    public List<DottedLineInstance> dottedLines = new List<DottedLineInstance>();

    /// <summary>
    /// Resets the color of all dotted lines to default.
    /// </summary>
    private void ResetColors()
    {
        foreach (var dottedLine in dottedLines)
        {
            dottedLine.ResetColor();
        }
    }

    /// <summary>
    /// Spawns a dotted line segment at a specific board location.
    /// </summary>
    public void Spawn(DottedLineSegment segment, Vector2Int location)
    {
        GameObject go = DottedLineFactory.Create();
        go.transform.position = Vector2.zero;
        go.transform.rotation = Quaternion.identity;
        var instance = go.GetComponent<DottedLineInstance>();
        instance.name = $"DottedLine_{Guid.NewGuid():N}";
        instance.parent = g.Board.transform;
        instance.Spawn(segment, location);
        dottedLines.Add(instance);
    }

    /// <summary>
    /// Removes a specific dotted line instance.
    /// </summary>
    public void Despawn(DottedLineInstance instance)
    {
        dottedLines.Remove(instance);
        Destroy(instance.gameObject);
    }

    /// <summary>
    /// Clears all dotted line objects from the scene.
    /// </summary>
    public void Clear()
    {
        var instances = GameObject.FindObjectsByType<DottedLineInstance>(FindObjectsSortMode.None);
        foreach (var instance in instances)
        {
            Destroy(instance.gameObject);
        }
    }
}
