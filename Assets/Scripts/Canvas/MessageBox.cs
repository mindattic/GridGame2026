using System;
using UnityEngine;
using UnityEngine.UI;
using Assets.Helper;
using c = Assets.Helpers.CanvasHelper;
using Assets.Scripts.Libraries;
using TMPro;

/// <summary>
/// Runtime instance of the MessageBox prefab.
/// Displays a single message with an OK button and invokes a callback when dismissed.
/// </summary>
public class MessageBoxInstance : MonoBehaviour
{
    private RectTransform panel;
    private RectTransform prompt;
    private RectTransform buttonOk;

    // Optional callback invoked when OK is pressed.
    public Action onOkClicked;

    /// <summary>
    /// Initializes the dialog, sizes the UI, binds the OK event, and sets the message text.
    /// </summary>
    /// <param name="text">Message text to display.</param>
    /// <param name="onOk">Callback invoked when OK is pressed.</param>
    public void Assign(string text, Action onOk = default)
    {
        Setup();
        ResizeUI();
        BindEvents();

        prompt.GetComponent<TextMeshProUGUI>().text = text;
        onOkClicked = onOk;
    }

    /// <summary>
    /// Resolves required RectTransform references from the scene.
    /// Call after the prefab has been instantiated.
    /// </summary>
    private void Setup()
    {
        panel = GameObject.Find(GameObjectHelper.MessageBox.Panel).GetComponent<RectTransform>();
        prompt = GameObject.Find(GameObjectHelper.MessageBox.Prompt).GetComponent<RectTransform>();
        buttonOk = GameObject.Find(GameObjectHelper.MessageBox.ButtonOk).GetComponent<RectTransform>();
    }

    /// <summary>
    /// Resizes the dialog elements based on current canvas dimensions.
    /// Matches the panel to the canvas and scales the OK button proportionally.
    /// </summary>
    private void ResizeUI()
    {
        float screenWidth = c.CanvasRect.rect.width;
        float screenHeight = c.CanvasRect.rect.height;

        float keyWidth = screenWidth * 0.9f / 10f;
        float keyHeight = keyWidth;

        panel.sizeDelta = new Vector2(screenWidth, screenHeight);
        panel.anchoredPosition = Vector2.zero;

        buttonOk.sizeDelta = new Vector2(keyWidth * 3f, keyHeight);
    }

    /// <summary>
    /// Binds the OK button click to Submit.
    /// </summary>
    private void BindEvents()
    {
        buttonOk.GetComponent<Button>().onClick.AddListener(Submit);
    }

    /// <summary>
    /// Invokes the OK callback and destroys this dialog instance.
    /// </summary>
    private void Submit()
    {
        onOkClicked?.Invoke();
        Destroy(gameObject);
    }
}

/// <summary>
/// Static helper for creating and showing a MessageBox instance.
/// </summary>
public static class MessageBox
{
    /// <summary>
    /// Shows the MessageBox prefab on the main canvas with the given text and callback.
    /// </summary>
    /// <param name="text">Message to display.</param>
    /// <param name="onOk">Callback invoked when OK is pressed.</param>
    /// <returns>The instantiated MessageBoxInstance.</returns>
    public static MessageBoxInstance Show(
        string text = "Message",
        Action onOk = null)
    {
        var prefab = PrefabLibrary.Prefabs["MessageBoxPrefab"];
        if (prefab == null)
            throw new UnityException("Prefab not found: MessageBoxPrefab");

        GameObject go = GameObject.Instantiate(prefab, c.CanvasRect);
        if (go == null)
            throw new UnityException("Failed to instantiate MessageBox prefab");

        go.name = "MessageBoxPrefab";

        var instance = go.GetComponent<MessageBoxInstance>();
        if (instance == null)
            throw new UnityException("MessageBoxInstance component not found on the prefab");

        instance.Assign(text, onOk);
        return instance;
    }
}
