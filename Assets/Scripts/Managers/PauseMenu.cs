using Scripts.Helpers;
using Scripts.Helpers;
using Scripts.Libraries;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using g = Scripts.Helpers.GameHelper;
using scene = Scripts.Helpers.SceneHelper;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Factories;
using Scripts.Hub;
using Scripts.Instances;
using Scripts.Instances.Actor;
using Scripts.Instances.Board;
using Scripts.Instances.SynergyLine;
using Scripts.Inventory;
using Scripts.Models;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Managers
{
/// <summary>
/// PAUSEMENU - Handles game pause state and pause menu UI.
/// 
/// PURPOSE:
/// Manages pausing/resuming the game and displays the pause menu
/// with Resume, Settings, and Quit options.
/// 
/// VISUAL APPEARANCE:
/// ```
/// ┌─────────────────────────┐
/// │                         │
/// │     [ Resume ]          │
/// │     [ Settings ]        │
/// │     [ Quit ]            │
/// │                         │
/// └─────────────────────────┘
/// ```
/// 
/// PAUSE STATE:
/// - IsPaused: True when Time.timeScale == 0
/// - Pausing sets Time.timeScale = 0 (freezes game)
/// - Resuming restores Time.timeScale = 1
/// 
/// BUTTONS:
/// - Resume: Closes menu, resumes game
/// - Settings: Opens settings panel
/// - Quit: Shows confirmation, returns to menu
/// 
/// INITIALIZATION:
/// Initialize() must be called after scene load to wire up buttons.
/// 
/// RELATED FILES:
/// - SettingsManager.cs: Settings panel
/// - ConfirmationDialogInstance.cs: Quit confirmation
/// - GameManager.cs: Central game state
/// - InputManager.cs: Checks IsPaused for input blocking
/// 
/// ACCESS: g.PauseMenu
/// </summary>
public class PauseMenu : MonoBehaviour
{
    /// <summary>True when game is paused (Time.timeScale == 0).</summary>
    public bool IsPaused => Time.timeScale == 0f;

    #region UI References

    private Image pauseButtonImage;
    private Sprite pauseIcon;
    private Sprite resumeIcon;

    private Button pauseButtonRoot;
    private Button resumeButton;
    private TextMeshProUGUI resumeButtonLabel;
    private Button settingsButton;
    private TextMeshProUGUI settingsButtonLabel;
    private Button quitButton;
    private TextMeshProUGUI quitButtonLabel;

    private bool isInitalized;

    #endregion

    #region Initialization

    /// <summary>Initializes component references and state.</summary>
    private void Awake() { }

    /// <summary>Wires up button events. Call after scene load.</summary>
    public void Initialize()
    {
        if (isInitalized) return;

        pauseIcon = SpriteLibrary.Sprites["Pause"];
        resumeIcon = SpriteLibrary.Sprites["Paused"];

        // PauseButton in main game UI
        pauseButtonRoot = GameObjectHelper.Game.PauseButton.Root;
        pauseButtonRoot.onClick.RemoveAllListeners();
        pauseButtonRoot.onClick.AddListener(OnPauseButtonClicked);

        pauseButtonImage = GameObjectHelper.Game.PauseButton.Image;
        pauseButtonImage.sprite = pauseIcon;
        pauseButtonImage.preserveAspect = true;
        pauseButtonImage.color = Color.white;

        // Use local references (transform.Find works on inactive children)
        var inner = transform.Find("Inner");

        var resumeGO = inner?.Find("ResumeButton");
        if (resumeGO != null)
        {
            resumeButton = resumeGO.GetComponent<Button>();
            resumeButtonLabel = resumeGO.Find("Label")?.GetComponent<TextMeshProUGUI>();
            resumeButton.onClick.RemoveAllListeners();
            resumeButton.onClick.AddListener(OnResumeButtonClicked);
        }

        var settingsGO = inner?.Find("SettingsButton");
        if (settingsGO != null)
        {
            settingsButton = settingsGO.GetComponent<Button>();
            settingsButtonLabel = settingsGO.Find("Label")?.GetComponent<TextMeshProUGUI>();
            settingsButton.onClick.RemoveAllListeners();
            settingsButton.onClick.AddListener(OnSettingsButtonClicked);
        }

        var quitGO = inner?.Find("QuitButton");
        if (quitGO != null)
        {
            quitButton = quitGO.GetComponent<Button>();
            quitButtonLabel = quitGO.Find("Label")?.GetComponent<TextMeshProUGUI>();
            quitButton.onClick.RemoveAllListeners();
            quitButton.onClick.AddListener(OnQuitButtonClicked);
        }

        // Ensure PauseMenu covers full screen
        var rt = GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; // Left, Bottom = 0
            rt.offsetMax = Vector2.zero; // Right, Top = 0
            rt.pivot = new Vector2(0.5f, 0.5f);
        }

        // Ensure we start inactive
        gameObject.SetActive(false);

        isInitalized = true;
    }


    /// <summary>Toggles this component.</summary>
    public void Toggle()
    {
        if (IsPaused) 
            OnResumeButtonClicked();
        else 
            OnPauseButtonClicked();
    }

    /// <summary>Pause.</summary>
    private void Pause()
    {
        Time.timeScale = 0f;
        pauseButtonImage.sprite = resumeIcon;
        pauseButtonImage.preserveAspect = true;
        gameObject.SetActive(true);
    }

    /// <summary>Resume.</summary>
    private void Resume()
    {
        Time.timeScale = 1f;
        pauseButtonImage.sprite = pauseIcon;
        pauseButtonImage.preserveAspect = true;
        gameObject.SetActive(false);
    }

    /// <summary>Runaway.</summary>
    private void Runaway()
    {
        Time.timeScale = 1f;
        scene.Fade.ToOverworld();
    }

    /// <summary>Handles the pause button clicked event.</summary>
    public void OnPauseButtonClicked() => Pause();
    /// <summary>Handles the resume button clicked event.</summary>
    public void OnResumeButtonClicked() => Resume();
    /// <summary>Handles the run away clicked event.</summary>
    public void OnRunAwayClicked() => Runaway();

    /// <summary>Handles the quick save game button clicked event.</summary>
    public void OnQuickSaveGameButtonClicked()
    {
        ProfileHelper.Save(overwrite: true);
        Resume();
    }

    /// <summary>Handles the create save game button clicked event.</summary>
    public void OnCreateSaveGameButtonClicked()
    {
        ProfileHelper.Save(overwrite: false);
        Resume();
    }

    /// <summary>Handles the restart stage button clicked event.</summary>
    public void OnRestartStageButtonClicked()
    {
        g.StageManager.RestartStage();
        Resume();
    }

    /// <summary>Handles the party manager button clicked event.</summary>
    public void OnPartyManagerButtonClicked()
    {
        Time.timeScale = 1f;
        scene.Fade.ToPartyManager();
    }

    /// <summary>Handles the stage select button clicked event.</summary>
    public void OnStageSelectButtonClicked()
    {
        Time.timeScale = 1f;
        scene.Fade.ToStageSelect();
    }

    /// <summary>Handles the settings button clicked event.</summary>
    public void OnSettingsButtonClicked()
    {
        Time.timeScale = 1f;
        scene.Fade.ToSettings();
    }

    /// <summary>Handles the quit button clicked event.</summary>
    public void OnQuitButtonClicked()
    {
        Time.timeScale = 1f;
        scene.Fade.ToTitleScreen();
    }

    #endregion
}
}
