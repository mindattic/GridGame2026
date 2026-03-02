using Scripts.Helpers;
using Scripts.Factories;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using c = Scripts.Helpers.CanvasHelper;
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
/// CONFIRMATIONDIALOGINSTANCE - Yes/No confirmation dialog.
/// 
/// PURPOSE:
/// Displays a prompt with Yes and No buttons for player confirmation.
/// Used for important decisions like quitting, restarting, etc.
/// 
/// VISUAL APPEARANCE:
/// ```
/// ┌────────────────────────────────┐
/// │                                │
/// │   Are you sure you want to     │
/// │   restart the stage?           │
/// │                                │
/// │    [ Yes ]      [ No ]         │
/// └────────────────────────────────┘
/// ```
/// 
/// USAGE:
/// ```csharp
/// var dialog = ConfirmationDialogFactory.Create(canvas);
/// var instance = dialog.GetComponent<ConfirmationDialogInstance>();
/// instance.Assign("Restart stage?", (confirmed) => {
///     if (confirmed) RestartStage();
///     Destroy(dialog);
/// });
/// ```
/// 
/// CALLBACK:
/// onSubmitClicked(bool):
/// - true: Player pressed Yes
/// - false: Player pressed No
/// 
/// RELATED FILES:
/// - ConfirmationDialogFactory.cs: Creates dialog GameObjects
/// - MessageBoxInstance.cs: OK-only variant
/// - PauseMenu.cs: Uses confirmation for quit
/// 
/// ACCESS: Created via ConfirmationDialogFactory.Create()
/// </summary>
public class ConfirmationDialogInstance : MonoBehaviour
{
    private RectTransform panel;
    private RectTransform prompt;
    private RectTransform buttonYes;
    private RectTransform buttonNo;

    /// <summary>Callback with true for Yes, false for No.</summary>
    public Action<bool> onSubmitClicked;

    /// <summary>
    /// Initializes dialog with prompt text and callback.
    /// </summary>
    public void Assign(string text, Action<bool> onSubmit = default)
    {
        Setup();
        ResizeUI();
        BindEvents();

        prompt.GetComponent<TextMeshProUGUI>().text = text;
        onSubmitClicked = onSubmit;
    }

    /// <summary>
    /// Resolves required RectTransform references from hierarchy.
    /// </summary>
    private void Setup()
    {
        panel = GameObject.Find(GameObjectHelper.ConfirmationDialog.Panel).GetComponent<RectTransform>();
        prompt = GameObject.Find(GameObjectHelper.ConfirmationDialog.Prompt).GetComponent<RectTransform>();
        buttonYes = GameObject.Find(GameObjectHelper.ConfirmationDialog.ButtonYes).GetComponent<RectTransform>();
        buttonNo = GameObject.Find(GameObjectHelper.ConfirmationDialog.ButtonNo).GetComponent<RectTransform>();
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

        buttonYes.sizeDelta = new Vector2(keyWidth * 2f, keyHeight);
        buttonNo.sizeDelta = new Vector2(keyWidth * 2f, keyHeight);
    }

    /// <summary>
    /// Binds Yes and No button clicks to Submit.
    /// </summary>
    private void BindEvents()
    {
        buttonYes.GetComponent<Button>().onClick.AddListener(() => Submit(true));
        buttonNo.GetComponent<Button>().onClick.AddListener(() => Submit(false));
    }

    /// <summary>
    /// Invokes the submit callback with the user's choice and destroys this dialog instance.
    /// </summary>
    /// <param name="result">True if Yes was selected, false if No.</param>
    private void Submit(bool result)
    {
        onSubmitClicked?.Invoke(result);
        Destroy(gameObject);
    }
}

/// <summary>
/// Static helper for creating and showing a ConfirmationDialog instance.
/// </summary>
public static class ConfirmationDialog
{
    /// <summary>
    /// Shows the ConfirmationDialog prefab on the main canvas with the given text and callback.
    /// </summary>
    /// <param name="text">Prompt to display.</param>
    /// <param name="onSubmit">Callback receiving true for Yes or false for No.</param>
    /// <returns>The instantiated ConfirmationDialogInstance.</returns>
    public static ConfirmationDialogInstance Show(
        string text = "Are you sure?",
        Action<bool> onSubmit = null)
    {
        // Use factory instead of Instantiate(prefab)
        GameObject go = ConfirmationDialogFactory.Create(c.CanvasRect);
        if (go == null)
            throw new UnityException("Failed to create ConfirmationDialog");

        go.name = "ConfirmationDialog";

        var instance = go.GetComponent<ConfirmationDialogInstance>();
        if (instance == null)
            throw new UnityException("ConfirmationDialogInstance component not found");

        instance.Assign(text, onSubmit);
        return instance;
    }
}

}
