using UnityEngine;
using UnityEngine.UI;
using g = Assets.Helpers.GameHelper;

namespace Assets.Scripts.GUI
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

        private void OnSpawnRandomEnemyButtonClicked()
        {
            g.DebugManager.SpawnRandomEnemy();
        }
    }
}
