using Assets.Helper;
using Assets.Helpers;
using Assets.Scripts.Factories;
using Assets.Scripts.Libraries;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;
using scene = Assets.Helpers.SceneHelper;

public class StageSelectManager : MonoBehaviour
{
    //Fields
    private TextMeshProUGUI header;
    private RectTransform scrollView;
    private Transform content;
    private VerticalLayoutGroup verticalLayoutGroup;
    private float screenWidth;
    private float screenHeight;
    private float buttonWidth;
    private float buttonHeight;
    private float spacing;


    private void Awake()
    {
        content = GameObject.Find(GameObjectHelper.StageSelect.Content).GetComponent<Transform>();

        //startX = canvas.rect.width;
        //startY = canvas.rect.height;

        //buttonWidth = 0.9f * startX;
        //buttonHeight = startY / 16f;

        //header.fontSize = buttonHeight / 2;
        //scrollView.anchoredPosition = scrollView.anchoredPosition.SetY(-buttonHeight);

        //spacing = 0.01f * startY;
        //verticalLayoutGroup.spacing = spacing;

        foreach (var stage in StageLibrary.Stages)
        {
            AddButton(stage.Value.Name);
        }
    }

    private void Start()
    {
        scene.FadeIn();
    }

    public void AddButton(string stageName)
    {
        // Use factory instead of Instantiate(prefab)
        GameObject instance = ScreenWidthButtonFactory.Create(content);
        instance.name = $"Button_{stageName}";

        //Show the button size
        //RectTransform buttonRect = instance.GetComponent<RectTransform>();
        //buttonRect.sizeDelta = new Vector2(buttonWidth, buttonHeight);

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

}
