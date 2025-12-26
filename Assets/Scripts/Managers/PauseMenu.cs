using Assets.Helper;
using Assets.Helpers;
using Assets.Scripts.Libraries;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using g = Assets.Helpers.GameHelper;
using scene = Assets.Helpers.SceneHelper;

public class PauseMenu : MonoBehaviour
{
    public bool IsPaused => Time.timeScale == 0f;

    private Image pauseButtonImage;
    private Sprite pauseIcon;
    private Sprite resumeIcon;

    private GameObject pauseMenuRoot;

    private Button pauseButtonRoot;
    private Button resumeButton;
    private TextMeshProUGUI resumeButtonLabel;
    private Button settingsButton;
    private TextMeshProUGUI settingsButtonLabel;
    private Button quitButton;
    private TextMeshProUGUI quitButtonLabel;

    private bool isInitalized;

    private void Awake() { }

    public void Initialize()
    {
        if (isInitalized) return;

        pauseIcon = SpriteLibrary.Sprites["Pause"];
        resumeIcon = SpriteLibrary.Sprites["Paused"];

        pauseButtonRoot = GameObjectHelper.Game.PauseButton.Root;
        pauseButtonRoot.onClick.RemoveAllListeners();
        pauseButtonRoot.onClick.AddListener(OnPauseButtonClicked);

        pauseButtonImage = GameObjectHelper.Game.PauseButton.Image;
        pauseButtonImage.sprite = pauseIcon;
        pauseButtonImage.preserveAspect = true;
        pauseButtonImage.color = Color.white;

        resumeButton = GameObjectHelper.Game.PauseMenu.ResumeButton;
        resumeButtonLabel = GameObjectHelper.Game.PauseMenu.ResumeButtonLabel;
        resumeButton.onClick.RemoveAllListeners();
        resumeButton.onClick.AddListener(OnResumeButtonClicked);

        settingsButton = GameObjectHelper.Game.PauseMenu.SettingsButton;
        settingsButtonLabel = GameObjectHelper.Game.PauseMenu.SettingsButtonLabel;
        settingsButton.onClick.RemoveAllListeners();
        settingsButton.onClick.AddListener(OnSettingsButtonClicked);

        quitButton = GameObjectHelper.Game.PauseMenu.QuitButton;
        quitButtonLabel = GameObjectHelper.Game.PauseMenu.QuitButtonLabel;
        quitButton.onClick.RemoveAllListeners();
        quitButton.onClick.AddListener(OnQuitButtonClicked);

        pauseMenuRoot = GameObjectHelper.Game.PauseMenu.Root;
        pauseMenuRoot.SetActive(false);

        isInitalized = true;
    }

  
    public void Toggle()
    {
        if (IsPaused) 
            OnResumeButtonClicked();
        else 
            OnPauseButtonClicked();
    }

    private void Pause()
    {
        Time.timeScale = 0f;
        pauseButtonImage.sprite = resumeIcon;
        pauseButtonImage.preserveAspect = true;
        pauseMenuRoot.SetActive(true);
    }

    private void Resume()
    {
        Time.timeScale = 1f;
        pauseButtonImage.sprite = pauseIcon;
        pauseButtonImage.preserveAspect = true;
        pauseMenuRoot.SetActive(false);
    }

    private void Runaway()
    {
        Time.timeScale = 1f;
        scene.Fade.ToOverworld();
    }

    public void OnPauseButtonClicked() => Pause();
    public void OnResumeButtonClicked() => Resume();
    public void OnRunAwayClicked() => Runaway();

    public void OnQuickSaveGameButtonClicked()
    {
        ProfileHelper.Save(overwrite: true);
        Resume();
    }

    public void OnCreateSaveGameButtonClicked()
    {
        ProfileHelper.Save(overwrite: false);
        Resume();
    }

    public void OnRestartStageButtonClicked()
    {
        g.StageManager.RestartStage();
        Resume();
    }

    public void OnPartyManagerButtonClicked()
    {
        Time.timeScale = 1f;
        scene.Fade.ToPartyManager();
    }

    public void OnStageSelectButtonClicked()
    {
        Time.timeScale = 1f;
        scene.Fade.ToStageSelect();
    }

    public void OnSettingsButtonClicked()
    {
        Time.timeScale = 1f;
        scene.Fade.ToSettings();
    }

    public void OnQuitButtonClicked()
    {
        Time.timeScale = 1f;
        scene.Fade.ToTitleScreen();
    }
}