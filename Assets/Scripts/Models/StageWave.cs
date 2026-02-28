using Assets.Scripts.Models;
using System.Collections.Generic;
using System;
using System.Linq;

/// <summary>
/// STAGEWAVE - Enemy wave definition for a stage.
/// 
/// PURPOSE:
/// Defines a single wave of enemies within a stage,
/// including enemy placements and dotted line indicators.
/// 
/// PROPERTIES:
/// - WaveID: Wave number (1-indexed)
/// - Actors: List of enemy placements
/// - DottedLines: Visual connections between actors
/// 
/// RELATED FILES:
/// - Stage.cs: Contains list of waves
/// - StageActor.cs: Individual enemy placement
/// - StageManager.cs: Wave execution
/// </summary>
[Serializable]
public class StageWave
{
    public int WaveID;
    public List<StageActor> Actors;
    public List<StageDottedLine> DottedLines;

    public StageWave() { }

    public StageWave(StageWave other)
    {
        WaveID = other.WaveID;

        Actors = other.Actors != null
            ? new List<StageActor>(other.Actors.Select(actor => new StageActor(actor)))
            : new List<StageActor>();

        DottedLines = other.DottedLines != null
            ? new List<StageDottedLine>(other.DottedLines.Select(line => new StageDottedLine(line)))
            : new List<StageDottedLine>();
    }
}
