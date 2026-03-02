using Scripts.Models;
using System.Collections.Generic;
using System;
using System.Linq;
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

}
