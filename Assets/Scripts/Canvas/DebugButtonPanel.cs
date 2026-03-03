using UnityEngine;
using UnityEngine.UI;
using g = Scripts.Helpers.GameHelper;
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
using Scripts.Models;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Canvas
{
    /// <summary>
    /// DEBUGBUTTONPANEL - Debug command button UI panel.
    /// 
    /// PURPOSE:
    /// Provides quick-access debug buttons for testing during
    /// development. Allows stage manipulation and enemy spawning.
    /// 
    /// BUTTONS:
    /// - ReloadStage: Restart current stage
    /// - PreviousStage: Load previous stage (disabled)
    /// - NextStage: Load next stage (disabled)
    /// - SpawnRandomEnemy: Spawn enemy at random location
    /// 
    /// VISIBILITY:
    /// Only shown in development builds when debug mode is enabled.
    /// 
    /// RELATED FILES:
    /// - DebugManager.cs: Handles debug commands
    /// - StageManager.cs: Stage reload/navigation
    /// - GameManager.cs: Debug settings
    /// </summary>
    public class DebugButtonPanel : MonoBehaviour
    {
        //Fields
        [SerializeField] private RectTransform PanelRect;
        [SerializeField] private Button ReloadStageButton;
        [SerializeField] private Button PreviousStageButton;
        [SerializeField] private Button NextStageButton;
        [SerializeField] private Button SpawnRandomEnemyButton;

        /// <summary>Wires button click listeners on startup.</summary>
        private void Start()
        {
            ////SelectProfile anchors and pivot to center
            //PanelRect.anchorMin = new Vector2(0.5f, 0.5f);
            //PanelRect.anchorMax = new Vector2(0.5f, 0.5f);
            //PanelRect.pivot = new Vector2(0.5f, 0.5f);

            ////SelectProfile the anchored position to (0, 0) for centering
            //PanelRect.anchoredPosition = Vector2.zero;

            //SelectProfile button click listeners
            ReloadStageButton.onClick.AddListener(OnReloadStageButtonClicked);
            //PreviousStageButton.onClick.AddListener(OnPreviousStageButtonClicked);
            //NextStageButton.onClick.AddListener(OnNextStageButtonClicked);
            SpawnRandomEnemyButton.onClick.AddListener(OnSpawnRandomEnemyButtonClicked);
        }


        /// <summary>Restarts the current stage when the Reload button is clicked.</summary>
        private void OnReloadStageButtonClicked()
        {
            g.StageManager.RestartStage();
        }

        //private void OnPreviousStageButtonClicked()
        //{
        //    g.StageManager.Previous();
        //}

        //private void OnNextStageButtonClicked()
        //{
        //    g.StageManager.None();
        //}

        /// <summary>Spawns a random enemy via DebugManager when the Spawn button is clicked.</summary>
        private void OnSpawnRandomEnemyButtonClicked()
        {
            g.DebugManager.SpawnRandomEnemy();
        }
    }
}
