using Assets.Helper;
using Assets.Helpers;
using TMPro;
using UnityEngine;
using scene = Assets.Helpers.SceneHelper;

public class TitleScreenManager : MonoBehaviour
{
    //Fields
    private RectTransform canvas;
    private RectTransform panel;
    private RectTransform continueButton;
    private RectTransform loadGameButton;
    private RectTransform settingsButton;
    private RectTransform creditsButton;
    private RectTransform profileButton;
    private RectTransform profileButtonLabel;

    private void Awake()
    {
        // Always default to Campaign on title load
        GameModeHelper.ToCampaignMode();

        //Verify that game is ready to run
        if (!ProfileHelper.HasProfiles())
            return;

        profileButtonLabel = GameObject.Find(GameObjectHelper.TitleScreen.ProfileButtonLabel).GetComponent<RectTransform>();
        profileButtonLabel.GetComponent<TextMeshProUGUI>().text = ProfileHelper.CurrentProfile.Key;
    }

    private void Start()
    {
        scene.FadeIn();
    }

    public void OnContinueButtonClicked()
    {
        ProfileHelper.CurrentProfile.CurrentSave = ProfileHelper.CurrentProfile.LatestSave;
        scene.Fade.ToGame();
    }

    public void OnLoadGameButtonClicked()
    {
        scene.Fade.ToSaveFileSelect();
    }

    public void OnNewGameButtonClicked()
    {
        scene.Fade.ToProfileCreate();
    }

    public void OnEndlessModeClicked()
    {
        GameModeHelper.ToEndlessMode();
    }

    public void OnPartyManagerClicked()
    {
        scene.Fade.ToPartyManager();
    }

    public void OnSettingsButtonClicked()
    {
        scene.Fade.ToSettings();
    }

    public void OnCreditsButtonClicked()
    {
        scene.Fade.ToCredits();
    }

    public void OnChangeProfileButtonClicked()
    {
        scene.Fade.ToProfileSelect();
    }
}
