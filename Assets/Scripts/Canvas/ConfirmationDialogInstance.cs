using Assets.Helper;
using Assets.Scripts.Libraries;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using c = Assets.Helpers.CanvasHelper;

/// <summary>
/// Runtime instance of the ConfirmationDialog prefab.
/// Handles wiring, sizing, and callback invocation for Yes/No.
/// </summary>
public class ConfirmationDialogInstance : MonoBehaviour
{
    private RectTransform panel;
    private RectTransform prompt;
    private RectTransform buttonYes;
    private RectTransform buttonNo;

    // Exposed so callers can subscribe if needed after creation.
    public Action<bool> onSubmitClicked;

    /// <summary>
    /// Initializes the dialog, sizes the UI, binds button events, and sets the prompt text.
    /// </summary>
    /// <param name="text">Prompt text to display.</param>
    /// <param name="onSubmit">Callback invoked with true for Yes and false for No.</param>
    public void Assign(string text, Action<bool> onSubmit = default)
    {
        Setup();
        ResizeUI();
        BindEvents();

        prompt.GetComponent<TextMeshProUGUI>().text = text;
        onSubmitClicked = onSubmit;
    }

    /// <summary>
    /// Resolves required RectTransform references from the scene.
    /// Call after the prefab has been instantiated.
    /// </summary>
    private void Setup()
    {
        panel = GameObject.Find(GameObjectHelper.ConfirmationDialog.Panel).GetComponent<RectTransform>();
        prompt = GameObject.Find(GameObjectHelper.ConfirmationDialog.Prompt).GetComponent<RectTransform>();
        buttonYes = GameObject.Find(GameObjectHelper.ConfirmationDialog.ButtonYes).GetComponent<RectTransform>();
        buttonNo = GameObject.Find(GameObjectHelper.ConfirmationDialog.ButtonNo).GetComponent<RectTransform>();
    }

    /// <summary>
    /// Resizes the dialog elements based on current canvas dimensions.
    /// Matches the panel to the canvas and scales buttons proportionally.
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
        var prefab = PrefabLibrary.Prefabs["ConfirmationDialogPrefab"];
        if (prefab == null)
            throw new UnityException("Prefab not found: ConfirmationDialog");

        GameObject go = GameObject.Instantiate(prefab, c.CanvasRect);
        if (go == null)
            throw new UnityException("Failed to instantiate ConfirmationDialog prefab");

        go.name = "ConfirmationDialog";

        var instance = go.GetComponent<ConfirmationDialogInstance>();
        if (instance == null)
            throw new UnityException("ConfirmationDialogInstance component not found on the prefab");

        instance.Assign(text, onSubmit);
        return instance;
    }
}
