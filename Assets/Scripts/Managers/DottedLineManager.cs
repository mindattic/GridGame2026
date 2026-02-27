using Assets.Scripts.Factories;
using System;
using System.Collections.Generic;
using UnityEngine;
using g = Assets.Helpers.GameHelper;

public class DottedLineManager : MonoBehaviour
{
    public List<DottedLineInstance> dottedLines = new List<DottedLineInstance>();

    /// <summary>
    /// Resets the color of all dotted lines.
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
        // Use factory instead of Instantiate(prefab)
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
    /// Clears all dotted line objects from the scene without using tags.
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
