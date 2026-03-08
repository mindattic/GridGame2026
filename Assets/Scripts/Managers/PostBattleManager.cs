using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Scripts.Helpers;
using Scripts.Factories;
using Scripts.Managers;
using scene = Scripts.Helpers.SceneHelper;
using Scripts.Helpers;
using Scripts.Libraries;
using Scripts.Canvas;
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
using Scripts.Models;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Managers
{
/// <summary>
/// POSTBATTLEMANAGER - Manages the post-battle results screen.
/// 
/// PURPOSE:
/// Displays battle results including experience gained, level ups,
/// and rewards after completing a stage.
/// 
/// VISUAL LAYOUT:
/// ```
/// ┌─────────────────────────────────────┐
/// │         Battle Results              │
/// ├─────────────────────────────────────┤
/// │  ┌─────────────────────────────┐   │
/// │  │ [Hero1] Paladin             │   │
/// │  │ EXP: ████████░░ +150        │   │ ← Experience pane
/// │  │ LEVEL UP! 5 → 6             │   │
/// │  └─────────────────────────────┘   │
/// │  ┌─────────────────────────────┐   │
/// │  │ [Hero2] Archer              │   │
/// │  │ EXP: ██████████ +200        │   │
/// │  └─────────────────────────────┘   │
/// │                                     │
/// │            [ Next ]                 │
/// └─────────────────────────────────────┘
/// ```
/// 
/// EXPERIENCE PANES:
/// Each hero gets a HeroExperiencePane showing:
/// - Portrait and name
/// - Experience bar animation
/// - Level up notification if applicable
/// 
/// FLOW:
/// 1. Stage completed successfully
/// 2. Experience calculated and applied
/// 3. PostBattle scene loaded
/// 4. Panes animate experience gain
/// 5. Player clicks Next to continue
/// 
/// RELATED FILES:
/// - HeroExperiencePaneFactory.cs: Creates pane GameObjects
/// - HeroExperiencePane.cs: Pane behavior/animation
/// - StageManager.cs: Triggers post-battle
/// - ProfileHelper.cs: Saves progress
/// 
/// ACCESS: Scene-based manager (PostBattle scene)
/// </summary>
public class PostBattleManager : MonoBehaviour
{
    #region Configuration

    private const float AutoEnableDelay = 0.25f;
    private const float PaneSpacing = 8f;

    #endregion

    #region References

    private RectTransform _scrollContent;
    private Button _nextButton;
    private string nextSceneName;

    #endregion

    #region Runtime State

    private readonly List<HeroExperiencePane> _panes = new List<HeroExperiencePane>();
    private bool _monitoring;
    private VerticalLayoutGroup _layout;
    private ContentSizeFitter _fitter;

    // Loot phase
    private bool _lootPhaseActive;
    private Button _doneButton;

    #endregion

    #region Initialization

    /// <summary>Initializes component references and state.</summary>
    private void Awake()
    {
        // Guard: require active save & party
        var save = ProfileHelper.CurrentProfile?.CurrentSave;
        if (save == null || save.Party?.Members == null || save.Party.Members.Count == 0)
        {
            SceneHelper.Switch.ToTitleScreen();
            return;
        }

        ResolveSceneReferences();
        ResolvePrefabsAndConfig();
        ConfigureLayout();
        BuildPanes();
    }

    /// <summary>Performs initial setup after all Awake calls complete.</summary>
    private void Start() => scene.FadeIn();

    /// <summary>Cleans up resources when the object is destroyed.</summary>
    private void OnDestroy()
    {
        if (_nextButton != null)
            _nextButton.onClick.RemoveListener(OnNext);
        if (_doneButton != null)
            _doneButton.onClick.RemoveListener(OnDone);
    }

    #endregion

    #region Scene Setup

    /// <summary>Resolve scene references.</summary>
    private void ResolveSceneReferences()
    {
        var contentGO = GameObject.Find(GameObjectHelper.PostBattleScreen.Content);
        if (contentGO == null)
        {
            Debug.LogError("PostBattleManager: Could not find content at path: " + GameObjectHelper.PostBattleScreen.Content);
            return;
        }
        _scrollContent = contentGO.GetComponent<RectTransform>();

        var nextGO = GameObject.Find(GameObjectHelper.PostBattleScreen.NextButton);
        if (nextGO == null)
        {
            Debug.LogError("PostBattleManager: Could not find NextButton at path: " + GameObjectHelper.PostBattleScreen.NextButton);
        }
        else
        {
            _nextButton = nextGO.GetComponent<Button>();
            _nextButton.onClick.RemoveListener(OnNext); // ensure single subscription
            _nextButton.onClick.AddListener(OnNext);
            _nextButton.gameObject.SetActive(false);
        }
    }

    /// <summary>Resolve prefabs and config.</summary>
    private void ResolvePrefabsAndConfig()
    {
        nextSceneName = ExperienceTracker.NextSceneAfterPostBattleScreen;
        if (string.IsNullOrEmpty(nextSceneName))
            nextSceneName = SceneHelper.Hub; // go to Hub by default
    }

    /// <summary>Configure layout.</summary>
    private void ConfigureLayout()
    {
        if (_scrollContent == null) return;
        _layout = _scrollContent.GetComponent<VerticalLayoutGroup>();
        if (_layout == null)
            _layout = _scrollContent.gameObject.AddComponent<VerticalLayoutGroup>();
        _layout.spacing = PaneSpacing;
        _layout.childAlignment = TextAnchor.UpperLeft;
        _layout.childControlHeight = true;
        _layout.childControlWidth = true;
        _layout.childForceExpandHeight = false;
        _layout.childForceExpandWidth = true;

        _fitter = _scrollContent.GetComponent<ContentSizeFitter>();
        if (_fitter == null)
            _fitter = _scrollContent.gameObject.AddComponent<ContentSizeFitter>();
        _fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    /// <summary>Runs per-frame update logic.</summary>
    private void Update()
    {
        if (!_monitoring || _panes.Count == 0) return;
        if (_panes.All(p => p != null && p.IsFillComplete))
        {
            _monitoring = false;
            StartCoroutine(EnableNextSoon());
        }
    }

    /// <summary>Enable next soons this component.</summary>
    private IEnumerator EnableNextSoon()
    {
        yield return new WaitForSeconds(AutoEnableDelay);
        if (_nextButton != null)
            _nextButton.gameObject.SetActive(true);
    }

    /// <summary>Creates the panes.</summary>
    private void BuildPanes()
    {
        if (_scrollContent == null) return;

        // Clear existing children
        for (int i = _scrollContent.childCount - 1; i >= 0; i--)
            Destroy(_scrollContent.GetChild(i).gameObject);

        var save = ProfileHelper.CurrentProfile.CurrentSave;
        var party = save.Party?.Members?.Select(m => m.CharacterClass).Where(s => s != CharacterClass.None).ToList() ?? new List<CharacterClass>();
        var roster = save.Roster?.Members?.Select(m => m.CharacterClass).Where(s => s != CharacterClass.None).ToList() ?? new List<CharacterClass>();

        _panes.Clear();

        foreach (var ch in party)
            CreatePane(ch, true, ExperienceTracker.GetXPGained(ch));
        foreach (var ch in roster.Where(c => !party.Contains(c)))
            CreatePane(ch, false, ExperienceTracker.GetXPGained(ch));

        _monitoring = true;
        if (_panes.All(p => p.IsFillComplete))
            StartCoroutine(EnableNextSoon());
    }

    /// <summary>Creates the pane.</summary>
    private void CreatePane(CharacterClass character, bool inParty, int xpGained)
    {
        if (_scrollContent == null) return;
        // Use factory instead of Instantiate(prefab)
        var go = HeroExperiencePaneFactory.Create(_scrollContent);
        go.name = $"Pane_{character}";
        var rt = go.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.localScale = Vector3.one;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition3D = Vector3.zero;
            var le = go.GetComponent<LayoutElement>();
            if (le == null) le = go.AddComponent<LayoutElement>();
            le.minHeight = 128f;
            le.preferredHeight = 128f;
            le.flexibleHeight = 0;
            le.flexibleWidth = 0;
        }
        var pane = go.GetComponent<HeroExperiencePane>();
        if (pane != null)
        {
            pane.Build(character, xpGained, inParty);
            _panes.Add(pane);
        }
    }

    /// <summary>Handles the next event — transitions from XP phase to Loot phase.</summary>
    private void OnNext()
    {
        // Apply XP gains to save data
        var save = ProfileHelper.CurrentProfile.CurrentSave;
        if (save != null)
        {
            foreach (var kv in ExperienceTracker.AllGains)
            {
                var id = kv.Key; 
                var gained = kv.Value;
                var entry = save.Party.Members.FirstOrDefault(m => m.CharacterClass == id) ?? save.Roster.Members.FirstOrDefault(m => m.CharacterClass == id);
                if (entry != null)
                {
                    entry.TotalXP = Mathf.Max(0, entry.TotalXP + gained);
                }
            }
            ProfileHelper.Save(true);
        }
        ExperienceTracker.Clear();

        // Transition to loot display phase
        if (LootTracker.HasLoot)
        {
            ShowLootPhase();
        }
        else
        {
            // No loot — go directly to next scene
            LootTracker.Clear();
            SceneHelper.Fade.To(nextSceneName);
        }
    }

    // ============================ Loot Phase ============================

    /// <summary>Builds the loot display replacing the XP panes.</summary>
    private void ShowLootPhase()
    {
        _lootPhaseActive = true;

        // Hide Next button
        if (_nextButton != null)
            _nextButton.gameObject.SetActive(false);

        // Clear XP panes
        if (_scrollContent != null)
        {
            for (int i = _scrollContent.childCount - 1; i >= 0; i--)
                Destroy(_scrollContent.GetChild(i).gameObject);
        }
        _panes.Clear();

        // Build loot header
        BuildLootHeader();

        // Build loot rows
        foreach (var loot in LootTracker.AllLoot)
        {
            BuildLootRow(loot.DisplayName, loot.Count);
        }

        // Commit loot to inventory save data
        LootTracker.CommitToInventory();
        ProfileHelper.Save(true);

        // Show Done button (reuse Next button position or create new)
        ShowDoneButton();
    }

    /// <summary>Creates a header text for the loot section.</summary>
    private void BuildLootHeader()
    {
        if (_scrollContent == null) return;

        var go = new GameObject("LootHeader");
        go.layer = LayerMask.NameToLayer("UI");
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(_scrollContent, false);
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 1f);

        var le = go.AddComponent<LayoutElement>();
        le.minHeight = 80f;
        le.preferredHeight = 80f;
        le.flexibleWidth = 1f;

        go.AddComponent<CanvasRenderer>();
        var tmp = go.AddComponent<TMPro.TextMeshProUGUI>();
        tmp.text = "Loot Collected";
        tmp.fontSize = 42;
        tmp.color = new Color(1f, 0.85f, 0.3f, 1f);
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;
        tmp.raycastTarget = false;
    }

    /// <summary>Creates a row for a single loot item.</summary>
    private void BuildLootRow(string displayName, int count)
    {
        if (_scrollContent == null) return;

        var go = HubItemRowFactory.Create(_scrollContent);
        var label = go.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        if (label != null)
        {
            label.text = count > 1 ? $"{displayName}  x{count}" : displayName;
            label.fontSize = 30;
        }

        // Disable button interaction on loot rows (display only)
        var btn = go.GetComponent<Button>();
        if (btn != null) btn.interactable = false;
    }

    /// <summary>Shows the Done button to leave post-battle.</summary>
    private void ShowDoneButton()
    {
        if (_nextButton != null)
        {
            // Reuse the existing button — change label to "Done"
            _doneButton = _nextButton;
            _doneButton.onClick.RemoveAllListeners();
            _doneButton.onClick.AddListener(OnDone);

            var label = _doneButton.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (label != null) label.text = "Done";

            _doneButton.gameObject.SetActive(true);
        }
    }

    /// <summary>Handles the done event — leaves post-battle and returns to overworld.</summary>
    private void OnDone()
    {
        LootTracker.Clear();

        // Route to Overworld instead of Hub after loot is collected
        string destination = ExperienceTracker.NextSceneAfterPostBattleScreen;
        if (string.IsNullOrEmpty(destination))
            destination = SceneHelper.Overworld;

        SceneHelper.Fade.To(destination);
    }

    #endregion
}

}
