using Assets.Scripts.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Assets.Scripts.Models
{
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

            // Deep copy each StageWave using its copy constructor
            Waves = other.Waves != null
                ? new List<StageWave>(other.Waves.Select(wave => new StageWave(wave)))
                : new List<StageWave>();
        }


        public string Name;
        public string Description;
        public string CompletionCondition;
        public int CompletionValue;
        public List<string> Tutorials;
        public List<StageWave> Waves; // Replacing Actors and DottedLines with waves
    }

}