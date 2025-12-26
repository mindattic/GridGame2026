using Assets.Helper;
using TMPro;
using UnityEngine;

public class TitleBarInstance : MonoBehaviour
{
    TitleBarInstance instance;
    TextMeshProUGUI label;

    void Awake()
    {
        instance = GameObjectHelper.Game.TitleBar.Instance;
        label = GameObjectHelper.Game.TitleBar.Label;
    }

    void Start()
    {
        Hide();
    }

    public void Show(string text)
    {
        label.text = text;
        instance.gameObject.SetActive(true);
    }


    public void Hide()
    {
        label.text = "";
        instance.gameObject.SetActive(false);
    }
}
