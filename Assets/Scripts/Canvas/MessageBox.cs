using System;
using UnityEngine;
using UnityEngine.UI;
using Scripts.Helpers;
using Scripts.Factories;
using c = Scripts.Helpers.CanvasHelper;
using TMPro;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Hub;
using Scripts.Instances;
using Scripts.Instances.Actor;
using Scripts.Instances.Board;
using Scripts.Instances.SynergyLine;
using Scripts.Inventory;
using Scripts.Libraries;
using Scripts.Managers;
using Scripts.Models;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Canvas
{
/// <summary>
/// MESSAGEBOXINSTANCE - Simple message dialog with OK button.
/// 
/// PURPOSE:
/// Displays a message to the player with a single OK button to dismiss.
/// Used for notifications, alerts, and simple confirmations.
/// 
/// VISUAL APPEARANCE:
/// ```
/// ┌────────────────────────────┐
/// │                            │
/// │   Your message text here   │
/// │                            │
/// │         [ OK ]             │
/// └────────────────────────────┘
/// ```
/// 
/// USAGE:
/// ```csharp
/// var dialog = MessageBoxFactory.Create(canvas);
/// var instance = dialog.GetComponent<MessageBoxInstance>();
/// instance.Assign("Stage Complete!", () => {
///     // Called when OK clicked
///     LoadNextStage();
/// });
/// ```
/// 
/// CALLBACK:
/// onOkClicked invoked when player presses OK button.
/// Dialog auto-destroys after callback.
/// 
/// RELATED FILES:
/// - MessageBoxFactory.cs: Creates dialog GameObjects
/// - ConfirmationDialogInstance.cs: Yes/No variant
/// - UIHelper.cs: UI sizing utilities
/// 
/// ACCESS: Created via MessageBoxFactory.Create()
/// </summary>
public class MessageBoxInstance : MonoBehaviour
{
    private RectTransform panel;
    private RectTransform prompt;
    private RectTransform buttonOk;

    /// <summary>Callback invoked when OK is pressed.</summary>
    public Action onOkClicked;

    /// <summary>
    /// Initializes the dialog with message text and optional callback.
    /// </summary>
    public void Assign(string text, Action onOk = default)
    {
        Setup();
        ResizeUI();
        BindEvents();

        prompt.GetComponent<TextMeshProUGUI>().text = text;
        onOkClicked = onOk;
    }

    /// <summary>
    /// Resolves required RectTransform references from hierarchy.
    /// </summary>
    private void Setup()
    {
        panel = GameObject.Find(GameObjectHelper.MessageBox.Panel).GetComponent<RectTransform>();
        prompt = GameObject.Find(GameObjectHelper.MessageBox.Prompt).GetComponent<RectTransform>();
        buttonOk = GameObject.Find(GameObjectHelper.MessageBox.ButtonOk).GetComponent<RectTransform>();
    }

    /// <summary>
    /// Resizes dialog elements based on canvas dimensions.
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
        // Use factory instead of Instantiate(prefab)
        GameObject go = MessageBoxFactory.Create(c.CanvasRect);
        if (go == null)
            throw new UnityException("Failed to create MessageBox");

        go.name = "MessageBox";

        var instance = go.GetComponent<MessageBoxInstance>();
        if (instance == null)
            throw new UnityException("MessageBoxInstance component not found");

        instance.Assign(text, onOk);
        return instance;
    }
}

}
