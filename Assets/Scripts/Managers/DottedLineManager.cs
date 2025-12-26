using Assets.Scripts.Libraries;
using System;
using System.Collections.Generic;
using UnityEngine;
using g = Assets.Helpers.GameHelper;

public class DottedLineManager : MonoBehaviour
{
    // Fields
    private GameObject DottedLinePrefab;

    public List<DottedLineInstance> dottedLines = new List<DottedLineInstance>();

    private void Awake()
    {
        DottedLinePrefab = PrefabLibrary.Prefabs["DottedLinePrefab"];
    }

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
        GameObject prefab = Instantiate(DottedLinePrefab, Vector2.zero, Quaternion.identity);
        var instance = prefab.GetComponent<DottedLineInstance>();
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
