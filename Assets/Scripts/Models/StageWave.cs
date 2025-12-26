using Assets.Scripts.Models;
using System.Collections.Generic;
using System;
using System.Linq;

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

        // Deep copy the Actors list
        Actors = other.Actors != null
            ? new List<StageActor>(other.Actors.Select(actor => new StageActor(actor)))
            : new List<StageActor>();

        // Deep copy the DottedLines list
        DottedLines = other.DottedLines != null
            ? new List<StageDottedLine>(other.DottedLines.Select(line => new StageDottedLine(line)))
            : new List<StageDottedLine>();
    }
}
