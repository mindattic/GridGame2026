using System;
using UnityEngine;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Factories;
using Scripts.Helpers;
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
/// STAGEDOTTEDLINE - Dotted line configuration for stage editor.
/// 
/// PURPOSE:
/// Defines a decorative dotted line segment placed in the stage
/// to visually connect or highlight grid positions.
/// 
/// PROPERTIES:
/// - Segment: Type of line segment (horizontal, vertical, corner, etc.)
/// - Location: Grid position for the line
/// 
/// RELATED FILES:
/// - StageWave.cs: Contains list of dotted lines
/// - DottedLineManager.cs: Renders lines
/// - DottedLineInstance.cs: Line behavior
/// </summary>
[Serializable]
public class StageDottedLine
{
    public DottedLineSegment Segment;
    public Vector2Int Location;

    public StageDottedLine() { }

    public StageDottedLine(StageDottedLine other)
    {
        Segment = other.Segment;
        Location = other.Location;  // Vector2Int is a struct, so it's copied by value.
    }
}

}
