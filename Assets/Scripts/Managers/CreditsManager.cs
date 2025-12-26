using Assets.Helper;
using TMPro;
using UnityEngine;
using c = Assets.Helpers.CanvasHelper;
using scene = Assets.Helpers.SceneHelper;

public class CreditsManager : MonoBehaviour
{
    //Fields
    private RectTransform title;
    private RectTransform scrollView;
    private RectTransform content;
    private RectTransform textarea;


    private void Awake()
    {
        //title = GameObject.Find(GameObjectHelper.Credits.Title).GetComponent<RectTransform>();
        //scrollView = GameObject.Find(GameObjectHelper.Credits.ScrollView).GetComponent<RectTransform>();
        //content = GameObject.Find(GameObjectHelper.Credits.Content).GetComponent<RectTransform>();
        textarea = GameObject.Find(GameObjectHelper.Credits.Textarea).GetComponent<RectTransform>();

        //var startX = canvas.rect.width;
        //var startY = canvas.rect.height;
        //var buttonWidth = 0.9f * startX;
        //var buttonHeight = startY / 16f;
        //var fontSize = buttonHeight / 2;
        //var rowSpacing = 0.01f * startY;

        //title.GetComponent<Label>().fontSize = fontSize;
        //scrollView.anchoredPosition = scrollView.anchoredPosition.SetY(-buttonHeight);
        //content.GetComponent<VerticalLayoutGroup>().spacing = rowSpacing;

    

        const string NL = "\r\n";
        string text
            = $"{NL}{NL}"
            + $"<size=80%>Game Design & Development</size>{NL}"
            + $"<size=150%>Ryan DeBraal</size>{NL}{NL}"
            + $"<size=80%>Typography</size>{NL}"
            + $"<size=150%>Brian Willson</size> <size=50%>(Attic)</size>{NL}"
            + $"<size=150%>Jonas Hecksher</size> <size=50%>(???)</size>{NL}{NL}"
            + $"<size=80%>Visual Effects</size>{NL}"
            + $"<size=150%>Eric Wang</size>{NL}{NL}"
            + $"<size=80%>Graphics</size>{NL}"
            + $"<size=150%>Sagak Art (Pururu) - Topdown 8-Direction Mini Characters</size>{NL}"
            + $"<size=10%>https://sagak-art-pururu.itch.io/24pxminicharacters</size>{NL}{NL}"
            + $"<size=150%>Hannemann - Virtual Joystick</size>{NL}"
            + $"<size=10%>https://hannemann.itch.io/virtual-joystick-pack-free</size>{NL}{NL}"
            + $"{NL}{NL}{NL}{NL}{NL}{NL}{NL}{NL}{NL}{NL}{NL}{NL}{NL}{NL}{NL}{NL}"
            + $"{NL}{NL}{NL}{NL}{NL}{NL}{NL}{NL}{NL}{NL}{NL}{NL}{NL}{NL}{NL}{NL}"
            + $"{NL}{NL}{NL}{NL}{NL}{NL}{NL}{NL}{NL}{NL}{NL}{NL}{NL}{NL}{NL}{NL}"
            + $"{NL}{NL}{NL}{NL}{NL}{NL}{NL}{NL}{NL}{NL}{NL}{NL}{NL}{NL}{NL}{NL}"
            + $"{NL}{NL}{NL}{NL}{NL}{NL}{NL}{NL}{NL}{NL}{NL}{NL}{NL}{NL}{NL}{NL}"
            + $"Thanks for playing!";
        var label = textarea.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.ForceMeshUpdate();

        var textareaHeight
            = label.textInfo.lineCount
            * label.textInfo.lineInfo[0].lineHeight
            + c.CanvasRect.rect.height * 0.5f;

        textarea.sizeDelta = new Vector2(c.CanvasRect.rect.width, textareaHeight);
    }
    private void Start()
    {
        scene.FadeIn();
    }

    public void OnBackButtonClicked()
    {

        scene.Fade.ToPreviousScene();
    }


}
