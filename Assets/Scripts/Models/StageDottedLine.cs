using System;
using UnityEngine;

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
