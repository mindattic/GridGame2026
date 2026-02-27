using Assets.Helper;
using Assets.Helpers;
using Assets.Scripts.Factories;
using Game.Models.Profile;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;
using scene = Assets.Helpers.SceneHelper;

public class SaveFileSelectManager : MonoBehaviour
{
    // Parent container that will hold instantiated buttons.
    private Transform content;

    // ----------------------------------------------------------------------------------------------------
    // Unity Lifecycle
    // ----------------------------------------------------------------------------------------------------

    private void Awake()
    {
        // Find the content container in the scene and validate it.
        GameObject contentGO = GameObject.Find(GameObjectHelper.StageSelect.Content);
        if (contentGO == null)
        {
            Debug.LogError($"Content container not found: {GameObjectHelper.StageSelect.Content}");
            return;
        }

        content = contentGO.GetComponent<Transform>();
        if (content == null)
        {
            Debug.LogError("Content transform component missing on content container.");
        }
    }

    private void Start()
    {
        // Validate that a current profile exists. If not, send the user to create one.
        if (!ProfileHelper.HasCurrentProfile)
        {
            Debug.LogError("No current profile selected.");
            scene.Fade.ToProfileCreate();
            return;
        }

        // Populate the list and fade in.
        Reload();
        scene.FadeIn();
    }

    // ----------------------------------------------------------------------------------------------------
    // UI Population
    // ----------------------------------------------------------------------------------------------------

    private void Clear()
    {
        // Remove any previously created buttons to avoid duplicates.
        if (content == null)
        {
            Debug.LogError("Cannot clear. Content transform is null.");
            return;
        }

        foreach (Transform child in content)
        {
            Destroy(child.gameObject);
        }
    }

    private void Reload()
    {
        // Hide existing content and prepare to repopulate.
        Clear();

        // Validate prerequisites before reading save data.
        if (ProfileHelper.CurrentProfile == null)
        {
            Debug.LogError("CurrentProfile is null during Reload.");
            return;
        }

        string savesPath = Path.Combine(ProfileHelper.CurrentProfile.Folder ?? string.Empty, "Saves");
        if (string.IsNullOrWhiteSpace(savesPath))
        {
            Debug.LogError("Saves path is invalid.");
            return;
        }

        // Ensure the saves directory exists.
        if (!Directory.Exists(savesPath))
        {
            Debug.LogWarning($"Saves directory does not exist at: {savesPath}");
            return;
        }

        // Optional existence check of json files. Not used for logic, but validates folder health.
        // Keeps original logic of enumerating SaveStates from the profile.
        var saveFiles = Directory.GetFiles(savesPath, "*.json").ToArray();
        if (saveFiles.Length == 0)
        {
            Debug.Log($"No save files found in: {savesPath}");
        }

        // Defensive null check on the profile SaveStates list.
        if (ProfileHelper.CurrentProfile.SaveStates == null)
        {
            Debug.LogWarning("CurrentProfile.SaveStates is null. Nothing to display.");
            return;
        }

        // Add each save as a button. Preserve existing enumeration behavior.
        foreach (var item in ProfileHelper.CurrentProfile.SaveStates)
        {
            if (item == null)
            {
                Debug.LogWarning("Encountered null SaveState. Skipping.");
                continue;
            }

            AddLoadSaveFileButton(item);
        }
    }

    public void AddLoadSaveFileButton(SaveState item)
    {
        // Validate required references and data early.
        if (item == null)
        {
            Debug.LogError("AddLoadSaveFileButton received a null SaveState.");
            return;
        }

        if (content == null)
        {
            Debug.LogError("Content transform is null. Cannot parent save file button.");
            return;
        }

        if (ProfileHelper.CurrentProfile == null)
        {
            Debug.LogError("CurrentProfile is null in AddLoadSaveFileButton.");
            return;
        }

        string savesPath = Path.Combine(ProfileHelper.CurrentProfile.Folder ?? string.Empty, "Saves");
        string filePath = Path.Combine(savesPath, item.FileName ?? string.Empty);

        if (string.IsNullOrWhiteSpace(filePath))
        {
            Debug.LogError("Computed file path is invalid for save button.");
            return;
        }

        // Use factory instead of Instantiate(prefab)
        GameObject instance = SaveFileButtonFactory.Create(content);
        instance.name = $"Button_{Path.GetFileNameWithoutExtension(item.FileName ?? "Unknown")}";

        // Wire up the click event to load the save.
        Button button = instance.GetComponent<Button>();
        if (button == null)
        {
            Debug.LogError("Instantiated save button is missing a Button component.");
        }
        else
        {
            // Disable clicking if the file is missing, but still render the row for visibility.
            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"Save file does not exist on disk: {filePath}");
                button.interactable = false;
            }
            else
            {
                button.onClick.AddListener(() => OnLoadSaveFileButtonClicked(filePath));
            }
        }

        // Apply text to labels with defensive lookups.
        Transform saveNumberT = instance.transform.Find("SaveNumber");
        Transform timestampT = instance.transform.Find("Timestamp");

        if (saveNumberT == null || timestampT == null)
        {
            Debug.LogError("Save button prefab is missing SaveNumber or Timestamp child objects.");
            return;
        }

        TextMeshProUGUI saveNumber = saveNumberT.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI timestamp = timestampT.GetComponent<TextMeshProUGUI>();

        if (saveNumber == null || timestamp == null)
        {
            Debug.LogError("SaveNumber or Timestamp is missing a TextMeshProUGUI component.");
            return;
        }

        // Render the index and a friendly elapsed time string.
        saveNumber.text = $"Save {item.Index:D3}";
        timestamp.text = DateTimeHelper.ParseTimeElapsed(item.Timestamp);
    }

    // ----------------------------------------------------------------------------------------------------
    // Actions
    // ----------------------------------------------------------------------------------------------------

    private void OnLoadSaveFileButtonClicked(string filePath)
    {
        // Validate the path before any IO.
        if (string.IsNullOrWhiteSpace(filePath))
        {
            Debug.LogError("OnLoadSaveFileButtonClicked received an invalid file path.");
            return;
        }

        if (!File.Exists(filePath))
        {
            Debug.LogError($"Save file not found: {filePath}");
            return;
        }

        try
        {
            // Read and deserialize the selected save file.
            string json = File.ReadAllText(filePath);
            if (string.IsNullOrWhiteSpace(json))
            {
                Debug.LogError($"Save file is empty: {filePath}");
                return;
            }

            SaveState selectedSave = JsonConvert.DeserializeObject<SaveState>(json);

            if (selectedSave != null)
            {
                // Apply the selected save to the current profile, then move to the game scene.
                if (ProfileHelper.CurrentProfile == null)
                {
                    Debug.LogError("CurrentProfile is null when applying selected save.");
                    return;
                }

                ProfileHelper.CurrentProfile.CurrentSave = selectedSave;
                scene.Fade.ToGame();
            }
            else
            {
                Debug.LogError("Failed to deserialize the selected save file.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error loading save file {filePath}: {ex.Message}");
        }
    }

    public void OnBackButtonClicked()
    {
        // Navigate back to the previous scene as defined by your scene helper.
        scene.Fade.ToPreviousScene();
    }
}
