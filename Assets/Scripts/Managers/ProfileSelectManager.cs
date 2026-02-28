using Assets.Helper;
using Assets.Helpers;
using Assets.Scripts.Factories;
using Game.Models.Profile;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;
using c = Assets.Helpers.CanvasHelper;
using scene = Assets.Helpers.SceneHelper;

/// <summary>
/// PROFILESELECTMANAGER - Manages the profile/save file selection screen.
/// 
/// PURPOSE:
/// Displays available player profiles (save files) and allows the player
/// to select, create, or delete profiles.
/// 
/// VISUAL LAYOUT:
/// ```
/// ┌─────────────────────────────────────┐
/// │         Select Profile              │
/// ├─────────────────────────────────────┤
/// │  ┌─────────────────────────────┐   │
/// │  │  Profile 1 - "Player1"     │   │
/// │  │  Progress: Stage 5         │   │
/// │  └─────────────────────────────┘   │
/// │  ┌─────────────────────────────┐   │
/// │  │  Profile 2 - "Empty"       │   │
/// │  └─────────────────────────────┘   │
/// │  ┌─────────────────────────────┐   │
/// │  │  Profile 3 - "Empty"       │   │
/// │  └─────────────────────────────┘   │
/// └─────────────────────────────────────┘
/// ```
/// 
/// PROFILE OPERATIONS:
/// - Select: Load existing profile
/// - Create: Opens keyboard dialog for name
/// - Delete: Confirmation dialog then remove
/// 
/// RELATED FILES:
/// - SaveFileButtonFactory.cs: Creates profile buttons
/// - KeyboardDialogInstance.cs: Profile naming
/// - ProfileManager.cs: Profile data persistence
/// - SaveSystem.cs: Save/load operations
/// 
/// ACCESS: Scene-based manager (ProfileSelect scene)
/// </summary>
public class ProfileSelectManager : MonoBehaviour
{
    #region UI References

    private TextMeshProUGUI header;
    private RectTransform scrollView;
    private RectTransform content;
    private VerticalLayoutGroup verticalLayoutGroup;

    #endregion

    #region Initialization

    private void Awake()
    {
        // Find the header object
        GameObject headerGO = GameObject.Find(GameObjectHelper.StageSelect.Title);
        if (headerGO == null)
        {
            Debug.LogError($"Header object not found: {GameObjectHelper.StageSelect.Title}");
            return;
        }

        header = headerGO.GetComponent<TextMeshProUGUI>();
        if (header == null)
        {
            Debug.LogError("Header object is missing a TextMeshProUGUI component.");
            return;
        }

        // Find the scroll view rect
        GameObject scrollGO = GameObject.Find(GameObjectHelper.StageSelect.ScrollView);
        if (scrollGO == null)
        {
            Debug.LogError($"ScrollView object not found: {GameObjectHelper.StageSelect.ScrollView}");
            return;
        }

        scrollView = scrollGO.GetComponent<RectTransform>();
        if (scrollView == null)
        {
            Debug.LogError("ScrollView object is missing a RectTransform component.");
            return;
        }

        // Find the content rect
        GameObject contentGO = GameObject.Find(GameObjectHelper.StageSelect.Content);
        if (contentGO == null)
        {
            Debug.LogError($"Content object not found: {GameObjectHelper.StageSelect.Content}");
            return;
        }

        content = contentGO.GetComponent<RectTransform>();
        if (content == null)
        {
            Debug.LogError("Content object is missing a RectTransform component.");
            return;
        }

        // Get the layout group used to space the rows.
        verticalLayoutGroup = content.GetComponent<VerticalLayoutGroup>();
        if (verticalLayoutGroup == null)
        {
            Debug.LogError("Content object is missing a VerticalLayoutGroup component.");
            return;
        }

        // Validate that the canvas rect exists so sizing math is safe.
        if (c.CanvasRect == null)
        {
            Debug.LogError("CanvasHelper.CanvasRect is null.");
            return;
        }

        // Compute sizing from the canvas.
        float screenWidth = c.CanvasRect.rect.width;
        float screenHeight = c.CanvasRect.rect.height;
        float buttonWidth = 0.9f * screenWidth;
        float buttonHeight = screenHeight / 16f;
        float fontSize = buttonHeight / 2f;
        float rowSpacing = 0.01f * screenHeight;

        // Apply header font size.
        header.fontSize = fontSize;

        // Nudge the list down one button height so the first item clears the header area.
        scrollView.anchoredPosition = scrollView.anchoredPosition.SetY(-buttonHeight);

        // Apply row spacing to the layout.
        verticalLayoutGroup.spacing = rowSpacing;

        // Note: Button width and height sizing is left to the prefab/layout.
    }

    private void Start()
    {
        Reload();
        scene.FadeIn();
    }

    #endregion

    #region Profile List

    private void Clear()
    {
        if (content == null)
        {
            Debug.LogError("Cannot clear content. Content RectTransform is null.");
            return;
        }

        foreach (Transform child in content)
        {
            Destroy(child.gameObject);
        }
    }

    private void Reload()
    {
        // Start with a clean slate.
        Clear();

        // Always include a button to create a new profile.
        AddCreateNewProfileButton();

        // If there are no profiles, we are done.
        if (!ProfileHelper.HasProfiles())
        {
            return;
        }

        // Validate the profiles collection before use.
        if (ProfileHelper.Profiles == null || ProfileHelper.Profiles.Values == null)
        {
            Debug.LogWarning("Profiles collection is null or invalid.");
            return;
        }

        // Add each profile as a selectable button.
        foreach (var item in ProfileHelper.Profiles.Values)
        {
            if (item == null)
            {
                Debug.LogWarning("Encountered a null Profile entry. Skipping.");
                continue;
            }

            AddProfileSelectButton(item);
        }
    }

    public void AddCreateNewProfileButton()
    {
        // Validate required references.
        if (content == null)
        {
            Debug.LogError("Cannot add Create New Profile button. Missing content.");
            return;
        }

        // Use factory instead of Instantiate(prefab)
        GameObject instance = ScreenWidthButtonFactory.Create(content);
        instance.name = "CreateNewProfile";

        // Optional explicit sizing if your prefab does not size itself.
        // var buttonRect = instance.GetComponent<RectTransform>();
        // if (buttonRect != null) buttonRect.sizeDelta = new Vector2(buttonWidth, buttonHeight);

        // Wire the click handler.
        Button button = instance.GetComponent<Button>();
        if (button == null)
        {
            Debug.LogError("CreateNewProfile instance is missing a Button component.");
        }
        else
        {
            button.onClick.AddListener(OnCreateNewProfileButtonClicked);
        }

        // Set the visible label text.
        TextMeshProUGUI label = instance.GetComponentInChildren<TextMeshProUGUI>();
        if (label == null)
        {
            Debug.LogError("CreateNewProfile instance is missing a TextMeshProUGUI label.");
        }
        else
        {
            label.text = "Create New Profile";
        }
    }

    public void AddProfileSelectButton(Profile item)
    {
        // Validate input and references.
        if (item == null)
        {
            Debug.LogError("AddProfileSelectButton received a null Profile.");
            return;
        }

        if (content == null)
        {
            Debug.LogError("Cannot add profile button. Missing content.");
            return;
        }

        // Use factory instead of Instantiate(prefab)
        GameObject instance = ScreenWidthButtonFactory.Create(content);
        instance.name = $"Profile_{item.Key}";

        // Optional explicit sizing if your prefab does not size itself.
        // var buttonRect = instance.GetComponent<RectTransform>();
        // if (buttonRect != null) buttonRect.sizeDelta = new Vector2(buttonWidth, buttonHeight);

        // Wire the click handler to select this profile key.
        Button button = instance.GetComponent<Button>();
        if (button == null)
        {
            Debug.LogError("Profile button instance is missing a Button component.");
        }
        else
        {
            string key = item.Key;
            if (string.IsNullOrWhiteSpace(key))
            {
                Debug.LogWarning("Profile key is null or empty. Disabling button.");
                button.interactable = false;
            }
            else
            {
                button.onClick.AddListener(() => OnProfileButtonClicked(key));
            }
        }

        // Set the visible label text to the profile key.
        TextMeshProUGUI label = instance.GetComponentInChildren<TextMeshProUGUI>();
        if (label == null)
        {
            Debug.LogError("Profile button instance is missing a TextMeshProUGUI label.");
        }
        else
        {
            label.text = item.Key ?? "Unknown Profile";
        }
    }

    private void OnProfileButtonClicked(string key)
    {
        // Select the profile and navigate to the title screen.
        if (string.IsNullOrWhiteSpace(key))
        {
            Debug.LogError("OnProfileButtonClicked received an invalid profile key.");
            return;
        }

        ProfileHelper.SelectProfile(key);
        scene.Fade.ToTitleScreen();
    }

    private void OnCreateNewProfileButtonClicked()
    {
        scene.Fade.ToProfileCreate();
    }

    public void OnBackButtonClicked()
    {
        scene.Fade.ToPreviousScene();
    }

    #endregion
}
