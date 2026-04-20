using UnityEngine;
using UnityEngine.UI;
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
    public class StageLineRenderer : CanvasLineRenderer
    {
        // Plain private fields — assigned programmatically when spawning the
        // renderer (no Inspector authoring). Awake / callers must set both
        // before Update reads them.
        private Button startButton;
        private Button endButton;

        /// <summary>Assigns the buttons this line renderer tracks.</summary>
        public void SetButtons(Button start, Button end)
        {
            startButton = start;
            endButton = end;
        }

        /// <summary>Runs per-frame update logic.</summary>
        private void Update()
        {
            if (startButton != null && endButton != null)
            {
                UpdateLine(startButton, endButton);
            }
        }
    }
}
