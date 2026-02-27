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

        // PauseButton is in the main game UI (always active)
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
        gameObject.SetActive(true);
    }

    private void Resume()
    {
        Time.timeScale = 1f;
        pauseButtonImage.sprite = pauseIcon;
        pauseButtonImage.preserveAspect = true;
        gameObject.SetActive(false);
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