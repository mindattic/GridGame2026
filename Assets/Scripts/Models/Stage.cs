using Assets.Scripts.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Assets.Scripts.Models
{
    /// <summary>
    /// STAGE - Level/stage data structure.
    /// 
    /// PURPOSE:
    /// Defines a playable stage including enemy waves,
    /// win conditions, and associated tutorials.
    /// 
    /// PROPERTIES:
    /// - Name: Stage identifier (e.g., "GreenValley-01")
    /// - Description: Display text
    /// - CompletionCondition: Win condition type
    /// - CompletionValue: Target for condition
    /// - Waves: List of enemy waves
    /// - Tutorials: Tutorial keys to show
    /// 
    /// RELATED FILES:
    /// - StageLibrary.cs: Stage registry
    /// - StageManager.cs: Stage execution
    /// - StageWave.cs: Wave data structure
    /// </summary>
    [Serializable]
    public class Stage
    {
        public Stage() { }

        public Stage(Stage other)
        {
            Name = other.Name;
            Description = other.Description;
            CompletionCondition = other.CompletionCondition;
            CompletionValue = other.CompletionValue;
            Tutorials = other.Tutorials != null ? new List<string>(other.Tutorials) : new List<string>();

            Waves = other.Waves != null
                ? new List<StageWave>(other.Waves.Select(wave => new StageWave(wave)))
                : new List<StageWave>();
        }


        public string Name;
        public string Description;
        public string CompletionCondition;
        public int CompletionValue;
        public List<string> Tutorials;
        public List<StageWave> Waves;
    }

}