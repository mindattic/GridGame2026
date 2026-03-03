using Scripts.Helpers;
using Scripts.Helpers;
using TMPro;
using UnityEngine;
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
using Scripts.Libraries;
using Scripts.Models;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Managers
{
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

    /// <summary>Initializes component references and state.</summary>
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

    /// <summary>Performs initial setup after all Awake calls complete.</summary>
    private void Start()
    {
        scene.FadeIn();
    }

    /// <summary>Handles the continue button clicked event.</summary>
    public void OnContinueButtonClicked()
    {
        ProfileHelper.CurrentProfile.CurrentSave = ProfileHelper.CurrentProfile.LatestSave;
        scene.Fade.ToGame();
    }

    /// <summary>Handles the load game button clicked event.</summary>
    public void OnLoadGameButtonClicked()
    {
        scene.Fade.ToSaveFileSelect();
    }

    /// <summary>Handles the new game button clicked event.</summary>
    public void OnNewGameButtonClicked()
    {
        scene.Fade.ToProfileCreate();
    }

    /// <summary>Handles the endless mode clicked event.</summary>
    public void OnEndlessModeClicked()
    {
        GameModeHelper.ToEndlessMode();
    }

    /// <summary>Handles the party manager clicked event.</summary>
    public void OnPartyManagerClicked()
    {
        scene.Fade.ToPartyManager();
    }

    /// <summary>Handles the settings button clicked event.</summary>
    public void OnSettingsButtonClicked()
    {
        scene.Fade.ToSettings();
    }

    /// <summary>Handles the credits button clicked event.</summary>
    public void OnCreditsButtonClicked()
    {
        scene.Fade.ToCredits();
    }

    /// <summary>Handles the change profile button clicked event.</summary>
    public void OnChangeProfileButtonClicked()
    {
        scene.Fade.ToProfileSelect();
    }
}

}
