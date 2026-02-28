using Assets.Helper;
using Assets.Helpers;
using Assets.Scripts.Factories;
using Assets.Scripts.Libraries;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;
using scene = Assets.Helpers.SceneHelper;

/// <summary>
/// STAGESELECTMANAGER - Manages the stage/level selection screen.
/// 
/// PURPOSE:
/// Displays available stages and allows the player to select which
/// stage to play. Shows unlocked stages based on profile progress.
/// 
/// VISUAL LAYOUT:
/// ```
/// ┌─────────────────────────────────────┐
/// │         Select Stage                │
/// ├─────────────────────────────────────┤
/// │  ┌─────────────────────────────┐   │
/// │  │  Stage 1 - Forest          │   │ ← Unlocked
/// │  └─────────────────────────────┘   │
/// │  ┌─────────────────────────────┐   │
/// │  │  Stage 2 - Castle          │   │ ← Unlocked
/// │  └─────────────────────────────┘   │
/// │  ┌─────────────────────────────┐   │
/// │  │  Stage 3 - 🔒 Locked       │   │ ← Locked
/// │  └─────────────────────────────┘   │
/// └─────────────────────────────────────┘
/// ```
/// 
/// STAGE LOADING:
/// Stages loaded from StageLibrary. Each button triggers
/// scene transition to the selected stage.
/// 
/// RELATED FILES:
/// - ScreenWidthButtonFactory.cs: Creates stage buttons
/// - StageLibrary.cs: Stage definitions
/// - StageManager.cs: Stage gameplay logic
/// - ProfileManager.cs: Progress tracking
/// 
/// ACCESS: Scene-based manager (StageSelect scene)
/// </summary>
public class StageSelectManager : MonoBehaviour
{
    #region UI References

    private TextMeshProUGUI header;
    private RectTransform scrollView;
    private Transform content;
    private VerticalLayoutGroup verticalLayoutGroup;
    private float screenWidth;
    private float screenHeight;
    private float buttonWidth;
    private float buttonHeight;
    private float spacing;

    #endregion

    #region Initialization

    private void Awake()
    {
        content = GameObject.Find(GameObjectHelper.StageSelect.Content).GetComponent<Transform>();

        // Create button for each stage
        foreach (var stage in StageLibrary.Stages)
        {
            AddButton(stage.Value.Name);
        }
    }

    private void Start()
    {
        scene.FadeIn();
    }

    #endregion

    #region Button Creation

    /// <summary>Creates a stage selection button.</summary>
    public void AddButton(string stageName)
    {
        GameObject instance = ScreenWidthButtonFactory.Create(content);
        instance.name = $"Button_{stageName}";

        //Show the button's click event
        Button button = instance.GetComponent<Button>();
        button.onClick.AddListener(() => OnStageSelectButtonClicked(stageName));

        //Show the button textarea
        TextMeshProUGUI label = instance.GetComponentInChildren<TextMeshProUGUI>();
        label.text = stageName;
    }

    private void OnStageSelectButtonClicked(string stageName)
    {
        ProfileHelper.CurrentProfile.LatestSave.Stage.CurrentStage = stageName;
        scene.Fade.ToGame();
    }

    public void OnBackButtonClicked()
    {
        scene.Fade.ToPreviousScene();
    }

    #endregion
}
